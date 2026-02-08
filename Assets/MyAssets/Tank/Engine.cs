using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Engine : MonoBehaviour
{
    [Header("Engine model")]
    [SerializeField] private EngineSO _engine;

    // Engine
    [SerializeField] private float currentRPM;
    private float idleRPM;
    private float maxRPM;
    private float minRPM;
    private float RPM_downshift;
    private float RPM_upshift;
    private float torqueAtMaxRPM;
    private float torqueAtMinRPM;
    private bool isEngineOn = true;

    [Header("Transmission Settings")]
    [SerializeField] private float _turningSensitivity = 1f;
    [SerializeField] private float _rpmAccelerationRate = 0.25f;
    [SerializeField] private float _rpmDeccelerationRate = 1f;
    [SerializeField] private float _currentSpeed;
    private float[] gearRatios_forward;
    private float[] gearRatios_reverse;
    private float[] forwardSpeeds_min;
    private float[] forwardSpeeds_max;
    private float[] reverseSpeeds_min;
    private float[] reverseSpeeds_max;
    private float currentTorgue;
    [SerializeField] private int currentGear = 0;
    [SerializeField] private float turning = 0f;
    [SerializeField] private float maxTurningSpeedDelta_Percentage = 0.1f;
    private enum GearState
    {
        Neutral,
        Forward,
        Reverse
    }
    private GearState gearState = GearState.Neutral;

    [Header("Track Settings")]
    [SerializeField] private List<WheelCollider> leftTrack;
    [SerializeField] private List<WheelCollider> rightTrack;
    [SerializeField] private bool _invertHorizontalAxis = false;
    private float _leftTrackSpeed = 0f;
    private float _rightTrackSpeed = 0f;

    [Header("Controlling the engine")]
    public float desiredSpeed = 0f;
    [SerializeField] private float desiredSpeed_step = 4f;
    private float desiredRPM;

    [Header("Sound Settings")]
    [SerializeField] private Transform enginePos;
    [SerializeField] private EventReference engineSound;
    private EventInstance engineSoundEvent;
    private bool engineSoundPlaying = false;





    // ----- TRACKS -----
    private enum TrackType { Both, Left, Right };
    private void ApplySpeedToAllWheels(float speedInKPH, float torque, TrackType track)
    {
        float speedMPS = speedInKPH / 3.6f;
        float circumference = leftTrack[0].radius * 2f * Mathf.PI;
        float angleSpeed = (speedMPS / circumference) * 360f;

        switch(track)
        {
            case TrackType.Both:
                foreach (WheelCollider wc in leftTrack)
                {
                    wc.rotationSpeed = angleSpeed;
                }
                foreach (WheelCollider wc in rightTrack)
                {
                    wc.rotationSpeed = angleSpeed;
                }
                break;
            case TrackType.Left:
                foreach (WheelCollider wc in leftTrack)
                {
                    wc.rotationSpeed = angleSpeed;
                }
                break;
            case TrackType.Right:
                foreach (WheelCollider wc in rightTrack)
                {
                    wc.rotationSpeed = angleSpeed;
                }
                break;
        }
    }
    private float GetCurrentSpeed(bool kph = false)
    {
        return (kph ? 3.6f : 1f) * ((_leftTrackSpeed + _rightTrackSpeed) / 2f);
    }
    private void UpdateSpeeds()
    {
        _leftTrackSpeed = (leftTrack[0].rotationSpeed / 360f) * (leftTrack[0].radius * 2f * Mathf.PI);
        _rightTrackSpeed = (rightTrack[0].rotationSpeed / 360f) * (rightTrack[0].radius * 2f * Mathf.PI);
    }


    // ----- START -----
    private void Start()
    {
        // Create sound instance
        if (!engineSoundEvent.isValid())
            engineSoundEvent = RuntimeManager.CreateInstance(engineSound);

        // Read values from engine model
        ReadValuesFromEngineModel();

        // Set variables
        desiredRPM = idleRPM;
    }
    private void ReadValuesFromEngineModel()
    {
        // Engine
        idleRPM = _engine.idleRPM;
        maxRPM = _engine.maxRPM;
        minRPM = _engine.minRPM;
        RPM_downshift = _engine.RPM_downshift;
        RPM_upshift = _engine.RPM_upshift;
        torqueAtMaxRPM = _engine.torgueAtMaxRPM;
        torqueAtMinRPM = _engine.torgueAtMinRPM;

        // Transmission
        gearRatios_forward = _engine.gearRatios_forward;
        gearRatios_reverse = _engine.gearRatios_reverse;
        forwardSpeeds_max = _engine.forwardSpeeds_max;
        forwardSpeeds_min = _engine.forwardSpeeds_min;
        reverseSpeeds_max = _engine.reverseSpeeds_max;
        reverseSpeeds_min = _engine.reverseSpeeds_min;

        // Min and max speed set suggestion
        if (_engine.forwardSpeeds_max == null || _engine.forwardSpeeds_max.Length == 0 ||
            _engine.forwardSpeeds_min == null || _engine.forwardSpeeds_min.Length == 0 ||
            _engine.reverseSpeeds_max == null || _engine.reverseSpeeds_max.Length == 0 ||
            _engine.reverseSpeeds_min == null || _engine.reverseSpeeds_min.Length == 0)
            Debug.Log($"Engine model {_engine.name} has no speeds setting!");
    }


    // ----- UPDATE -----
    private void Update()
    {
        // Update speeds from tracks
        UpdateSpeeds();

        // Engine
        if (isEngineOn)
        {
            _currentSpeed = Mathf.Abs(GetCurrentSpeed(true));
            if(_currentSpeed < 0.1f) _currentSpeed = 0f;

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
                gearState == GearState.Forward ? forwardSpeeds_max[^1] : gearState == GearState.Reverse ? reverseSpeeds_max[^1] : 0f);
            // Turning
            float turningDir = _invertHorizontalAxis ? 1f : -1f;
            if (Input.GetKey(KeyCode.A))
                turning -= _turningSensitivity * Time.deltaTime * turningDir;
            else if (Input.GetKey(KeyCode.D))
                turning += _turningSensitivity * Time.deltaTime * turningDir;
            else
            {
                turning = Mathf.Lerp(turning, 0f, Time.deltaTime * _turningSensitivity);
                if (Mathf.Abs(turning) <= 1f) turning = 0f;
            }
            turning = Mathf.Clamp(turning, -100f, 100f);

            // Gear state
            if (Input.GetKeyDown(KeyCode.E) && gearState == GearState.Neutral)
                ShiftForward();
            else if (Input.GetKeyDown(KeyCode.E) && gearState == GearState.Reverse)
                ShiftNeutral();
            else if (Input.GetKeyDown(KeyCode.Q) && gearState == GearState.Neutral)
                ShiftReverse();
            else if (Input.GetKeyDown(KeyCode.Q) && gearState == GearState.Forward)
                ShiftNeutral();

            if (currentGear == 0 && gearState != GearState.Neutral)
                ShiftNeutral();

            // RPM from engine
            if (gearState != GearState.Neutral)
            {
                float additionalAccelRate = gearState == GearState.Forward ? gearRatios_forward[currentGear - 1] : gearRatios_reverse[currentGear - 1];
                if (desiredSpeed > _currentSpeed)
                {
                    desiredRPM = Mathf.Lerp(desiredRPM, maxRPM, Time.deltaTime * _rpmAccelerationRate * additionalAccelRate);
                    desiredRPM = Mathf.Clamp(desiredRPM, minRPM, maxRPM);
                }
                else
                {
                    desiredRPM = Mathf.Lerp(desiredRPM, idleRPM, Time.deltaTime * _rpmDeccelerationRate * additionalAccelRate);
                    desiredRPM = Mathf.Clamp(desiredRPM, minRPM, maxRPM);
                }
            }
            else
                desiredRPM = idleRPM;

            currentRPM = Mathf.Lerp(currentRPM, desiredRPM, Time.deltaTime * _rpmDeccelerationRate);

            // Shift gears
            if (gearState == GearState.Forward) // Forward
            {
                if (currentRPM >= RPM_upshift || _currentSpeed >= forwardSpeeds_max[currentGear - 1]) // Upshift
                {
                    if (currentGear < gearRatios_forward.Length)
                        Upshift();
                }
                else if (currentRPM <= RPM_downshift || _currentSpeed <= forwardSpeeds_min[currentGear - 1]) // Downshift
                {
                    if (currentGear > 1)
                        Downshift();
                }
            }
            else if (gearState == GearState.Reverse) // Reverse
            {
                if (currentRPM >= RPM_upshift || _currentSpeed >= reverseSpeeds_max[currentGear - 1]) // Upshift
                {
                    if (currentGear < gearRatios_reverse.Length)
                        Upshift();
                }
                else if (currentRPM <= RPM_downshift || _currentSpeed <= reverseSpeeds_min[currentGear - 1]) // Downshift
                {
                    if (currentGear > 1)
                        Downshift();
                }
            }

            // Apply speed and torque
            if (gearState != GearState.Neutral && desiredSpeed > 0.1f)
            {
                float currentRPM01 = (currentRPM - RPM_downshift) / (RPM_upshift - RPM_downshift);

                // Torque calculation
                float currentTorque01 = ((torqueAtMaxRPM - torqueAtMinRPM) * currentRPM01) + torqueAtMinRPM;
                float gearRatio = gearState == GearState.Forward ? gearRatios_forward[currentGear - 1] : gearRatios_reverse[currentGear - 1];
                currentTorgue = currentTorque01 * gearRatio;

                // Speed calculation
                float speedMin = gearState == GearState.Forward ? forwardSpeeds_min[currentGear - 1] : reverseSpeeds_min[currentGear - 1];
                float speedMax = gearState == GearState.Forward ? forwardSpeeds_max[currentGear - 1] : reverseSpeeds_max[currentGear - 1];
                float speedByRPMandGearRatio = ((speedMax - speedMin) * currentRPM01) + speedMin;

                // Direction
                float dir = gearState == GearState.Forward ? 1f : -1f;
                float finSpeed = dir * speedByRPMandGearRatio;
                float finTorque = dir * currentTorgue;

                if (turning == 0)
                    ApplySpeedToAllWheels(finSpeed, dir * 0f, TrackType.Both);
                else
                {
                    float maxSpeed = gearState == GearState.Forward ? forwardSpeeds_max[currentGear - 1] : reverseSpeeds_max[currentGear - 1];
                    float turnSpeedDelta = maxSpeed * maxTurningSpeedDelta_Percentage * (Mathf.Abs(turning) / 100f);
                    if (turning > 0) // Right
                    {
                        ApplySpeedToAllWheels(finSpeed - turnSpeedDelta, dir * 0f, TrackType.Left);
                        ApplySpeedToAllWheels(finSpeed + turnSpeedDelta, dir * 0f, TrackType.Right);
                    }
                    else // Left
                    {
                        ApplySpeedToAllWheels(finSpeed + turnSpeedDelta, dir * 0f, TrackType.Left);
                        ApplySpeedToAllWheels(finSpeed - turnSpeedDelta, dir * 0f, TrackType.Right);
                    }
                }
            }
        }

        // Engine sound
        HandleSounds();
    }


    // ----- TRANSMISSION -----
    private void ShiftForward()
    {
        gearState = GearState.Forward;
        currentGear = 1;
    }
    private void ShiftReverse()
    {
        gearState = GearState.Reverse;
        currentGear = 1;
    }
    private void ShiftNeutral()
    {
        gearState = GearState.Neutral;
        currentGear = 0;
        Brake();
    }
    private void Upshift()
    {
        switch(gearState)
        {
            case GearState.Forward:
                if(currentGear < gearRatios_forward.Length)
                {
                    currentRPM *= gearRatios_forward[currentGear] / gearRatios_forward[currentGear - 1];
                    currentGear++;
                }
                break;
            case GearState.Reverse:
                if (currentGear < gearRatios_reverse.Length)
                {
                    currentRPM *= gearRatios_reverse[currentGear] / gearRatios_reverse[currentGear - 1];
                    currentGear++;
                }
                break;
        }
    }
    private void Downshift()
    {
        switch (gearState)
        {
            case GearState.Forward:
                if (currentGear > 1)
                {
                    currentGear--;
                    currentRPM *= gearRatios_forward[currentGear - 1] / gearRatios_forward[currentGear];
                }
                break;
            case GearState.Reverse:
                if (currentGear > 1)
                {
                    currentGear--;
                    currentRPM *= gearRatios_forward[currentGear - 1] / gearRatios_forward[currentGear];
                }
                break;
        }
    }
    private void Brake()
    {
        foreach (WheelCollider wc in leftTrack)
        {
            wc.rotationSpeed = 0;
            wc.brakeTorque = 100f;
        }
        foreach (WheelCollider wc in rightTrack)
        {
            wc.rotationSpeed = 0;
            wc.brakeTorque = 100f;
        }
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
    }
}
