using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Effects")]
    [SerializeField] private GameObject KE_Impact_Effect_Ground;

    [Header("Ammo stats")]
    public Loader.AmmoType ammoType;


    private void OnCollisionEnter(Collision collision)
    {
        // Get impact point
        ContactPoint contact = collision.contacts[0];
        bool isGround = collision.gameObject.CompareTag("Ground");

        // Spawn explosion effect based on ammo type
        switch (ammoType)
        {
            case Loader.AmmoType.KE:
                if (isGround)
                {
                    Instantiate(KE_Impact_Effect_Ground, contact.point, Quaternion.LookRotation(Vector3.forward));
                }
                else
                {
                    
                }
                break;
            case Loader.AmmoType.MZ:

                break;
            case Loader.AmmoType.HE:

                break;
            default: break;
        }

        // Destroy the projectile after impact
        Destroy(gameObject);
    }
}
