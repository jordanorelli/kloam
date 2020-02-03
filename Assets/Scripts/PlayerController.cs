using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MoveController))]
public class PlayerController : MonoBehaviour {
    public float jumpHeight = 4f;
    public float jumpDuration = 0.4f;
    public float moveSpeed = 6f;
    public float accelerationTimeAirborne = 0.2f;
    public float accelerationTimeGrounded = 0.05f;
    public Canvas hud;
    public GameObject soulPrefab;
    public Networking networking;

    private Vector3 velocity;
    private float jumpVelocity;
    private MoveController moveController;
    private float gravity;
    private float moveXSmoothing;
    private bool quitting;

    void Start() {
        moveController = GetComponent<MoveController>();

        gravity = -(2*jumpHeight)/Mathf.Pow(jumpDuration, 2f);
        jumpVelocity = Mathf.Abs(gravity) * jumpDuration;

        networking.Connect();
    }

    void Update() {
        networking.CheckForMessages();

        if (moveController.collisions.above || moveController.collisions.below) {
            velocity.y = 0;
        }
        if (moveController.collisions.left || moveController.collisions.right) {
            velocity.x = 0;
        }

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (Input.GetKeyDown(KeyCode.Space) && moveController.collisions.below) {
            velocity.y = jumpVelocity;
        }

        float targetx = input.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetx, ref moveXSmoothing, moveController.Grounded() ? accelerationTimeGrounded : accelerationTimeAirborne);
        velocity.y += gravity * Time.deltaTime;
        moveController.Move(velocity * Time.deltaTime);
    }

    void FixedUpdate() {
    }

    void OnDestroy() {
        Debug.Log("I'm dead");
        // hud.gameObject.SetActive(true);
        // HudController c = hud.gameObject.GetComponent<HudController>();
        // if (c) {
        //     c.SetDead(true);
        // }
        if (!quitting) {
            GameObject soul = Instantiate(soulPrefab, transform.position + Vector3.up * 0.5f, transform.rotation);
            soul.GetComponent<SoulController>().playerName = "fartface";
        }
    }

    void OnCollisionEnter(Collision other) {
        Debug.LogFormat("Player collided with {0}", other);
    }

    void OnTriggerEnter(Collider other) {
        Debug.LogFormat("Player triggered other: {0}", other);
        if (other.CompareTag("Soul")) {
            SoulController soul = other.GetComponent<SoulController>();
            networking.SendCollectSoul(soul.playerName, other.transform.position);
            Destroy(other.gameObject);
        }
        if (other.CompareTag("Fatal")) {
            networking.SendDeath(transform.position);
            Destroy(gameObject);
        }
    }

    void OnApplicationQuit() {
        quitting = true;
    }
}
