using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Driver : MonoBehaviour
{
    public int turning = 0;
    private void GoForward(int gear)
    {

    }
    private void GoBackward(int gear)
    {

    }

    public void Go1() => GoForward(1);
    public void Go2() => GoForward(2);
    public void Go3() => GoForward(3);
    public void Go4() => GoForward(4);
    public void Reverse1() => GoBackward(1);
    public void Reverse2() => GoBackward(2);
    public void Stop()
    {

    }
    public void TurnLeft1() => turning = -1;
    public void TurnLeft2() => turning = -2;
    public void TurnRight1() => turning = 1;
    public void TurnRight2() => turning = 2;
    public void Straight() => turning = 0;
}
