using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootingSpike : MonoBehaviour {
    public float range = 20f;
    public LayerMask collisionMask;
    public float acceleration = 25f;
    public float maxSpeed = 12.5f;

    private Collider hitCollider;
    private Vector3 triggerRayOrigin;
    private Vector3 triggerRayDirection;
    private float speed;
    private Vector3 moveDirection;
    private bool isMoving;

    // Start is called before the first frame update
    void Start() { 
        isMoving = false;
        hitCollider = GetComponent<MeshCollider>();
    }

    // Update is called once per frame
    void Update() {
        if (isMoving) {
            speed += acceleration * Time.deltaTime;
            if (speed > maxSpeed) {
                speed = maxSpeed;
            }

            Vector3 hitRayOrigin = transform.position + transform.up * transform.localScale.y;
            Vector3 hitRayDirection = transform.TransformDirection(0, 1, 0);
            RaycastHit hit;
            if (Physics.Raycast(hitRayOrigin, hitRayDirection, out hit, speed * Time.deltaTime, collisionMask)) {
                if (hit.collider.gameObject.layer == 11) {
                    Destroy(gameObject);
                    return;
                }
            } else {
                Debug.DrawRay(hitRayOrigin, hitRayDirection * speed * Time.deltaTime, Color.green);
            }

            transform.position += moveDirection * speed * Time.deltaTime;
        }

        if (!isMoving) {
            triggerRayOrigin = transform.position + transform.up * transform.localScale.y;
            triggerRayDirection = transform.TransformDirection(0, 1, 0);

            RaycastHit hit;
            if (Physics.Raycast(triggerRayOrigin, triggerRayDirection, out hit, range, collisionMask)) {
                if (hit.collider.gameObject.layer == 11) {
                    Debug.DrawRay(triggerRayOrigin, triggerRayDirection * hit.distance, Color.green);
                }
                if (hit.collider.gameObject.layer == 10) {
                    Debug.DrawRay(triggerRayOrigin, triggerRayDirection * hit.distance, Color.red);
                    isMoving = true;
                    moveDirection = triggerRayDirection;
                }
            } else {
                Debug.DrawRay(triggerRayOrigin, triggerRayDirection * range, Color.green);
            }
        }
    }

    void OnTriggerEnter(Collider other) {
        Debug.LogFormat("Falling spike collided with other: {0}", other);
    }
}
