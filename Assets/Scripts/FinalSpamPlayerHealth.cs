using UnityEngine;

/// <summary>
/// Vida simples para o jogador do FinalSpam. Compatível com os raycasts
/// já usados pela pistola, sem interferir na movimentação existente.
/// </summary>
[DisallowMultipleComponent]
public sealed class FinalSpamPlayerHealth : MonoBehaviour, IRaycastDamageable
{
    [Header("Vida do jogador")]
    [Min(1f)] public float maxHealth = 100f;
    [Tooltip("Exibe no Console o dano recebido dos inimigos.")]
    public bool logDamage = true;
    [Tooltip("Exibe um pequeno indicador da vida no canto superior esquerdo.")]
    public bool showHealthBar = true;

    public float CurrentHealth { get; private set; }
    public bool IsDead { get { return CurrentHealth <= 0f; } }

    private void Awake()
    {
        CurrentHealth = Mathf.Max(1f, maxHealth);
    }

    public void TakeRaycastDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (IsDead || amount <= 0f) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        if (logDamage)
            Debug.Log("[FinalSpam Player] Dano: " + amount.ToString("F1") +
                " | Vida: " + CurrentHealth.ToString("F1") + "/" +
                maxHealth.ToString("F1"), this);
        if (IsDead)
        {
            Debug.Log("[FinalSpam Player] Jogador derrotado.", this);
            var controller = GetComponent<ThirdPersonController>();
            if (controller != null) controller.enabled = false;
            var pistol = GetComponent<RaycastPistol>();
            if (pistol != null) pistol.enabled = false;
        }
    }

    [ContextMenu("FinalSpam/Restaurar vida do jogador")]
    public void RestoreHealth()
    {
        CurrentHealth = Mathf.Max(1f, maxHealth);
        var controller = GetComponent<ThirdPersonController>();
        if (controller != null) controller.enabled = true;
        var pistol = GetComponent<RaycastPistol>();
        if (pistol != null) pistol.enabled = true;
    }

    private void OnGUI()
    {
        if (!showHealthBar || !Application.isPlaying) return;
        const float w = 156f;
        const float h = 11f;
        Rect label = new Rect(18f, 18f, w + 30f, 19f);
        GUI.Label(label, "VIDA " + Mathf.CeilToInt(CurrentHealth) + "/" +
            Mathf.CeilToInt(maxHealth));
        Color previous = GUI.color;
        GUI.color = new Color(0.28f, 0.17f, 0.43f, 0.80f);
        GUI.DrawTexture(new Rect(18f, 41f, w, h), Texture2D.whiteTexture);
        GUI.color = new Color(0.72f, 0.47f, 0.95f);
        GUI.DrawTexture(new Rect(18f, 41f,
            w * Mathf.Clamp01(CurrentHealth / Mathf.Max(1f, maxHealth)), h),
            Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
