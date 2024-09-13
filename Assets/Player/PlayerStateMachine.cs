using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerStateMachine : MonoBehaviour
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
    [SerializeField] private float _DashVelocity;
    [SerializeField] private float _DashDuration;

    private PlayerControlls playerControlls;
    private InputAction moveAction;

    public PlayerStateIdle idleState { get; private set; }
    public PlayerStateMoving movingState { get; private set; }
    public PlayerStateJump jumpState { get; private set; }
    public PlayerStateDash dashState { get; private set; }
    private PlayerState currentState;

    private int forwardDirection = MyUtils.Constants.RIGHT;
    private BoxCollider2D boxCollider;
    private Rigidbody2D body;
    private float jumpEyeTime;

    private void Update()
    {
        updateGravity();
        jumpEyeTime += Time.deltaTime;
        float newDirection = moveAction.ReadValue<float>();
        if(newDirection != 0)
            forwardDirection = newDirection > 0 ? MyUtils.Constants.RIGHT : MyUtils.Constants.LEFT;

        currentState.update(Time.deltaTime, newDirection);
    }


    void Awake()
    {
        playerControlls = new PlayerControlls();
        boxCollider = GetComponent<BoxCollider2D>();    
        body = GetComponent<Rigidbody2D>();
        jumpEyeTime = _JumpEyeFrameTime + 1;

        idleState = new PlayerStateIdle(this);
        movingState = new PlayerStateMoving(this);
        jumpState = new PlayerStateJump(this);
        dashState = new PlayerStateDash(this);
        currentState = idleState;
    }
    public void OnJump()
    {
        changeState(jumpState);
    }

    public void OnDash()
    {
        changeState(dashState);
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
        currentState.onCollisionEnter(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag.Equals(MyUtils.Constants.Tags.Platform))
        {
            if (shouldStartJumpEyeFrames())
                jumpEyeTime = 0;
        }
    }

    public bool isGrounded()
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
        RaycastHit2D hitLeft = Physics2D.Raycast(BLcorner + Vector2.left * 0.01f, Vector2.down, 0.05f, mask);
        RaycastHit2D hitRight = Physics2D.Raycast(BRcorner + Vector2.right * 0.01f, Vector2.down, 0.05f, mask);

        return hitLeft.distance > 0 || hitRight.distance > 0;
    }

    private bool shouldStartJumpEyeFrames()
    {
        Vector2 extents = boxCollider.bounds.extents;
        Vector2 pos = transform.position;
        Vector2 BLcorner = pos - extents;
        Vector2 BRcorner = pos + new Vector2(extents.x, -extents.y);

        Vector2 start = body.velocity.x > 0 ? BLcorner : BRcorner;
        Vector2 dir = body.velocity.x > 0 ? Vector2.left : Vector2.right;
        LayerMask mask = 1 << MyUtils.Constants.Layers.Platform;
        float maxDist = _JumpEyeFrameTime * _MaxVelocity;
        RaycastHit2D hitSide = Physics2D.Raycast(start, dir, maxDist, mask);
        RaycastHit2D hitSideUp = Physics2D.Raycast(start, dir + Vector2.up, maxDist * 1.18f, mask);

        return hitSide.distance > 0 && hitSideUp.distance == 0;
    }

    private void updateGravity()
    {
        if (body.velocity.y < _JumpDownCutoff)
            body.gravityScale = _JumpDownGravity / -Physics2D.gravity.y;
        else
            body.gravityScale = _JumpUpGravity / -Physics2D.gravity.y;
    }
    public void changeState(PlayerState state)
    {
        currentState.exit();
        currentState = state;
        currentState.enter();
    }
    public float getDashVelocity() { return _DashVelocity; }
    public float getDashDuration() { return _DashDuration; }
    public float getForwardDir() { return forwardDirection; }
    public float getJumpBufferTime() { return _JumpBufferTime; }
    public float getJumpVelocity() { return _JumpVelocity; }
    public float getMaxVelocity() { return _MaxVelocity; }
    public float getDecelerationTime(float targetValue)
    {
        return MyUtils.AnimationCurves.getTimeOnDescendingCurve(targetValue, _DeccelerationCurve);
    }

    public float getDecelerationValue(float time)
    {
        return _DeccelerationCurve.Evaluate(time);
    }

    public float getAccelerationTime(float targetValue)
    {
        return MyUtils.AnimationCurves.getTimeOnAscendingCurve(targetValue, _AccelerationCurve);
    }

    public float getAccelerationValue(float time)
    {
        return _AccelerationCurve.Evaluate(time);
    }

    public float getMaxDeccelerationT() { return MyUtils.AnimationCurves.getCurveDuration(_DeccelerationCurve); }
    public float getMaxAccelerationT() { return MyUtils.AnimationCurves.getCurveDuration(_AccelerationCurve); }
    public float getMovementTimeMult() { return 1; }
}
