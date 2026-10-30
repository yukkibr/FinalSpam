using System;
using UnityEngine;
using UnityEngine.AI;

namespace FinalSpam.Enemies
{
    /// <summary>
    /// Visao e ataque corpo a corpo do Soldado de Troia.
    /// Nao interfere em eixos/pose de espada e escudo.
    /// A perseguicao fica DESATIVADA ate existir spawn/NavMesh.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyProceduralAnimator))]
    public sealed class TrojanSoldierCombat : MonoBehaviour
    {
        [Header("Campo de visao")]
        [Tooltip("Angulo TOTAL de visao em graus. 110 = 55 graus para cada lado.")]
        [Range(0f, 360f)] public float viewAngle = 110f;
        [Min(0.1f)] public float sightDistance = 12f;
        [Tooltip("Direcao da FRENTE do soldado relativa a raiz. Se o FBX estiver ao contrario, use (0,0,-1).")]
        public Vector3 viewLocalForward = Vector3.forward;
        [Min(0f)] public float eyeHeight = 1.45f;
        [Tooltip("O soldado nao enxerga atraves de paredes ou outros colliders.")]
        public bool requireLineOfSight = true;

        [Header("Ataque de espada | curto alcance")]
        public bool enableMeleeAttack = true;
        [Min(0.3f)] public float meleeRange = 2.3f;
        [Min(0f)] public float meleeDamage = 18f;
        [Min(0.3f)] public float attackInterval = 1.65f;
        [Range(0f, 1f)]
        [Tooltip("Momento do acerto dentro da animacao procedural de espada.")]
        public float swordHitMoment = 0.48f;

        [Header("Perseguicao (reservada para spawn/NavMesh)")]
        [Tooltip("DESATIVADO por padrao; ativar somente depois de configurar NavMesh + spawn.")]
        public bool enableChase = false;
        [Tooltip("Permite perseguicao basica sem NavMesh SOMENTE quando habilitado no spawnpoint.")]
        public bool allowGroundChaseWithoutNavMesh = false;
        [Min(0.3f)] public float chaseSpeed = 2.8f;
        [Min(0.3f)] public float chaseStopDistance = 1.8f;
        [Min(0.05f)] public float chasePathUpdate = 0.25f;

        [Header("Teste")]
        [Tooltip("Exibe o cone de visao e alcance da espada na Scene.")]
        public bool drawVisionGizmos = true;
        public ThirdPersonController targetPlayer;

        private EnemyProceduralAnimator animator;
        private NavMeshAgent navAgent;
        private FinalSpamPlayerHealth playerHealth;
        private float nextTargetSearch;
        private float nextAttackTime;
        private float nextPathTime;
        private float pendingDamageTime = -1f;
        private bool warnedNoAgent;
        private CharacterController fallbackMover;
        private float verticalSpeed;

        private void Awake()
        {
            animator = GetComponent<EnemyProceduralAnimator>();
            navAgent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (animator == null || animator.enemyKind !=
                EnemyProceduralAnimator.EnemyKind.TrojanSoldier ||
                animator.IsDead) return;

            if (targetPlayer == null && Time.time >= nextTargetSearch)
            {
                nextTargetSearch = Time.time + 1f;
                targetPlayer = FindFirstObjectByType<ThirdPersonController>();
                playerHealth = targetPlayer != null
                    ? targetPlayer.GetComponent<FinalSpamPlayerHealth>() : null;
            }
            if (targetPlayer == null) return;
            if (playerHealth == null)
                playerHealth = targetPlayer.GetComponent<FinalSpamPlayerHealth>();
            if (playerHealth != null && playerHealth.IsDead) return;

            Vector3 aim = AimAtPlayer();
            bool visible = CanSee(aim);
            float distance = Vector3.Distance(transform.position,
                targetPlayer.transform.position);

            // Perseguicao e completamente opcional e NAO muda a pose dos
            // bracos. Um NavMeshAgent com NavMesh preparado sera necessario.
            if (enableChase && visible)
                FollowTarget();
            else if (navAgent != null && navAgent.enabled &&
                navAgent.isOnNavMesh && navAgent.hasPath)
                navAgent.ResetPath();

            if (pendingDamageTime >= 0f && Time.time >= pendingDamageTime)
            {
                pendingDamageTime = -1f;
                // Revalidar linha de visao/alcance no instante do golpe:
                // fugir ou sair do cone impede o dano.
                if (enableMeleeAttack && distance <= meleeRange &&
                    CanSee(aim) && playerHealth != null &&
                    !playerHealth.IsDead)
                {
                    Vector3 direction = (aim - transform.position).normalized;
                    playerHealth.TakeRaycastDamage(meleeDamage, aim, direction);
                }
            }

            // Ataques apenas perto, dentro do cone e com cooldown.
            if (enableMeleeAttack && visible && distance <= meleeRange &&
                Time.time >= nextAttackTime && !animator.IsAttacking &&
                !animator.previewTrojanAxes)
            {
                nextAttackTime = Time.time + Mathf.Max(0.3f, attackInterval);
                animator.TriggerAttack();  // reaproveita o golpe de espada ja acertado
                pendingDamageTime = Time.time +
                    animator.attackDuration * Mathf.Clamp01(swordHitMoment);
            }
        }

