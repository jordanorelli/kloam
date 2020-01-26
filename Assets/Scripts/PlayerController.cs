using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MoveController))]
public class PlayerController : MonoBehaviour {
    public MoveController moveController;

    private float gravity = -20;
    private Vector3 velocity;

    void Start() {
        moveController = GetComponent<MoveController>();
    }

    void Update() {
        velocity.y += gravity * Time.deltaTime;
        moveController.Move(velocity * Time.deltaTime);
    }

    void FixedUpdate() {
    }
}
