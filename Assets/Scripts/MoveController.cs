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
    public float maxClimbAngle = 60f;

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

        if (velocity.x != 0) {
            HorizontalCollisions(ref velocity);
        }
        if (velocity.y != 0) {
            VerticalCollisions(ref velocity);
        }
        transform.Translate(velocity);
    }

    private void HorizontalCollisions(ref Vector3 velocity) {
        float directionX = Mathf.Sign(velocity.x);           // -1 for left, 1 for right
        float rayLength = Mathf.Abs(velocity.x) + skinWidth;

        for (int i = 0; i < horizontalRayCount; i++) {
            Vector3 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector3.up * (horizontalRaySpacing * i);

            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.right * directionX, rayLength, collisionMask);
            if (hits.Length == 0) {
                Debug.DrawRay(rayOrigin + velocity, Vector3.right * directionX * rayLength, Color.green);
                continue;
            }

            RaycastHit hit = hits[0];
            for (int h = 1; h < hits.Length; h++) {
                if (hits[h].distance < hit.distance) {
                    hit = hits[h];
                }
            }

            if (hit.collider.gameObject.CompareTag("Fatal")) {
                Destroy(gameObject);
                return;
            }

            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (i == 0 && slopeAngle <= maxClimbAngle) {
                float distanceToSlopeStart = 0f;
                if (slopeAngle!= collisions.slopeAngleOld) {
                    distanceToSlopeStart = hit.distance - skinWidth;
                    velocity.x -= distanceToSlopeStart * directionX;
                }
                ClimbSlope(ref velocity, slopeAngle);
                velocity.x += distanceToSlopeStart * directionX;
                Debug.DrawRay(rayOrigin + velocity, Vector3.right * directionX * rayLength, Color.magenta);
            }

            if (!collisions.climbingSlope || slopeAngle > maxClimbAngle) {
                velocity.x = (hit.distance - skinWidth) * directionX;
                rayLength = hit.distance;

                if (collisions.climbingSlope) {
                    velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
                }

                Debug.DrawRay(rayOrigin + velocity, Vector3.right * directionX * rayLength, Color.red);

                if (directionX == -1) {
                    collisions.left = true;
                } else {
                    collisions.right = true;
                }
            }
        }
        return;
    }

    private void VerticalCollisions(ref Vector3 velocity) {
        float directionY = Mathf.Sign(velocity.y);           // -1 for down, 1 for up
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++) {
            Vector3 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector3.right * (verticalRaySpacing * i);
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.up * directionY, rayLength, collisionMask);
            if (hits.Length == 0) {
                Debug.DrawRay(rayOrigin + velocity, Vector3.up * directionY * rayLength, Color.green);
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

            if (collisions.climbingSlope) {
                velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
            }
            Debug.DrawRay(rayOrigin + velocity, Vector3.up * directionY * rayLength, Color.red);

            if (directionY == -1) {
                collisions.below = true;
            } else {
                collisions.above = true;
            }
        }
    }

    private void ClimbSlope(ref Vector3 velocity, float slopeAngle) {
        float dist = Mathf.Abs(velocity.x);
        float climbY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * dist;
        if (velocity.y <= climbY) {
            velocity.y = climbY;
            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * dist * Mathf.Sign(velocity.x);
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
    }

    void UpdateRaycastOrigins() {
        Bounds bounds = collider.bounds;
        float depth = (bounds.max.z + bounds.min.z) * 0.5f;
        bounds.Expand(skinWidth * -2);

        raycastOrigins.bottomLeft = new Vector3(bounds.min.x, bounds.min.y, depth);
        raycastOrigins.topLeft = new Vector3(bounds.min.x, bounds.max.y, depth);
        raycastOrigins.bottomRight = new Vector3(bounds.max.x, bounds.min.y, depth);
        raycastOrigins.topRight = new Vector3(bounds.max.x, bounds.max.y, depth);
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
        public Vector3 topLeft;
        public Vector3 topRight;
        public Vector3 bottomLeft;
        public Vector3 bottomRight;
    }

    public struct CollisionInfo {
        public bool above;
        public bool below;
        public bool left;
        public bool right;
        public bool climbingSlope;
        public float slopeAngle;
        public float slopeAngleOld;

        public void Reset() {
            above = false;
            below = false;
            left = false;
            right = false;
            climbingSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0f;
        }
    }
}
