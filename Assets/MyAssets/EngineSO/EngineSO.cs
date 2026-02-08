using UnityEngine;

[CreateAssetMenu(fileName = "Engine model", menuName = "Scriptable objects/Engine model")]
public class EngineSO : ScriptableObject
{
    [Header("Engine")]
    public float idleRPM;
    public float maxRPM;
    public float minRPM;
    public float RPM_downshift;
    public float RPM_upshift;
    public float torgueAtMaxRPM;
    public float torgueAtMinRPM;

    [Header("Transmission")]
    public float[] gearRatios_forward;
    public float[] gearRatios_reverse;
    public float[] forwardSpeeds_min;
    public float[] forwardSpeeds_max;
    public float[] reverseSpeeds_min;
    public float[] reverseSpeeds_max;
}
