using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {
    public float moveSpeed;
    public float jumpForce;
    public CharacterController cc;
    public float gravityScale;

    private Vector3 moveDirection;

    void Start() {
        cc = GetComponent<CharacterController>();
    }

    void Update() {
        float dx = Input.GetAxis("Horizontal") * moveSpeed;
        moveDirection = new Vector3(dx, moveDirection.y, 0f);
        if (cc.isGrounded) {
            if (Input.GetButtonDown("Jump")) {
                moveDirection.y = jumpForce;
            } else {
                moveDirection.y = moveDirection.y + Physics.gravity.y*gravityScale*Time.deltaTime;
                if (moveDirection.y < 0f) {
                    moveDirection.y = 0f;
                }
            }
        } else {
            moveDirection.y = moveDirection.y + Physics.gravity.y*gravityScale*Time.deltaTime;
        }
        Debug.Log(moveDirection);
        cc.Move(moveDirection * Time.deltaTime);
    }

    void FixedUpdate() {
        float motor = Input.GetAxis("Vertical");
        float steering = Input.GetAxis("Horizontal");
    }
}
