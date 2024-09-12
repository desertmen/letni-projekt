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
    [SerializeField] private float _JumpUpGravity;
    [SerializeField] private float _JumpDownGravity;
    [SerializeField] private float _JumpDownCutoff; // what is min velocity.y for jump to be considered going down -> apply _JumpDownGravity
    [SerializeField] private float _JumpAccelMultiplier; // multiply time when jumping -> slower(< 1)/faster(> 1) acceleration
    [SerializeField] private float _JumpEyeFrameTime;
    [SerializeField] private float _JumpBufferTime;

    private PlayerControlls playerControlls;
    private BoxCollider2D boxCollider;
    private Rigidbody2D body;

    private InputAction moveAction;

    private float maxAccelerationT;
    private float maxDeccelerationT;

    private float direction = 0;
    private float movementTime = 0;
    private float movementTimeMult = 1;
    private bool touchingPlatform = false;

    private float jumpEyeTime;
    private float jumpBufferTime;

    void Awake()
    {
        // TODO - grounded and jump

        boxCollider = GetComponent<BoxCollider2D>();
        body = GetComponent<Rigidbody2D>();
        playerControlls = new PlayerControlls();
        maxAccelerationT = _AccelerationCurve.keys.Last().time;
        maxDeccelerationT = _DeccelerationCurve.keys.Last().time;
        jumpEyeTime = _JumpEyeFrameTime + 1;
        jumpBufferTime = _JumpBufferTime + 1;

        if (_MaxVelocity <= 0)
            _MaxVelocity = 1;
    }

    void Update()
    {
        jumpEyeTime += Time.deltaTime;
        jumpBufferTime += Time.deltaTime;
        float newDirection = moveAction.ReadValue<float>();

        updateGravity();
        tryBufferJump();

        if (shouldStop(newDirection))
            stop(newDirection);
        else if(shouldMove(newDirection))
            move(newDirection);

        direction = newDirection;
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag.Equals(MyUtils.Constants.Tags.Platform))
        {
            movementTimeMult = 1;
            touchingPlatform = true;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.tag.Equals(MyUtils.Constants.Tags.Platform))
        {
            movementTimeMult = 1;
            touchingPlatform = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if(collision.gameObject.tag.Equals(MyUtils.Constants.Tags.Platform))
        {
            Vector2 extents = boxCollider.bounds.extents;
            Vector2 pos = transform.position;
            Vector2 BLcorner = pos - extents;
            Vector2 BRcorner = pos + new Vector2(extents.x, -extents.y);

            Vector2 start = body.velocity.x > 0 ? BLcorner : BRcorner;
            Vector2 dir = body.velocity.x > 0 ? Vector2.left : Vector2.right;
            LayerMask mask = 1 << MyUtils.Constants.Layers.Platform;
            float maxDist = _JumpEyeFrameTime * _MaxVelocity;
            RaycastHit2D hit = Physics2D.Raycast(start, dir, maxDist, mask);
            if(hit.distance > 0)
                jumpEyeTime = 0;
   
            touchingPlatform = false;
        }
    }

    public void OnJump()
    {
        jumpBufferTime = 0;
        jump();
    }

    private void jump()
    {
        if (isGrounded())
        {
            body.velocity = new Vector2(body.velocity.x, _JumpVelocity);
            movementTimeMult = _JumpAccelMultiplier;
            jumpBufferTime = _JumpBufferTime + 1;
        }
    }

    private void tryBufferJump()
    {
        if (jumpBufferTime < _JumpBufferTime)
        {
            jump();
        }
    }

    private bool isGrounded()
    {
        if (jumpEyeTime < _JumpEyeFrameTime)
        {
            return true;
        }

        Vector2 extents = boxCollider.bounds.extents;
        Vector2 pos = transform.position;
        Vector2 BLcorner = pos - extents;
        Vector2 BRcorner = pos + new Vector2(extents.x, -extents.y);

        LayerMask mask = 1 << MyUtils.Constants.Layers.Platform;
        RaycastHit2D hitLeft = Physics2D.Raycast(BLcorner, Vector2.down, 0.05f, mask);
        RaycastHit2D hitRight = Physics2D.Raycast(BRcorner, Vector2.down, 0.05f, mask);

        return hitLeft.collider != null || hitRight.collider != null;
    }

    private bool shouldStop(float direction)
    {
        return direction == 0f && body.velocity.x != 0;
    }

    private bool shouldMove(float direction)
    {
        return direction != 0f;
    }

    private void updateGravity()
    {
        if (body.velocity.y < _JumpDownCutoff)
            body.gravityScale = _JumpDownGravity / -Physics2D.gravity.y;
        else
            body.gravityScale = _JumpUpGravity / -Physics2D.gravity.y;
    }

    private void stop(float newDirection)
    {
        // find where on decceleration curve player is and set it as time
        if(newDirection != direction)
        {
            float speedPerc = Mathf.Abs(body.velocity.x) / _MaxVelocity;
            movementTime = -maxDeccelerationT + getTimeOnDescendingCurve(speedPerc, _DeccelerationCurve);
        }
        movementTime += Time.deltaTime * movementTimeMult;

        float Xvelocity;
        if(movementTime < 0)
        {
            Xvelocity = _DeccelerationCurve.Evaluate(maxDeccelerationT + movementTime) * Mathf.Sign(body.velocity.x) * _MaxVelocity;
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

        if (touchingPlatform && !isGrounded())
        {
            return;
        }

        // changed to accelerating
        if (direction != newDirection && Mathf.Sign(newDirection) == Mathf.Sign(body.velocity.x))
        {
            movementTime = getTimeOnAscendingCurve(speedPerc, _AccelerationCurve);
        }
        // changed to deccelerating
        else if (direction != newDirection)
        {
            movementTime = -maxDeccelerationT + getTimeOnDescendingCurve(speedPerc, _DeccelerationCurve);
        }
        movementTime += Time.deltaTime * movementTimeMult;

        float xSpeed;
        if(movementTime < 0)
        {
            xSpeed = _DeccelerationCurve.Evaluate(maxDeccelerationT + movementTime) * -Mathf.Sign(newDirection) * _MaxVelocity;
        }
        else if(movementTime < maxAccelerationT)
        {
            xSpeed = _AccelerationCurve.Evaluate(movementTime) * Mathf.Sign(newDirection) * _MaxVelocity;
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
