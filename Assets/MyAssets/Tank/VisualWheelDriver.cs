using UnityEngine;

public class WheelVisualDriver : MonoBehaviour
{
    [Header("Sources")]
    public WheelCollider wheelCol;      // for road wheels
    public Rigidbody tankRb;            // for sprocket/idler based on track speed
    public Transform tankRoot;          // orientation reference

    [Header("Wheel Settings")]
    public float radius = 0.35f;
    public bool rotateOnly = false;     // sprocket, idler: true
    public float rotationScale = 1.0f;  // tweak if mesh doesn’t match movement

    private float rotationAngle = 0f;

    [SerializeField] private bool isRoadWheel = true;

    void Awake()
    {
        _ = TryGetComponent(out wheelCol);
    }

    void LateUpdate()
    {
        if (!tankRb || !tankRoot) return;

        // ===== Wheel rotation =====
        float forwardVel = Vector3.Dot(tankRb.velocity, tankRoot.forward);
        float wheelCircum = 2f * Mathf.PI * radius;
        float wheelRpm = (forwardVel / wheelCircum) * rotationScale;

        rotationAngle += wheelRpm * 360f * Time.deltaTime;
        if(isRoadWheel)
            transform.localRotation = Quaternion.Euler(rotationAngle, -90f, -90f);
        else
            transform.localRotation = Quaternion.Euler(rotationAngle, 0f, 0f);
    }
}
