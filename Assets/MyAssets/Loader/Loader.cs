using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Loader : MonoBehaviour
{
    [Header("Animations")]
    private Animator animator;
    [SerializeField] private Animator turretAnimator;
    [SerializeField] private Gunner gunner;
    public float loaderSpeedMultiplier = 1f;

    [Header("Ammunition loading")]
    [SerializeField] private GameObject ammo_KE;
    private bool loaderHatch_Open = false;
    private bool switchingAmmoTypes = false;
    public enum AmmoType
    {
        None,
        KE,
        MZ,
        HE,
    }
    public AmmoType currentAmmoTypeLoaded;

    [Header("Status")]
    public bool ammoBunkerDoors_Open = false;
    public bool manipulatingWithGun = false;


    private Turret turret;


    // ----- ON START -----
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    private void Start()
    {
        turret = turretAnimator.GetComponent<Turret>();
        ammo_KE.SetActive(false);
    }


    // ----- COMMANDS -----
    public void LoadAmmoType(AmmoType ammoType)
    {
        animator.SetFloat("LoaderSpeedMultiplier", loaderSpeedMultiplier);
        manipulatingWithGun = true;
        switch (ammoType)
        {
            case AmmoType.KE:
                animator.SetTrigger("Load_KE");
                currentAmmoTypeLoaded = AmmoType.KE;
                break;
            case AmmoType.MZ:

                currentAmmoTypeLoaded = AmmoType.MZ;
                break;
            case AmmoType.HE:

                currentAmmoTypeLoaded = AmmoType.HE;
                break;
        }
    }
    public void Hatch_OpenClose(bool open)
    {
        if(loaderHatch_Open == open) return;
        turretAnimator.SetTrigger(open ? "Open_LoaderHatch" : "Close_LoaderHatch");
        loaderHatch_Open = open;
    }
    public enum LoadersPanelAction
    {
        Sicher,
        Fire,
        SwitchAmmoType_To_KE,
        SwitchAmmoType_To_MZ,
        SwitchAmmoType_To_HE,
    }
    public void ClickOnPanel(LoadersPanelAction action)
    {
        // Trigger animation
        animator.SetTrigger("ClickOnLoadersPanel");

        // Trigger specific action
        switch (action)
        {
            case LoadersPanelAction.Sicher:

                break;
            case LoadersPanelAction.Fire:

                break;
            case LoadersPanelAction.SwitchAmmoType_To_KE:
                currentAmmoTypeLoaded = AmmoType.KE;
                break;
            case LoadersPanelAction.SwitchAmmoType_To_MZ:
                currentAmmoTypeLoaded = AmmoType.MZ;
                break;
            case LoadersPanelAction.SwitchAmmoType_To_HE:
                currentAmmoTypeLoaded = AmmoType.HE;
                break;
        }
    }


    // ----- ANIMATION EVENTS -----
    public void LoadersPanel_Clicked()
    {
        if(switchingAmmoTypes) gunner.TraverseToAngle_Y();
    }
    public void Ammo_KE_Visibility_Off() => ammo_KE.SetActive(false);
    public void Ammo_KE_Visibility_On() => ammo_KE.SetActive(true);
    public void Ammo_Loaded()
    {
        Ammo_KE_Visibility_Off();
        manipulatingWithGun = false;
        turret.loadingAngle_Active = false;
    }
    public void CloseBreach()
    {
        turretAnimator.SetTrigger("CloseBreach");
    }
    public void AmmoBunkerDoors_Open()
    {
        turretAnimator.SetTrigger("Open_AmmoBunkerDoors");
        ammoBunkerDoors_Open = true;
    }
    public void AmmoBunkerDoors_Close()
    {
        turretAnimator.SetTrigger("Close_AmmoBunkerDoors");
        ammoBunkerDoors_Open = false;
    }
}
