using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class MoveController : MonoBehaviour
{
    public LayerMask collisionMask;
    public const float skinWidth = 0.015f;
    public int horizontalRayCount = 4;
    public int verticalRayCount = 4;
    public CollisionInfo collisions;
    public float maxClimbAngle = 80f;

    new private BoxCollider collider;
    private RaycastOrigins raycastOrigins;
    private float horizontalRaySpacing;
    private float verticalRaySpacing;

    void Start() {
        collider = GetComponent<BoxCollider>();
        CalculateRaySpacing();
    }

    public void Move(Vector3 velocity) {
        UpdateRaycastOrigins();
        collisions.Reset();

        HorizontalCollisions(ref velocity);
        VerticalCollisions(ref velocity);
        // if (velocity.x != 0) {
        // }
        // if (velocity.y != 0) {
        // }
        transform.Translate(velocity);
    }

    private void HorizontalCollisions(ref Vector3 velocity) {
        // https://www.youtube.com/watch?v=cwcC2tIKObU&t=331s
        float directionX = Mathf.Sign(velocity.x);           // -1 for left, 1 for right
        float rayLength = Mathf.Abs(velocity.x) + skinWidth;

        for (int i = 0; i < horizontalRayCount; i++) {
            for (int j = 0; j < horizontalRayCount; j++) {
                Vector3 rayOrigin = (directionX == -1) ? raycastOrigins.bottomFrontLeft : raycastOrigins.bottomFrontRight;
                rayOrigin += Vector3.up * (horizontalRaySpacing * i);
                rayOrigin += Vector3.back * (horizontalRaySpacing * j);

                RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.right * directionX, rayLength, collisionMask);
                if (hits.Length == 0) {
                    Debug.DrawRay(rayOrigin, Vector3.right * directionX * rayLength * 10, Color.magenta);
                    continue;
                }

                RaycastHit hit = hits[0];
                for (int h = 1; h < hits.Length; h++) {
                    if (hits[h].distance < hit.distance) {
                        hit = hits[h];
                    }
                }

                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (i == 0 && j == 0) {
                    Debug.LogFormat("Slope angle: {0}", slopeAngle);
                }

                velocity.x = (hit.distance - skinWidth) * directionX;
                // Debug.LogFormat("with RayLength {0} MinHitDist {1} setting velocity.y to {2}", rayLength, hit.distance, velocity.y);
                rayLength = hit.distance;
                Debug.DrawRay(rayOrigin, Vector3.right * directionX * rayLength, Color.red);

                if (directionX == -1) {
                    collisions.left = true;
                } else {
                    collisions.right = true;
                }
            }
        }
    }

    private void VerticalCollisions(ref Vector3 velocity) {
        float directionY = Mathf.Sign(velocity.y);           // -1 for down, 1 for up
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++) {
            for (int j = 0; j < verticalRayCount; j++) {
                Vector3 rayOrigin = (directionY == -1) ? raycastOrigins.bottomFrontLeft : raycastOrigins.topFrontLeft;
                rayOrigin += Vector3.right * (verticalRaySpacing * i + velocity.x);
                rayOrigin += Vector3.forward * (verticalRaySpacing * j + velocity.x);
                RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.up * directionY, rayLength, collisionMask);
                if (hits.Length == 0) {
                    Debug.DrawRay(rayOrigin, Vector3.up * directionY * rayLength * 10, Color.green);
                    continue;
                }
                RaycastHit hit = hits[0];
                for (int h = 1; h < hits.Length; h++) {
                    if (hits[h].distance < hit.distance) {
                        hit = hits[h];
                    }
                }

                velocity.y = (hit.distance - skinWidth) * directionY;
                // Debug.LogFormat("with RayLength {0} MinHitDist {1} setting velocity.y to {2}", rayLength, hit.distance, velocity.y);
                rayLength = hit.distance;
                Debug.DrawRay(rayOrigin, Vector3.up * directionY * rayLength, Color.red);

                if (directionY == -1) {
                    collisions.below = true;
                } else {
                    collisions.above = true;
                }
            }
        }
    }

    void UpdateRaycastOrigins() {
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2);

        raycastOrigins.bottomFrontLeft = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);  // 0 0 0
        raycastOrigins.bottomBackLeft = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);   // 0 0 1
        raycastOrigins.topFrontLeft = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);     // 0 1 0
        raycastOrigins.topBackLeft = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);      // 0 1 1

        raycastOrigins.bottomFrontRight = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z); // 1 0 0
        raycastOrigins.bottomBackRight = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);  // 1 0 1
        raycastOrigins.topFrontRight = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);    // 1 1 0
        raycastOrigins.topBackRight = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);     // 1 1 1
    }

    void CalculateRaySpacing() {
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2);

        if (horizontalRayCount < 2) { horizontalRayCount = 2; }
        if (verticalRayCount < 2) { verticalRayCount = 2; }

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }

    public bool Grounded() { return collisions.below; }

    struct RaycastOrigins {
        public Vector3 topBackLeft;
        public Vector3 topBackRight;
        public Vector3 topFrontLeft;
        public Vector3 topFrontRight;
        public Vector3 bottomBackLeft;
        public Vector3 bottomBackRight;
        public Vector3 bottomFrontLeft;
        public Vector3 bottomFrontRight;
    }

    public struct CollisionInfo {
        public bool above;
        public bool below;
        public bool left;
        public bool right;

        public void Reset() { above = below = left = right = false; }
    }
}
