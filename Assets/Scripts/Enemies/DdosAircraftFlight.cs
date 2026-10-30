using UnityEngine;

namespace FinalSpam.Enemies
{
    /// <summary>
    /// Voo arcade com inercia real de Rigidbody. O aviao desloca-se com
    /// aceleracao limitada e gira fisicamente para alinhar os canos com o jogador.
    /// Nunca gira projeteis: somente move e orienta o proprio aviao.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DdosAircraftWeapons))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class DdosAircraftFlight : MonoBehaviour
    {
        [Header("Piloto automatico / voo fisico")]
        public bool enableFlight = true;
        [Min(0.1f)] public float flightSpeed = 9f;
        [Min(0.1f)] public float maximumAcceleration = 12f;
        [Min(0.1f)] public float velocityResponse = 1.3f;
        [Min(1f)] public float altitudeAbovePlayer = 8f;
        [Min(0f)] public float preferredDistance = 12f;
        [Min(0.1f)] public float minimumGroundClearance = 3.5f;
        [Min(1f)] public float groundProbeDistance = 40f;
        [Min(0.1f)] public float rotationStrength = 14f;
        [Min(0.1f)] public float rotationDamping = 7f;
        [Min(0.1f)] public float maximumAngularSpeed = 2.5f;
        [Tooltip("Impede que as torretas disparem contra o proprio aviao enquanto ele vira.")]
        public bool keepAircraftUpright = true;
        public bool showFlightGizmos = true;

        private Rigidbody rb;
        private DdosAircraftWeapons weapons;
        private EnemyProceduralAnimator animationController;
        private ThirdPersonController target;
        private float nextTargetSearch;
        private Vector3 startingPosition;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            weapons = GetComponent<DdosAircraftWeapons>();
            animationController = GetComponent<EnemyProceduralAnimator>();
            startingPosition = transform.position;
            rb.useGravity = false; // sustentacao aerodinamica simulada abaixo
            rb.isKinematic = false;
            rb.mass = Mathf.Max(1f, rb.mass);
            rb.linearDamping = 0.5f;
            rb.angularDamping = 1f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.maxAngularVelocity = maximumAngularSpeed;
        }

        private void FixedUpdate()
        {
            if (!enableFlight || rb == null ||
                (animationController != null && animationController.IsDead)) return;

            if (weapons != null && weapons.targetPlayer != null)
                target = weapons.targetPlayer;
            if (target == null && Time.time >= nextTargetSearch)
            {
                nextTargetSearch = Time.time + 1f;
                target = FindFirstObjectByType<ThirdPersonController>();
            }
            if (target == null)
            {
                ApplyMovement(startingPosition, Time.fixedDeltaTime);
                return;
            }
            FinalSpamPlayerHealth hp = target.GetComponent<FinalSpamPlayerHealth>();
            if (hp != null && hp.IsDead)
            {
                ApplyMovement(startingPosition, Time.fixedDeltaTime);
                return;
            }

            Vector3 aim = GetTargetPoint();
            Vector3 horizontal = Vector3.ProjectOnPlane(aim - rb.position, Vector3.up);
            Vector3 horizontalDirection = horizontal.sqrMagnitude > 0.001f
                ? horizontal.normalized : transform.forward;
            Vector3 desiredPosition = aim - horizontalDirection * preferredDistance;
            desiredPosition.y = Mathf.Max(startingPosition.y,
                aim.y + altitudeAbovePlayer);
            ApplyMovement(desiredPosition, Time.fixedDeltaTime);
            AlignMuzzlesTo(aim);
        }

        private Vector3 GetTargetPoint()
        {
            CharacterController character = target.GetComponent<CharacterController>();
            return character != null ? character.bounds.center
                : target.transform.position + Vector3.up;
        }

        private void ApplyMovement(Vector3 desiredPosition, float dt)
        {
            // Nao atravessar o chao ao procurar a altura de cruzeiro.
            RaycastHit ground;
            if (Physics.Raycast(rb.position + Vector3.up * 0.5f,
                Vector3.down, out ground, groundProbeDistance,
                ~0, QueryTriggerInteraction.Ignore))
            {
                if (!ground.transform.IsChildOf(transform))
                    desiredPosition.y = Mathf.Max(desiredPosition.y,
                        ground.point.y + minimumGroundClearance);
            }
            Vector3 velocityGoal = Vector3.ClampMagnitude(
                (desiredPosition - rb.position) * velocityResponse, flightSpeed);
            Vector3 accel = (velocityGoal - rb.linearVelocity) /
                Mathf.Max(0.02f, dt);
            rb.AddForce(Vector3.ClampMagnitude(accel, maximumAcceleration),
                ForceMode.Acceleration);
        }

        private void AlignMuzzlesTo(Vector3 aim)
        {
            if (weapons == null) return;
            if (animationController == null)
                animationController = GetComponent<EnemyProceduralAnimator>();
            if (animationController == null) return;

            Transform left = animationController.ddosMuzzleLeft;
            Transform right = animationController.ddosMuzzleRight;
            if (left == null && right == null) return;
            Vector3 muzzleAxis = weapons.fireAlongAircraftForward
                ? transform.TransformDirection(
                    weapons.turretViewLocalForward.sqrMagnitude > 0.0001f
                        ? weapons.turretViewLocalForward.normalized : Vector3.forward)
                : left != null && right != null
                    ? (left.forward + right.forward).normalized
                    : left != null ? left.forward : right.forward;
            if (muzzleAxis.sqrMagnitude < 0.0001f) return;
            Vector3 origin = left != null && right != null
                ? (left.position + right.position) * 0.5f
                : left != null ? left.position : right.position;
            Vector3 toTarget = aim - origin;
            if (toTarget.sqrMagnitude < 0.001f) return;
            toTarget.Normalize();

            // A propria aeronave vira ate colocar os canos na reta do alvo.
            // Sem alterar rotacao de qualquer MuzzlePoint do usuario.
            Quaternion requested = Quaternion.FromToRotation(muzzleAxis, toTarget)
                * rb.rotation;
            if (keepAircraftUpright)
            {
                // A correcao por eixo mais curto conserva os canos alinhados;
                // aplicamos torque, nao teleporte de rotacao.
                Vector3 desiredUp = requested * Vector3.up;
                if (Vector3.Dot(desiredUp, Vector3.up) < -0.3f)
                    requested = Quaternion.AngleAxis(180f, toTarget) * requested;
            }
            Quaternion error = requested * Quaternion.Inverse(rb.rotation);
            error.ToAngleAxis(out float degrees, out Vector3 axis);
            if (degrees > 180f) degrees -= 360f;
            if (axis.sqrMagnitude < 0.0001f) return;
            Vector3 angularAcceleration = axis.normalized *
                (degrees * Mathf.Deg2Rad * rotationStrength) -
                rb.angularVelocity * rotationDamping;
            rb.AddTorque(angularAcceleration, ForceMode.Acceleration);
        }

        private void OnDrawGizmosSelected()
        {
            if (!showFlightGizmos) return;
            Gizmos.color = new Color(0.25f, 0.8f, 1f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawLine(transform.position,
                transform.position + transform.forward * 3f);
        }
    }
}
