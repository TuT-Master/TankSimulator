using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour
{
    [SerializeField] private Gunner gunner;

    public bool loadingAngle_Active = false;

    [Header("Turret state")]
    public TurretState turretState = TurretState.TurmAus;
    public enum TurretState
    {
        TurmAus,
        Beobachten,
        StabEin
    }

    public void TraverseToLoadingAngle() => gunner.TraverseToAngle_Y();
}
