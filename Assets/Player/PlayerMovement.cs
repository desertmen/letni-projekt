using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private AnimationCurve _AccelerationCurve;
    [SerializeField] private AnimationCurve _DeccelerationCurve;
    [SerializeField] private float _MaxVelocity;
    [SerializeField] private float _JumpVelocity;

    private PlayerControlls playerControlls;
    private BoxCollider2D boxCollider;
    private Rigidbody2D body;

    private InputAction moveAction;

    private float maxAccelerationT;
    private float maxDeccelerationT;

    private float direction = 0;
    private float time = 0;

    void Awake()
    {
        // TODO - grounded and jump

        boxCollider = GetComponent<BoxCollider2D>();
        body = GetComponent<Rigidbody2D>();
        playerControlls = new PlayerControlls();
        maxAccelerationT = _AccelerationCurve.keys.Last().time;
        maxDeccelerationT = _DeccelerationCurve.keys.Last().time;

        if (_MaxVelocity <= 0)
            _MaxVelocity = 1;
    }

    void Update()
    {
        float newDirection = moveAction.ReadValue<float>();

        if (shouldStop(newDirection))
            stop(newDirection);
        else if(shouldMove(newDirection))
            move(newDirection);

        direction = newDirection;
    }

    public void OnJump()
    {
        body.velocity = new Vector2(body.velocity.x, _JumpVelocity); 
    }

    private void OnEnable()
    {
        moveAction = playerControlls.GamePlay.Move;
        moveAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
    }

    private bool shouldStop(float direction)
    {
        return direction == 0f && body.velocity.x != 0;
    }

    private bool shouldMove(float direction)
    {
        return direction != 0f;
    }

    private void stop(float newDirection)
    {
        // find where on decceleration curve player is and set it as time
        if(newDirection != direction)
        {
            float speedPerc = Mathf.Abs(body.velocity.x) / _MaxVelocity;
            time = -maxDeccelerationT + getTimeOnDescendingCurve(speedPerc, _DeccelerationCurve);
        }
        // continue with stopping
        else
        {
            time += Time.deltaTime;
        }

        float Xvelocity;
        if(time < 0)
        {
            Xvelocity = _DeccelerationCurve.Evaluate(maxDeccelerationT + time) * Mathf.Sign(body.velocity.x) * _MaxVelocity;
        }
        else
        {
            Xvelocity = 0;
        }
        body.velocity = new Vector2(Xvelocity, body.velocity.y);
    }

    private void move(float newDirection)
    {
        Vector2 velocity = body.velocity;
        float speedPerc = Mathf.Abs(velocity.x) / _MaxVelocity;

        // changed to accelerating
        if (direction != newDirection && Mathf.Sign(newDirection) == Mathf.Sign(body.velocity.x))
        {
            time = getTimeOnAscendingCurve(speedPerc, _AccelerationCurve);
        }
        // changed to deccelerating
        else if (direction != newDirection)
        {
            time = -maxDeccelerationT + getTimeOnDescendingCurve(speedPerc, _DeccelerationCurve);
        }
        // no change, continue in movement
        else
        {
            time += Time.deltaTime;
        }

        float xSpeed;
        if(time < 0)
        {
            xSpeed = _DeccelerationCurve.Evaluate(maxDeccelerationT + time) * -Mathf.Sign(newDirection) * _MaxVelocity;
        }
        else if(time < maxAccelerationT)
        {
            xSpeed = _AccelerationCurve.Evaluate(time) * Mathf.Sign(newDirection) * _MaxVelocity;
        }
        else
        {
            xSpeed = Mathf.Sign(newDirection) * _MaxVelocity;
        }
        body.velocity = new Vector2(xSpeed, body.velocity.y);
    }

    private float getTimeOnAscendingCurve(float targetValue, AnimationCurve curve)
    {
        float maxTime = curve.keys.Last().time;
        if (curve.Evaluate(0) > targetValue)
            return 0;
        if (curve.Evaluate(maxTime) < targetValue)
            return maxTime;
        float minDist = 0.001f;
        float center = 0;
        float left = 0, right = maxTime;
        while(right - left > minDist)
        {
            center = (right + left) / 2f;
            float value = curve.Evaluate(center);
            // go left
            if (value > targetValue)
            {
                right = center;
            }
            // go right
            else if (value < targetValue)
            {
                left = center;
            }
            else
                return center;
        }
        return center;
    }

    private float getTimeOnDescendingCurve(float targetValue, AnimationCurve curve)
    {
        float maxTime = curve.keys.Last().time;
        if (curve.Evaluate(0) < targetValue)
            return 0;
        if (curve.Evaluate(maxTime) > targetValue)
            return maxTime;
        float minDist = 0.001f;
        float center = 0;
        float left = 0, right = maxTime;
        while (right - left > minDist)
        {
            center = (right + left) / 2f;
            float value = curve.Evaluate(center);
            // go left
            if (value < targetValue)
            {
                right = center;
            }
            // go right
            else if (value > targetValue)
            {
                left = center;
            }
            else
                return center;
        }
        return center;
    }
}
