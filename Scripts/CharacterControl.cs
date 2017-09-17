using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

[RequireComponent (typeof(Character))]
[RequireComponent (typeof(ShadowController))]
public class CharacterControl : MonoBehaviour
{

    private KeyCode k_shadowKey = KeyCode.Q;

    private Character m_Character; // A reference to the ThirdPersonCharacter on the object
    private ShadowController m_ShadowController;
    private Transform m_Cam;                  // A reference to the main camera in the scenes transform
    private Vector3 m_CamForward;             // The current forward direction of the camera
    private Vector3 m_Move;
    private bool m_Jump;                      // the world-relative desired move direction, calculated from the camForward and user input.
    private bool m_Shadow = false;
    Animator m_Animator;

    private void Start()
    {
        // get the transform of the main camera
        if (Camera.main != null)
        {
            m_Cam = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning(
                "Warning: no main camera found. Third person character needs a Camera tagged \"MainCamera\", for camera-relative controls.", gameObject);
            // we use self-relative controls in this case, which probably isn't what the user wants, but hey, we warned them!
        }

        // get the third person character ( this should never be null due to require component )
        m_Character = GetComponent<Character>();
        m_ShadowController = GetComponent<ShadowController>();
        m_Animator = GetComponent<Animator>();
    }


    private void Update()
    {
        m_Jump = Input.GetButton("Jump");

        HandleShadowToggle();
    }

    private void HandleShadowToggle()
    {
        if (m_Shadow)
        {
            if (Input.GetKeyDown(k_shadowKey) || !m_ShadowController.IsUnderShadow())
            {
                m_Shadow = false;
                ShadowToggle(0);
            }
        } else
        {
            if (m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded") || m_Animator.GetCurrentAnimatorStateInfo(0).IsName("ToMove"))
            {
                if (Input.GetKeyDown(k_shadowKey) && m_ShadowController.IsUnderShadow())
                {
                    m_Shadow = true;
                }
            }
        }
    }


    // Fixed update is called in sync with physics
    private void FixedUpdate()
    {
        // read inputs
        float h = CrossPlatformInputManager.GetAxis("Horizontal");
        float v = CrossPlatformInputManager.GetAxis("Vertical");
        bool crouch = Input.GetKey(KeyCode.C);

        // calculate move direction to pass to character
        if (m_Cam != null)
        {
            // calculate camera relative direction to move:
            m_CamForward = Vector3.Scale(m_Cam.forward, new Vector3(1, 0, 1)).normalized;
            m_Move = v * m_CamForward + h * m_Cam.right;
        }
        else
        {
            // we use world-relative directions in the case of no main camera
            m_Move = v * Vector3.forward + h * Vector3.right;
        }
#if !MOBILE_INPUT
        // walk speed multiplier
        if (Input.GetKey(KeyCode.LeftShift)) m_Move *= 0.5f;
#endif

        // pass all parameters to the character control script
        m_Character.Move(m_Move, crouch, m_Jump, m_Shadow);
        //m_Jump = false;
    }

    public void ShadowToggle(int toShadow)
    {
        GetComponentsInChildren<SkinnedMeshRenderer>()[0].enabled = toShadow != 1;
        GetComponentsInChildren<MeshRenderer>()[0].enabled = toShadow == 1;
    }

}
