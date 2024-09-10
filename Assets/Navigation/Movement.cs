using System;
using UnityEngine;

public abstract class Movement : MonoBehaviour
{
    public enum State
    {
        MOVING_RIGHT,
        MOVING_LEFT,
        JUMPING,
        STOPPING,
        SLIDING,
        IDLE
    }

    public abstract void startGoingRight();
    public abstract void startGoingLeft();
    public abstract void jump(Vector2 jumpVelocity);
    public abstract void stopMoving();
    public abstract void setActionOnLanding(Action<Polygon> action);
    public abstract bool isGrounded();
}
