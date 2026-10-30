using System;
using UnityEngine;

namespace FinalSpam.Enemies
{
    /// <summary>
    /// Combate autonomo do AVIAO DDOS:
    ///  - tiros alternados dos dois MuzzlePoints reais (raycast + bala visual);
    ///  - Mini Nuke fisica, instanciada no DropPoint existente;
    ///  - alvo = ThirdPersonController, sem usar a animacao do soldado.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyProceduralAnimator))]
    public sealed class DdosAircraftWeapons : MonoBehaviour
    {
        [Header("Prefabs existentes")]
        public GameObject bulletPrefab;
        public GameObject miniNukePrefab;

        [Header("Tiro das duas torretas")]
        public bool enableTurrets = true;

        [Header("Campo de visao | torretas")]
        [Tooltip("Angulo TOTAL do cone 3D de visao (graus). 120 = 60 de cada lado; 360 = visao completa.")]
        [Range(10f, 170f)] public float turretViewAngle = 100f;
        [Tooltip("Frente FISICA do FBX do DDoS: nariz em +Z. Altere caso rotacione o modelo no prefab.")]
        public Vector3 turretViewLocalForward = Vector3.forward;
        [Range(10f, 170f)]
        [Tooltip("Abertura vertical TOTAL dos tiros. 120 = maximo 60 graus para cima/baixo.")]
        public float turretVerticalAngle = 120f;
        [Tooltip("Altura do olho da IA em relacao a raiz do aviao.")]
        public float turretEyeHeight = 0.2f;
        [Tooltip("Desenha o cone de visao e a area da Mini Nuke na Scene quando selecionado.")]
        public bool drawCombatGizmos = true;
        [Min(1f)] public float detectionRange = 55f;
        [Min(0.2f)] public float shotsPerSecond = 3f;
        [Min(0f)] public float bulletDamage = 7f;
        [Min(1f)] public float bulletSpeed = 75f;
        [HideInInspector] public float bulletSpreadDegrees = 0f; // campo legado, nao afeta o tiro reto
        [Header("Alinhamento real do cano")]
        [Tooltip("Distancia MAXIMA, em metros, entre a reta do cano e o centro do jogador. 0.65 = precisa estar bem na linha do tiro.")]
        [Range(0.1f, 2f)] public float muzzleHitRadius = 0.65f;
        [Tooltip("Ativado: projeteis saem dos MuzzlePoints e seguem RETO para a frente do AVIAO. Desative para usar o eixo Z de cada cano.")]
        public bool fireAlongAircraftForward = true;
        [Tooltip("Use 0 para nenhum desvio.")]
        public bool enableTracer = true;
        public Color bulletTracerColor = new Color(0.8f, 0.4f, 1f, 0.95f);

        [Header("Mini Nuke")]
        public bool enableMiniNuke = true;
        [Min(1f)] public float miniNukeInterval = 10f;
        [Tooltip("Raio horizontal ABAIXO do MiniNukeDropPoint. 4 = jogador precisa estar quase diretamente sob o aviao.")]
        [Min(0.1f)] public float miniNukeHorizontalRange = 4f;
        [Tooltip("A cabeca do jogador deve estar abaixo do DropPoint por pelo menos esta distancia (metros).")]
        [Min(0f)] public float miniNukeMinVerticalGap = 0.75f;
        [Min(0f)] public float miniNukeFallSpeed = 3.5f;
        [Min(0.5f)] public float miniNukeExplosionRadius = 5f;
        [Min(0f)] public float miniNukeDamage = 65f;
        [Min(1f)] public float miniNukeLifetime = 12f;

        [Header("Alvo (automatico)")]
        [Tooltip("Se vazio, procura o ThirdPersonController da cena.")]
        public ThirdPersonController targetPlayer;

        private EnemyProceduralAnimator animationController;
        private FinalSpamPlayerHealth targetHealth;
        private float nextShot;
        private float nextBomb;
        private float nextTargetCheck;
        private bool fireLeft = true;
        private bool warnedMissingPrefabs;

        private void Awake()
        {
            animationController = GetComponent<EnemyProceduralAnimator>();
        }

        private void Start()
        {
            nextShot = Time.time + 0.6f;
            nextBomb = Time.time + Mathf.Max(1f, miniNukeInterval);
        }

