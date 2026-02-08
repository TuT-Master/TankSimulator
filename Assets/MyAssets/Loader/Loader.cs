using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Loader;

public class Loader : MonoBehaviour
{
    [Header("Animations")]
    private Animator animator;
    [SerializeField] private Animator turretAnimator;
    [SerializeField] private Gunner gunner;
    public float loaderSpeedMultiplier = 1f;

    [Header("Ammunition loading")]
    [SerializeField] private GameObject ammo_KE;
    [SerializeField] private GameObject ammo_MZ;
    private bool loaderHatch_Open = false;
    public enum AmmoType
    {
        None,
        KE,
        MZ,
        HE,
    }
    public AmmoType currentAmmoTypeLoaded;
    private AmmoType nextAmmoTypeToLoad;

    [Header("Status")]
    public bool _AmmoBunkerDoors_Open = false;
    public bool _AutoReloadGunAfterFiring = true;
    public enum LoaderStatus
    {
        Idle,
        Fire,
        Sicher,
        ManipulatingWithGun,
        Loading,
    }
    public LoaderStatus status;
    private LoaderStatus desiredStatus;

    // References
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
        if(currentAmmoTypeLoaded != AmmoType.None)
            nextAmmoTypeToLoad = currentAmmoTypeLoaded;

        // Reset animation (IDK why I must do it this way bruh..)
        ClickOnPanel(LoadersPanelAction.Sicher);
    }


    // ----- COMMANDS -----
    public void SetNextAmmoType(AmmoType ammoType)
    {
        nextAmmoTypeToLoad = ammoType;
        switch (nextAmmoTypeToLoad)
        {
            case AmmoType.KE:
                ClickOnPanel(LoadersPanelAction.SwitchAmmoType_To_KE);
                break;
            case AmmoType.MZ:
                ClickOnPanel(LoadersPanelAction.SwitchAmmoType_To_MZ);
                break;
            case AmmoType.HE:
                ClickOnPanel(LoadersPanelAction.SwitchAmmoType_To_HE);
                break;
            case AmmoType.None:
                // Unload the gun
                break;
        }
    }
    public void ReloadMainGun()
    {
        if(nextAmmoTypeToLoad == AmmoType.None)
        {
            Debug.Log("No ammo type selected to load! Can't reload the main gun.");
            return;
        }

        animator.SetFloat("LoaderSpeedMultiplier", loaderSpeedMultiplier);
        status = LoaderStatus.Loading;

        switch (nextAmmoTypeToLoad)
        {
            case AmmoType.KE:
                currentAmmoTypeLoaded = AmmoType.KE;
                animator.SetTrigger("Load");
                break;
            case AmmoType.MZ:
                currentAmmoTypeLoaded = AmmoType.MZ;
                animator.SetTrigger("Load");
                break;
            case AmmoType.HE:
                currentAmmoTypeLoaded = AmmoType.HE;
                animator.SetTrigger("Load");
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

        // Set desired status
        switch (action)
        {
            case LoadersPanelAction.Sicher:
                desiredStatus = LoaderStatus.Sicher;
                break;
            case LoadersPanelAction.Fire:
                desiredStatus = LoaderStatus.Fire;
                break;
            case LoadersPanelAction.SwitchAmmoType_To_KE:
                desiredStatus = LoaderStatus.ManipulatingWithGun;
                break;
            case LoadersPanelAction.SwitchAmmoType_To_MZ:
                desiredStatus = LoaderStatus.ManipulatingWithGun;
                break;
            case LoadersPanelAction.SwitchAmmoType_To_HE:
                desiredStatus = LoaderStatus.ManipulatingWithGun;
                break;
        }
    }


    // ----- ANIMATION EVENTS -----
    public void LoadersPanel_Clicked()
    {
        switch(desiredStatus)
        {
            case LoaderStatus.Fire:
                status = LoaderStatus.Fire;
                break;
            case LoaderStatus.Sicher:
                status = LoaderStatus.Sicher;
                break;
            case LoaderStatus.ManipulatingWithGun:
                status = LoaderStatus.ManipulatingWithGun;
                gunner.TraverseToAngle_Y();
                break;
            case LoaderStatus.Loading:
                status = LoaderStatus.Loading;
                break;
            default:
                break;
        }
    }
    public void Ammo_Visibility_Off()
    {
        ammo_KE.SetActive(false);
        ammo_MZ.SetActive(false);
    }
    public void Ammo_Visibility_On()
    {
        Ammo_Visibility_Off();
        switch (currentAmmoTypeLoaded)
        {
            case AmmoType.KE:
                ammo_KE.SetActive(true);
                break;
            case AmmoType.MZ:
                ammo_MZ.SetActive(true);
                break;
            case AmmoType.HE:
                break;
        }
    }
    public void Ammo_Loaded()
    {
        Ammo_Visibility_Off();
        status = LoaderStatus.Idle;
        turret.loadingAngle_Active = false;
        gunner._CanShoot = true;
    }
    public void CloseBreach()
    {
        turretAnimator.SetTrigger("CloseBreach");
    }
    public void AmmoBunkerDoors_Open()
    {
        turretAnimator.SetTrigger("Open_AmmoBunkerDoors");
        _AmmoBunkerDoors_Open = true;
    }
    public void AmmoBunkerDoors_Close()
    {
        turretAnimator.SetTrigger("Close_AmmoBunkerDoors");
        _AmmoBunkerDoors_Open = false;
    }
}
