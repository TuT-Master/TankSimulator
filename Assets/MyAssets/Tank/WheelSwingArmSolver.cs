using UnityEngine;

public class WheelSwingArmSolver : MonoBehaviour
{
    public enum HeightMode
    {
        WheelPose,     // use WheelCollider.GetWorldPose (recommended & simplest)
        HullDownRay    // cast from anchor along tank hull's down (your custom method)
    }

    [Header("Height source")]
    public WheelCollider wheelCol;            // required
    public HeightMode heightMode = HeightMode.WheelPose;

    [Tooltip("Used only when HeightMode = HullDownRay. If empty, falls back to this transform.")]
    public Transform tankRoot;
    [Tooltip("Used only when HeightMode = HullDownRay.")]
    [SerializeField] private LayerMask groundMask = ~0;
    [Tooltip("Meters added to ray length (HullDownRay). Scale down if your rig is tiny.")]
    [SerializeField] private float rayMargin = 0.02f;

    [Header("Arm rig")]
    public Transform hub;                     // visual hub at arm tip
    public bool autoLength = true;
    public float armLength = 0f;

    [Tooltip("Arm local axis the arm rotates around (hinge). For you this is local Z.")]
    public Vector3 hingeAxisLocal = Vector3.forward;

    [Header("Travel axis")]
    [Tooltip("If ON, the solver computes the arm's local up/travel axis from world up automatically.")]
    public bool deriveUpFromWorld = true;
    [Tooltip("Manual local up/travel axis (used only if deriveUpFromWorld = false).")]
    public Vector3 armLocalUpAxis = Vector3.up;

    [Tooltip("Flip lateral side if the mirrored arm solves inward instead of outward.")]
    public bool invertSide = false;

    [Header("Behaviour")]
    public float minAngleDeg = -50f;
    public float maxAngleDeg = +50f;
    public float smoothDegPerSec = 720f;
    [Tooltip("Extra offset along the suspension axis (meters). Positive pushes further 'down'.")]
    public float heightOffset = 0f;

    [Header("Polarity")]
    [Tooltip("If the wheel moves the wrong way (compresses when tank goes up), tick this.")]
    public bool invertPolarity = false;

    // ---- internal cache ----
    Quaternion bindLocalRotation;
    Vector3 u_up;        // arm local travel axis (in-plane)
    Vector3 n_hinge;     // arm local hinge axis (plane normal)
    Vector3 r_lat;       // arm local lateral axis (in-plane, perpendicular to up)
    Vector2 bindRU;      // bind vector in RU plane
    float bindAngleRad;
    float currentAngleDeg;

    void Reset()
    {
        hingeAxisLocal = Vector3.forward;
        deriveUpFromWorld = true;     // default for arbitrary arm rotations
        armLocalUpAxis = Vector3.up;  // fallback if deriveUpFromWorld is off
    }

    void Awake()
    {
        CacheBasis();
        CacheBindPose();
    }

    void OnValidate()
    {
        CacheBasis();
        CacheBindPose();
    }

    // Build the RU plane basis from current transform
    void CacheBasis()
    {
        // Hinge axis in arm local space
        n_hinge = hingeAxisLocal.sqrMagnitude < 1e-6f ? Vector3.forward : hingeAxisLocal.normalized;

        // Up/travel axis in arm local space
        u_up = deriveUpFromWorld
            ? transform.InverseTransformDirection(Vector3.up)
            : (armLocalUpAxis.sqrMagnitude < 1e-6f ? Vector3.up : armLocalUpAxis).normalized;

        // Orthonormal basis for swing plane (R,U) hinge
        r_lat = Vector3.Cross(n_hinge, u_up).normalized;
        u_up = Vector3.Cross(r_lat, n_hinge).normalized; // re-orthogonalize
    }

    // Capture bind pose (rest) direction and arm length
    void CacheBindPose()
    {
        if (!hub) return;

        bindLocalRotation = transform.localRotation;

        // Hub in arm-local
        Vector3 hubLocal = transform.InverseTransformPoint(hub.position);
        float r0 = Vector3.Dot(hubLocal, r_lat);
        float u0 = Vector3.Dot(hubLocal, u_up);
        bindRU = new Vector2(r0, u0);

        if (autoLength || armLength <= 0f)
            armLength = Mathf.Max(1e-6f, bindRU.magnitude);

        // Choose starting side from bind (optionally inverted)
        float sideSign = Mathf.Sign(bindRU.x);
        if (invertSide) sideSign = -sideSign;
        bindRU = new Vector2(sideSign * Mathf.Abs(bindRU.x), bindRU.y);

        bindAngleRad = Mathf.Atan2(bindRU.y, bindRU.x);
        currentAngleDeg = bindAngleRad * Mathf.Rad2Deg;
    }

