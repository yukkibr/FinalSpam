using UnityEngine;

namespace FinalSpam.Enemies
{
    /// <summary>
    /// Mini Nuke real: cai por gravidade a partir do marcador do DDOS,
    /// explode ao colidir e aplica dano em area ao jogador.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DdosMiniNukeProjectile : MonoBehaviour
    {
        private float blastRadius;
        private float blastDamage;
        private float expiry;
        private Transform owner;
        private bool exploded;
        private readonly Collider[] overlaps = new Collider[128];

        public void Launch(Transform aircraft, Vector3 initialVelocity,
            float radius, float damage, float lifetime)
        {
            owner = aircraft;
            blastRadius = Mathf.Max(0.5f, radius);
            blastDamage = Mathf.Max(0f, damage);
            expiry = Time.time + Mathf.Max(1f, lifetime);

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.mass = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = initialVelocity;

            // Nao explodir no proprio aviao ao sair do compartimento.
            if (aircraft != null)
            {
                Collider[] planeColliders =
                    aircraft.GetComponentsInChildren<Collider>(true);
                Collider[] bombColliders =
                    GetComponentsInChildren<Collider>(true);
                foreach (Collider plane in planeColliders)
                    foreach (Collider bomb in bombColliders)
                        if (plane != null && bomb != null)
                            Physics.IgnoreCollision(plane, bomb, true);
            }
        }

        private void Update()
        {
            if (!exploded && expiry > 0f && Time.time >= expiry)
                Detonate();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (exploded || (owner != null &&
                collision.transform.IsChildOf(owner))) return;
            Detonate();
        }

        private void Detonate()
        {
            if (exploded) return;
            exploded = true;

            // Dano radial. Um unico dano por alvo, mesmo com varios colliders.
            int count = Physics.OverlapSphereNonAlloc(transform.position,
                blastRadius, overlaps, ~0, QueryTriggerInteraction.Ignore);
            FinalSpamPlayerHealth damagedPlayer = null;
            for (int i = 0; i < count; i++)
            {
                Collider hit = overlaps[i];
                if (hit == null) continue;
                FinalSpamPlayerHealth player =
                    hit.GetComponentInParent<FinalSpamPlayerHealth>();
                if (player == null || player == damagedPlayer || player.IsDead)
                    continue;
                damagedPlayer = player;
                float distance = Vector3.Distance(
                    transform.position, hit.ClosestPoint(transform.position));
                float amount = blastDamage *
                    Mathf.Clamp01(1f - distance / blastRadius);
                if (amount > 0f)
                    player.TakeRaycastDamage(amount, transform.position,
                        (player.transform.position - transform.position).normalized);
            }

            DdosExplosionFlash.Create(transform.position, blastRadius);
            Destroy(gameObject);
        }
    }

    /// <summary>Flash luminoso de curta duracao para mostrar a detonacao.</summary>
    public sealed class DdosExplosionFlash : MonoBehaviour
    {
        private float started;
        private float maxRadius;
        private Light glow;

        public static void Create(Vector3 worldPosition, float radius)
        {
            GameObject fx = new GameObject("DDoS | Mini Nuke - Explosao");
            fx.transform.position = worldPosition;
            var visual = fx.AddComponent<DdosExplosionFlash>();
            visual.maxRadius = radius;
            visual.started = Time.time;
            visual.glow = fx.AddComponent<Light>();
            visual.glow.type = LightType.Point;
            visual.glow.color = new Color(1f, 0.53f, 0.23f);
            visual.glow.range = Mathf.Max(3f, radius * 2f);
            visual.glow.intensity = 10f;
        }

        private void Update()
        {
            float t = (Time.time - started) / 0.65f;
            if (t >= 1f) { Destroy(gameObject); return; }
            if (glow != null)
            {
                glow.intensity = 10f * (1f - t);
                glow.range = maxRadius * 2f * (0.4f + 0.6f * t);
            }
        }
    }
}
