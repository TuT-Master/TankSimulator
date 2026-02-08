using FMODUnity;
using UnityEngine;

public abstract class CrewVoiceSO : ScriptableObject
{
    [Header("Global Settings")]
    public bool IsMuted = false;
    public enum Language
    {
        Czech,
        English,
    }
    public Language VoiceLanguage;

    [Header("Responses")]
    public EventReference Roger;
    public EventReference IDidNotUnderstand;

    [Header("Turret State")]
    public EventReference TurmAus;
    public EventReference Beobachten;
    public EventReference StabEin;

    public abstract bool ValidateVoiceLines();
}
