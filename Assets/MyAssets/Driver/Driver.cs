using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Driver : MonoBehaviour
{
    public float gear = 0;
    public bool stop = true;
    public bool reverse = false;
    public float turning = 0; // -2  -  +2 as turning (- left, + right)
    public float maxSpeed;
    public float maxTorque;

    [SerializeField] private TankMovement tankMovement;
    [SerializeField] private float turningMultiplier_onStop = 0.25f;
    [SerializeField] private float turningMultiplier_onMove = 1.5f;
    [SerializeField] private float gearShiftDelay = 0.5f;


    private void Update()
    {
        // Move();
        // Turn();
    }

    private void Move()
    {
        if(stop)
        {
            // Brake
            tankMovement.targetSpeed = 0;
            return;
        }

        float speed = gear / 4 * maxSpeed;
        float torque = maxTorque / (gear / 4);
        if (reverse)
            speed = -speed;
        // Apply speed and torque to the vehicle's movement system
        tankMovement.torque = torque;
        tankMovement.targetSpeed = speed;
    }
    private void Turn()
    {
        if(turning == 0)
        {
            tankMovement.targetTurning = 0;
            return;
        }
        if (gear == 0)
            tankMovement.targetTurning = turning * turningMultiplier_onStop;
        else
            tankMovement.targetTurning = turning * turningMultiplier_onMove * gear;
    }


    private void GoForward(int gear)
    {
        this.gear = gear;
        reverse = false;
        stop = false;
    }
    private void GoBackward(int gear)
    {
        this.gear = gear;
        reverse = true;
        stop = false;
    }

    public void Go1() => GoForward(1);
    public void Go2() => GoForward(2);
    public void Go3() => GoForward(3);
    public void Go4() => GoForward(4);
    public void Reverse1() => GoBackward(1);
    public void Reverse2() => GoBackward(2);
    public void Stop()
    {
        gear = 0;
        stop = true;
    }
    public void TurnLeft1() => turning = -1;
    public void TurnLeft2() => turning = -2;
    public void TurnRight1() => turning = 1;
    public void TurnRight2() => turning = 2;
    public void Straight() => turning = 0;
}
