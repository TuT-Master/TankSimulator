using UnityEngine;
using Unity.Mathematics;                 // float3, float4x4
using UnityEngine.Splines;

public class TrackSplineDriver : MonoBehaviour
{
    [Header("Spline (closed loop)")]
    public SplineContainer spline;       // closed loop around wheels
    public bool clockwise = true;        // direction of belt travel along the spline
    public Transform upHint;             // usually the tank root (for global right/up)

    [Header("Bones (in belt order)")]
    public Transform[] trackBones;       // all belt bones, ordered around the loop
    public Vector3 boneEulerOffset = new(-90, 0, 0); // fix rig roll if needed
    public Vector3 boneLocalOutAxis = Vector3.right; // bone's "outward" axis AFTER offset
    public float outwardOffset = 0f;     // small push away from hull

    [Header("Side & Auto Motion")]
    public bool isLeftSide = true;       // left belt (true) or right belt (false)

    [Tooltip("Auto-compute belt speed from chassis motion (recommended).")]
    public bool autoDrive = true;

    [Tooltip("RigidBody of the hull (source of velocity and angular velocity).")]
    public Rigidbody tankRb;

    [Tooltip("Transform that defines forward/up (usually the hull/root).")]
    public Transform tankRoot;

    [Tooltip("Half the distance from hull centerline to track centerline (meters).")]
    public float halfTrackWidth = 0.8f;

    [Tooltip("Extra sign fix if your belt scrolls the wrong way (+1 or -1).")]
    public float beltDirSign = 1f;

    [Tooltip("Smoothing for the auto-computed belt speed.")]
    public float mpsSmoothing = 10f;

    [Tooltip("Optional clamp of the visual belt speed.")]
    public float maxAbsMetersPerSec = 20f;

    [Header("Manual (used if autoDrive = false)")]
    public float metersPerSec = 0f;      // belt speed (manual)

    // --- cached lengths ---
    float loopLenWorld = 0f;             // world-space loop length
    float loopLenLocal = 0f;             // local-space loop length
    float phaseMeters = 0f;              // world meters advanced along loop

    void Awake() => RecalcLength();

    void Update()
    {
        if (spline == null || spline.Spline == null || trackBones == null || trackBones.Length == 0)
            return;
        if (loopLenWorld <= 1e-6f || loopLenLocal <= 1e-6f)
            return;

        // ---------- Auto-compute belt m/s from hull motion ----------
        if (autoDrive && tankRb && tankRoot)
        {
            // Forward linear speed (m/s)
            float vFwd = Vector3.Dot(tankRb.velocity, tankRoot.forward);

            // Yaw rate (rad/s) about hull up
            float yaw = Vector3.Dot(tankRb.angularVelocity, tankRoot.up);

            // Linear speed contribution at this track due to yaw
            // Right side: +yaw * halfTrackWidth ; Left side: -yaw * halfTrackWidth
            float sideLinear = yaw * (isLeftSide ? -halfTrackWidth : +halfTrackWidth);

            // Desired belt linear speed relative to hull (m/s)
            float targetMps = (vFwd + sideLinear) * beltDirSign;

            // Smooth & clamp
            float lerp = 1f - Mathf.Exp(-mpsSmoothing * Time.deltaTime);
            metersPerSec = Mathf.Lerp(metersPerSec, targetMps, lerp);
            if (maxAbsMetersPerSec > 0f)
                metersPerSec = Mathf.Clamp(metersPerSec, -maxAbsMetersPerSec, +maxAbsMetersPerSec);
        }

        // ---------- Advance belt phase (in world meters) ----------
        float dir = clockwise ? 1f : -1f;
        phaseMeters = Repeat(phaseMeters + dir * metersPerSec * Time.deltaTime, loopLenWorld);

        float spacingWorld = loopLenWorld / trackBones.Length;
        float worldToLocal = loopLenLocal / loopLenWorld;

        Transform tr = spline.transform;

        for (int i = 0; i < trackBones.Length; i++)
        {
            // 1) Distance along loop (world) for bone i
            float sWorld = Repeat(phaseMeters + i * spacingWorld, loopLenWorld);

            // 2) Convert to local distance, then to normalized t
            float sLocal = sWorld * worldToLocal;
            float t = SplineUtility.ConvertIndexUnit(
                spline.Spline,
                sLocal,
                PathIndexUnit.Distance,
                PathIndexUnit.Normalized);

            // 3) Evaluate in LOCAL space
            SplineUtility.Evaluate(
                spline.Spline, t,
                out float3 posL, out float3 tanL, out float3 upL);

            // 4) Transform to WORLD
            Vector3 pos = tr.TransformPoint((Vector3)posL);
            Vector3 tangent = tr.TransformDirection((Vector3)tanL).normalized;

            // 5) Build a stable frame whose OUTWARD is locked to the tank side
            float sideSign = isLeftSide ? -1f : 1f;
            Vector3 refRight = upHint ? upHint.right : transform.right;
            Vector3 outward = Vector3.ProjectOnPlane(sideSign * refRight, tangent).normalized;
            if (outward.sqrMagnitude < 1e-6f)
                outward = Vector3.Cross(tangent, (upHint ? upHint.up : Vector3.up)).normalized;

            Vector3 binormal = Vector3.Cross(outward, tangent).normalized;

            // 6) Base rotation + rig offset
            Quaternion rot = Quaternion.LookRotation(tangent, binormal) * Quaternion.Euler(boneEulerOffset);

            // 7) Enforce bone's local outward axis to match world outward (fixes 180° flips)
            Vector3 boneOutWorld = rot * boneLocalOutAxis;
            if (Vector3.Dot(boneOutWorld, outward) < 0f)
                rot = Quaternion.AngleAxis(180f, tangent) * rot;

            // 8) Apply pose
            trackBones[i].SetPositionAndRotation(pos + outward * outwardOffset, rot);
        }
    }

    public void RecalcLength()
    {
        if (spline == null || spline.Spline == null)
        {
            loopLenWorld = loopLenLocal = 0f;
            return;
        }

        // World-space length (needs matrix)
        float4x4 l2w = (float4x4)spline.transform.localToWorldMatrix;
        loopLenWorld = SplineUtility.CalculateLength(spline.Spline, l2w);

        // Local-space length (identity matrix)
        loopLenLocal = SplineUtility.CalculateLength(spline.Spline, float4x4.identity);
    }

    static float Repeat(float v, float len)
    {
        if (len <= 1e-5f) return 0f;
        v %= len; if (v < 0f) v += len; return v;
    }
}
