using UnityEngine;
using Unity.Netcode;
#if NEW_INPUT_SYSTEM_INSTALLED
using UnityEngine.InputSystem;
#endif


public class PlayerMovementRB : NetworkBehaviour
{
    public float moveSpeed = 500.0f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate() // 物理演算はFixedUpdateで行うのが推奨
    {
        if (!IsOwner || !IsSpawned) return;
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontalInput, 0f, verticalInput);
        // Rigidbodyを使って移動
        rb.linearVelocity = new Vector3(movement.x * moveSpeed * Time.deltaTime, rb.linearVelocity.y, movement.z * moveSpeed * Time.deltaTime);
    }
}