using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gunner : MonoBehaviour
{
    [Header("Gun handling (legacy max speeds)")]
    [SerializeField] private float gunTraverseSpeed_X = 45f;     // used as max speed in AUTO
    [SerializeField] private float gunTraverseSpeed_Y = 30f;     // used as max speed in AUTO
    [SerializeField] private float gunTraverseSpeed_X_Manual = 3f;  // used as max speed in MANUAL
    [SerializeField] private float gunTraverseSpeed_Y_Manual = 3f;  // used as max speed in MANUAL
    public bool isManualTraversing = false;
    [SerializeField] private float stabYawSnapEps = 0.05f;    // deg
    [SerializeField] private float stabPitchSnapEps = 0.05f;  // deg
    private float yawZero;
    private bool yawZeroInit = false;

    [Header("Accel/Decel")]
    [SerializeField] private float gunAccel_X = 120f;
    [SerializeField] private float gunDecel_X = 160f;
    [SerializeField] private float gunAccel_Y = 90f;
    [SerializeField] private float gunDecel_Y = 120f;

    [Header("Elevation limits")]
    [SerializeField] private bool useElevationLimits = true;
    [SerializeField] private float elevationMax_Front = 20f;
    [SerializeField] private float elevationMin_Front = 10f;
    [SerializeField] private float azimuthLimitForElevationLimits = 130f;
    [SerializeField] private float elevationMax_Rear = 20f;
    [SerializeField] private float elevationMin_Rear = 2f;

    [Header("Loading angle")]
    [SerializeField] private float loadingAngle = 5f;
    private bool isTraversingToLoadingAngle = false;
    private bool isTraversing_Y = false;
    private bool isTraversing_X = false;

    [Header("Projectile settings")]
    [SerializeField] private GameObject KE_shot_prefab;
    [SerializeField] private GameObject MZ_shot_prefab;
    [SerializeField] private GameObject HE_shot_prefab;
    [Tooltip("Muzzle velocity in m/s (KE, MZ, HE)")]
    [SerializeField] private float[] projectileMuzzleVelocities = new float[3];
    private Dictionary<Loader.AmmoType, float> projectileMuzzleVelocityDict;

    [Header("Effects")]
    [SerializeField] private Transform canonMuzzlePoint;
    [SerializeField] private GameObject canonFire_effect_prefab;
    [SerializeField] private GameObject canonSmokeAfterFire_effect_prefab;

    [Header("Sounds for effects")]
    [SerializeField] private EventReference canonFire_sound;
    private EventInstance canonFire_Event;

    [Header("References")]
    [SerializeField] private Transform tankPivot;
    [SerializeField] private Transform gunPivot;      // local X = elevation
    [SerializeField] private Transform turretPivot;   // local Y = azimuth
    [SerializeField] private Animator turretAnimator;
    [SerializeField] private Loader loader;

    // Private variables
    private Coroutine yRoutine;
    private Coroutine xRoutine;
    private Turret turret;
    private GunnerVoiceManager voiceManager;
    private float currentElevation = 0f;
    private float currentAzimuth = 0f;
    public Vector3 target;

    // FCS variables
    private readonly float maxLaserRange = 99999f;
    private readonly float minLaserRange = 200f;
    [HideInInspector] public readonly float maxFCSCalculationRange = 4000f;
    private readonly float minFCSCalculationRange = 200f;
    private float rangeToTarget = 200f;

    [SerializeField] private bool invertPitch = false; // flip if needed
    [SerializeField] private bool invertYaw = false;   // optional

    [SerializeField] private float localAzimuthAngle = 0f;
    [SerializeField] private float range = 1000f;

    // ----- ON START -----
    private void Start()
    {
        turret = turretAnimator.GetComponent<Turret>();
        turret.turretState = Turret.TurretState.StabEin;
        voiceManager = GetComponent<GunnerVoiceManager>();

        // Prepare sounds
        if (!canonFire_Event.isValid())
            canonFire_Event = RuntimeManager.CreateInstance(canonFire_sound);

        // Prepare projectile muzzle velocity dictionary
        projectileMuzzleVelocityDict = new()
        {
            { Loader.AmmoType.KE, projectileMuzzleVelocities[0] },
            { Loader.AmmoType.MZ, projectileMuzzleVelocities[1] },
            { Loader.AmmoType.HE, projectileMuzzleVelocities[2] },
        };

        yawZero = NormalizeAngle(turretPivot.localEulerAngles.y);
        currentAzimuth = yawZero;
        yawZeroInit = true;

        currentAzimuth = NormalizeAngle(turretPivot.localEulerAngles.y);
        currentElevation = NormalizeAngle(gunPivot.localEulerAngles.x);
    }


    // ----- ON UPDATE -----
    private void Update()
    {
        // Cheack turret state
        switch(turret.turretState)
        {
            case Turret.TurretState.TurmAus:
                isManualTraversing = true;
                break;
            case Turret.TurretState.Beobachten:
                isManualTraversing = false;
                break;
            case Turret.TurretState.StabEin:
                isManualTraversing = false;
                if (!isTraversing_X && !isTraversing_Y)
                    StabilizeGun();
                break;
        }

        // Test input
        if (Input.GetKeyDown(KeyCode.X))
        {
            Fire_MainCannon();
        }
    }


    // ----- GUN STABILIZATOR -----
    private void StabilizeGun()
    {
        // Sync "current" angles to the real transform every frame (great for debugging stability)
        currentAzimuth = NormalizeAngle(turretPivot.localEulerAngles.y);
        currentElevation = NormalizeAngle(gunPivot.localEulerAngles.x);

        // Need valid target
        Vector3 toTargetFromTurret = target - turretPivot.position;
        if (toTargetFromTurret.sqrMagnitude < 0.0001f) return;

        // =========================================================
        // YAW (axis-based, incline-safe)
        // =========================================================
        Vector3 yawAxis = turretPivot.up;
        Vector3 turretForward = turretPivot.forward;

        Vector3 turretFwdOnPlane = Vector3.ProjectOnPlane(turretForward, yawAxis);
        Vector3 targetOnPlaneYaw = Vector3.ProjectOnPlane(toTargetFromTurret, yawAxis);

        if (turretFwdOnPlane.sqrMagnitude < 0.0001f || targetOnPlaneYaw.sqrMagnitude < 0.0001f)
            return;

        turretFwdOnPlane.Normalize();
        targetOnPlaneYaw.Normalize();

        float yawError = Vector3.SignedAngle(turretFwdOnPlane, targetOnPlaneYaw, yawAxis);
        if (invertYaw) yawError = -yawError;

        float desiredYaw = currentAzimuth + yawError;

        currentAzimuth = Mathf.MoveTowardsAngle(currentAzimuth, desiredYaw, gunTraverseSpeed_X * Time.deltaTime);

        var yawEuler = turretPivot.localEulerAngles;
        turretPivot.localRotation = Quaternion.Euler(yawEuler.x, currentAzimuth, yawEuler.z);

        // =========================================================
        // PITCH (axis-based, Armature-safe)
        // =========================================================
        if (turret.loadingAngle_Active) return;

        Vector3 toTargetFromGun = target - gunPivot.position;
        if (toTargetFromGun.sqrMagnitude < 0.0001f) return;

        // Elevation axis: red axis (since you rotate local X for elevation)
        Vector3 pitchAxis = gunPivot.right;

        // Reference forward for pitch: gun's forward (blue axis)
        Vector3 gunForward = -gunPivot.up;

        Vector3 gunFwdOnPlane = Vector3.ProjectOnPlane(gunForward, pitchAxis);
        Vector3 targetOnPlanePitch = Vector3.ProjectOnPlane(toTargetFromGun, pitchAxis);

        if (gunFwdOnPlane.sqrMagnitude < 0.0001f || targetOnPlanePitch.sqrMagnitude < 0.0001f)
            return;

        gunFwdOnPlane.Normalize();
        targetOnPlanePitch.Normalize();

        float pitchError = Vector3.SignedAngle(gunFwdOnPlane, targetOnPlanePitch, pitchAxis);
        if (invertPitch) pitchError = -pitchError;

        float desiredPitch = currentElevation + pitchError;

        // Clamp desired pitch in normalized range (matches your limits meaningfully)
        if (useElevationLimits)
        {
            float az = Mathf.Abs(NormalizeAngle(currentAzimuth));
            if (az >= azimuthLimitForElevationLimits)
                desiredPitch = Mathf.Clamp(desiredPitch, -elevationMax_Rear, elevationMin_Rear);
            else
                desiredPitch = Mathf.Clamp(desiredPitch, -elevationMax_Front, elevationMin_Front);
        }

        currentElevation = Mathf.MoveTowardsAngle(currentElevation, desiredPitch, gunTraverseSpeed_Y * Time.deltaTime);

        var pitchEuler = gunPivot.localEulerAngles;
        gunPivot.localRotation = Quaternion.Euler(currentElevation, pitchEuler.y, pitchEuler.z);
    }


    // ----- TRAVERSING -----
    public void TraverseToDirection(Vector3 direction)
    {
        Vector3 localDir = turretPivot.parent.InverseTransformDirection(direction);

        // Calculate target position
        if(Physics.Raycast(gunPivot.position, localDir, out RaycastHit hit, 5000f))
        {
            target = hit.point;
        }

        float targetY = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        float targetX = Mathf.Asin(-localDir.y / localDir.magnitude) * Mathf.Rad2Deg;
        TraverseToAngle_X(targetY);
        TraverseToAngle_Y(targetX);

        voiceManager.PlayOneShot(GunnerVoiceManager.OneShot.IDidNotUnderstand);
    }
    public void TraverseToPoint(Vector3 point)
    {
        target = point;
        Vector3 direction = point - gunPivot.position;

        Vector3 localDir = turretPivot.parent.InverseTransformDirection(direction);

        float targetY = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        float targetX = Mathf.Asin(-localDir.y / localDir.magnitude) * Mathf.Rad2Deg;
        TraverseToAngle_X(targetY);
        TraverseToAngle_Y(targetX);
        StartCoroutine(voiceManager.PlayContactReport(GunnerVoiceManager.ContactType.Tank_Frontaly, localAzimuthAngle, range));
    }
    public void TraverseToAngle_Y(float? targetAngle = null)
    {
        if (isTraversingToLoadingAngle) return;
        else if (turret.loadingAngle_Active && !isManualTraversing) return;

        float maxSpeed = isManualTraversing ? gunTraverseSpeed_Y_Manual : gunTraverseSpeed_Y;
        float angle;
        bool toLoading = false;

        if (targetAngle.HasValue)
        {
            isTraversingToLoadingAngle = false;
            angle = targetAngle.Value;
        }
        else
        {
            angle = -loadingAngle;
            isTraversingToLoadingAngle = true;
            toLoading = true;
        }

        // Check elevation limits
        if (useElevationLimits)
        {
            if (Mathf.Abs(currentAzimuth) >= azimuthLimitForElevationLimits)
                angle = Mathf.Clamp(angle, -elevationMax_Rear, elevationMin_Rear);
            else
                angle = Mathf.Clamp(angle, -elevationMax_Front, elevationMin_Front);
        }

        if (yRoutine != null) StopCoroutine(yRoutine);
        yRoutine = StartCoroutine(TraverseToAngle_Y_Coroutine(
            gunPivot.localEulerAngles.x, angle,
            maxSpeed, gunAccel_Y, gunDecel_Y, toLoading
        ));
    }
    private IEnumerator TraverseToAngle_Y_Coroutine(float startAngle, float targetAngle, float maxSpeed, float accel, float decel, bool toLoadingAngle = false)
    {
        float current = NormalizeAngle(startAngle);
        float target = NormalizeAngle(targetAngle);

        float speed = 0f;                 // scalar speed (>= 0)
        const float snapEps = 0.5f;       // deg

        isTraversing_Y = true;
        while (true)
        {
            float delta = ShortestSignedDelta(current, target); // signed
            float dist = Mathf.Abs(delta);
            if (dist <= snapEps) break;

            int dir = delta >= 0 ? 1 : -1;

            // stopping distance for current speed (v2 / 2a)
            float stopDist = (speed * speed) / (2f * Mathf.Max(0.0001f, decel));

            // accelerate or decelerate
            if (stopDist >= dist)
                speed = Mathf.Max(0f, speed - decel * Time.deltaTime);
            else
                speed = Mathf.Min(maxSpeed, speed + accel * Time.deltaTime);

            // step this frame
            float step = dir * speed * Time.deltaTime;

            // don't overshoot
            if (Mathf.Abs(step) > dist) step = dir * dist;

            current = NormalizeAngle(current + step);

            // write only local X (elevation)
            var e = gunPivot.localEulerAngles;
            gunPivot.localEulerAngles = new Vector3(current, e.y, e.z);
            currentElevation = current;

            yield return null;
        }

        // snap exact
        var eFinal = gunPivot.localEulerAngles;
        gunPivot.localEulerAngles = new Vector3(target, eFinal.y, eFinal.z);

        if (toLoadingAngle)
        {
            isTraversingToLoadingAngle = false;
            turret.loadingAngle_Active = true;
        }

        isTraversing_Y = false;
        yRoutine = null;
    }
    public void TraverseToAngle_X(float targetAngle)
    {
        float maxSpeed = isManualTraversing ? gunTraverseSpeed_X_Manual : gunTraverseSpeed_X;

        if (xRoutine != null) StopCoroutine(xRoutine);
        xRoutine = StartCoroutine(TraverseToAngle_X_Coroutine(
            turretPivot.localEulerAngles.y, targetAngle,
            maxSpeed, gunAccel_X, gunDecel_X
        ));
    }
    private IEnumerator TraverseToAngle_X_Coroutine(float startAngle, float targetAngle, float maxSpeed, float accel, float decel)
    {
        float current = NormalizeAngle(startAngle);
        float target = NormalizeAngle(targetAngle);

        float speed = 0f;
        const float snapEps = 0.5f;

        isTraversing_X = true;
        while (true)
        {
            float delta = ShortestSignedDelta(current, target);
            float dist = Mathf.Abs(delta);
            if (dist <= snapEps) break;

            int dir = delta >= 0 ? 1 : -1;

            float stopDist = (speed * speed) / (2f * Mathf.Max(0.0001f, decel));

            if (stopDist >= dist)
                speed = Mathf.Max(0f, speed - decel * Time.deltaTime);
            else
                speed = Mathf.Min(maxSpeed, speed + accel * Time.deltaTime);

            float step = dir * speed * Time.deltaTime;
            if (Mathf.Abs(step) > dist) step = dir * dist;

            current = NormalizeAngle(current + step);

            var e = turretPivot.localEulerAngles;
            turretPivot.localEulerAngles = new Vector3(e.x, current, e.z);
            currentAzimuth = current;

            yield return null;
        }

        var eFinal = turretPivot.localEulerAngles;
        turretPivot.localEulerAngles = new Vector3(eFinal.x, target, eFinal.z);

        isTraversing_X = false;
        xRoutine = null;
    }
    public void StopAllTraverses()
    {
        if (yRoutine != null) StopCoroutine(yRoutine);
        if (xRoutine != null) StopCoroutine(xRoutine);
        isTraversing_Y = isTraversing_X = false;
        yRoutine = xRoutine = null;
        isTraversingToLoadingAngle = false;
    }
    private static float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        if (a < -180f) a += 360f;
        return a;
    }
    private static float ShortestSignedDelta(float from, float to)
    {
        return Mathf.DeltaAngle(from, to);
    }


    // ----- WEAPONS -----
    private void Fire_MainCannon()
    {
        // Spawn projectile
        GameObject shot = null;
        Quaternion rotation = Quaternion.Euler(gunPivot.rotation.eulerAngles.x, gunPivot.rotation.eulerAngles.y + 180f, gunPivot.rotation.eulerAngles.z);
        switch (loader.currentAmmoTypeLoaded)
        {
            case Loader.AmmoType.KE:
                shot = Instantiate(KE_shot_prefab, canonMuzzlePoint.position, rotation);
                break;
            case Loader.AmmoType.MZ:
                shot = Instantiate(MZ_shot_prefab, canonMuzzlePoint.position, rotation);
                break;
            case Loader.AmmoType.HE:
                shot = Instantiate(HE_shot_prefab, canonMuzzlePoint.position, rotation);
                break;
            default:
                Debug.Log("Gunner: Cannot fire main cannon - no ammo loaded!");
                return;
        }
        if (shot.TryGetComponent(out Rigidbody rb))
        {
            rb.AddForce(canonMuzzlePoint.forward * projectileMuzzleVelocityDict[loader.currentAmmoTypeLoaded], ForceMode.VelocityChange);
        }
        else
        {
            Debug.Log("No Rigidbody on shot found!");
            Destroy(shot);
        }

        // Visual and sound effect
        SpawnEffect(Effect.CanonFire);

        // Reset current ammo type loaded
        loader.currentAmmoTypeLoaded = Loader.AmmoType.None;

        // Play firing animation
        if (turretAnimator) turretAnimator.SetTrigger("Fire");
    }


    // ----- EFFECTS -----
    private enum Effect
    {
        CanonFire,
    }
    private void SpawnEffect(Effect effect)
    {
        switch (effect)
        {
            case Effect.CanonFire:
                if (canonFire_effect_prefab && canonMuzzlePoint)
                {
                    // Spawn effects
                    Instantiate(canonFire_effect_prefab, canonMuzzlePoint.position, canonMuzzlePoint.rotation);
                    Instantiate(canonSmokeAfterFire_effect_prefab, canonMuzzlePoint.position, canonMuzzlePoint.rotation, canonMuzzlePoint);

                    // Play sound
                    canonFire_Event.set3DAttributes(RuntimeUtils.To3DAttributes(canonMuzzlePoint));
                    RuntimeManager.PlayOneShotAttached(canonFire_sound, canonMuzzlePoint.gameObject);
                }
                break;
        }
    }
}
