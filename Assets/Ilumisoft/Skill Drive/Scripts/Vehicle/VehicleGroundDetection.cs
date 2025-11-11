using UnityEngine;

namespace Ilumisoft.SkillDrive
{
    [System.Serializable]
    public class VehicleGroundDetection
    {
        [Tooltip("Which layers should be handled as ground")]
        public LayerMask GroundLayers = Physics.DefaultRaycastLayers;

        [Tooltip("Primary raycast distance straight down from collider center")]
        public float RaycastDist = 5.0f;

        [Tooltip("Extra grace distance to still count as 'near ground' on slopes / small gaps")]
        public float NearGroundExtraDistance = 0.75f;

        [Tooltip("Sphere cast radius used to detect ground under wide bodies / slopes")]
        public float GroundProbeRadius = 0.6f;

        [Tooltip("Maximum slope angle in degrees accepted as drivable ground")]
        public float MaxSlopeAngle = 55f;

        Vehicle vehicle;
        Collider cachedCollider;

        public bool IsGrounded { get; private set; }
        public bool IsNearGround { get; private set; }
        public Vector3 LastGroundNormal { get; private set; } = Vector3.up;

        public void Initialize(Vehicle vehicle)
        {
            this.vehicle = vehicle;
            cachedCollider = vehicle.GetComponentInChildren<Collider>();
        }

        /// <summary>
        /// Robust ground check: sphere cast + multi-offset raycasts + grace distance.
        /// </summary>
        public void CheckGround()
        {
            if (vehicle == null) { IsGrounded = false; IsNearGround = false; return; }

            Vector3 up = vehicle.transform.up;
            Vector3 origin;
            float baseRayDist = RaycastDist;

            if (cachedCollider != null)
            {
                origin = cachedCollider.bounds.center; // center of collider
            }
            else
            {
                origin = vehicle.transform.position;
            }

            bool grounded = false;
            bool nearGround = false;
            Vector3 groundNormal = Vector3.up;

            // 1. SphereCast (helps on slopes / partial contact)
            if (Physics.SphereCast(origin + up * 0.1f, GroundProbeRadius, -up, out RaycastHit sphereHit, baseRayDist + NearGroundExtraDistance, GroundLayers, QueryTriggerInteraction.Ignore))
            {
                float angle = Vector3.Angle(sphereHit.normal, Vector3.up);
                if (angle <= MaxSlopeAngle)
                {
                    if (sphereHit.distance <= baseRayDist) grounded = true; else nearGround = true;
                    groundNormal = sphereHit.normal;
                }
            }

            // 2. Central Ray (precise direct check)
            if (!grounded)
            {
                if (Physics.Raycast(origin, -up, out RaycastHit centerHit, baseRayDist, GroundLayers, QueryTriggerInteraction.Ignore))
                {
                    float angle = Vector3.Angle(centerHit.normal, Vector3.up);
                    if (angle <= MaxSlopeAngle)
                    {
                        grounded = true; groundNormal = centerHit.normal;
                    }
                }
            }

            // 3. Offset rays (front/back/left/right) if still not grounded
            if (!grounded)
            {
                Vector3[] offsets = new Vector3[4];
                float lateral = cachedCollider != null ? cachedCollider.bounds.extents.x * 0.8f : 0.8f;
                float longitudinal = cachedCollider != null ? cachedCollider.bounds.extents.z * 0.8f : 0.8f;
                offsets[0] = vehicle.transform.forward * longitudinal;
                offsets[1] = -vehicle.transform.forward * longitudinal;
                offsets[2] = vehicle.transform.right * lateral;
                offsets[3] = -vehicle.transform.right * lateral;

                foreach (var off in offsets)
                {
                    Vector3 o = origin + off;
                    if (Physics.Raycast(o, -up, out RaycastHit hit, baseRayDist, GroundLayers, QueryTriggerInteraction.Ignore))
                    {
                        float angle = Vector3.Angle(hit.normal, Vector3.up);
                        if (angle <= MaxSlopeAngle)
                        {
                            grounded = true; groundNormal = hit.normal; break;
                        }
                    }
                    else if (!nearGround && Physics.Raycast(o, -up, out RaycastHit nearHit, baseRayDist + NearGroundExtraDistance, GroundLayers, QueryTriggerInteraction.Ignore))
                    {
                        float angle = Vector3.Angle(nearHit.normal, Vector3.up);
                        if (angle <= MaxSlopeAngle)
                        {
                            nearGround = true; groundNormal = nearHit.normal; // grace
                        }
                    }
                }
            }

            // Update properties
            IsGrounded = grounded;
            IsNearGround = nearGround && !grounded;
            if (grounded || nearGround) LastGroundNormal = groundNormal;
        }

        /// <summary>
        /// Draw gizmos for debugging in editor
        /// </summary>
        public void OnDrawGizmosSelected(Vehicle vehicle)
        {
#if UNITY_EDITOR
            if (vehicle == null) return;
            Gizmos.color = IsGrounded ? Color.green : (IsNearGround ? Color.yellow : Color.red);
            Vector3 up = vehicle.transform.up;
            Vector3 origin = cachedCollider != null ? cachedCollider.bounds.center : vehicle.transform.position;
            Gizmos.DrawWireSphere(origin - up * (RaycastDist * 0.5f), GroundProbeRadius);
            Debug.DrawRay(origin, -up * RaycastDist, Gizmos.color);
#endif
        }
    }
}