        private void Update()
        {
            if (animationController == null ||
                animationController.enemyKind != EnemyProceduralAnimator.EnemyKind.DdosPlane ||
                animationController.IsDead) return;

            if (targetPlayer == null && Time.time >= nextTargetCheck)
            {
                nextTargetCheck = Time.time + 1f;
                targetPlayer = FindFirstObjectByType<ThirdPersonController>();
                targetHealth = targetPlayer != null
                    ? targetPlayer.GetComponent<FinalSpamPlayerHealth>() : null;
            }

            if (targetPlayer == null) return;
            if (targetHealth == null)
                targetHealth = targetPlayer.GetComponent<FinalSpamPlayerHealth>();
            if (targetHealth != null && targetHealth.IsDead) return;

            if (!warnedMissingPrefabs &&
                ((enableTurrets && bulletPrefab == null) ||
                 (enableMiniNuke && miniNukePrefab == null)))
            {
                warnedMissingPrefabs = true;
                Debug.LogWarning("[DDoS] Associe os prefabs Bala e MiniNuke ao " +
                    "DdosAircraftWeapons no prefab do aviao.", this);
            }

            Vector3 aim = AimPoint();
            // So atira dentro da FRENTE geometrica do DDoS.
            // O mesmo teste sera repetido quando cada muzzle disparar.
            if (enableTurrets && IsTargetInsideFrontArc(aim) &&
                CanAnyMuzzleHit(aim) && Time.time >= nextShot)
            {
                nextShot = Time.time + 1f / Mathf.Max(0.2f, shotsPerSecond);
                FireNextTurret(aim);
            }

            // Mini Nuke independe do cone frontal: usa sua zona ABAIXO
            // do DropPoint em coordenadas do mundo, nunca atr?s como
            // ataque horizontal nem fora da vertical do avi?o.
            Transform drop = animationController.ddosMiniNukeDropPoint;
            if (enableMiniNuke && miniNukePrefab != null &&
                drop != null &&
                IsPlayerBelowDropPoint(drop.position, aim) &&
                Time.time >= nextBomb)
            {
                nextBomb = Time.time + Mathf.Max(1f, miniNukeInterval);
                DropMiniNuke();
            }
        }

        /// <summary>
        /// Campo frontal real: alcance + angulo horizontal + vertical.
        /// Mesmo um valor antigo serializado em 360 nao permite tiros atras.
        /// </summary>
        public bool IsTargetInsideFrontArc(Vector3 targetPosition)
        {
            Vector3 eye = transform.position + transform.up * turretEyeHeight;
            Vector3 toTarget = targetPosition - eye;
            if (toTarget.sqrMagnitude < 0.0001f ||
                toTarget.sqrMagnitude > detectionRange * detectionRange)
                return false;

            Vector3 forward = transform.TransformDirection(
                turretViewLocalForward.sqrMagnitude > 0.0001f
                    ? turretViewLocalForward.normalized : Vector3.forward);
            Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up);
            Vector3 flatTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f ||
                flatTarget.sqrMagnitude < 0.0001f)
                return false; // jogador exatamente embaixo: usar Mini Nuke

            flatForward.Normalize();
            flatTarget.Normalize();
            if (Vector3.Dot(flatForward, flatTarget) <= 0.0001f)
                return false; // BLOQUEIO ABSOLUTO DE TIROS NA TRASEIRA

            float horizontalAngle = Vector3.Angle(flatForward, flatTarget);
            float halfHorizontal = Mathf.Clamp(turretViewAngle, 10f, 170f) * 0.5f;
            if (horizontalAngle > halfHorizontal) return false;

