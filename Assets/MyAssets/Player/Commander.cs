using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    [SerializeField] private float commanderPeriscope_ElevationMin = -20f;
    [SerializeField] private float commanderPeriscope_ElevationMax = 60f;
    private float commanderPeriscope_CurrentElevationSpeed = 0f;
    private float commanderPeriscope_CurrentRotationSpeed = 0f;
    public bool commanderPeriscope_Active = false;
    private float commanderPeriscope_CurrentAzimuth = 90f; // Because unity vs blender rotation :/
    private float commanderPeriscope_CurrentElevation_World = 0f;
    private readonly Dictionary<ZoomLevel, float> commanderPeriscope_ZoomLevels = new()
    {
        { ZoomLevel.Low, 30f },
        { ZoomLevel.High, 8f },
    };
    private ZoomLevel commanderPeriscope_currentZoomLevel = ZoomLevel.Low;
    [SerializeField] private Transform turretTransform;
    [SerializeField] private LayerMask periHitMask;
    private bool commanderPeriscope_FastMode_Active = false;
    private readonly float commanderPeriscope_maxSpeed = 20f;
    private readonly float commanderPeriscope_maxSpeedInFastMode = 40f;

    [Header("Player UI")]
    [SerializeField] private GameObject playerUI;
    [SerializeField] private ESCScreen ESCScreen;
    [SerializeField] private TextMeshProUGUI lastCommand_text;
    [SerializeField] private TextMeshProUGUI helpTextOn_text;
    [SerializeField] private TextMeshProUGUI helpTextOff_text;

    [Header("Commander Hatch Cameras")]
    [SerializeField] private List<Camera> cameras;
    private bool _lookingThroughPeriscope;


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



    // ----- ON START -----
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentPosition = Position.Inside;
        StartCoroutine(GoToPosition(currentPosition));
        commanderPeriscope.transform.localEulerAngles = new(90f, 90f, 90f);
        TogglePeriView(false);
    }


    // ----- ON UPDATE -----
    void Update()
    {
        MyInput();
    }


    // ----- COMMANDER PERI -----
    private void CommanderPeriMovement()
    {
        // --- PLAYER INPUT ---
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            float speedMultiplier = commanderPeriscope_FastMode_Active ? 2f : 1f;
            commanderPeriscope_CurrentRotationSpeed += mouseX * speedMultiplier;
            commanderPeriscope_CurrentElevationSpeed += mouseY * speedMultiplier;

            commanderPeriscope_CurrentRotationSpeed = Mathf.Clamp(
                commanderPeriscope_CurrentRotationSpeed,
                commanderPeriscope_FastMode_Active ? -commanderPeriscope_maxSpeedInFastMode : -commanderPeriscope_maxSpeed,
                commanderPeriscope_FastMode_Active ? commanderPeriscope_maxSpeedInFastMode : commanderPeriscope_maxSpeed);

            commanderPeriscope_CurrentElevationSpeed = Mathf.Clamp(
                commanderPeriscope_CurrentElevationSpeed,
                commanderPeriscope_FastMode_Active ? -commanderPeriscope_maxSpeedInFastMode : -commanderPeriscope_maxSpeed,
                commanderPeriscope_FastMode_Active ? commanderPeriscope_maxSpeedInFastMode : commanderPeriscope_maxSpeed);
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
        commanderPeriscope_clock.localEulerAngles = new Vector3(0f, 0f, angleY);
    }
    private Vector3 GetTargetPositionFromPeri(out GameObject target)
    {
        Vector3 origin = commanderPeriscope_camera.transform.position;
        Vector3 dir = commanderPeriscope_camera.transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, 5000f, periHitMask))
        {
            target = hit.collider.gameObject;
            if(target.CompareTag("Ground")) // If target is ground then it's not valid target
                target = null;

            return hit.point;
        }
        else
        {
            target = null;
            return commanderPeriscope_camera.transform.forward * 5000f;
        }
    }


    // ----- INPUT -----
    private void MyInput()
    {
        // ESC screen
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (binoculars_Active)
            {
                binoculars_Active = false;
                binoculars.enabled = false;
                SetLastCommandText("Boniculars disabled");
            }
            else if (commanderPeriscope_Active)
            {
                TogglePeriView(false);
                SetLastCommandText("Exitted peri view");
            }
            else
            {
                ToggleESCScreen(!ESCScreen.IsOpen);
            }
        }

        if (ESCScreen.IsOpen)
            return;
        
        // Positioning
        if(!binoculars_Active && !commanderPeriscope_Active)
        {
            if (isInVehicle && Input.GetKeyDown(KeyCode.I))
            {
                StartCoroutine(GoToPosition(Position.Inside, hatchAction: HatchAction.Close));
                SetLastCommandText("Switch position from outside to inside");
            }
            else if (isInVehicle && Input.GetKeyDown(KeyCode.O) && currentPosition == Position.Outside_low)
            {
                StartCoroutine(GoToPosition(Position.Outside_high));
                SetLastCommandText("Switch position from outside low to outside high");
            }
            else if (isInVehicle && Input.GetKeyDown(KeyCode.O))
            {
                if (currentPosition == Position.Inside)
                {
                    StartCoroutine(GoToPosition(Position.Outside_low, hatchAction: HatchAction.Open));
                    SetLastCommandText("Switch position from inside to outside");
                }
                else
                {
                    StartCoroutine(GoToPosition(Position.Outside_low));
                    SetLastCommandText("Switch position from outside high to outside low");
                }
            }
        }

        // Binoculars
        if (Input.GetKeyDown(KeyCode.B) && currentPosition != Position.Inside)
        {
            binoculars_Active = !binoculars_Active;
            binoculars.enabled = binoculars_Active;
            SetLastCommandText($"Binoculars {(binoculars_Active ? "enabled" : "disabled")}");
        }

        // UI
        ToggleHelpText(Input.GetKey(KeyCode.F1));

        // Test
        if (Input.GetKeyDown(KeyCode.L))
        {
            loader.SetNextAmmoType(loader.currentAmmoTypeLoaded == Loader.AmmoType.KE ? Loader.AmmoType.MZ : Loader.AmmoType.KE);
            SetLastCommandText($"Set next ammo type {(loader.currentAmmoTypeLoaded == Loader.AmmoType.KE ? Loader.AmmoType.MZ : Loader.AmmoType.KE)}");
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            loader.ClickOnPanel(Loader.LoadersPanelAction.Fire);
            SetLastCommandText("Loader - Click on panel - Fire");
        }
        else if (Input.GetKeyDown(KeyCode.V))
        {
            Vector3 targetPos = GetTargetPositionFromPeri(out GameObject target);

            if (target == null)
            {
                gunner.TraverseToPoint(targetPos);
                SetLastCommandText("Aim gun at position");
            }
            else
            {
                gunner.TraverseToTarget(target);
                SetLastCommandText($"Aim gun at target {target.name}");
            }
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            commanderPeriscope_FastMode_Active = !commanderPeriscope_FastMode_Active;
            SetLastCommandText($"Commander periscope fast mode {(commanderPeriscope_FastMode_Active ? "enabled" : "disabled")}");
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            gunner.StabilizerActive = false;
            gunner.TraverseToAngle_X(-15f);
            gunner.TraverseToAngle_Y(-10f);
            SetLastCommandText("Turret to rest position");
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
    private void ToggleESCScreen(bool toggle)
    {
        ESCScreen.IsOpen = toggle;
        ESCScreen.gameObject.SetActive(toggle);
        Cursor.visible = toggle;
        Cursor.lockState = toggle ? CursorLockMode.None : CursorLockMode.Locked;
    }


    // ----- LATE UPDATE -----
    private void LateUpdate()
    {
        if (!commanderPeriscope_Active)
            PlayerCameraMovement();
        else
            CommanderPeriMovement();


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
    private void PlayerCameraMovement()
    {
        if (ESCScreen.IsOpen || _lookingThroughPeriscope) return;

        float mouseX = Input.GetAxis("Mouse X") * camera_X_sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * camera_Y_sensitivity * Time.deltaTime;

        Vector3 currentRotation = playerCamera.transform.localEulerAngles;

        float desiredX = currentRotation.y + mouseX;
        float desiredY = currentRotation.x - mouseY;

        // Convert to -180 to 180 range
        desiredX = (desiredX > 180) ? desiredX - 360 : desiredX;
        //desiredX = Mathf.Clamp(desiredX, camera_X_minAngle, camera_X_maxAngle);
        desiredY = (desiredY > 180) ? desiredY - 360 : desiredY;
        desiredY = Mathf.Clamp(desiredY, camera_Y_minAngle, camera_Y_maxAngle);

        playerCamera.transform.localEulerAngles = new Vector3(desiredY, desiredX, 0f);
    }


    // ----- SWITCHING POSITIONS -----
    public enum Position
    {
        Inside,
        Outside_low,
        Outside_high,
    }
    private enum HatchAction
    {
        None,
        Open,
        Close,
    }
    private IEnumerator GoToPosition(Position position, HatchAction hatchAction = HatchAction.None)
    {
        Transform targetPosition = position switch
        {
            Position.Inside => position_inside,
            Position.Outside_low => position_outside_low,
            Position.Outside_high => position_outside_high,
            _ => null
        };
        
        if (targetPosition != null)
        {
            switch (hatchAction)
            {
                case HatchAction.Open:
                    loader.Hatch_OpenClose(true);
                    Hatch_OpenCLose(true);

                    yield return new WaitForSeconds(1.5f);

                    transform.SetParent(targetPosition);
                    transform.localPosition = Vector3.zero;
                    currentPosition = position;
                    break;
                case HatchAction.Close:
                    loader.Hatch_OpenClose(false);
                    Hatch_OpenCLose(false);

                    yield return new WaitForSeconds(1f);

                    transform.SetParent(targetPosition);
                    transform.localPosition = Vector3.zero;
                    currentPosition = position;
                    break;
                case HatchAction.None:
                    transform.SetParent(targetPosition);
                    transform.localPosition = Vector3.zero;
                    currentPosition = position;
                    break;
            }
        }
    }
    public void TogglePeriView(bool toggle)
    {
        playerCamera.enabled = !toggle;
        playerUI.SetActive(!toggle);

        commanderPeriscope_UI.SetActive(toggle);
        commanderPeriscope_camera.enabled = toggle;

        commanderPeriscope_Active = toggle;
    }


    // ----- UI -----
    public void SetLastCommandText(string commandText)
    {
        lastCommand_text.text = $"Last command:\n{commandText}";
    }
    private void ToggleHelpText(bool toggle)
    {
        helpTextOn_text.enabled = toggle;
        helpTextOff_text.enabled = !toggle;
    }


    // ----- INTERACTING WITH TANK -----
    private void Hatch_OpenCLose(bool open)
    {
        if (hatch_Open == open) return;

        hatch_Open = open;
        turretAnimator.SetTrigger(open ? "Open_CommanderHatch" : "Close_CommanderHatch");
    }
    public void LookIntoPeriscope(Camera periscopeCamera)
    {
        _lookingThroughPeriscope = !_lookingThroughPeriscope;
        playerCamera.enabled = !_lookingThroughPeriscope;
        playerUI.SetActive(!_lookingThroughPeriscope);

        if(_lookingThroughPeriscope) // Disable all cameras except one you are looking through
        {
            foreach (Camera c in cameras)
                if (c != periscopeCamera)
                    c.enabled = false;
        }
        else // Enable all cameras when NOT looking through any of it rn
        {
            foreach (Camera c in cameras)
                c.enabled = true;
        }
    }
}
