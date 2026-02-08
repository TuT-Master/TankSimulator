using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Enemy;
using static Gunner;

public class GunnerVoiceManager : MonoBehaviour
{
    [Header("Voice Settings")]
    [SerializeField] private GunnerVoiceSO voice;

    // Contact Report
    private EventReference contact;
    private EventReference ifv_frontaly;
    private EventReference ifv_side;
    private EventReference tank_frontaly;
    private EventReference tank_side;
    private EventReference troops;
    private EventReference oneOClock;
    private EventReference twoOClock;
    private EventReference threeOClock;
    private EventReference fourOClock;
    private EventReference fiveOClock;
    private EventReference sixOClock;
    private EventReference sevenOClock;
    private EventReference eightOClock;
    private EventReference nineOClock;
    private EventReference tenOClock;
    private EventReference elevenOClock;
    private EventReference twelveOClock;
    private EventReference range;
    private EventReference r50;
    private EventReference r100;
    private EventReference r200;
    private EventReference r300;
    private EventReference r400;
    private EventReference r500;
    private EventReference r600;
    private EventReference r700;
    private EventReference r800;
    private EventReference r900;
    private EventReference r1000;
    private EventReference r2000;
    private EventReference meters;

    // Hit Report
    private EventReference tankDestroyed;
    private EventReference ifvDestroyed;
    private EventReference troopsDestroyed;
    private EventReference firing;

    // Controls
    private EventReference changingToMainGun;
    private EventReference changingToMachinegun;
    private EventReference turmAus;
    private EventReference beobachten;
    private EventReference stabEin;

    // Responses
    private EventReference roger;
    private EventReference iDidNotUnderstand;



    public enum OneShot
    {
        TankDestroyed,
        IfvDestroyed,
        TroopsDestroyed,
        Firing,
        ChangingToMainGun,
        ChangingToMachinegun,
        TurmAus,
        Beobachten,
        StabEin,
        Roger,
        IDidNotUnderstand,
    }
    private Dictionary<OneShot, EventReference> oneShotVoiceLines;


    // ----- ON START -----
    private void Start()
    {
        if (voice == null || !voice.ValidateVoiceLines())
        {
            Debug.Log("Voice lines for Gunner was NOT loaded correctly!");
            return;
        }

        LoadVoiceLines();

        oneShotVoiceLines = new()
        {
            {OneShot.TankDestroyed, tankDestroyed },
            {OneShot.IfvDestroyed, ifvDestroyed },
            {OneShot.TroopsDestroyed, troopsDestroyed },
            {OneShot.Firing, firing },
            {OneShot.ChangingToMainGun, changingToMainGun},
            {OneShot.ChangingToMachinegun, changingToMachinegun },
            {OneShot.TurmAus, turmAus },
            {OneShot.Beobachten, beobachten},
            {OneShot.StabEin, stabEin },
            {OneShot.Roger, roger },
            {OneShot.IDidNotUnderstand, iDidNotUnderstand },
        };
    }
    private void LoadVoiceLines()
    {
        contact = voice.Contact;
        ifv_frontaly = voice.Ifv_frontaly;
        ifv_side = voice.Ifv_side;
        tank_frontaly = voice.Tank_frontaly;
        tank_side = voice.Tank_side;
        troops = voice.Troops;
        oneOClock = voice.OneOClock;
        twoOClock = voice.TwoOClock;
        threeOClock = voice.ThreeOClock;
        fourOClock = voice.FourOClock;
        fiveOClock = voice.FiveOClock;
        sixOClock = voice.SixOClock;
        sevenOClock = voice.SevenOClock;
        eightOClock = voice.EightOClock;
        nineOClock = voice.NineOClock;
        tenOClock = voice.TenOClock;
        elevenOClock = voice.ElevenOClock;
        twelveOClock = voice.TwelveOClock;
        range = voice.Range;
        r50 = voice.R50;
        r100 = voice.R100;
        r200 = voice.R200;
        r300 = voice.R300;
        r400 = voice.R400;
        r500 = voice.R500;
        r600 = voice.R600;
        r700 = voice.R700;
        r800 = voice.R800;
        r900 = voice.R900;
        r1000 = voice.R1000;
        r2000 = voice.R2000;
        meters = voice.Meters;

        tankDestroyed = voice.TankDestroyed;
        ifvDestroyed = voice.IfvDestroyed;
        troopsDestroyed = voice.TroopsDestroyed;
        firing = voice.Firing;

        changingToMainGun = voice.ChangingToMainGun;
        changingToMachinegun = voice.ChangingToMachinegun;
        turmAus = voice.TurmAus;
        beobachten = voice.Beobachten;
        stabEin = voice.StabEin;

        roger = voice.Roger;
        iDidNotUnderstand = voice.IDidNotUnderstand;
    }


