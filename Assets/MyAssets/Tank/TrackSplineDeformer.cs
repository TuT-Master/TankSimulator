using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;

[ExecuteAlways]
public class TrackSplineDeformer : MonoBehaviour
{
    [System.Serializable]
    public class Pin
    {
        [Tooltip("Index of the knot (control point) on the Spline's lower run.")]
        public int knotIndex = -1;

        [Header("Driver (pick one)")]
        public WheelCollider wheelCollider;
        public Transform driverTransform;

        [Header("Options")]
        public float yOffset = 0f;              // offset ALONG localDeformAxis
        public float smoothTime = 0.05f;
        [Range(0f, 1f)] public float weight = 1f;

        [HideInInspector] public float axisVel; // SmoothDamp velocity (scalar along axis)
    }

    [Header("Spline to deform (same as TrackSplineDriver.spline)")]
    public SplineContainer spline;

    [Header("Pins on the LOWER RUN (one per road wheel, + optional ends)")]
    public Pin[] pins;

    [Header("Default pin options (applied to all pins)")]
    [SerializeField] float YOffset = 0f;
    [SerializeField] float SmoothTime = 0.05f;
    [SerializeField] float Weight = 1f;

    [Header("Deformation axis (LOCAL space of the Spline object)")]
    public Vector3 localDeformAxis = Vector3.up;   // <- set this to the lower-run “up”
    public bool invertAxis = false;                // quick flip if it pushes inward

    [Header("Behaviour")]
    public bool deformInEditMode = false;
    public bool forceLinearTangents = true;
    public TrackSplineDriver driverToRecalc;

    // cache
    float3[] baselineLocalPos;
    int cachedSplineCount = -1;

    void OnEnable()
    {
        ApplyDefaultsToPins();
        CaptureBaseline();
        if (pins != null) foreach (var p in pins) p.axisVel = 0f;
    }

    void OnValidate()
    {
        ApplyDefaultsToPins();
        CaptureBaseline();
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.SceneView.RepaintAll();
#endif
    }

    void LateUpdate()
    {
        if (!spline || spline.Spline == null || pins == null || pins.Length == 0)
            return;
        if (!Application.isPlaying && !deformInEditMode)
            return;

        var sp = spline.Spline;

        // normalize axis in local space (with optional invert)
        Vector3 axisLocal = localDeformAxis.sqrMagnitude < 1e-10f ? Vector3.up : localDeformAxis.normalized;
        if (invertAxis) axisLocal = -axisLocal;

        if (sp.Count != cachedSplineCount)
            CaptureBaseline();

        for (int i = 0; i < pins.Length; i++)
        {
            var pin = pins[i];
            if (pin.knotIndex < 0 || pin.knotIndex >= sp.Count) continue;

            // baseline
            float3 basePos = (baselineLocalPos != null && i < baselineLocalPos.Length)
                ? baselineLocalPos[i]
                : sp[pin.knotIndex].Position;

            // current knot value along axis
            var knot = sp[pin.knotIndex];
            float currentScalar = Vector3.Dot((Vector3)knot.Position, axisLocal);

            // driver position in LOCAL space
            Vector3 driverLocal;
            if (pin.wheelCollider)
            {
                if (pin.wheelCollider.GetGroundHit(out WheelHit hit))
                    driverLocal = spline.transform.InverseTransformPoint(hit.point);
                else
                    driverLocal = spline.transform.InverseTransformPoint(pin.wheelCollider.transform.position);
            }
            else if (pin.driverTransform)
            {
                driverLocal = spline.transform.InverseTransformPoint(pin.driverTransform.position);
            }
            else
            {
                driverLocal = (Vector3)basePos; // no driver -> stick to baseline
            }

            // target scalar along axis (absolute coordinate) + offset
            float targetScalar = Vector3.Dot(driverLocal, axisLocal) + pin.yOffset;

            // smooth the scalar (no smoothing in edit mode)
            float smoothed = Application.isPlaying
                ? Mathf.SmoothDamp(currentScalar, targetScalar, ref pin.axisVel, Mathf.Max(0.0001f, pin.smoothTime))
                : targetScalar;

            float newScalar = Mathf.Lerp(currentScalar, smoothed, pin.weight);

            // reconstruct position: keep components perpendicular to axis, change only axis component
            // remove old-axis component and add new one
            Vector3 baseVec = (Vector3)basePos;
            float baseScalar = Vector3.Dot(baseVec, axisLocal);
            Vector3 perp = baseVec - axisLocal * baseScalar;
            Vector3 newPosVec = perp + axisLocal * newScalar;

            knot.Position = (float3)newPosVec;
            sp[pin.knotIndex] = knot;

            if (forceLinearTangents)
                sp.SetTangentMode(pin.knotIndex, TangentMode.Linear);
        }

        if (driverToRecalc)
            driverToRecalc.RecalcLength();
    }

    void ApplyDefaultsToPins()
    {
        if (pins == null) return;
        foreach (var p in pins)
        {
            if (p == null) continue;
            p.yOffset = YOffset;
            p.smoothTime = SmoothTime;
            p.weight = Weight;
        }
    }

    void CaptureBaseline()
    {
        if (!spline || spline.Spline == null || pins == null)
        {
            baselineLocalPos = null;
            cachedSplineCount = -1;
            return;
        }

        var sp = spline.Spline;
        cachedSplineCount = sp.Count;

        baselineLocalPos = new float3[pins.Length];
        for (int i = 0; i < pins.Length; i++)
        {
            int idx = Mathf.Clamp(pins[i].knotIndex, 0, sp.Count - 1);
            baselineLocalPos[i] = sp[idx].Position;
        }

        if (pins != null) foreach (var p in pins) p.axisVel = 0f;
    }
}
