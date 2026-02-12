using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float MuzzleVelocity;
    public float Mass;
    public float AirDrag;
    [SerializeField] private string groundTag = "Ground";
    [SerializeField] private string enemyTag = "Enemy";
    private Vector3 velocity;
    private bool isLaunched;

    [Header("Effects")]
    [SerializeField] private GameObject KE_Impact_Effect_Ground;
    [SerializeField] private GameObject KE_Impact_Effect_Armor;
    [SerializeField] private GameObject MZ_Impact_Effect_Ground;
    [SerializeField] private GameObject MZ_Impact_Effect_Armor;

    [Header("Ammo stats")]
    public Loader.AmmoType ammoType;

    [Header("References")]
    public Gunner gunner;


    // ----- PHYSICS SIMULATION -----
    public void Launch(Vector3 direction)
    {
        velocity = direction.normalized * MuzzleVelocity;
        isLaunched = true;
    }
    private void Update()
    {
        if (!isLaunched)
            return;

        float dt = Time.deltaTime;

        float speed = velocity.magnitude;

        Vector3 dragAccel = -(AirDrag / Mass) * speed * velocity;
        Vector3 gravity = new(0f, -9.81f, 0f);

        Vector3 acceleration = gravity + dragAccel;

        velocity += acceleration * dt;
        Vector3 newPos = transform.position + velocity * dt;

        if (Physics.Raycast(transform.position, velocity.normalized, out RaycastHit hit, (newPos - transform.position).magnitude))
        {
            transform.position = hit.point;
            OnImpact(hit);
        }
        else
        {
            transform.position = newPos;
            if (transform.position.y <= 0f)
            {
                isLaunched = false;
            }
        }
    }


    // ----- COLLISION HANDLING -----
    private void OnImpact(RaycastHit hit)
    {
        isLaunched = false;

        // Get impact point
        bool isGround = hit.collider.CompareTag("Ground");

        // Enemy hit
        if (hit.collider.gameObject.layer == 9 && hit.collider.TryGetComponent(out PartOfEnemy poe))
        {
            // For testing purposes it's insta death
            gunner.SetResultOfFiring(poe.enemy.targetType, true, true);
        }

        // Spawn explosion effect based on ammo type
        GameObject effect = null;
        switch (ammoType)
        {
            case Loader.AmmoType.KE:
                if (isGround)
                    effect = KE_Impact_Effect_Ground;
                else
                    effect = KE_Impact_Effect_Armor;
                break;
            case Loader.AmmoType.MZ:
                if (isGround)
                    effect = MZ_Impact_Effect_Ground;
                else
                    effect = MZ_Impact_Effect_Armor;
                break;
            case Loader.AmmoType.HE:

                break;
            default: break;
        }
        Instantiate(effect, hit.point, Quaternion.LookRotation(Vector3.forward));

        // Destroy the projectile after impact
        Destroy(gameObject);
    }
}
