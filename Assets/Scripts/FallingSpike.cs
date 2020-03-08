using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingSpike : MonoBehaviour {
    public float fallAcceleration = 25f;
    public float maxFallSpeed = 12.5f;
    public Collider stopper;
    public int numRays = 2;
    public LayerMask collisionMask;

    public float fallSpeed = 0f;
    public bool isFalling = false;
    public bool doneFalling = false;

    private RaycastOrigins raycastOrigins;
    private Vector3 startPosition;

    // Start is called before the first frame update
    void Start() { 
        startPosition = transform.position;
    }

    // Update is called once per frame
    void Update() {
        if (isFalling && !doneFalling) {
            SetRayOrigins();

            fallSpeed += fallAcceleration * Time.deltaTime;
            if (fallSpeed > maxFallSpeed) {
                fallSpeed = maxFallSpeed;
            }

            float dx = fallSpeed * Time.deltaTime;

            if (Vector3.Distance(transform.position, startPosition) > 0.5f) {
                RaycastHit hit;
                if (Physics.Raycast(raycastOrigins.left, Vector3.down, out hit, dx, collisionMask) ||
                    Physics.Raycast(raycastOrigins.right, Vector3.down, out hit, dx, collisionMask)) {

                    // Debug.LogFormat("the spike hit something: {0}", hit);
                    dx = hit.distance;
                    doneFalling = true;
                    isFalling = false;
                }
            }

            transform.Translate(dx * Vector3.down);
        }
    }

    void OnTriggerEnter(Collider other) {
        // Debug.LogFormat("Falling spike collided with other: {0}", other);
        PlayerController player = other.GetComponent<PlayerController>();
        if (player) {
            StartFalling();
        }
    }

    void StartFalling() {
        if (isFalling) {
            return;
        }
        isFalling = true;
    }

    void SetRayOrigins() {
        Bounds bounds = stopper.bounds;

        raycastOrigins.left = new Vector3(bounds.min.x, bounds.min.y, 0);
        raycastOrigins.right = new Vector3(bounds.max.x, bounds.min.y, 0);
    }

    struct RaycastOrigins {
        public Vector3 left;
        public Vector3 right;
    }
}
