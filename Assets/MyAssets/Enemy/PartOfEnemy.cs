using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartOfEnemy : MonoBehaviour
{
    public Enemy enemy;
    public enum EnemyPart
    {
        Hull,
        Turret,
        Cannon,
    }
    public EnemyPart enemyPart;
}
