using UnityEngine;
using FinalSpam.Enemies;

/// <summary>Vida existente dos inimigos, preservando campos da cena/prefabs.</summary>
public class RaycastDamageable : MonoBehaviour, IRaycastDamageable
{
    [Header("Vida")]
    public float maxHealth = 100f;
    public bool destroyOnDeath = true;
    public bool logDamage = true;
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private void Awake()
    {
        CurrentHealth = Mathf.Max(1f, maxHealth);
    }

    public void TakeRaycastDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (IsDead || damage <= 0f) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        if (logDamage)
            Debug.Log("[Alvo] " + name + " recebeu " + damage.ToString("F1") +
                " de dano. Vida: " + CurrentHealth.ToString("F1") +
                "/" + maxHealth.ToString("F1"), this);

        EnemyProceduralAnimator anim = GetComponent<EnemyProceduralAnimator>();
        if (CurrentHealth <= 0f)
        {
            IsDead = true;
            if (anim != null)
            {
                anim.TriggerDeath();
                var soldier = GetComponent<TrojanSoldierCombat>();
                if (soldier != null) soldier.enabled = false;
                var weapons = GetComponent<DdosAircraftWeapons>();
                if (weapons != null) weapons.enabled = false;
                var flight = GetComponent<DdosAircraftFlight>();
                if (flight != null) flight.enabled = false;
                var rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }
            }
            if (destroyOnDeath)
                Destroy(gameObject, anim != null
                    ? Mathf.Max(0.1f, anim.deathDuration + 0.25f) : 0f);
            return;
        }
        if (anim != null) anim.TriggerHit();
    }
}
