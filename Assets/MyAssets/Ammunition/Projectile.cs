using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Effects")]
    [SerializeField] private GameObject KE_Impact_Effect_Ground;

    [Header("Ammo stats")]
    public Loader.AmmoType ammoType;

    public Gunner gunner;


    private void OnCollisionEnter(Collision collision)
    {
        // Get impact point
        ContactPoint contact = collision.contacts[0];
        bool isGround = collision.gameObject.CompareTag("Ground");

        // Enemy hit
        if (collision.collider.gameObject.layer == 9 && collision.collider.TryGetComponent(out PartOfEnemy poe))
        {
            // For testing purposes it's insta death
            gunner.SetResultOfFiring(poe.enemy.targetType, true, true);
        }

        // Spawn explosion effect based on ammo type
        switch (ammoType)
        {
            case Loader.AmmoType.KE:
                GameObject effect = null;
                if (isGround)
                    effect = KE_Impact_Effect_Ground;
                else
                    effect = KE_Impact_Effect_Ground;
                Instantiate(effect, contact.point, Quaternion.LookRotation(Vector3.forward));
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