    // ----- PUBLIC METHODS -----
    public void PlayOneShot(OneShot voiceLine)
    {
        if (oneShotVoiceLines.ContainsKey(voiceLine))
        {
            RuntimeManager.PlayOneShot(oneShotVoiceLines[voiceLine]);
        }
    }
    public IEnumerator PlayContactReport(TargetFacingDirection targetDir, TargetType contactType, float localAzimuth, float range)
    {
        // Contact
        EventInstance instance = RuntimeManager.CreateInstance(contact);
        instance.start();
        instance.getPlaybackState(out PLAYBACK_STATE state);
        // Wait until it stops
        while (state != PLAYBACK_STATE.STOPPED)
        {
            instance.getPlaybackState(out state);
            yield return null;
        }

        // Contact type
        if (contactType == TargetType.Tank && targetDir == TargetFacingDirection.Front)
            instance = RuntimeManager.CreateInstance(tank_frontaly);
        else if (contactType == TargetType.Tank && targetDir == TargetFacingDirection.Side)
            instance = RuntimeManager.CreateInstance(tank_side);
        else if (contactType == TargetType.IFV && targetDir == TargetFacingDirection.Front)
            instance = RuntimeManager.CreateInstance(ifv_frontaly);
        else if (contactType == TargetType.IFV && targetDir == TargetFacingDirection.Side)
            instance = RuntimeManager.CreateInstance(ifv_side);
        else if (contactType == TargetType.Infantry)
            instance = RuntimeManager.CreateInstance(troops);
        else // Fallback 
        {
            Debug.Log($"Fallback for contact type {contactType} and direction {targetDir}");
            instance = RuntimeManager.CreateInstance(tank_frontaly);
        }
        
        instance.start();
        instance.getPlaybackState(out state);
        // Wait until it stops
        while (state != PLAYBACK_STATE.STOPPED)
        {
            instance.getPlaybackState(out state);
            yield return null;
        }

        // Direction
        instance = GetDirectionFromLocalAzimuth(localAzimuth) switch
        {
            1 => RuntimeManager.CreateInstance(oneOClock),
            2 => RuntimeManager.CreateInstance(twoOClock),
            3 => RuntimeManager.CreateInstance(threeOClock),
            4 => RuntimeManager.CreateInstance(fourOClock),
            5 => RuntimeManager.CreateInstance(fiveOClock),
            6 => RuntimeManager.CreateInstance(sixOClock),
            7 => RuntimeManager.CreateInstance(sevenOClock),
            8 => RuntimeManager.CreateInstance(eightOClock),
            9 => RuntimeManager.CreateInstance(nineOClock),
            10 => RuntimeManager.CreateInstance(tenOClock),
            11 => RuntimeManager.CreateInstance(elevenOClock),
            12 => RuntimeManager.CreateInstance(twelveOClock),
            _ => RuntimeManager.CreateInstance(twelveOClock),
        };
        instance.start();
        instance.getPlaybackState(out state);
        // Wait until it stops
        while (state != PLAYBACK_STATE.STOPPED)
        {
            instance.getPlaybackState(out state);
            yield return null;
        }

        // Range
        instance = RuntimeManager.CreateInstance(this.range);
        instance.start();
        instance.getPlaybackState(out state);
        // Wait until it stops
        while (state != PLAYBACK_STATE.STOPPED)
        {
            instance.getPlaybackState(out state);
            yield return null;
        }

        // Range value 1000
        if(range >= 975f)
        {
            int thousands = 1;
            if (range >= 1975f) thousands = 2;
            switch (thousands)
            {
                case 1:
                    instance = RuntimeManager.CreateInstance(r1000);
                    break;
                case 2:
                    instance = RuntimeManager.CreateInstance(r2000);
                    break;
                default:

                    break;
            }
            instance.start();
            instance.getPlaybackState(out state);
            // Wait until it stops
            while (state != PLAYBACK_STATE.STOPPED)
            {
                instance.getPlaybackState(out state);
                yield return null;
            }
            range -= thousands * 1000f;
        }
        // Range value 100
        if (range >= 75f)
        {
            int hundreds = 1;
            if (range >= 875f) hundreds = 9;
            else if (range >= 775f) hundreds = 8;
            else if (range >= 675f) hundreds = 7;
            else if (range >= 575f) hundreds = 6;
            else if (range >= 475f) hundreds = 5;
            else if (range >= 375f) hundreds = 4;
            else if (range >= 275f) hundreds = 3;
            else if (range >= 175f) hundreds = 2;
            switch (hundreds)
            {
                case 1:
                    instance = RuntimeManager.CreateInstance(r100);
                    break;
                case 2:
                    instance = RuntimeManager.CreateInstance(r200);
                    break;
                case 3:
                    instance = RuntimeManager.CreateInstance(r300);
                    break;
                case 4:
                    instance = RuntimeManager.CreateInstance(r400);
                    break;
                case 5:
                    instance = RuntimeManager.CreateInstance(r500);
                    break;
                case 6:
                    instance = RuntimeManager.CreateInstance(r600);
                    break;
                case 7:
                    instance = RuntimeManager.CreateInstance(r700);
                    break;
                case 8:
                    instance = RuntimeManager.CreateInstance(r800);
                    break;
                case 9:
                    instance = RuntimeManager.CreateInstance(r900);
                    break;
                default:

                    break;
            }
            instance.start();
            instance.getPlaybackState(out state);
            // Wait until it stops
            while (state != PLAYBACK_STATE.STOPPED)
            {
                instance.getPlaybackState(out state);
                yield return null;
            }
            range -= hundreds * 100f;
        }
        // Range value 50
        if (range >= 25f)
        {
            instance = RuntimeManager.CreateInstance(r50);
            instance.start();
            instance.getPlaybackState(out state);
            // Wait until it stops
            while (state != PLAYBACK_STATE.STOPPED)
            {
                instance.getPlaybackState(out state);
                yield return null;
            }
        }

        // Meters
        instance = RuntimeManager.CreateInstance(meters);
        instance.start();
    }
    private int GetDirectionFromLocalAzimuth(float localAzimuth)
    {
        if (localAzimuth < 0) localAzimuth += 360f;
        if (localAzimuth >= 345f || localAzimuth < 15f) return 12;
        else if (localAzimuth >= 15f && localAzimuth < 45f) return 1;
        else if (localAzimuth >= 45f && localAzimuth < 75f) return 2;
        else if (localAzimuth >= 75f && localAzimuth < 105f) return 3;
        else if (localAzimuth >= 105f && localAzimuth < 135f) return 4;
        else if (localAzimuth >= 135f && localAzimuth < 165f) return 5;
        else if (localAzimuth >= 165f && localAzimuth < 195f) return 6;
        else if (localAzimuth >= 195f && localAzimuth < 225f) return 7;
        else if (localAzimuth >= 225f && localAzimuth < 255f) return 8;
        else if (localAzimuth >= 255f && localAzimuth < 285f) return 9;
        else if (localAzimuth >= 285f && localAzimuth < 315f) return 10;
        else if (localAzimuth >= 315f && localAzimuth < 345f) return 11;
        return 0;
    }
}
