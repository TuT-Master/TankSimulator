using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum TargetType
    {
        Tank,
        IFV,
        Infantry,
    }
    public TargetType targetType;

    [SerializeField] private Transform hullStart;
    [SerializeField] private Transform hullEnd;
    [SerializeField] private Transform turretStart;
    [SerializeField] private Transform turretEnd;
    public Vector3 HullForward => (hullEnd.position - hullStart.position).normalized;
    public Vector3 TurretForward => (turretEnd.position - turretStart.position).normalized;
}
