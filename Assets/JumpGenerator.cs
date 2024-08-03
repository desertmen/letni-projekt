using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class JumpGenerator
{
    float gravity;

    public enum Mode
    {
        DIRECTED_JUMP = 0,
        CONST_X_VARIABLE_Y = 1,
        CONST_Y_VARIABLE_X = 2
    }
    private Mode currentMode;

    private Vector2 jumpVelocityDirection = Vector2.one;
    private float maxVelocity = 1;

    public JumpGenerator(float gravity)
    {
        this.gravity = gravity;
    }

    public void setModeDirectedJump(Vector2 jumpVelocityDirection)
    {
        this.jumpVelocityDirection = jumpVelocityDirection;
        this.currentMode = Mode.DIRECTED_JUMP;
    }

    public void setModeConstXvariableYJump(float xVelocity)
    {
        this.maxVelocity = xVelocity;
        this.currentMode = Mode.CONST_X_VARIABLE_Y;
    }

    public void setModeConstYvariableXJump(float yVelocity)
    {
        this.maxVelocity = yVelocity;
        this.currentMode = Mode.CONST_Y_VARIABLE_X;
    }

    public Vector2 getVelocityByMode(Vector2 jumpStart, Vector2 target)
    {
        switch(this.currentMode)
        {
            case Mode.DIRECTED_JUMP:
                return getVelocityToTargetDirected(jumpStart, target, this.jumpVelocityDirection);
            case Mode.CONST_X_VARIABLE_Y:
                return getVelocityToTargetVariableY(jumpStart, target, maxVelocity);
            case Mode.CONST_Y_VARIABLE_X:
                return getVelocityToTargetVariableX(jumpStart, target, maxVelocity);
            default:
                Debug.LogError("Jumpgenerator - UNKNOWN MODE ?!?!? WTF");
                return Vector2.negativeInfinity;
        }
    }

    public List<JumpTrajectory> getJumpsDirected(Vector2 jumpStart, List<Vector2> targets, Vector2 jumpDirection, Vector2 maxVelocity, out List<Vector2> reachableTargets)
    {
        List<JumpTrajectory> jumps = new List<JumpTrajectory>();
        reachableTargets = new List<Vector2>();
        foreach(Vector2 target in targets) 
        {
            JumpTrajectory jump = getJumpDirected(jumpStart, target, jumpDirection);
            if(jump != null && isVectorAbsSmaller(jump.velocity, maxVelocity))
            {
                reachableTargets.Add(target);
                jumps.Add(jump);
            }
        }
        return jumps;
    }

    public List<JumpTrajectory> getJumpsVariableXvelocity(Vector2 jumpStart, List<Vector2> targets, float yVelocity, Vector2 maxVelocity, out List<Vector2> reachableTargets)
    {
        List<JumpTrajectory> jumps = new List<JumpTrajectory>();
        reachableTargets = new List<Vector2>();
        foreach (Vector2 target in targets)
        {
            JumpTrajectory jump = getJumpVariableXvelocity(jumpStart, target, yVelocity);
            if (jump != null && isVectorAbsSmaller(jump.velocity, maxVelocity))
            {
                reachableTargets.Add(target);
                jumps.Add(jump);
            }
        }
        return jumps;
    }

    public List<JumpTrajectory> getJumpsVariableYvelocity(Vector2 jumpStart, List<Vector2> targets, float xVelocity, Vector2 maxVelocity, out List<Vector2> reachableTargets)
    {
        List<JumpTrajectory> jumps = new List<JumpTrajectory>();
        reachableTargets = new List<Vector2>();
        foreach (Vector2 target in targets)
        {
            JumpTrajectory jump = getJumpVariableYvelocity(jumpStart, target, xVelocity);
            if (jump != null && isVectorAbsSmaller(jump.velocity, maxVelocity))
            {
                reachableTargets.Add(target);
                jumps.Add(jump);
            }
        }
        return jumps;
    }

    public List<JumpTrajectory> getJumpsWithTangent(Vector2 jumpStart, List<Vector2> targets, Vector2 tangent, Vector2 maxVelocity, out List<Vector2> reachableTargets)
    {
        List<JumpTrajectory> jumps = new List<JumpTrajectory>();
        reachableTargets = new List<Vector2>();
        foreach (Vector2 target in targets)
        {
            JumpTrajectory jump = getJumpWithTangent(jumpStart, target, tangent);
            if (jump != null && isVectorAbsSmaller(jump.velocity, maxVelocity))
            {
                reachableTargets.Add(target);
                jumps.Add(jump);
            }
        }
        return jumps;
    }

    private bool isVectorAbsSmaller(Vector2 a, Vector2 b)
    {
        return Mathf.Abs(a.x) < Mathf.Abs(b.x) && Mathf.Abs(a.y) < Mathf.Abs(b.y);
    }

    public JumpTrajectory getJumpDirected(Vector2 jumpStart, Vector2 target, Vector2 jumpDirection)
    {
        Vector2 diff = target - jumpStart;
        float v_y = jumpDirection.y;
        float v_x = jumpDirection.x;
        float x = diff.x;
        float y = diff.y;
        float sqrt = -gravity / (2f * (v_y * (x / v_x) - y));
        if (sqrt < 0)
            return null;
        float perc = (x / v_x) * Mathf.Sqrt(sqrt);
        Vector2 velocity =  new Vector2(perc, perc) * jumpDirection;
        return new JumpTrajectory(jumpStart, velocity, gravity);
    }

    public Vector2 getVelocityToTargetDirected(Vector2 jumpStart, Vector2 target, Vector2 jumpDirection)
    {
        Vector2 diff = target - jumpStart;
        float v_y = jumpDirection.y;
        float v_x = jumpDirection.x;
        float x = diff.x;
        float y = diff.y;
        float sqrt = -gravity / (2f * (v_y * (x / v_x) - y));
        if (sqrt < 0)
            return Vector2.negativeInfinity;
        float perc = (x / v_x) * Mathf.Sqrt(sqrt);
        Vector2 velocity = new Vector2(perc, perc) * jumpDirection;
        return velocity;
    }

    public JumpTrajectory getJumpVariableXvelocity(Vector2 jumpStart, Vector2 target, float yVelocity)
    {
        Vector2 diff = target - jumpStart;
        float D = yVelocity * yVelocity - 2 * -gravity * diff.y;
        if (D < 0)
            return null;
        D = Mathf.Sqrt(D);
        float a1 = (-yVelocity + D) / gravity;
        float a2 = (-yVelocity - D) / gravity;
        float a = Mathf.Abs(a1) > Mathf.Abs(a2) ? a1 : a2;
        float xVelocity = diff.x / a;
        return new JumpTrajectory(jumpStart, new Vector2(xVelocity, yVelocity), gravity);
    }

    public Vector2 getVelocityToTargetVariableX(Vector2 jumpStart, Vector2 target, float yVelocity)
    {
        Vector2 diff = target - jumpStart;
        float D = yVelocity * yVelocity - 2 * -gravity * diff.y;
        if (D < 0)
            return Vector2.negativeInfinity;
        D = Mathf.Sqrt(D);
        float a1 = (-yVelocity + D) / gravity;
        float a2 = (-yVelocity - D) / gravity;
        float a = Mathf.Abs(a1) > Mathf.Abs(a2) ? a1 : a2;
        float xVelocity = diff.x / a;
        return new Vector2(xVelocity, yVelocity);
    }

    public JumpTrajectory getJumpVariableYvelocity(Vector2 jumpStart, Vector2 target, float xVelocity)
    {
        Vector2 diff = target - jumpStart;
        float t = diff.x / xVelocity;
        float yVelocity = (diff.y / t - 0.5f * gravity * t);
        return new JumpTrajectory(jumpStart, new Vector2(xVelocity, yVelocity), gravity);
    }

    public Vector2 getVelocityToTargetVariableY(Vector2 jumpStart, Vector2 target, float xVelocity)
    {
        Vector2 diff = target - jumpStart;
        float t = diff.x / xVelocity;
        float yVelocity = (diff.y / t - 0.5f * gravity * t);
        return new Vector2(xVelocity, yVelocity);
    }
    
    public JumpTrajectory getJumpWithTangent(Vector2 jumpStart, Vector2 target, Vector2 tangent)
    {
        Vector2 diff = target - jumpStart;
        if (tangent.x == 0)
        {
            return null;
        }
        float d = tangent.y / tangent.x;

        float toSqrt = (2 * (diff.y - diff.x * d));
        if (toSqrt == 0)
            return null;
        toSqrt = -gravity / toSqrt;
        if (toSqrt < 0)
            return null;
        float velocityX = diff.x * Mathf.Sqrt(toSqrt);
        float velocityY = velocityX * d + -gravity * diff.x / velocityX;

        return new JumpTrajectory(jumpStart, new Vector2(velocityX, velocityY), gravity);
    }

    public Vector2 getVelocityToTargetTangent(Vector2 jumpStart, Vector2 target, Vector2 tangent)
    {
        Vector2 diff = target - jumpStart;
        if (tangent.x == 0)
        {
            return Vector2.negativeInfinity;
        }
        float d = tangent.y / tangent.x;

        float toSqrt = (2 * (diff.y - diff.x * d));
        if (toSqrt == 0)
            return Vector2.negativeInfinity;
        toSqrt = -gravity / toSqrt;
        if (toSqrt < 0)
            return Vector2.negativeInfinity;
        float velocityX = diff.x * Mathf.Sqrt(toSqrt);
        float velocityY = velocityX * d + -gravity * diff.x / velocityX;

        return new Vector2(velocityX, velocityY);
    }
}
