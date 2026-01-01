using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Engine : MonoBehaviour
{
    [Header("Engine Settings")]
    [SerializeField] private float currentRPM;
    [SerializeField] private float idleRPM;
    [SerializeField] private float maxRPM;
    [SerializeField] private float minRPM;
    [SerializeField] private float RPM_downshift;
    [SerializeField] private float RPM_upshift;
    [SerializeField] private float torgueAtMaxRPM;
    [SerializeField] private float torgueAtMinRPM;
    private bool isEngineOn = true;

    [Header("Transmission Settings")]
    [SerializeField] private float[] gearRatios_forward = { 3f, 2.1f, 1.4f, 1.0f };
    [SerializeField] private float[] gearRatios_reverse = { 3f, 2.1f };
    [SerializeField] private float[] fwdMinSpeed = new float[] { 0f, 10f, 22f, 36f };
    [SerializeField] private float[] fwdMaxSpeed = new float[] { 15f, 30f, 45f, 60f };
    [SerializeField] private float[] revMinSpeed = new float[] { 0f, 10f };
    [SerializeField] private float[] revMaxSpeed = new float[] { 15f, 30f };
    [SerializeField] private float currentTorgue;
    [SerializeField] private int currentGear = 0;
    private enum GearState { Neutral, Forward, Reverse }
    private GearState gearState = GearState.Neutral;

    [Header("Controlling the engine")]
    public float desiredSpeed = 0f;
    [SerializeField] private float desiredSpeed_step = 4f;
    private float desiredRPM;

    [Header("Sound Settings")]
    [SerializeField] private Transform enginePos;
    [SerializeField] private EventReference engineSound;
    private EventInstance engineSoundEvent;
    private bool engineSoundPlaying = false;



    private Rigidbody rb;


    private void Start()
    {
        // Get references
        rb = GetComponent<Rigidbody>();

        // Create sound instance
        if (!engineSoundEvent.isValid())
            engineSoundEvent = RuntimeManager.CreateInstance(engineSound);

        // Set variables
        desiredRPM = idleRPM;
    }
    private void Update()
    {
        if (isEngineOn)
        {
            // Key inputs
            // Desired speed
            if(Input.GetKeyDown(KeyCode.W))
            {
                desiredSpeed += desiredSpeed_step;
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                desiredSpeed -= desiredSpeed_step;
            }
            desiredSpeed = Mathf.Clamp(
                desiredSpeed,
                0f,
                gearState == GearState.Reverse ? revMaxSpeed[^1] : fwdMaxSpeed[^1]);
            // Gear state
            if (Input.GetKeyDown(KeyCode.E))
            {
                gearState = GearState.Forward;
                if(currentGear == 0)
                {
                    currentGear = 1; // Shift to first gear if in neutral
                }
            }
            else if(Input.GetKeyDown(KeyCode.Q))
            {
                gearState = GearState.Reverse;
                if(currentGear == 0)
                {
                    currentGear = 1; // Shift to first gear if in neutral
                }
            }
            if(currentGear == 0)
            {
                gearState = GearState.Neutral;
            }
            // Turning
            if(Input.GetKey(KeyCode.A))
            {
                rb.AddTorque(currentTorgue * -Vector3.up);
            }
            else if(Input.GetKey(KeyCode.D))
            {
                rb.AddTorque(currentTorgue * Vector3.up);
            }

            // Set variables
            float realSpeed = rb.velocity.magnitude * 3.6f; // Convert to km/h
            float desiredRPM01;
            float expectedSpeed;
            float currentRPM01 = (currentRPM - minRPM) / (maxRPM - minRPM);

            // RPM
            if (gearState == GearState.Neutral)
            {
                desiredRPM = idleRPM;
            }
            else
            {
                if (desiredSpeed > realSpeed)
                {
                    desiredRPM = Mathf.Lerp(desiredRPM, maxRPM, Time.deltaTime * 0.25f);
                    desiredRPM = Mathf.Clamp(desiredRPM, minRPM, maxRPM);
                }
                else
                {
                    desiredRPM = Mathf.Lerp(desiredRPM, idleRPM, Time.deltaTime * 0.25f);
                    desiredRPM = Mathf.Clamp(desiredRPM, minRPM, maxRPM);
                }
            }
            desiredRPM01 = (desiredRPM - minRPM) / (maxRPM - minRPM);
            currentRPM = Mathf.Lerp(currentRPM, desiredRPM, Time.deltaTime * 0.25f);
            if (gearState == GearState.Forward)
            {
                if (currentGear > 1)
                {
                    expectedSpeed = desiredRPM01 * fwdMaxSpeed[currentGear - 1];
                }
            }
            else if(gearState == GearState.Reverse)
            {
                if (currentGear > 1)
                {
                    expectedSpeed = desiredRPM01 * revMaxSpeed[currentGear - 1];
                }
            }
            else
            {
                currentRPM = Mathf.Lerp(currentRPM, idleRPM, Time.deltaTime * 0.25f);
                expectedSpeed = 0f;
            }

            // Gear shifting
            if (gearState == GearState.Forward)
            {
                // Upshift
                if (currentGear < gearRatios_forward.Length - 1 && currentRPM > RPM_upshift)
                {
                    currentGear++;
                    currentRPM *= gearRatios_forward[currentGear - 1] / gearRatios_forward[currentGear - 2];
                }
                // Downshift
                else if (currentGear > 1 && (currentRPM < RPM_downshift | realSpeed < fwdMinSpeed[currentGear - 1]))
                {
                    currentRPM /= gearRatios_forward[currentGear - 1] / gearRatios_forward[currentGear - 2];
                    currentGear--;
                }
                else if (currentGear == 1 && currentRPM < minRPM)
                {
                    currentGear = 0; // Shift to neutral
                    currentRPM *= gearRatios_forward[currentGear] / gearRatios_forward[currentGear + 1];
                    gearState = GearState.Neutral;
                }
            }
            else if (gearState == GearState.Reverse)
            {
                // Upshift
                if (currentGear < gearRatios_reverse.Length - 1 && currentRPM > RPM_upshift)
                {
                    currentGear++;
                    currentRPM *= gearRatios_reverse[currentGear - 1] / gearRatios_reverse[currentGear - 2];
                }
                // Downshift
                else if (currentGear > 1 && (currentRPM < RPM_downshift | realSpeed < revMinSpeed[currentGear - 1]))
                {
                    currentRPM /= gearRatios_reverse[currentGear - 1] / gearRatios_reverse[currentGear - 2];
                    currentGear--;
                }
                else if (currentGear == 1 && currentRPM < minRPM)
                {
                    currentGear = 0; // Shift to neutral
                    currentRPM *= gearRatios_reverse[currentGear] / gearRatios_reverse[currentGear + 1];
                    gearState = GearState.Neutral;
                }
            }

            currentRPM = Mathf.Clamp(currentRPM, minRPM, maxRPM);

            // Apply torgue to rigidbody
            if (gearState != GearState.Neutral)
            {
                // Torgue calculation
                currentTorgue = torgueAtMinRPM + ((torgueAtMaxRPM - torgueAtMinRPM) * currentRPM01);
                currentTorgue *= gearState == GearState.Forward ? gearRatios_forward[currentGear - 1] : gearRatios_reverse[currentGear - 1];

                // Apply force
                int direction = gearState == GearState.Forward ? 1 : -1;
                rb.AddForce(currentTorgue * direction * transform.forward);
            }
        }

        // Engine sound
        HandleSounds();
    }


    // SOUND HANDLING
    private float _lastSentRPM = -999f;
    private void SendRpmToFmod(float rpm)
    {
        if (Mathf.Abs(rpm - _lastSentRPM) < 10f) return;
        _lastSentRPM = rpm;
        engineSoundEvent.setParameterByName("RPM", rpm);
    }
    private void HandleSounds()
    {
        if (isEngineOn && !engineSoundPlaying)
        {
            engineSoundEvent.start();
            engineSoundPlaying = true;
        }
        else if (!isEngineOn && engineSoundPlaying)
        {
            CleanupSound();
        }

        if (engineSoundEvent.isValid() && isEngineOn && engineSoundPlaying)
        {
            SendRpmToFmod(currentRPM);
            engineSoundEvent.set3DAttributes(RuntimeUtils.To3DAttributes(enginePos ? enginePos : transform));
        }
    }
    private void OnDisable() => CleanupSound();
    private void OnDestroy() => CleanupSound();
    private void CleanupSound()
    {
        if (!engineSoundEvent.isValid())
            return;

        engineSoundEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        engineSoundEvent.release();
        engineSoundPlaying = false;
        engineSoundEvent.clearHandle();

        Debug.Log("Engine sound cleaned up.");
    }
}
