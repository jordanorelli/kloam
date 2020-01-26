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
        VerticalCollisions(ref velocity);
        transform.Translate(velocity);
    }

    private void VerticalCollisions(ref Vector3 velocity) {
        float directionY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;

        // bottom face
        for (int i = 0; i < verticalRayCount; i++) {
            for (int j = 0; j < verticalRayCount; j++) {
                Vector3 rayOrigin = (directionY == -1) ? raycastOrigins.bottomFrontLeft : raycastOrigins.topFrontLeft;
                rayOrigin += Vector3.right * (verticalRaySpacing * i + velocity.x);
                RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.up * directionY, rayLength, collisionMask);

                if (hits.Length > 0) {
                    float minHitDist = 0f;
                    foreach(RaycastHit hit in hits) {
                        if (hit.distance > minHitDist) {
                            minHitDist = hit.distance;
                        }
                    }
                    velocity.y = (minHitDist - skinWidth) * directionY;
                    rayLength = minHitDist;
                    Debug.DrawRay(raycastOrigins.bottomFrontLeft + Vector3.right * verticalRaySpacing * i - Vector3.back * verticalRaySpacing * j, Vector3.down * 0.25f, Color.red);
                } else {
                    Debug.DrawRay(raycastOrigins.bottomFrontLeft + Vector3.right * verticalRaySpacing * i - Vector3.back * verticalRaySpacing * j, Vector3.down * 0.25f, Color.green);
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
}