        public bool CanSee(Vector3 aim)
        {
            Vector3 origin = transform.position + transform.up * eyeHeight;
            if (!EnemySight.InsideCone(origin, aim,
                EnemySight.Forward(transform, viewLocalForward),
                viewAngle, sightDistance))
                return false;
            if (!requireLineOfSight) return true;
            Vector3 delta = aim - origin;
            if (delta.sqrMagnitude < 0.0001f) return true;

            RaycastHit[] hits = Physics.RaycastAll(origin, delta.normalized,
                delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null ||
                    hit.collider.transform.IsChildOf(transform))
                    continue;
                return hit.collider.GetComponentInParent<ThirdPersonController>()
                    == targetPlayer;
            }
            // Sem obstrucao de fisica: linha livre ate o jogador.
            return true;
        }

        private Vector3 AimAtPlayer()
        {
            CharacterController character =
                targetPlayer.GetComponent<CharacterController>();
            return character != null
                ? character.bounds.center
                : targetPlayer.transform.position + Vector3.up;
        }

        private void FollowTarget()
        {
            if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
            {
                navAgent.speed = Mathf.Max(0.1f, chaseSpeed);
                navAgent.stoppingDistance = Mathf.Max(0.3f, chaseStopDistance);
                if (Time.time >= nextPathTime)
                {
                    nextPathTime = Time.time + Mathf.Max(0.05f, chasePathUpdate);
                    navAgent.SetDestination(targetPlayer.transform.position);
                }
                return;
            }

            // Fallback de teste para spawnpoints. Soldado da cena principal
            // nao se move: este campo e FALSE no prefab por padrao.
            if (!allowGroundChaseWithoutNavMesh)
            {
                if (!warnedNoAgent)
                {
                    warnedNoAgent = true;
                    Debug.LogWarning("[Troia] Perseguicao sem NavMesh desabilitada. " +
                        "Use spawnpoint ou configure NavMeshAgent.", this);
                }
                return;
            }
            if (fallbackMover == null)
                fallbackMover = GetComponent<CharacterController>();
            if (fallbackMover == null || !fallbackMover.enabled) return;

            Vector3 towards = targetPlayer.transform.position - transform.position;
            towards.y = 0f;
            Vector3 movement = Vector3.zero;
            if (towards.magnitude > Mathf.Max(0.3f, chaseStopDistance))
            {
                movement = towards.normalized * Mathf.Max(0.1f, chaseSpeed);
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(towards.normalized),
                    300f * Time.deltaTime);
            }
            if (fallbackMover.isGrounded && verticalSpeed < 0f)
                verticalSpeed = -1f;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            fallbackMover.Move((movement + Vector3.up * verticalSpeed) * Time.deltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawVisionGizmos) return;
            EnemySight.DrawVisionCone(transform,
                transform.position + transform.up * eyeHeight,
                viewLocalForward, viewAngle, sightDistance,
                new Color(0.73f, 0.48f, 1f, 0.90f));
            Gizmos.color = new Color(1f, 0.3f, 0.25f, 0.85f);
            Gizmos.DrawWireSphere(transform.position +
                transform.up * 0.7f, meleeRange);
        }
    }
}
