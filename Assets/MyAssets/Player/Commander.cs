using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Commander : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float camera_Y_maxAngle = 60f;
    [SerializeField] private float camera_Y_minAngle = -60f;
    [SerializeField] private float camera_Y_sensitivity = 300f;
    [SerializeField] private float camera_X_maxAngle = 60f;
    [SerializeField] private float camera_X_minAngle = -60f;
    [SerializeField] private float camera_X_sensitivity = 300f;

    [Header("Positioning")]
    [SerializeField] private bool isInVehicle = true;
    [SerializeField] private Transform position_inside;
    [SerializeField] private Transform position_outside_low;
    [SerializeField] private Transform position_outside_high;
    public Position currentPosition;

    [Header("Crew")]
    [SerializeField] private Loader loader;
    [SerializeField] private Gunner gunner;
    [SerializeField] private Driver driver;

    [Header("Animations")]
    [SerializeField] private Animator turretAnimator;

    [Header("Binoculars")]
    [SerializeField] private Image binoculars;
    [SerializeField] private float binoculars_ZoomMin = 2f;
    [SerializeField] private float binoculars_ZoomMax = 8f;
    private bool binoculars_Active = false;

    [Header("Commander Periscope")]
    [SerializeField] private GameObject commanderPeriscope;
    [SerializeField] private Camera commanderPeriscope_camera;
    [SerializeField] private GameObject commanderPeriscope_UI;
    [SerializeField] private RectTransform commanderPeriscope_clock;
    [SerializeField] private RectTransform commanderPeriscope_strichbild;
    [SerializeField] private float commanderPeriscope_ElevationMin = -20f;
    [SerializeField] private float commanderPeriscope_ElevationMax = 60f;
    private readonly float commanderPeriscope_maxSpeed = 40f;
    private float commanderPeriscope_CurrentElevationSpeed = 0f;
    private float commanderPeriscope_CurrentRotationSpeed = 0f;
    public bool commanderPeriscope_Active = false;
    private float commanderPeriscope_CurrentAzimuth = 90f;
    private float commanderPeriscope_CurrentElevation_World = 0f;
    private readonly Dictionary<ZoomLevel, float> commanderPeriscope_ZoomLevels = new()
    {
        { ZoomLevel.Low, 30f },
        { ZoomLevel.High, 8f },
    };
    private ZoomLevel commanderPeriscope_currentZoomLevel = ZoomLevel.Low;
    [SerializeField] private Transform turretTransform;
    [SerializeField] private LayerMask periHitMask;

    // Zoom levels
    private enum ZoomLevel
    {
        Low,
        High,
    }

    // Hatches
    private bool hatch_Open = false;

    // Interact key
    public bool canInteract = true;
    private readonly float interactCooldown = 0.25f;

    // Some variables
    private Vector3 _lastPeriAimPoint;
    private bool _hasLastPeriAimPoint;




    // ----- ON START -----
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentPosition = Position.Inside;
        GoToPosition(currentPosition);
        commanderPeriscope.transform.localEulerAngles = new(90f, 90f, 90f);
        TogglePeriView(false);
    }


    // ----- CAMERA MOVEMENT -----
    void Update()
    {
        if (!commanderPeriscope_Active)
            CameraMovement_WithXLimit();
        else
            CameraMovement_CommanderPeri_World();

        MyInput();
    }
    private void CameraMovement_WithXLimit()
    {
        float mouseX = Input.GetAxis("Mouse X") * camera_X_sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * camera_Y_sensitivity * Time.deltaTime;

        Vector3 currentRotation = playerCamera.transform.localEulerAngles;

        float desiredX = currentRotation.y + mouseX;
        float desiredY = currentRotation.x - mouseY;

        // Convert to -180 to 180 range
        desiredX = (desiredX > 180) ? desiredX - 360 : desiredX;
        desiredX = Mathf.Clamp(desiredX, camera_X_minAngle, camera_X_maxAngle);
        desiredY = (desiredY > 180) ? desiredY - 360 : desiredY;
        desiredY = Mathf.Clamp(desiredY, camera_Y_minAngle, camera_Y_maxAngle);

        playerCamera.transform.localEulerAngles = new Vector3(desiredY, desiredX, 0f);
    }


    // ----- COMMANDER PERI -----
    private void CameraMovement_CommanderPeri_World()
    {
        // --- PLAYER INPUT ---
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            commanderPeriscope_CurrentRotationSpeed += mouseX;
            commanderPeriscope_CurrentElevationSpeed += mouseY;

            commanderPeriscope_CurrentRotationSpeed = Mathf.Clamp(
                commanderPeriscope_CurrentRotationSpeed,
                -commanderPeriscope_maxSpeed,
                commanderPeriscope_maxSpeed);

            commanderPeriscope_CurrentElevationSpeed = Mathf.Clamp(
                commanderPeriscope_CurrentElevationSpeed,
                -commanderPeriscope_maxSpeed,
                commanderPeriscope_maxSpeed);
        }
        else
        {
            commanderPeriscope_CurrentRotationSpeed = Mathf.Lerp(commanderPeriscope_CurrentRotationSpeed, 0f, Time.deltaTime * 5f);
            commanderPeriscope_CurrentElevationSpeed = Mathf.Lerp(commanderPeriscope_CurrentElevationSpeed, 0f, Time.deltaTime * 5f);
        }

        // --- UPDATE ANGLES ---
        commanderPeriscope_CurrentAzimuth -= commanderPeriscope_CurrentRotationSpeed * Time.deltaTime;
        commanderPeriscope_CurrentAzimuth = Mathf.Repeat(commanderPeriscope_CurrentAzimuth, 360f);

        commanderPeriscope_CurrentElevation_World -= commanderPeriscope_CurrentElevationSpeed * Time.deltaTime;
        commanderPeriscope_CurrentElevation_World = Mathf.Clamp(commanderPeriscope_CurrentElevation_World, commanderPeriscope_ElevationMin, commanderPeriscope_ElevationMax);

        // Calculate world rotations
        Quaternion worldYaw = Quaternion.AngleAxis(commanderPeriscope_CurrentAzimuth, Vector3.down);
        Vector3 yawForward = worldYaw * Vector3.forward;
        Vector3 worldUp = Vector3.up;
        Vector3 worldRight = Vector3.Cross(worldUp, yawForward).normalized;
        Quaternion worldPitch = Quaternion.AngleAxis(commanderPeriscope_CurrentElevation_World, worldRight);

        // Apply rotations
        commanderPeriscope.transform.rotation = worldYaw;
        commanderPeriscope_camera.transform.rotation = worldPitch * worldYaw;

        // Rotate strichbild + clock
        Quaternion rel = Quaternion.Inverse(turretTransform.rotation) * commanderPeriscope.transform.rotation;
        float angleY = rel.eulerAngles.y;
        //commanderPeriscope_strichbild.localEulerAngles = new Vector3(0f, 0f, -angleY);
        commanderPeriscope_clock.localEulerAngles = new Vector3(0f, 0f, angleY);
    }
    private Vector3 GetDirectionFromPeri()
    {
        Quaternion worldYaw = Quaternion.AngleAxis(commanderPeriscope_CurrentAzimuth, Vector3.down);
        Vector3 yawForward = worldYaw * Vector3.forward;
        Vector3 worldRight = Vector3.Cross(Vector3.up, yawForward).normalized;
        Quaternion worldPitch = Quaternion.AngleAxis(commanderPeriscope_CurrentElevation_World, worldRight);

        return worldPitch * worldYaw * Vector3.forward;
    }
    private Vector3 GetTargetPointFromPeri()
    {
        Vector3 origin = commanderPeriscope_camera.transform.position;
        Vector3 dir = commanderPeriscope_camera.transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, gunner.maxFCSCalculationRange, periHitMask, QueryTriggerInteraction.Ignore))
        {
            _lastPeriAimPoint = hit.point;
            _hasLastPeriAimPoint = true;
            return hit.point;
        }

        return _hasLastPeriAimPoint ? _lastPeriAimPoint : -(origin + dir * gunner.maxFCSCalculationRange);
    }


    // ----- INPUT -----
    private void MyInput()
    {
        if (isInVehicle && Input.GetKeyDown(KeyCode.I))
        {
            GoToPosition(Position.Inside);

            loader.Hatch_OpenClose(false);
            Hatch_OpenCLose(false);
        }
        else if (isInVehicle && Input.GetKeyDown(KeyCode.O) && currentPosition == Position.Outside_low)
        {
            GoToPosition(Position.Outside_high);
        }
        else if (isInVehicle && Input.GetKeyDown(KeyCode.O))
        {
            if(currentPosition == Position.Inside)
            {
                loader.Hatch_OpenClose(true);
                Hatch_OpenCLose(true);
            }

            GoToPosition(Position.Outside_low);
        }

        // Binoculars
        if (Input.GetKeyDown(KeyCode.B))
        {
            binoculars_Active = !binoculars_Active;
            binoculars.enabled = binoculars_Active;
        }

        // Test
        if (Input.GetKeyDown(KeyCode.L))
        {
            loader.ClickOnPanel(Loader.LoadersPanelAction.SwitchAmmoType_To_KE);
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            loader.LoadAmmoType(Loader.AmmoType.KE);
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            loader.ClickOnPanel(Loader.LoadersPanelAction.Fire);
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            gunner.TraverseToDirection(GetDirectionFromPeri());
        }
        else if (Input.GetKeyDown(KeyCode.V))
        {
            gunner.TraverseToPoint(GetTargetPointFromPeri());
        }
    }
    public void StartInteractionCooldown()
    {
        canInteract = false;
        StartCoroutine(InteractionCooldown());
    }
    private IEnumerator InteractionCooldown()
    {
        yield return new WaitForSeconds(interactCooldown);
        canInteract = true;
    }


    // ----- LATE UPDATE -----
    private void LateUpdate()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        // Binoculars Zoom
        if (binoculars_Active)
        {
            if (scroll != 0f)
            {
                float currentZoom = playerCamera.fieldOfView;
                currentZoom -= scroll * 20f;
                currentZoom = Mathf.Clamp(currentZoom, binoculars_ZoomMin, binoculars_ZoomMax);
                playerCamera.fieldOfView = currentZoom;

                // Update camera sensitivity based on zoom level
                float zoomFactor = (currentZoom - binoculars_ZoomMin) / (binoculars_ZoomMax - binoculars_ZoomMin);
                camera_X_sensitivity = Mathf.Lerp(100f, 300f, zoomFactor);
                camera_Y_sensitivity = Mathf.Lerp(100f, 300f, zoomFactor);
            }
        }
        // Commander peri Zoom
        else if (commanderPeriscope_Active)
        {
            if (scroll > 0f)
                commanderPeriscope_currentZoomLevel = ZoomLevel.High;
            else if (scroll < 0f)
                commanderPeriscope_currentZoomLevel = ZoomLevel.Low;

            commanderPeriscope_camera.fieldOfView = commanderPeriscope_ZoomLevels[commanderPeriscope_currentZoomLevel];
        }
        // Reset FOV when not using binoculars or peri
        else
        {
            playerCamera.fieldOfView = 60f;
        }
    }


    // ----- SWITCHING POSITIONS -----
    public enum Position
    {
        Inside,
        Outside_low,
        Outside_high,
    }
    private void GoToPosition(Position position)
    {
        Transform targetPosition = position switch
        {
            Position.Inside => position_inside,
            Position.Outside_low => position_outside_low,
            Position.Outside_high => position_outside_high,
            _ => null
        };

        if (targetPosition == null) return;

        transform.SetParent(targetPosition);
        transform.localPosition = Vector3.zero;
        currentPosition = position;
    }
    public void TogglePeriView(bool toggle)
    {
        playerCamera.enabled = !toggle;

        commanderPeriscope_UI.SetActive(toggle);
        commanderPeriscope_camera.enabled = toggle;

        commanderPeriscope_Active = toggle;
    }


    // ----- INTERACTING WITH TANK -----
    private void Hatch_OpenCLose(bool open)
    {
        if (hatch_Open == open) return;

        hatch_Open = open;
        turretAnimator.SetTrigger(open ? "Open_CommanderHatch" : "Close_CommanderHatch");
    }
}
