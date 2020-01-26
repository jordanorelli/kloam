using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MoveController))]
public class PlayerController : MonoBehaviour {
    public MoveController moveController;

    void Start() {
        moveController = GetComponent<MoveController>();
    }

    void Update() {
    }

    void FixedUpdate() {
    }
}
