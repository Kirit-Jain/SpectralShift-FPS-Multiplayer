using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float lookSpeed = 2f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Dash Settings")]
    [SerializeField] private float dashForce = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 2f;

    [Header("Camera Settings")]
    [SerializeField] private float minVerticalCameraAngle = -30f;
    [SerializeField] private float maxVerticalCameraAngle = 60f;

    [Header("Components")]
    [SerializeField] private Animator animator;

    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private AudioListener playerListener;
    [SerializeField] private Transform spineBone;

    private CharacterController characterController;
    private PlayerHealth playerHealth;
    private float rotationX;
    private Vector3 lastPosition;
    private float lastDashTime = -999f;
    private bool isDashing;
    private Vector3 velocity;
    private bool isGrounded;

    // Network variables to sync rotation and animation speed across the network
    private NetworkVariable<float> networkRotationX = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<float> networkMoveSpeed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Disable camera and audio for other players' clones
            if (playerCamera) playerCamera.gameObject.SetActive(false);
            if (playerListener) playerListener.enabled = false;
            return;
        }

        // Setup for the local player
        if (playerCamera) playerCamera.gameObject.SetActive(true);
        if (playerListener) playerListener.enabled = true;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        HideMeshForLocalPlayer();
        StartCoroutine(SyncInitialPosition());
    }

    private void Update()
    {
        // Don't move if match hasn't started or we are dead
        if (GameManager.Instance != null && !GameManager.Instance.matchActive.Value) return;
        if (playerHealth != null && playerHealth.currentHealth.Value <= 0) return;

        // Force animator position to stay with controller
        if (animator != null)
        {
            animator.transform.localPosition = Vector3.zero;
            animator.transform.localRotation = Quaternion.identity;
        }

        if (IsOwner)
        {
            HandleDash();
            if (!isDashing)
            {
                HandleMovement();
                HandleRotation();
            }
            HandleAnimations();
        }
    }

    private void LateUpdate()
    {
        // Spine rotation logic (to make players look up/down in multiplayer)
        if (IsOwner)
        {
            networkRotationX.Value = rotationX;
        }

        if (spineBone != null)
        {
            float x = IsOwner ? rotationX : networkRotationX.Value;
            Vector3 localEuler = spineBone.localEulerAngles;
            spineBone.localRotation = Quaternion.Euler(x, localEuler.y, localEuler.z);
        }
    }

    private void HandleMovement()
    {
        isGrounded = characterController.isGrounded;
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;

        Vector3 move = transform.right * h + transform.forward * v;
        characterController.Move(move * currentSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("Jump");
            TriggerJumpServerRpc();
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

        transform.Rotate(Vector3.up * mouseX);
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, minVerticalCameraAngle, maxVerticalCameraAngle);

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        }
    }

    private void HandleDash()
    {
        if (Input.GetKeyDown(KeyCode.Q) && Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(DashCoroutine());
        }
    }

    private void HandleAnimations()
    {
        if (animator == null) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float mag = new Vector2(h, v).magnitude;

        if (Mathf.Abs(networkMoveSpeed.Value - mag) > 0.01f)
        {
            networkMoveSpeed.Value = mag;
        }
        animator.SetFloat("Speed", mag);
    }

    private IEnumerator DashCoroutine()
    {
        isDashing = true;
        lastDashTime = Time.time;

        float hRaw = Input.GetAxisRaw("Horizontal");
        float vRaw = Input.GetAxisRaw("Vertical");
        Vector3 dashDir = transform.right * hRaw + transform.forward * vRaw;

        if (dashDir.magnitude < 0.01f) dashDir = transform.forward;
        dashDir.Normalize();

        float startTime = Time.time;
        while (Time.time < startTime + dashDuration)
        {
            characterController.Move(dashDir * dashForce * Time.deltaTime);
            yield return null;
        }
        isDashing = false;
    }

    [ServerRpc]
    private void TriggerJumpServerRpc()
    {
        TriggerJumpClientRpc();
    }

    [ClientRpc]
    private void TriggerJumpClientRpc()
    {
        if (!IsOwner)
        {
            animator.SetTrigger("Jump");
        }
    }

    private void HideMeshForLocalPlayer()
    {
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var smr in renderers)
        {
            // Make sure your player's body mesh is named exactly "Body_Mesh"
            if (smr.gameObject.name == "Body_Mesh")
            {
                smr.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }
    }

    private IEnumerator SyncInitialPosition()
    {
        yield return new WaitForSeconds(0.1f);
        lastPosition = transform.position;
    }
}