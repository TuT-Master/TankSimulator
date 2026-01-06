using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMOD.Studio;
using FMODUnity;

public class GunnerVoiceManager : MonoBehaviour
{
    [Header("Contact Report")]
    [SerializeField] private EventReference contact;
    [SerializeField] private EventReference ifv_frontaly;
    [SerializeField] private EventReference ifv_side;
    [SerializeField] private EventReference tank_frontaly;
    [SerializeField] private EventReference tank_side;
    [SerializeField] private EventReference troops;
    [SerializeField] private EventReference oneOClock;
    [SerializeField] private EventReference twoOClock;
    [SerializeField] private EventReference threeOClock;
    [SerializeField] private EventReference fourOClock;
    [SerializeField] private EventReference fiveOClock;
    [SerializeField] private EventReference sixOClock;
    [SerializeField] private EventReference sevenOClock;
    [SerializeField] private EventReference eightOClock;
    [SerializeField] private EventReference nineOClock;
    [SerializeField] private EventReference tenOClock;
    [SerializeField] private EventReference elevenOClock;
    [SerializeField] private EventReference twelveOClock;
    [SerializeField] private EventReference range;
    [SerializeField] private EventReference r50;
    [SerializeField] private EventReference r100;
    [SerializeField] private EventReference r200;
    [SerializeField] private EventReference r300;
    [SerializeField] private EventReference r400;
    [SerializeField] private EventReference r500;
    [SerializeField] private EventReference r600;
    [SerializeField] private EventReference r700;
    [SerializeField] private EventReference r800;
    [SerializeField] private EventReference r900;
    [SerializeField] private EventReference r1000;
    [SerializeField] private EventReference r2000;
    [SerializeField] private EventReference meters;

    [Header("Hit Report")]
    [SerializeField] private EventReference tankDestroyed;
    [SerializeField] private EventReference ifvDestroyed;
    [SerializeField] private EventReference troopsDestroyed;
    [SerializeField] private EventReference firing;

    [Header("Controls")]
    [SerializeField] private EventReference changingToMainGun;
    [SerializeField] private EventReference changingToMachinegun;
    [SerializeField] private EventReference turmAus;
    [SerializeField] private EventReference beobachten;
    [SerializeField] private EventReference stabEin;

    [Header("Responses")]
    [SerializeField] private EventReference roger;
    [SerializeField] private EventReference iDidNotUnderstand;



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


    // ----- PUBLIC METHODS -----
    public void PlayOneShot(OneShot voiceLine)
    {
        if (oneShotVoiceLines.ContainsKey(voiceLine))
        {
            RuntimeManager.PlayOneShot(oneShotVoiceLines[voiceLine]);
        }
    }
    public enum ContactType
    {
        Ifv_Frontaly,
        Ifv_Side,
        Tank_Frontaly,
        Tank_Side,
        Troops,
    }
    public IEnumerator PlayContactReport(ContactType contactType, float localAzimuth, float range)
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
        switch (contactType)
        {
            case ContactType.Ifv_Frontaly:
                instance = RuntimeManager.CreateInstance(ifv_frontaly);
                break;
            case ContactType.Ifv_Side:
                instance = RuntimeManager.CreateInstance(ifv_side);
                break;
            case ContactType.Tank_Frontaly:
                instance = RuntimeManager.CreateInstance(tank_frontaly);
                break;
            case ContactType.Tank_Side:
                instance = RuntimeManager.CreateInstance(tank_side);
                break;
            case ContactType.Troops:
                instance = RuntimeManager.CreateInstance(troops);
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