            float horizontalDistance = Vector3.ProjectOnPlane(toTarget, Vector3.up).magnitude;
            float elevation = Mathf.Abs(Mathf.Atan2(
                toTarget.y, horizontalDistance) * Mathf.Rad2Deg);
            return elevation <= Mathf.Clamp(turretVerticalAngle, 10f, 170f) * 0.5f;
        }

        public bool IsPlayerBelowDropPoint(Vector3 dropPosition, Vector3 targetPoint)
        {
            Vector3 diff = targetPoint - dropPosition;
            Vector2 lateral = new Vector2(diff.x, diff.z);
            return diff.y <= -Mathf.Max(0f, miniNukeMinVerticalGap) &&
                lateral.sqrMagnitude <=
                    miniNukeHorizontalRange * miniNukeHorizontalRange;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawCombatGizmos) return;
            // Gizmo no MESMO eixo/frente usados pelo teste de disparo.
            Vector3 eye = transform.position + transform.up * turretEyeHeight;
            Vector3 forward = transform.TransformDirection(
                turretViewLocalForward.sqrMagnitude > 0.0001f
                    ? turretViewLocalForward.normalized : Vector3.forward);
            forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            float half = Mathf.Clamp(turretViewAngle, 10f, 170f) * 0.5f;
            float range = Mathf.Max(1f, detectionRange);
            Vector3 left = Quaternion.AngleAxis(-half, Vector3.up) * forward;
            Vector3 right = Quaternion.AngleAxis(half, Vector3.up) * forward;
            Gizmos.color = new Color(0.65f, 0.35f, 1f, 0.95f);
            Gizmos.DrawRay(eye, forward * range);
            Gizmos.DrawRay(eye, left * range);
            Gizmos.DrawRay(eye, right * range);
            Gizmos.DrawRay(eye, Vector3.up * 0.2f);
#if UNITY_EDITOR
            UnityEditor.Handles.color = Gizmos.color;
            UnityEditor.Handles.DrawWireArc(eye, Vector3.up, left,
                half * 2f, range);
            UnityEditor.Handles.Label(eye + forward * Mathf.Min(range, 4f),
                "FRENTE DO DDOS (ATIRA)");
            UnityEditor.Handles.Label(eye - forward * 1.8f,
                "TRASEIRA (SEM TIRO)");
