using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankMovement : MonoBehaviour
{
    [SerializeField] private List<Wheel> wheels;
    [SerializeField] private LayerMask groundMask = -1;
    [SerializeField] private float trackWidth = 2.0f;       // distance between left/right wheels
    public float torque = 10.0f;                            // how quickly to approach commanded speed
    public float targetTurning = 0;                         // rad/s, +cw
    public float targetSpeed = 0;                           // m/s, +forward
    [SerializeField] private float currentSpeed = 0;        // smoothed speed

    [Header("Wheels")]
    [SerializeField] private float wheelRadius = 0.35f;
    [SerializeField] private float wheelRestLength = 0.3f;
    [SerializeField] private float wheelSpringK = 40000f;
    [SerializeField] private float wheelDamperC = 4000f;
    [SerializeField] private float wheelMaxMu = 1.0f;
    [SerializeField] private float wheelLatFriction = 100f;
    [SerializeField] private float wheelRollFriction = 30f;
    [SerializeField] private Vector3 visualStartLocal;

    [SerializeField] float idleDamp = 5.0f;         // how quickly to bleed sideways/forward motion when idle
    [SerializeField] float inputDeadZone = 0.4f;   // what counts as "no command"

    private Rigidbody rb;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        foreach (Wheel w in wheels)
        {
            w.rollFriction = wheelRollFriction;
            w.latFriction = wheelLatFriction;
            w.maxMu = wheelMaxMu;
            w.damperC = wheelDamperC;
            w.springK = wheelSpringK;
            w.restLen = wheelRestLength;
            w.radius = wheelRadius;
            w.visualStartLocal = visualStartLocal;
        }
    }
    void FixedUpdate()
    {
        // Smooth acceleration/deceleration of target speed
        float accelRate = 0.5f; // adjust responsiveness
        float decelRate = 1.0f;
        float blendRate = targetSpeed > currentSpeed ? accelRate : decelRate;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.fixedDeltaTime * blendRate);
        if(currentSpeed <= 0.1f && targetSpeed == 0f) currentSpeed = 0f;

        foreach (Wheel w in wheels)
        {
            Vector3 origin = w.wheel.position;
            Vector3 dir = w.isLeft ? w.wheel.right : -w.wheel.right;

            if (Physics.Raycast(origin, dir, out var hit, w.restLen + w.radius, groundMask))
            {
                // 1. Define local orientation basis -------------------------------------------
                // Get local "down" (spring) axis and build orthogonal frame from it.
                dir = w.isLeft ? -w.wheel.right : w.wheel.right; // or +right if your X points up
                Vector3 up = -dir;                               // local up
                Vector3 fwd = Vector3.Cross(up, w.wheel.forward).normalized; // track-forward axis
                Vector3 right = Vector3.Cross(up, fwd);           // side axis (for scrub/friction)

                // 2. Compute suspension forces -----------------------------------------------
                float compression = (w.restLen + w.radius) - hit.distance;
                float relVel = Vector3.Dot(rb.GetPointVelocity(hit.point), dir);
                float springForce = compression * w.springK - relVel * w.damperC;

                if (springForce < 0f) springForce = 0f;           // no suction
                rb.AddForceAtPosition(dir * springForce, hit.point, ForceMode.Force);

                // 3. Project current velocity onto tangential axes ---------------------------
                Vector3 pVel = rb.GetPointVelocity(hit.point);
                float vLong = Vector3.Dot(pVel, fwd);             // along track motion
                float vLat = Vector3.Dot(pVel, right);           // sideways scrub

                // 4. Determine commanded track speed (drive control) -------------------------
                float commanded = targetTurning;           // forward/reverse
                commanded += currentSpeed * (w.isLeft ? +0.5f : -0.5f) * trackWidth;

                // 5. Compute drive and friction forces ---------------------------------------
                float driveAcc;
                if (Mathf.Abs(currentSpeed) < 0.01f && Mathf.Abs(targetTurning) < 0.01f)
                { 
                    driveAcc = -vLong * 0.2f;
                }
                else
                {
                    // Normal drive
                    driveAcc = (commanded - vLong) * torque;
                }

                Vector3 F_drive = driveAcc * rb.mass * fwd / wheels.Count;
                Vector3 F_lateral = vLat * w.latFriction * -right;
                Vector3 F_rollRes = vLong * w.rollFriction * -fwd;
                Vector3 Ft = F_drive + F_lateral + F_rollRes;

                // 6. Clamp grip to mu * normal load ------------------------------------------
                float maxGrip = springForce * w.maxMu;
                if (Ft.magnitude > maxGrip)
                    Ft = Ft.normalized * maxGrip;

                // 7. Apply tangential forces --------------------------------------------------
                rb.AddForceAtPosition(Ft, hit.point, ForceMode.Force);

                // 8. (Optional) Visual wheel animation ---------------------------------------
                if (w.visual)
                {
                    // Move visual wheel vertically
                    if(!w.visualRotateOnly)
                        w.visual.localPosition = (w.isLeft ? w.visualStartLocal : -w.visualStartLocal) + (w.wheel.InverseTransformDirection(dir) * compression);

                    // Rotate visual wheel around its local spin axis
                    w.visual.Rotate(Vector3.forward, vLong / w.radius * Time.fixedDeltaTime * Mathf.Rad2Deg);
                }
            }
            else if (w.alwaysGrounded && w.visual)
            {
                // Determine track forward direction consistently
                dir = w.isLeft ? -w.wheel.right : w.wheel.right;
                Vector3 up = -dir;
                Vector3 fwd = Vector3.Cross(up, w.wheel.forward).normalized;

                // Project rigidbody velocity onto track-forward direction
                float vLong = Vector3.Dot(rb.GetPointVelocity(w.wheel.position), fwd);

                // Rotate the wheel visually
                w.visual.Rotate(Vector3.forward, vLong / w.radius * Time.fixedDeltaTime * Mathf.Rad2Deg);
            }
            else
            {
                // wheel off ground -> relax toward rest pose visually
            }

            Vector3 planeN = Vector3.up;
            ApplyIdleDamping(planeN);
        }
    }
    void ApplyIdleDamping(Vector3 planeNormal)
    {
        bool noCommand =
            Mathf.Abs(currentSpeed) < inputDeadZone &&
            Mathf.Abs(targetTurning) < inputDeadZone;

        if (!noCommand) return;

        // split velocity into along-plane (horizontal) and normal (vertical)
        Vector3 v = rb.velocity;
        Vector3 vPlane = Vector3.ProjectOnPlane(v, planeNormal);
        Vector3 vNormal = v - vPlane;

        // gently damp only the plane component (so it coasts, not bricks)
        vPlane = Vector3.Lerp(vPlane, Vector3.zero, idleDamp * Time.fixedDeltaTime);
        rb.velocity = vPlane + vNormal;
    }

}


[System.Serializable]
public class Wheel
{
    public Transform wheel;              // Position/orientation of the wheel in the tank model
    public Transform visual;             // Optional: mesh or bone to visually move
    public bool isLeft;                  // Left/right side flag for turning logic
    public float restLen = 0.3f;         // Suspension rest length
    public float radius = 0.35f;         // Wheel radius
    public float springK = 40000;        // Spring stiffness
    public float damperC = 4000;         // Damping
    public float maxMu = 1.0f;           // Grip coefficient
    public float latFriction = 100;      // Lateral friction (scrub)
    public float rollFriction = 30;      // Rolling resistance
    public Vector3 visualStartLocal;     // for suspension animation
    public bool visualRotateOnly;        // For sprocket and idler wheels
    public bool alwaysGrounded = false;  // For sprocket and idler wheels
}
