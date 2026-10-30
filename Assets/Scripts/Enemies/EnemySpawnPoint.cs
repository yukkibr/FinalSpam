using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace FinalSpam.Enemies
{
    /// <summary>
    /// Coloque este componente em qualquer GameObject vazio da cena.
    /// O Transform define exatamente o local e a direcao do nascimento.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpawnPoint : MonoBehaviour
    {
        [Header("Inimigo a gerar")]
        [Tooltip("Arraste SoldadoTroia.prefab, AviaoDDOS.prefab ou VirusTorre.prefab.")]
        public GameObject enemyPrefab;
        public bool spawnOnStart = true;
        [Tooltip("0 = sem limite de nascimentos ao longo do tempo.")]
        [Min(0)] public int maxTotalSpawns = 0;
        [Min(1)] public int maxAlive = 2;
        [Min(0.25f)] public float spawnInterval = 5f;
        [Min(0f)] public float positionRandomRadius = 0f;
        public bool orientToPlayerOnSpawn = true;
        [Min(0f)] public float minimumPlayerDistance = 2f;

        [Header("Teste: nascidos perseguem o jogador")]
        [Tooltip("Somente soldados gerados por ESTE spawnpoint seguem o jogador.")]
        public bool enableChaseForSpawnedSoldiers = true;
        [Tooltip("Se nao houver NavMesh, usa CharacterController para testar a perseguicao.")]
        public bool enableGroundChaseWithoutNavMesh = true;
        [Tooltip("Aeronaves geradas voam e alinham as torretas automaticamente.")]
        public bool enableFlightForSpawnedAircraft = true;

        [Header("Posicionamento")]
        public bool snapGroundEnemiesToSurface = true;
        [Min(1f)] public float groundProbeHeight = 8f;
        [Min(1f)] public float groundProbeDepth = 30f;
        public bool drawSpawnGizmos = true;

        private readonly List<GameObject> alive = new List<GameObject>();
        private ThirdPersonController player;
        private float nextSpawn;
        private int spawned;

        public int AliveCount
        {
            get { Cleanup(); return alive.Count; }
        }

        private void Start()
        {
            player = FindFirstObjectByType<ThirdPersonController>();
            nextSpawn = Time.time + (spawnOnStart ? 0f : spawnInterval);
        }

        private void Update()
        {
            if (enemyPrefab == null || Time.time < nextSpawn) return;
            nextSpawn = Time.time + Mathf.Max(0.25f, spawnInterval);
            SpawnOne();
        }

        [ContextMenu("FinalSpam/Spawnar inimigo agora")]
        public GameObject SpawnOne()
        {
            if (!Application.isPlaying || enemyPrefab == null) return null;
            Cleanup();
            if (alive.Count >= Mathf.Max(1, maxAlive) ||
                (maxTotalSpawns > 0 && spawned >= maxTotalSpawns)) return null;
            if (player == null) player = FindFirstObjectByType<ThirdPersonController>();

            Vector3 pos = transform.position;
            if (positionRandomRadius > 0f)
            {
                Vector2 random = Random.insideUnitCircle * positionRandomRadius;
                pos += new Vector3(random.x, 0f, random.y);
            }
            var prefabAnimator = enemyPrefab.GetComponent<EnemyProceduralAnimator>();
            bool isAircraft = prefabAnimator != null &&
                prefabAnimator.enemyKind == EnemyProceduralAnimator.EnemyKind.DdosPlane;
            if (!isAircraft && snapGroundEnemiesToSurface)
            {
                RaycastHit hit;
                if (Physics.Raycast(pos + Vector3.up * groundProbeHeight,
                    Vector3.down, out hit,
                    groundProbeHeight + groundProbeDepth,
                    ~0, QueryTriggerInteraction.Ignore))
                    pos.y = hit.point.y;
            }
            if (player != null && Vector3.Distance(pos, player.transform.position)
                < minimumPlayerDistance) return null;

            Quaternion rot = transform.rotation;
            if (player != null && orientToPlayerOnSpawn && !isAircraft)
            {
                Vector3 towards = player.transform.position - pos;
                towards.y = 0f;
                if (towards.sqrMagnitude > 0.01f)
                    rot = Quaternion.LookRotation(towards.normalized);
            }
            GameObject enemy = Instantiate(enemyPrefab, pos, rot);
            enemy.name = enemyPrefab.name + " | Spawn";
            alive.Add(enemy);
            spawned++;

            // O soldado da cena principal continua com perseguicao desligada.
            // Somente a instancia recem gerada pode seguir.
            TrojanSoldierCombat soldier = enemy.GetComponent<TrojanSoldierCombat>();
            if (soldier != null)
            {
                soldier.enableChase = enableChaseForSpawnedSoldiers;
                soldier.allowGroundChaseWithoutNavMesh = enableGroundChaseWithoutNavMesh;
                if (player != null) soldier.targetPlayer = player;
                if (soldier.enableChase && soldier.allowGroundChaseWithoutNavMesh)
                {
                    NavMeshAgent nav = enemy.GetComponent<NavMeshAgent>();
                    if (nav == null || !nav.enabled || !nav.isOnNavMesh)
                    {
                        if (enemy.GetComponent<CharacterController>() == null)
                        {
                            CharacterController body = enemy.AddComponent<CharacterController>();
                            body.height = 1.8f;
                            body.radius = 0.32f;
                            body.center = new Vector3(0f, 0.9f, 0f);
                            body.stepOffset = 0.3f;
                        }
                    }
                }
            }
            DdosAircraftWeapons weapons = enemy.GetComponent<DdosAircraftWeapons>();
            if (weapons != null && player != null) weapons.targetPlayer = player;
            DdosAircraftFlight flight = enemy.GetComponent<DdosAircraftFlight>();
            if (flight != null) flight.enableFlight = enableFlightForSpawnedAircraft;
            return enemy;
        }

        private void Cleanup()
        {
            for (int i = alive.Count - 1; i >= 0; i--)
                if (alive[i] == null) alive.RemoveAt(i);
        }

        private void OnDrawGizmos()
        {
            if (!drawSpawnGizmos) return;
            Gizmos.color = new Color(0.72f, 0.41f, 1f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 0.55f);
            Gizmos.DrawLine(transform.position,
                transform.position + transform.forward * 1.8f);
            if (positionRandomRadius > 0f)
                Gizmos.DrawWireSphere(transform.position, positionRandomRadius);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up,
                "SPAWN: " + (enemyPrefab != null ? enemyPrefab.name : "SEM PREFAB"));
#endif
        }
    }
}