#endif
            if (animationController == null)
                animationController = GetComponent<EnemyProceduralAnimator>();
            Transform drop = animationController != null
                ? animationController.ddosMiniNukeDropPoint : null;
            if (drop == null) return;
            // Linhas exatas dos tiros; sem curva ou correcao automatica.
            Gizmos.color = Color.cyan;
            if (animationController.ddosMuzzleLeft != null)
                Gizmos.DrawRay(animationController.ddosMuzzleLeft.position,
                    ShotDirection(animationController.ddosMuzzleLeft) * Mathf.Min(12f, detectionRange));
            if (animationController.ddosMuzzleRight != null)
                Gizmos.DrawRay(animationController.ddosMuzzleRight.position,
                    ShotDirection(animationController.ddosMuzzleRight) * Mathf.Min(12f, detectionRange));
            Gizmos.color = new Color(1f, 0.42f, 0.22f, 0.95f);
            Gizmos.DrawRay(drop.position,
                Vector3.down * Mathf.Max(1f, miniNukeMinVerticalGap));
            Gizmos.DrawWireSphere(drop.position -
                Vector3.up * (miniNukeMinVerticalGap + 0.1f),
                miniNukeHorizontalRange);
        }

        private Vector3 AimPoint()
        {
            CharacterController character =
                targetPlayer.GetComponent<CharacterController>();
            return character != null
                ? character.bounds.center
                : targetPlayer.transform.position + Vector3.up * 1f;
        }

        /// <summary>
        /// Direcao fixa do tiro: por padrao e a FRENTE DO AVIAO, mesmo se
        /// os MuzzlePoints tiverem uma rotacao antiga do FBX. A origem
        /// permanece na posicao EXATA de cada MuzzlePoint.
        /// Opcionalmente, respeita o forward individual do marker.
        /// </summary>
        public Vector3 ShotDirection(Transform muzzle)
        {
            if (muzzle == null) return Vector3.forward;
            if (!fireAlongAircraftForward) return muzzle.forward.normalized;
            Vector3 local = turretViewLocalForward.sqrMagnitude > 0.0001f
                ? turretViewLocalForward.normalized : Vector3.forward;
            return transform.TransformDirection(local).normalized;
        }

        public bool IsMuzzleLinedUp(Transform muzzle, Vector3 aim)
        {
            if (muzzle == null || !IsTargetInsideFrontArc(aim)) return false;
            Vector3 direction = ShotDirection(muzzle);
            Vector3 fromMuzzle = aim - muzzle.position;
            float along = Vector3.Dot(fromMuzzle, direction);
            if (along <= 0f || along > detectionRange) return false;
            Vector3 closestOnRay = muzzle.position + direction * along;
            return (aim - closestOnRay).sqrMagnitude <=
                muzzleHitRadius * muzzleHitRadius;
        }

        private bool CanAnyMuzzleHit(Vector3 aim)
        {
            if (animationController == null)
                animationController = GetComponent<EnemyProceduralAnimator>();
            if (animationController == null) return false;
            return IsMuzzleLinedUp(animationController.ddosMuzzleLeft, aim) ||
                   IsMuzzleLinedUp(animationController.ddosMuzzleRight, aim);
        }

        private void FireNextTurret(Vector3 aim)
        {
            if (!IsTargetInsideFrontArc(aim)) return;
            if (animationController == null)
                animationController = GetComponent<EnemyProceduralAnimator>();
            if (animationController == null) return;
            Transform left = animationController.ddosMuzzleLeft;
            Transform right = animationController.ddosMuzzleRight;
            // Prefere alternar, mas nao inventa curva para o outro cano.
            Transform first = fireLeft ? left : right;
            Transform second = fireLeft ? right : left;
            if (FireStraightFromMuzzle(first, aim) ||
                FireStraightFromMuzzle(second, aim))
                fireLeft = !fireLeft;
        }

        private bool FireStraightFromMuzzle(Transform muzzle, Vector3 aim)
        {
            if (!IsMuzzleLinedUp(muzzle, aim)) return false;
            if (muzzle == null || bulletPrefab == null) return false;

            // Origem EXATA do MuzzlePoint, direcao fixa do aviao/cano.
            // NUNCA usar (aim - origin).normalized: isso guiava a bala.
            Vector3 origin = muzzle.position;
            Vector3 direction = ShotDirection(muzzle);
            float distance = Mathf.Max(1f, detectionRange);
            Vector3 endPoint = origin + direction * distance;

            RaycastHit[] hits = Physics.RaycastAll(origin, direction,
                distance, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null ||
                    hit.collider.transform.IsChildOf(transform)) continue;
                endPoint = hit.point;
                FinalSpamPlayerHealth victim =
                    hit.collider.GetComponentInParent<FinalSpamPlayerHealth>();
                if (victim != null && !victim.IsDead)
                    victim.TakeRaycastDamage(bulletDamage, hit.point, direction);
                break; // Primeiro obstaculo: sem atravessar paredes.
            }

            GameObject projectile = Instantiate(bulletPrefab,
                origin, Quaternion.LookRotation(direction));
            RaycastBulletVisual visual = projectile.GetComponent<RaycastBulletVisual>();
            if (visual == null)
                visual = projectile.AddComponent<RaycastBulletVisual>();
            float duration = Mathf.Max(0.45f,
                Vector3.Distance(origin, endPoint) / Mathf.Max(1f, bulletSpeed) + 0.3f);
            visual.Initialize(origin, endPoint, bulletSpeed, duration,
                Vector3.zero, enableTracer, 0.20f, 0.055f, 0.001f,
                0.01f, null, bulletTracerColor);
            return true;
        }

        private void DropMiniNuke()
        {
            Transform drop = animationController.ddosMiniNukeDropPoint;
            if (drop == null || miniNukePrefab == null) return;

            // Spawn na posicao E rotacao do marker editado pelo usuario.
            GameObject bomb = Instantiate(miniNukePrefab, drop.position, drop.rotation);
            DdosMiniNukeProjectile projectile =
                bomb.GetComponent<DdosMiniNukeProjectile>();
            if (projectile == null)
                projectile = bomb.AddComponent<DdosMiniNukeProjectile>();
            projectile.Launch(transform,
                drop.forward * Mathf.Max(0f, miniNukeFallSpeed),
                miniNukeExplosionRadius, miniNukeDamage, miniNukeLifetime);
        }

        // Testes sem esperar cooldown (Play Mode).
        [ContextMenu("FinalSpam/DDoS/Disparar torreta agora")]
        public void TestTurretShot()
        {
            if (animationController == null)
                animationController = GetComponent<EnemyProceduralAnimator>();
            if (targetPlayer == null)
                targetPlayer = FindFirstObjectByType<ThirdPersonController>();
            if (targetPlayer != null) FireNextTurret(AimPoint());
        }

        [ContextMenu("FinalSpam/DDoS/Lancar Mini Nuke agora")]
        public void TestMiniNuke()
        {
            if (animationController == null)
                animationController = GetComponent<EnemyProceduralAnimator>();
            DropMiniNuke();
        }
    }
}
