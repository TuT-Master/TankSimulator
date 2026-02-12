using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BallisticTable", menuName = "Scriptable objects/Ballistic Table")]
public class BallisticTable : ScriptableObject
{
    public List<RangeAnglePair> ballisticTable_Deg = new();

    [Header("Projectile Settings")]
    [SerializeField] private Projectile projectile;
    private float muzzleVelocity;
    private float mass;
    private float dragK;

    [Header("Table Settings")]
    public float minRange;
    public float maxRange;
    [SerializeField] private float rangeStep;
    [SerializeField] private float maxTime;
    [SerializeField] private float timeStep;



    [ContextMenu("Generate Ballistic Table")]
    private void CreateTable()
    {
        if (projectile == null) return;

        muzzleVelocity = projectile.MuzzleVelocity;
        mass = projectile.Mass;
        dragK = projectile.AirDrag;

        ballisticTable_Deg.Clear();

        for (float range = minRange; range <= maxRange; range += rangeStep)
        {
            float angle = SolveAngleForRange(range);
            ballisticTable_Deg.Add(new RangeAnglePair
            {
                range = range,
                angle = angle
            });
            Debug.Log($"Range: {range} m -> Angle: {angle} deg");
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }
    private float SolveAngleForRange(float targetRange)
    {
        float minAngle = 0f;
        float maxAngle = 20f;
        float tolerance = 0.25f; // meters

        for (int i = 0; i < 30; i++)
        {
            float mid = (minAngle + maxAngle) * 0.5f;

            float range = SimulateImpactDistance(mid);

            float error = range - targetRange;

            if (Mathf.Abs(error) < tolerance)
                return mid;

            if (error < 0f)
                minAngle = mid;
            else
                maxAngle = mid;
        }

        return (minAngle + maxAngle) * 0.5f;
    }
    public float SimulateImpactDistance(float angleDeg)
    {
        float angleRad = angleDeg * Mathf.Deg2Rad;
        Vector2 position = Vector2.zero;
        Vector2 velocity = new(muzzleVelocity * Mathf.Cos(angleRad), muzzleVelocity * Mathf.Sin(angleRad));
        float t = 0f;

        while (t < maxTime && position.y >= 0f)
        {
            float speed = velocity.magnitude;

            Vector2 dragAccel = -(dragK / mass) * speed * velocity;
            Vector2 gravity = new(0f, -9.81f);
            Vector2 acceleration = gravity + dragAccel;

            velocity += acceleration * timeStep;
            position += velocity * timeStep;

            t += timeStep;
        }

        return position.x;
    }
}

[System.Serializable]
public struct RangeAnglePair
{
    public float range;
    public float angle;
}