    void LateUpdate()
    {
        if (!hub || armLength <= 1e-6f) return;

        // (Tiny rigs or editor tweaks) keep basis fresh
        CacheBasis();

        // --- 1) Desired wheel center (WORLD) ---
        Vector3 desiredCenterWorld = GetDesiredWheelCenterWorld();

        // --- 2) Convert to arm-local, project into swing plane, normalize length ---
        Vector3 tgtLocal = transform.InverseTransformPoint(desiredCenterWorld);

        // Remove hinge-axis component -> stay in RU plane
        float vN = Vector3.Dot(tgtLocal, n_hinge);
        Vector3 v = tgtLocal - n_hinge * vN;

        float vMag = v.magnitude;
        if (vMag < 1e-9f) return;

        // Keep mechanical length consistent
        if (autoLength || armLength <= 0f)
        {
            Vector3 hubLocalNow = transform.InverseTransformPoint(hub.position);
            armLength = Mathf.Max(1e-6f, hubLocalNow.magnitude);
        }

        v *= (armLength / vMag);

        // --- 3) Signed angle from bind vector to target vector in RU plane ---
        Vector2 bind = bindRU;
        if (bind.sqrMagnitude < 1e-12f)
        {
            Vector3 hubLocalNow = transform.InverseTransformPoint(hub.position);
            bind = new Vector2(Vector3.Dot(hubLocalNow, r_lat), Vector3.Dot(hubLocalNow, u_up));
        }

        Vector2 tgt = new Vector2(Vector3.Dot(v, r_lat), Vector3.Dot(v, u_up));

        float a0 = Mathf.Atan2(bind.y, bind.x);
        float a1 = Mathf.Atan2(tgt.y, tgt.x);
        float targetDeg = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg);

        // -------- Polarity flip (the fix you asked to be included) --------
        if (invertPolarity) targetDeg = -targetDeg;

        // Clamp & smooth
        targetDeg = Mathf.Clamp(targetDeg, minAngleDeg, maxAngleDeg);
        float maxStep = smoothDegPerSec * (Application.isPlaying ? Time.deltaTime : 1f / 60f);
        currentAngleDeg = Mathf.MoveTowards(currentAngleDeg, targetDeg, maxStep);

        // --- 4) Apply rotation relative to bind pose ---
        Quaternion rotDelta = Quaternion.AngleAxis(currentAngleDeg, n_hinge);
        transform.localRotation = bindLocalRotation * rotDelta;
    }

    // Compute the desired wheel center in WORLD space
    Vector3 GetDesiredWheelCenterWorld()
    {
        Vector3 desired;

        if (!wheelCol)
        {
            if (tankRoot) return tankRoot.position;
            if (hub) return hub.position;
            return transform.position;
        }

        if (heightMode == HeightMode.WheelPose)
        {
            // Use Unity's solved suspension (most robust)
            wheelCol.GetWorldPose(out desired, out _);
            if (Mathf.Abs(heightOffset) > 1e-6f)
            {
                Vector3 down = (tankRoot ? -tankRoot.up : -transform.up).normalized;
                desired += down * heightOffset;
            }
            return desired;
        }
        else // HullDownRay
        {
            Vector3 top = wheelCol.transform.position; // suspension anchor
            Vector3 down = (tankRoot ? -tankRoot.up : -transform.up).normalized;

            // Ray length must reach ground from anchor: suspension + radius (+ margin)
            float rayLen = wheelCol.suspensionDistance + wheelCol.radius + Mathf.Max(0f, rayMargin);

            if (Physics.Raycast(top, down, out var hit, rayLen, groundMask, QueryTriggerInteraction.Ignore))
            {
                // Wheel center sits one radius above the contact point along the suspension axis
                desired = hit.point - down * wheelCol.radius;
            }
            else
            {
                // Full droop: bottom of legal travel
                desired = top + down * wheelCol.suspensionDistance;
            }

            if (Mathf.Abs(heightOffset) > 1e-6f)
                desired += down * heightOffset;

            return desired;
        }
    }
}
