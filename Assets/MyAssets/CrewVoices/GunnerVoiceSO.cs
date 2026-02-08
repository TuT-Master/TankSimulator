using FMODUnity;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Gunner Voice", menuName = "Scriptable objects/Crew Voice/Gunner Voice")]
public class GunnerVoiceSO : CrewVoiceSO
{
    [Header("Contact Report")]
    public EventReference Contact;
    public EventReference Ifv_frontaly;
    public EventReference Ifv_side;
    public EventReference Tank_frontaly;
    public EventReference Tank_side;
    public EventReference Troops;
    public EventReference OneOClock;
    public EventReference TwoOClock;
    public EventReference ThreeOClock;
    public EventReference FourOClock;
    public EventReference FiveOClock;
    public EventReference SixOClock;
    public EventReference SevenOClock;
    public EventReference EightOClock;
    public EventReference NineOClock;
    public EventReference TenOClock;
    public EventReference ElevenOClock;
    public EventReference TwelveOClock;
    public EventReference Range;
    public EventReference R50;
    public EventReference R100;
    public EventReference R200;
    public EventReference R300;
    public EventReference R400;
    public EventReference R500;
    public EventReference R600;
    public EventReference R700;
    public EventReference R800;
    public EventReference R900;
    public EventReference R1000;
    public EventReference R2000;
    public EventReference Meters;

    [Header("Hit Report")]
    public EventReference TankDestroyed;
    public EventReference IfvDestroyed;
    public EventReference TroopsDestroyed;
    public EventReference Firing;

    [Header("Controls")]
    public EventReference ChangingToMainGun;
    public EventReference ChangingToMachinegun;


    public override bool ValidateVoiceLines()
    {
        List<EventReference> voiceLines = new()
        {
            Roger,
            IDidNotUnderstand,
            TurmAus,
            Beobachten,
            StabEin,
            Contact,
            Ifv_frontaly,
            Ifv_side,
            Tank_frontaly,
            Tank_side,
            Troops,
            OneOClock,
            TwoOClock,
            ThreeOClock,
            FourOClock,
            FiveOClock,
            SixOClock,
            SevenOClock,
            EightOClock,
            NineOClock,
            TenOClock,
            ElevenOClock,
            TwelveOClock,
            Range,
            R50,
            R100,
            R200,
            R300,
            R400,
            R500,
            R600,
            R700,
            R800,
            R900,
            R1000,
            R2000,
            Meters,
            TankDestroyed,
            IfvDestroyed,
            TroopsDestroyed,
            Firing,
            ChangingToMainGun,
            ChangingToMachinegun,
        };

        foreach (EventReference er in voiceLines)
            if (er.Path.Length == 0)
                return false;

        return true;
    }
}
