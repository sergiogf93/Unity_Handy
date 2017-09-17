using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Animator))]
public class Character : MonoBehaviour
{
    [SerializeField] float m_velocity = 200f;
    [SerializeField] float m_MovingTurnSpeed = 360;
    [SerializeField] float m_StationaryTurnSpeed = 180;
    [SerializeField] float m_MaxJumpPower = 10f;
    [SerializeField] float m_AnimSpeedMultiplier = 1f;
    [SerializeField] float m_GroundCheckDistance = 0.2f;

    Rigidbody m_Rigidbody;
    Animator m_Animator;
    bool m_IsGrounded;
    bool m_MaxJumped;
    float m_OrigGroundCheckDistance;
    const float k_Half = 0.5f;
    float m_TurnAmount;
    float m_ForwardAmount;
    Vector3 m_GroundNormal;
    Vector3 m_velocityVector = new Vector3(0, 0, 0);
    bool m_Shadow;
    bool m_WallJump;


    void Start()
    {
        m_Animator = GetComponent<Animator>();
        m_Rigidbody = GetComponent<Rigidbody>();

        m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        m_OrigGroundCheckDistance = m_GroundCheckDistance;
    }


    public void Move(Vector3 move, bool crouch, bool jump, bool shadow)
    {
        m_velocityVector = new Vector3(move.x * m_velocity * Time.deltaTime, m_velocityVector.y, move.z * m_velocity * Time.deltaTime);
        m_Shadow = shadow;
        // convert the world relative moveInput vector into a local-relative
        // turn amount and forward amount required to head in the desired
        // direction.
        if (move.magnitude > 1f) move.Normalize();
        move = transform.InverseTransformDirection(move);
        CheckGroundStatus();
        move = Vector3.ProjectOnPlane(move, m_GroundNormal);
        m_TurnAmount = Mathf.Atan2(move.x, move.z);
        m_ForwardAmount = move.z;

        if (!m_WallJump)
        {
            ApplyExtraTurnRotation();
        }

        // control and velocity handling is different when grounded and airborne:
        if (m_IsGrounded)
        {
            HandleGroundedMovement(crouch, jump);
        }
        else
        {
            HandleAirborneMovement(jump);
        }

        // send input and other state parameters to the animator
        UpdateAnimator(move);
        m_Rigidbody.velocity = m_velocityVector;
    }

    void UpdateAnimator(Vector3 move)
    {
        // update the animator parameters
        m_Animator.SetFloat("Forward", m_ForwardAmount, 0.1f, Time.deltaTime);
        m_Animator.SetFloat("Turn", m_TurnAmount, 0.1f, Time.deltaTime);
        m_Animator.SetBool("OnGround", m_IsGrounded);
        m_Animator.SetFloat("Jump", m_Rigidbody.velocity.y);
        m_Animator.SetBool("Shadow", m_Shadow);
        m_Animator.SetBool("WallJump", m_WallJump);

        // the anim speed multiplier allows the overall speed of walking/running to be tweaked in the inspector,
        // which affects the movement speed because of the root motion.
        if (m_IsGrounded && move.magnitude > 0)
        {
            m_Animator.speed = m_AnimSpeedMultiplier;
        }
        else
        {
            // don't use that while airborne
            m_Animator.speed = 1;
        }
    }


    void HandleAirborneMovement(bool jump)
    {
        if (m_MaxJumped)
        {
            Fall();
        }
        else if (jump)
        {
            if (m_Rigidbody.velocity.y > m_MaxJumpPower)
            {
                m_MaxJumped = true;
            }
            else if (m_Rigidbody.velocity.y >= 0)
            {
                m_velocityVector = new Vector3(m_velocityVector.x, m_velocityVector.y + GetJumpForce(m_velocityVector.y), m_velocityVector.z);
            }
        }
        else
        {
            m_MaxJumped = true;
        }

        if (m_WallJump && jump)
        {
            Jump();
        }

        m_GroundCheckDistance = m_Rigidbody.velocity.y < 0 ? m_OrigGroundCheckDistance : 0.1f;
    }


    void HandleGroundedMovement(bool crouch, bool jump)
    {
        // check whether conditions are right to allow a jump:
        if (!m_MaxJumped)
        {
            if (jump && !crouch && m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded"))
            {
                Jump();
            }
        }
        else if (!jump)
        {
            m_MaxJumped = false;
        }
    }

    private void Jump()
    {
        m_velocityVector.y = 0;
        m_velocityVector = new Vector3(m_velocityVector.x, m_velocityVector.y + GetJumpForce(m_velocityVector.y), m_velocityVector.z);
        m_IsGrounded = false;
        m_Animator.applyRootMotion = false;
        m_GroundCheckDistance = m_OrigGroundCheckDistance;
        m_WallJump = false;
        m_MaxJumped = false;
        m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
    }

    float GetJumpForce(float y)
    {
        return Mathf.Lerp(m_MaxJumpPower * 0.7f, 0.2f, y / m_MaxJumpPower);
    }

    void Fall()
    {
        int maxFallingVelocity = m_WallJump ? 2 : 20;
        if (m_velocityVector.y > -maxFallingVelocity)
        {
            m_velocityVector = new Vector3(m_velocityVector.x, m_velocityVector.y - 1, m_velocityVector.z);
        }
        else
        {
            m_velocityVector.y = -maxFallingVelocity;
        }
    }

    void ApplyExtraTurnRotation()
    {
        // help the character turn faster (this is in addition to root rotation in the animation)
        float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount);
        transform.Rotate(0, m_TurnAmount * turnSpeed * Time.deltaTime, 0);
    }

    void CheckGroundStatus()
    {
        RaycastHit hitInfo;

        // helper to visualise the ground check ray in the scene view
        Debug.DrawLine(transform.position + (Vector3.up * 0.1f), transform.position + (Vector3.up * 0.1f) + (Vector3.down * m_GroundCheckDistance));

        // 0.1f is a small offset to start the ray from inside the character
        // it is also good to note that the transform position in the sample assets is at the base of the character
        if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out hitInfo, m_GroundCheckDistance))
        {
            m_GroundNormal = hitInfo.normal;
            m_IsGrounded = true;
            m_WallJump = false;
            m_velocityVector.y = 0;
            m_Animator.applyRootMotion = true;
            m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        }
        else
        {
            m_IsGrounded = false;
            m_GroundNormal = Vector3.up;
            m_Animator.applyRootMotion = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("WallJumpable") && m_velocityVector.y <= 0)
        {
            if (m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Airborne"))
            {
                PrepareWallJump();
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag("WallJumpable") && m_velocityVector.y <= 0)
        {
            if (m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Airborne"))
            {
                PrepareWallJump();
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag("WallJumpable"))
        {
            m_WallJump = false;
            m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    void PrepareWallJump()
    {
        if (!m_WallJump)
        {
            m_WallJump = true;
            m_velocityVector.y = 0;
            m_Rigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }
    }
}
