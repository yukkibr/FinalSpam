using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public interface IRaycastDamageable
{
    void TakeRaycastDamage(
        float damage,
        Vector3 hitPoint,
        Vector3 hitDirection
    );
}

[DefaultExecutionOrder(25)]
public class RaycastPistol : MonoBehaviour
{
    // ============================================================
    // REFERÊNCIAS
    // ============================================================

    [Header("Referências")]

    public ThirdPersonController playerController;

    [Tooltip("Se ficar vazio, usa a câmera do ThirdPersonController ou Camera.main.")]
    public Transform cameraTransform;

    [Tooltip("Crie um Empty na ponta da arma. Se ficar vazio, usa o próprio Transform da arma.")]
    public Transform muzzlePoint;


    // ============================================================
    // ARMA
    // ============================================================

    [Header("Pistola")]

    public bool weaponEquipped = true;

    public float damage = 25f;

    public float range = 100f;

    [Tooltip("Limite máximo de disparos por segundo.")]
    public float shotsPerSecond = 5f;

    public float impactForce = 8f;

    public LayerMask hitMask = ~0;

    public QueryTriggerInteraction triggerInteraction =
        QueryTriggerInteraction.Ignore;

    public bool allowFireUnderwater = false;


    // ============================================================
    // MIRA / ORIENTAÇÃO
    // ============================================================

    [Header("Mira")]

    [Tooltip("Faz o personagem olhar horizontalmente para a direção da câmera enquanto a pistola estiver equipada.")]
    public bool alignPlayerWithCamera = true;

    public float playerAimRotationSpeed = 14f;

    [Tooltip("Se ativo, o personagem só alinha com a câmera enquanto o botão de mira estiver pressionado.")]
    public bool enableAimMode = true;

    [Tooltip("Opcional. Se ativado, impede disparar sem estar mirando.")]
    public bool requireAimToFire = false;


    // ============================================================
    // INPUT
    // ============================================================

    [Header("Input")]

    public bool enableMouseFire = true;
    public bool enableMouseAim = true;

    public bool enableControllerFire = true;
    public bool enableControllerAim = true;

    [Tooltip("Botão do joystick USB genérico. Use o número mostrado pelo log do ThirdPersonController.")]
    public int genericFireButton = 0;

    [Tooltip("Botão do joystick USB genérico usado para mirar. Ajuste pelo log do ThirdPersonController.")]
    public int genericAimButton = 6;


    // ============================================================
    // PROJÉTIL VISUAL
    // ============================================================

    [Header("Projétil Visual / Bala")]

    [Tooltip("Arraste aqui o modelo/prefab 'bala' de Assets/Models.")]
    public GameObject bulletPrefab;

    [Tooltip("Cria a bala visual percorrendo exatamente o traçado resolvido pelo Raycast.")]
    public bool spawnBulletVisual = true;

    [Tooltip("Velocidade visual da bala em unidades por segundo.")]
    public float bulletVisualSpeed = 120f;

    [Tooltip("Tempo máximo de vida da bala visual.")]
    public float bulletVisualLifetime = 3f;

    [Tooltip("Offset de rotação caso o eixo frontal do modelo 'bala' não seja +Z.")]
    public Vector3 bulletRotationOffset = Vector3.zero;

    [Tooltip("Multiplicador uniforme do tamanho visual da bala. 1 = tamanho original do prefab.")]
    [Min(0.001f)]
    public float bulletVisualScale = 1f;

    [Header("Trail / Tracer da Bala")]

    [Tooltip("Adiciona uma esteira visual atrás da bala enquanto ela percorre o Raycast.")]
    public bool enableBulletTrail = true;

    [Tooltip("Tempo que o rastro permanece visível após a bala passar.")]
    [Min(0.01f)]
    public float bulletTrailTime = 0.08f;

    [Tooltip("Espessura do rastro junto da bala.")]
    [Min(0.0001f)]
    public float bulletTrailStartWidth = 0.018f;

    [Tooltip("Espessura do final do rastro.")]
    [Min(0f)]
    public float bulletTrailEndWidth = 0.002f;

    [Tooltip("Distância mínima entre pontos do TrailRenderer.")]
    [Min(0.001f)]
    public float bulletTrailMinVertexDistance = 0.03f;

    [Tooltip("Material opcional para o tracer. Se vazio, cria um material simples automaticamente.")]
    public Material bulletTrailMaterial;

    [Tooltip("Cor do tracer quando o material automático for usado.")]
    public Color bulletTrailColor = new Color(1f, 0.78f, 0.28f, 1f);


    // ============================================================
    // CÁPSULA / ESTOJO
    // ============================================================

    [Header("Cápsula / Estojo")]

    [Tooltip("Arraste aqui o modelo/prefab 'capsula' de Assets/Models.")]
    public GameObject casingPrefab;

    [Tooltip("Crie um Empty na janela de ejeção da pistola. A cápsula visual será alinhada exatamente a este ponto.")]
    public Transform casingEjectionPoint;

    public bool ejectCasing = true;

    [Tooltip("Multiplicador uniforme do tamanho da cápsula ejetada.")]
    [Min(0.001f)]
    public float casingVisualScale = 1f;

    [Tooltip("Se ativo, corrige automaticamente prefabs/modelos cujo pivot não fica no centro da malha, alinhando a parte visível ao EjectionPoint.")]
    public bool alignCasingVisualToEjectionPoint = true;

    [Tooltip("Ignora colisão da cápsula com o próprio personagem/arma, evitando ela ricochetear assim que nasce.")]
    public bool ignoreOwnerCollisionForCasing = true;

    [Tooltip("Força lateral de ejeção. Normalmente positiva joga a cápsula para a direita do EjectionPoint.")]
    public float casingSideForce = 2.2f;

    public float casingUpForce = 1.35f;

    public float casingForwardForce = 0.25f;

    [Tooltip("Variação aleatória aplicada à força para a ejeção não ficar robótica.")]
    public float casingForceRandomness = 0.35f;

    public float casingTorque = 7f;

    public float casingMass = 0.02f;

    [Tooltip("Tempo até a cápsula ser destruída para não acumular objetos na cena.")]
    public float casingLifetime = 8f;

    [Tooltip("Se o modelo não tiver Collider, cria automaticamente um BoxCollider aproximado.")]
    public bool autoAddCasingCollider = true;


    // ============================================================
    // ANIMAÇÃO DA PISTOLA / ARMATURE
    // ============================================================

    [Header("Animação da Pistola / Armature")]

    [Tooltip("Armature da própria pistola. Se ficar vazio, tenta localizar automaticamente um filho cujo nome comece com 'Armature'.")]
    public Transform pistolArmature;

    [Tooltip("Bone do gatilho. Auto-detecta 'Gatilho' dentro do armature.")]
    public Transform triggerBone;

    [Tooltip("Bone do martelo. No seu modelo ele se chama 'Mecanismo'.")]
    public Transform hammerBone;

    [Tooltip("Bone do ferrolho/corpo móvel. Auto-detecta 'Corpo'. Desative Animate Slide se este bone não for o ferrolho.")]
    public Transform slideBone;

    public bool animateTrigger = true;
    public bool animateHammer = true;
    public bool animateSlide = true;

    [Tooltip("Duração total da animação mecânica de um disparo.")]
    [Min(0.02f)]
    public float weaponMechanismDuration = 0.14f;

    [Tooltip("Eixo LOCAL de rotação do gatilho. Inverta o sinal do ângulo se puxar para o lado errado.")]
    public Vector3 triggerRotationAxis = Vector3.right;
    public float triggerPullAngle = 18f;

    [Tooltip("Quando ativo, calcula automaticamente o recuo do Mecanismo para TRÁS da pistola usando a direção do MuzzlePoint. Recomendado para armatures com eixos importados diferentes.")]
    public bool hammerUseMuzzleBackward = true;

    [Tooltip("Eixo LOCAL manual usado somente se Hammer Use Muzzle Backward estiver desligado. O padrão agora é -X para testar um eixo diferente do antigo -Z.")]
    public Vector3 hammerLocalAxis = Vector3.left;

    [Tooltip("Distância que o Mecanismo recua antes de voltar para a posição original.")]
    [Min(0f)]
    public float hammerTravel = 0.012f;

    [Tooltip("Direção LOCAL do recuo do ferrolho/Corpo.")]
    public Vector3 slideLocalAxis = Vector3.back;
    public float slideTravel = 0.025f;

    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Debug")]

    public bool drawDebugRay = true;

    public float debugRayDuration = 0.35f;

    [Header("Raycast Visível no Jogo")]

    [Tooltip("Mostra uma linha do cano até o ponto do Raycast enquanto estiver mirando.")]
    public bool visibleRayEnabled = false;

    [Tooltip("Tecla para ligar/desligar a linha visível durante o jogo.")]
    public Key visibleRayToggleKey = Key.F3;

    [Tooltip("No Gamepad padrão, Select/Back também alterna a linha.")]
    public bool enableGamepadVisibleRayToggle = true;

    [Tooltip("Joystick USB genérico: -1 desativa. Use o índice mostrado no log se quiser mapear um botão.")]
    public int genericVisibleRayToggleButton = -1;

    public float visibleRayWidth = 0.012f;

    [Tooltip("Opcional. Se ficar vazio, o script cria um material simples em runtime.")]
    public Material visibleRayMaterial;

    [Tooltip("Opcional. Se ficar vazio, o script cria um LineRenderer automaticamente.")]
    public LineRenderer visibleRayRenderer;

    public bool logShots = true;

    public bool logHits = true;


    // ============================================================
    // ESTADO PÚBLICO
    // ============================================================

    public bool IsEquipped =>
        weaponEquipped &&
        enabled &&
        gameObject.activeInHierarchy;

    public bool IsFiringThisFrame { get; private set; }

    public bool IsAiming { get; private set; }

    public RaycastHit LastHit { get; private set; }

    public bool HasLastHit { get; private set; }


    // ============================================================
    // INTERNOS
    // ============================================================

    private Transform ownerRoot;

    private float nextAllowedShotTime;
    private Material runtimeVisibleRayMaterial;
    private Material runtimeBulletTrailMaterial;

    private Quaternion triggerInitialLocalRotation;
    private Quaternion hammerInitialLocalRotation;
    private Vector3 hammerInitialLocalPosition;
    private Vector3 slideInitialLocalPosition;
    private Quaternion slideInitialLocalRotation;
    private bool weaponMechanismAnimating;
    private float weaponMechanismTimer;


    // ============================================================
    // AWAKE
    // ============================================================

    void Awake()
    {
        if (playerController == null)
        {
            playerController =
                GetComponentInParent<ThirdPersonController>();
        }

        if (cameraTransform == null &&
            playerController != null)
        {
            cameraTransform =
                playerController.cameraTransform;
        }

        if (cameraTransform == null &&
            Camera.main != null)
        {
            cameraTransform =
                Camera.main.transform;
        }

        if (muzzlePoint == null)
        {
            // Buscar somente o Empty criado no cano, nunca o pivot.
            muzzlePoint = FindChildRecursive(transform, "MuzzlePoint");
        }
        if (muzzlePoint == null || muzzlePoint == transform)
        {
            Debug.LogError(
                "[Pistola] MuzzlePoint ausente/invalido em " + name +
                ". Atribua no Inspector o Transform na boca do cano.",
                this);
        }

        if (casingEjectionPoint == null)
        {
            casingEjectionPoint =
                FindChildRecursive(
                    transform,
                    "CasingEjectionPoint"
                );

            if (casingEjectionPoint == null)
            {
                Debug.LogWarning(
                    "[Pistola] CasingEjectionPoint não atribuído/encontrado. " +
                    "A cápsula usará o Transform da pistola como fallback."
                );
            }
        }

        ownerRoot =
            playerController != null
                ? playerController.transform
                : transform.root;

        InitializePistolArmature();

        EnsureVisibleRayRenderer();
        SetVisibleRayRenderer(false);
    }


    // ============================================================
    // UPDATE
    // ============================================================

    void Update()
    {
        IsFiringThisFrame = false;

        HandleVisibleRayToggle();

        if (!IsEquipped)
        {
            SetAiming(false);
            SetVisibleRayRenderer(false);
            return;
        }

        bool canAim =
            playerController == null ||
            !playerController.IsUnderwater;

        bool wantsToAim =
            enableAimMode &&
            canAim &&
            ReadAimHeld();

        SetAiming(wantsToAim);

        if (
            playerController != null &&
            playerController.IsUnderwater &&
            !allowFireUnderwater
        )
        {
            return;
        }

        if (
            requireAimToFire &&
            !IsAiming
        )
        {
            return;
        }

        if (
            ReadFirePressed() &&
            Time.time >= nextAllowedShotTime
        )
        {
            Fire();

            float fireInterval =
                1f / Mathf.Max(0.01f, shotsPerSecond);

            nextAllowedShotTime =
                Time.time + fireInterval;
        }
    }


    // ============================================================
    // LATE UPDATE - PERSONAGEM APONTA PARA A CÂMERA
    // ============================================================

    void LateUpdate()
    {
        UpdateWeaponMechanismAnimation();

        if (
            IsEquipped &&
            IsAiming &&
            alignPlayerWithCamera &&
            playerController != null &&
            cameraTransform != null &&
            !playerController.IsUnderwater
        )
        {
            Vector3 cameraForward =
                cameraTransform.forward;

            cameraForward.y = 0f;

            if (cameraForward.sqrMagnitude > 0.0001f)
            {
                cameraForward.Normalize();

                Quaternion targetRotation =
                    Quaternion.LookRotation(
                        cameraForward,
                        Vector3.up
                    );

                playerController.transform.rotation =
                    Quaternion.Slerp(
                        playerController.transform.rotation,
                        targetRotation,
                        Mathf.Clamp01(
                            playerAimRotationSpeed *
                            Time.deltaTime
                        )
                    );
            }
        }

        if (
            visibleRayEnabled &&
            IsEquipped &&
            IsAiming
        )
        {
            UpdateVisibleRay();
        }
        else
        {
            SetVisibleRayRenderer(false);
        }
    }


    // ============================================================
    // DISPARO
    // ============================================================

    public void Fire()
    {
        if (!IsEquipped)
            return;

        if (requireAimToFire && !IsAiming)
            return;

        if (!BuildShotRay(
                out Vector3 muzzleOrigin,
                out Vector3 aimPoint,
                out Vector3 shotDirection,
                out float shotDistance
            ))
        {
            Debug.LogWarning(
                "[Pistola] Não foi possível construir o Raycast. Verifique cameraTransform."
            );

            return;
        }

        IsFiringThisFrame = true;
        HasLastHit = false;

        PlayWeaponMechanismAnimation();

        bool hitSomething =
            TryRaycastIgnoringOwner(
                muzzleOrigin,
                shotDirection,
                shotDistance + 0.25f,
                out RaycastHit hit
            );


        // --------------------------------------------------------
        // RESULTADO
        // --------------------------------------------------------

        Vector3 visualEndPoint =
            hitSomething
                ? hit.point
                : aimPoint;

        SpawnBulletVisual(
            muzzleOrigin,
            visualEndPoint
        );

        EjectCasing();

        if (hitSomething)
        {
            LastHit = hit;
            HasLastHit = true;

            ApplyHit(hit, shotDirection);

            if (drawDebugRay)
            {
                Debug.DrawLine(
                    muzzleOrigin,
                    hit.point,
                    Color.red,
                    debugRayDuration
                );
            }
        }
        else
        {
            if (drawDebugRay)
            {
                Debug.DrawLine(
                    muzzleOrigin,
                    aimPoint,
                    Color.green,
                    debugRayDuration
                );
            }
        }

        if (logShots)
        {
            Debug.Log(
                "[Pistola] DISPARO | Origem: " +
                "Muzzle=" +
                (muzzlePoint != null ? muzzlePoint.name : "NULL") +
                " | Pivot=" + transform.position +
                " | Mundo=" +
                muzzleOrigin +
                " | Direção: " +
                shotDirection
            );
        }
    }


    // ============================================================
    // PROJÉTIL VISUAL
    // ============================================================

    void SpawnBulletVisual(
        Vector3 startPoint,
        Vector3 endPoint
    )
    {
        if (!spawnBulletVisual ||
            bulletPrefab == null)
        {
            return;
        }

        Vector3 direction =
            endPoint - startPoint;

        // A rotacao final, incluindo bulletRotationOffset, e aplicada
        // UMA vez por RaycastBulletVisual.Initialize().
        Quaternion rotation = direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized)
            : transform.rotation;

        GameObject bullet = Instantiate(
            bulletPrefab, startPoint, rotation);

        bullet.transform.localScale *=
            Mathf.Max(0.001f, bulletVisualScale);

        RaycastBulletVisual visual =
            bullet.GetComponent<RaycastBulletVisual>();

        if (visual == null)
        {
            visual =
                bullet.AddComponent<RaycastBulletVisual>();
        }

        visual.Initialize(
            startPoint,
            endPoint,
            bulletVisualSpeed,
            bulletVisualLifetime,
            bulletRotationOffset,
            enableBulletTrail,
            bulletTrailTime,
            bulletTrailStartWidth,
            bulletTrailEndWidth,
            bulletTrailMinVertexDistance,
            GetBulletTrailMaterial(),
            bulletTrailColor
        );
    }


    // ============================================================
    // EJEÇÃO DA CÁPSULA
    // ============================================================

    void EjectCasing()
    {
        if (!ejectCasing ||
            casingPrefab == null)
        {
            return;
        }

        Transform point =
            casingEjectionPoint != null
                ? casingEjectionPoint
                : transform;

        // O novo FBX usa eixos locais diferentes. O estojo deve nascer
        // no lado DIREITO do jogador, independente da rotacao do modelo da arma.
        Transform shooter = ownerRoot != null ? ownerRoot : transform;
        Vector3 casingOrigin = point.position;
        Vector3 shooterRight = shooter.right;
        Vector3 shooterUp = shooter.up;
        Vector3 shooterForward = shooter.forward;

        // Deslocamento pequeno para evitar atravessar a mao ou o corpo.
        casingOrigin += shooterRight * 0.075f + shooterUp * 0.035f;
        GameObject casing = Instantiate(
            casingPrefab,
            casingOrigin,
            Quaternion.LookRotation(shooterForward, shooterUp));

        casing.transform.localScale *=
            Mathf.Max(0.001f, casingVisualScale);

        // Modelos FBX/OBJ às vezes possuem o pivot longe da geometria.
        // Esta correção garante que a PARTE VISÍVEL da cápsula nasça
        // exatamente no CasingEjectionPoint.
        if (alignCasingVisualToEjectionPoint)
        {
            AlignVisibleObjectCenter(
                casing,
                casingOrigin
            );
        }

        if (autoAddCasingCollider &&
            casing.GetComponentInChildren<Collider>() == null)
        {
            AddAutomaticCasingCollider(casing);
        }

        Rigidbody body =
            casing.GetComponent<Rigidbody>();

        if (body == null)
        {
            body =
                casing.AddComponent<Rigidbody>();
        }

        body.isKinematic = false;
        body.useGravity = true;
        body.detectCollisions = true;
        body.mass = Mathf.Max(0.001f, casingMass);
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;

        if (ignoreOwnerCollisionForCasing)
        {
            IgnoreCasingOwnerCollisions(casing);
        }

        float randomSide =
            UnityEngine.Random.Range(
                -casingForceRandomness,
                casingForceRandomness
            );

        float randomUp =
            UnityEngine.Random.Range(
                -casingForceRandomness * 0.5f,
                casingForceRandomness
            );

        float randomForward =
            UnityEngine.Random.Range(
                -casingForceRandomness * 0.5f,
                casingForceRandomness * 0.5f
            );

        Vector3 ejectionVelocity =
            shooterRight * (Mathf.Abs(casingSideForce) + randomSide) +
            shooterUp * (casingUpForce + randomUp) +
            shooterForward * (casingForwardForce + randomForward);

        // Na V6 era ForceMode.Impulse. Com massa 0.02 e força 2.2,
        // a cápsula podia ganhar ~110 m/s e parecer que não nasceu
        // no EjectionPoint. VelocityChange faz os valores acima
        // representarem uma velocidade de ejeção previsível.
        body.AddForce(
            ejectionVelocity,
            ForceMode.VelocityChange
        );

        body.AddTorque(
            UnityEngine.Random.insideUnitSphere *
            casingTorque,
            ForceMode.VelocityChange
        );

        Destroy(
            casing,
            Mathf.Max(0.1f, casingLifetime)
        );
    }


    void AlignVisibleObjectCenter(
        GameObject target,
        Vector3 worldPosition
    )
    {
        Renderer[] renderers =
            target.GetComponentsInChildren<Renderer>();

        if (renderers == null ||
            renderers.Length == 0)
        {
            target.transform.position = worldPosition;
            return;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1;
             i < renderers.Length;
             i++)
        {
            bounds.Encapsulate(
                renderers[i].bounds
            );
        }

        Vector3 correction =
            worldPosition - bounds.center;

        target.transform.position += correction;
    }


    void IgnoreCasingOwnerCollisions(
        GameObject casing
    )
    {
        if (ownerRoot == null)
            return;

        Collider[] casingColliders =
            casing.GetComponentsInChildren<Collider>();

        Collider[] ownerColliders =
            ownerRoot.GetComponentsInChildren<Collider>();

        foreach (Collider casingCollider in casingColliders)
        {
            if (casingCollider == null)
                continue;

            foreach (Collider ownerCollider in ownerColliders)
            {
                if (ownerCollider == null)
                    continue;

                Physics.IgnoreCollision(
                    casingCollider,
                    ownerCollider,
                    true
                );
            }
        }
    }


    void AddAutomaticCasingCollider(
        GameObject casing
    )
    {
        Renderer[] renderers =
            casing.GetComponentsInChildren<Renderer>();

        BoxCollider box =
            casing.AddComponent<BoxCollider>();

        if (renderers == null ||
            renderers.Length == 0)
        {
            box.center = Vector3.zero;
            box.size = new Vector3(
                0.04f,
                0.015f,
                0.015f
            );

            return;
        }

        Bounds bounds =
            renderers[0].bounds;

        for (int i = 1;
             i < renderers.Length;
             i++)
        {
            bounds.Encapsulate(
                renderers[i].bounds
            );
        }

        box.center =
            casing.transform.InverseTransformPoint(
                bounds.center
            );

        Vector3 localSize =
            casing.transform.InverseTransformVector(
                bounds.size
            );

        box.size =
            new Vector3(
                Mathf.Abs(localSize.x),
                Mathf.Abs(localSize.y),
                Mathf.Abs(localSize.z)
            );
    }


    // ============================================================
    // ANIMAÇÃO MECÂNICA DA PISTOLA
    // ============================================================

    void InitializePistolArmature()
    {
        if (pistolArmature == null)
        {
            Transform[] allTransforms =
                GetComponentsInChildren<Transform>(true);

            foreach (Transform candidate in allTransforms)
            {
                if (candidate == null ||
                    candidate == transform)
                {
                    continue;
                }

                if (candidate.name.StartsWith(
                        "Armature",
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    pistolArmature = candidate;
                    break;
                }
            }
        }

        Transform searchRoot =
            pistolArmature != null
                ? pistolArmature
                : transform;

        if (triggerBone == null)
        {
            triggerBone =
                FindChildRecursive(
                    searchRoot,
                    "Gatilho"
                );
        }

        if (hammerBone == null)
        {
            hammerBone =
                FindChildRecursive(
                    searchRoot,
                    "Mecanismo"
                );
        }

        if (slideBone == null)
        {
            slideBone =
                FindChildRecursive(
                    searchRoot,
                    "Corpo"
                );
        }

        if (triggerBone != null)
        {
            triggerInitialLocalRotation =
                triggerBone.localRotation;
        }

        if (hammerBone != null)
        {
            hammerInitialLocalRotation =
                hammerBone.localRotation;

            hammerInitialLocalPosition =
                hammerBone.localPosition;
        }

        if (slideBone != null)
        {
            slideInitialLocalPosition =
                slideBone.localPosition;

            slideInitialLocalRotation =
                slideBone.localRotation;
        }
    }


    Transform FindChildRecursive(
        Transform parent,
        string childName
    )
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
        {
            if (child.name.Equals(
                    childName,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return child;
            }

            Transform result =
                FindChildRecursive(
                    child,
                    childName
                );

            if (result != null)
                return result;
        }

        return null;
    }


    void PlayWeaponMechanismAnimation()
    {
        weaponMechanismTimer = 0f;
        weaponMechanismAnimating = true;
    }


    void UpdateWeaponMechanismAnimation()
    {
        if (!weaponMechanismAnimating)
            return;

        float duration =
            Mathf.Max(
                0.02f,
                weaponMechanismDuration
            );

        weaponMechanismTimer +=
            Time.deltaTime;

        float t =
            Mathf.Clamp01(
                weaponMechanismTimer /
                duration
            );

        // Gatilho: puxa e retorna durante o ciclo.
        float triggerWeight =
            Mathf.Sin(
                t * Mathf.PI
            );

        // Mecanismo: recua rapidamente no início e volta para a
        // posição original antes do fim do ciclo.
        float hammerWeight;

        if (t < 0.22f)
        {
            hammerWeight =
                Smooth01(
                    t / 0.22f
                );
        }
        else
        {
            hammerWeight =
                1f -
                Smooth01(
                    (t - 0.22f) /
                    0.78f
                );
        }

        // Ferrolho/Corpo começa logo depois da batida do martelo,
        // recua e retorna.
        float slideT =
            Mathf.Clamp01(
                (t - 0.08f) / 0.82f
            );

        float slideWeight =
            Mathf.Sin(
                slideT * Mathf.PI
            );

        if (animateTrigger &&
            triggerBone != null)
        {
            triggerBone.localRotation =
                triggerInitialLocalRotation *
                Quaternion.AngleAxis(
                    triggerPullAngle *
                    triggerWeight,
                    SafeAxis(triggerRotationAxis)
                );
        }

        if (animateHammer &&
            hammerBone != null)
        {
            Vector3 hammerAxis =
                GetHammerRetractionLocalAxis();

            hammerBone.localPosition =
                hammerInitialLocalPosition +
                hammerAxis *
                hammerTravel *
                hammerWeight;

            // O Mecanismo não gira: preserva sempre sua rotação original.
            hammerBone.localRotation =
                hammerInitialLocalRotation;
        }

        if (animateSlide &&
            slideBone != null)
        {
            slideBone.localPosition =
                slideInitialLocalPosition +
                SafeAxis(slideLocalAxis) *
                slideTravel *
                slideWeight;

            slideBone.localRotation =
                slideInitialLocalRotation;
        }

        if (t >= 1f)
        {
            ResetWeaponMechanismPose();
            weaponMechanismAnimating = false;
        }
    }


    Vector3 GetHammerRetractionLocalAxis()
    {
        if (!hammerUseMuzzleBackward ||
            hammerBone == null ||
            muzzlePoint == null)
        {
            return SafeAxis(hammerLocalAxis);
        }

        Transform localSpace =
            hammerBone.parent != null
                ? hammerBone.parent
                : hammerBone;

        // "Para trás" da pistola é o sentido oposto ao forward do cano.
        // TransformDirection/InverseTransformDirection convertem esse vetor
        // para o espaço local correto do parent do bone, evitando depender
        // de como o FBX/armature foi exportado.
        Vector3 worldBackward =
            -muzzlePoint.forward.normalized;

        Vector3 localBackward =
            localSpace.InverseTransformDirection(
                worldBackward
            );

        if (localBackward.sqrMagnitude < 0.0001f)
        {
            return SafeAxis(hammerLocalAxis);
        }

        return localBackward.normalized;
    }


    Material GetBulletTrailMaterial()
    {
        if (bulletTrailMaterial != null)
            return bulletTrailMaterial;

        if (runtimeBulletTrailMaterial != null)
        {
            runtimeBulletTrailMaterial.color =
                bulletTrailColor;

            return runtimeBulletTrailMaterial;
        }

        Shader shader =
            Shader.Find("Sprites/Default");

        if (shader == null)
        {
            shader =
                Shader.Find("Unlit/Color");
        }

        if (shader == null)
            return null;

        runtimeBulletTrailMaterial =
            new Material(shader);

        runtimeBulletTrailMaterial.name =
            "Runtime_BulletTracer_Material";

        runtimeBulletTrailMaterial.color =
            bulletTrailColor;

        return runtimeBulletTrailMaterial;
    }


    float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);

        return value * value *
               (3f - 2f * value);
    }


    Vector3 SafeAxis(Vector3 axis)
    {
        if (axis.sqrMagnitude < 0.0001f)
            return Vector3.right;

        return axis.normalized;
    }


    void ResetWeaponMechanismPose()
    {
        if (triggerBone != null)
        {
            triggerBone.localRotation =
                triggerInitialLocalRotation;
        }

        if (hammerBone != null)
        {
            hammerBone.localPosition =
                hammerInitialLocalPosition;

            hammerBone.localRotation =
                hammerInitialLocalRotation;
        }

        if (slideBone != null)
        {
            slideBone.localPosition =
                slideInitialLocalPosition;

            slideBone.localRotation =
                slideInitialLocalRotation;
        }
    }


    public void SetBulletVisualScale(float newScale)
    {
        bulletVisualScale =
            Mathf.Max(
                0.001f,
                newScale
            );
    }


    // ============================================================
    // MIRA / RAYCAST VISÍVEL
    // ============================================================

    void SetAiming(bool aiming)
    {
        IsAiming =
            IsEquipped &&
            aiming;

        if (playerController != null)
        {
            playerController.SetWeaponAiming(IsAiming);
        }
    }


    bool BuildShotRay(
        out Vector3 muzzleOrigin,
        out Vector3 aimPoint,
        out Vector3 shotDirection,
        out float shotDistance
    )
    {
        muzzleOrigin = Vector3.zero;
        aimPoint = Vector3.zero;
        shotDirection = Vector3.forward;
        shotDistance = 0f;

        // Nunca substituir o MuzzlePoint pelo pivot da pistola
        // nem por uma posicao estimada a partir da camera.
        if (muzzlePoint == null || muzzlePoint == transform)
        {
            Debug.LogError(
                "[Pistola] Disparo cancelado: MuzzlePoint nao atribuido " +
                "ou aponta para o pivot. Configure-o no cano.",
                this);
            return false;
        }

        if (cameraTransform == null)
        {
            Debug.LogWarning(
                "[Pistola] Camera ausente: impossivel calcular mira.",
                this);
            return false;
        }

        // Origem real do dano, bala e raycast visivel (F3).
        muzzleOrigin = muzzlePoint.position;

        // O ray da camera serve apenas para ENCONTRAR o alvo;
        // o tiro real parte do MuzzlePoint em direcao ao alvo.
        Vector3 cameraOrigin = cameraTransform.position;
        Vector3 cameraDirection = cameraTransform.forward.normalized;
        aimPoint = cameraOrigin + cameraDirection * range;

        if (TryRaycastIgnoringOwner(
                cameraOrigin, cameraDirection, range,
                out RaycastHit cameraHit))
        {
            aimPoint = cameraHit.point;
        }

        Vector3 toAimPoint = aimPoint - muzzleOrigin;

        if (toAimPoint.sqrMagnitude > 0.0001f)
        {
            shotDistance = toAimPoint.magnitude;
            shotDirection = toAimPoint / shotDistance;
        }
        else
        {
            shotDirection = cameraDirection;
            shotDistance = range;
        }
        return true;
    }

    // Mostra a origem real no Scene View ao selecionar a arma.
    void OnDrawGizmosSelected()
    {
        if (muzzlePoint == null || muzzlePoint == transform)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(muzzlePoint.position, 0.035f);
        Gizmos.DrawLine(
            muzzlePoint.position,
            muzzlePoint.position + muzzlePoint.forward * 0.22f);
    }

    void UpdateVisibleRay()
    {
        EnsureVisibleRayRenderer();

        if (visibleRayRenderer == null)
            return;

        if (!BuildShotRay(
                out Vector3 muzzleOrigin,
                out Vector3 aimPoint,
                out Vector3 shotDirection,
                out float shotDistance
            ))
        {
            SetVisibleRayRenderer(false);
            return;
        }

        Vector3 endPoint = aimPoint;
        bool hitSomething =
            TryRaycastIgnoringOwner(
                muzzleOrigin,
                shotDirection,
                shotDistance + 0.25f,
                out RaycastHit hit
            );

        if (hitSomething)
        {
            endPoint = hit.point;
            visibleRayRenderer.startColor = Color.red;
            visibleRayRenderer.endColor = Color.red;
        }
        else
        {
            visibleRayRenderer.startColor = Color.green;
            visibleRayRenderer.endColor = Color.green;
        }

        visibleRayRenderer.positionCount = 2;
        visibleRayRenderer.SetPosition(0, muzzleOrigin);
        visibleRayRenderer.SetPosition(1, endPoint);
        visibleRayRenderer.startWidth = visibleRayWidth;
        visibleRayRenderer.endWidth = visibleRayWidth;
        visibleRayRenderer.enabled = true;
    }


    void HandleVisibleRayToggle()
    {
        bool toggle = false;

        if (
            Keyboard.current != null &&
            Keyboard.current[visibleRayToggleKey].wasPressedThisFrame
        )
        {
            toggle = true;
        }

        if (
            enableGamepadVisibleRayToggle &&
            Gamepad.current != null &&
            Gamepad.current.selectButton.wasPressedThisFrame
        )
        {
            toggle = true;
        }

        if (
            genericVisibleRayToggleButton >= 0 &&
            WasGenericJoystickButtonPressed(
                genericVisibleRayToggleButton
            )
        )
        {
            toggle = true;
        }

        if (!toggle)
            return;

        visibleRayEnabled = !visibleRayEnabled;

        if (!visibleRayEnabled)
        {
            SetVisibleRayRenderer(false);
        }

        Debug.Log(
            "[Pistola] Raycast visível: " +
            (visibleRayEnabled ? "ATIVADO" : "DESATIVADO")
        );
    }


    void EnsureVisibleRayRenderer()
    {
        if (visibleRayRenderer != null)
            return;

        GameObject lineObject =
            new GameObject("VisibleRaycastLine");

        lineObject.transform.SetParent(
            transform,
            false
        );

        visibleRayRenderer =
            lineObject.AddComponent<LineRenderer>();

        visibleRayRenderer.useWorldSpace = true;
        visibleRayRenderer.positionCount = 2;
        visibleRayRenderer.startWidth = visibleRayWidth;
        visibleRayRenderer.endWidth = visibleRayWidth;
        visibleRayRenderer.numCapVertices = 2;
        visibleRayRenderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        visibleRayRenderer.receiveShadows = false;

        if (visibleRayMaterial != null)
        {
            visibleRayRenderer.material = visibleRayMaterial;
        }
        else
        {
            Shader shader =
                Shader.Find("Sprites/Default");

            if (shader == null)
            {
                shader =
                    Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                shader =
                    Shader.Find("Unlit/Color");
            }

            if (shader != null)
            {
                runtimeVisibleRayMaterial =
                    new Material(shader);

                visibleRayRenderer.material =
                    runtimeVisibleRayMaterial;
            }
        }
    }


    void SetVisibleRayRenderer(bool state)
    {
        if (visibleRayRenderer != null)
        {
            visibleRayRenderer.enabled = state;
        }
    }


    // ============================================================
    // HIT
    // ============================================================

    void ApplyHit(
        RaycastHit hit,
        Vector3 shotDirection
    )
    {
        if (logHits)
        {
            Debug.Log(
                "[Pistola] HIT -> " +
                hit.collider.name +
                " | Distância: " +
                hit.distance.ToString("F2") +
                " | Ponto: " +
                hit.point
            );
        }

        Rigidbody body =
            hit.rigidbody;

        if (body != null &&
            impactForce > 0f)
        {
            body.AddForceAtPosition(
                shotDirection * impactForce,
                hit.point,
                ForceMode.Impulse
            );
        }

        MonoBehaviour[] behaviours =
            hit.collider.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IRaycastDamageable damageable)
            {
                damageable.TakeRaycastDamage(
                    damage,
                    hit.point,
                    shotDirection
                );

                break;
            }
        }
    }


    // ============================================================
    // RAYCAST IGNORANDO O PRÓPRIO PERSONAGEM
    // ============================================================

    bool TryRaycastIgnoringOwner(
        Vector3 origin,
        Vector3 direction,
        float distance,
        out RaycastHit validHit
    )
    {
        validHit = default;

        RaycastHit[] hits =
            Physics.RaycastAll(
                origin,
                direction,
                distance,
                hitMask,
                triggerInteraction
            );

        if (hits == null ||
            hits.Length == 0)
        {
            return false;
        }

        Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            Transform hitTransform =
                hit.collider.transform;

            if (
                ownerRoot != null &&
                (
                    hitTransform == ownerRoot ||
                    hitTransform.IsChildOf(ownerRoot)
                )
            )
            {
                continue;
            }

            validHit = hit;
            return true;
        }

        return false;
    }


    // ============================================================
    // INPUT
    // ============================================================

    bool ReadAimHeld()
    {
        if (
            enableMouseAim &&
            Mouse.current != null &&
            Mouse.current.rightButton.isPressed
        )
        {
            return true;
        }

        if (!enableControllerAim)
            return false;

        if (
            Gamepad.current != null &&
            Gamepad.current.leftTrigger.ReadValue() > 0.35f
        )
        {
            return true;
        }

        foreach (InputDevice device in InputSystem.devices)
        {
            if (!(device is Joystick joystick))
                continue;

            ButtonControl button =
                GetGenericJoystickButton(
                    joystick,
                    genericAimButton
                );

            if (button != null && button.isPressed)
            {
                return true;
            }
        }

        return false;
    }


    bool WasGenericJoystickButtonPressed(
        int buttonIndex
    )
    {
        if (buttonIndex < 0)
            return false;

        foreach (InputDevice device in InputSystem.devices)
        {
            if (!(device is Joystick joystick))
                continue;

            ButtonControl button =
                GetGenericJoystickButton(
                    joystick,
                    buttonIndex
                );

            if (
                button != null &&
                button.wasPressedThisFrame
            )
            {
                return true;
            }
        }

        return false;
    }


    bool ReadFirePressed()
    {
        if (
            enableMouseFire &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame
        )
        {
            return true;
        }

        if (!enableControllerFire)
            return false;

        if (
            Gamepad.current != null &&
            Gamepad.current.rightTrigger.wasPressedThisFrame
        )
        {
            return true;
        }

        foreach (InputDevice device in InputSystem.devices)
        {
            if (!(device is Joystick joystick))
                continue;

            ButtonControl button =
                GetGenericJoystickButton(
                    joystick,
                    genericFireButton
                );

            if (
                button != null &&
                button.wasPressedThisFrame
            )
            {
                return true;
            }
        }

        return false;
    }


    ButtonControl GetGenericJoystickButton(
        Joystick joystick,
        int buttonIndex
    )
    {
        if (joystick == null ||
            buttonIndex < 0)
        {
            return null;
        }

        int currentIndex = 0;

        foreach (InputControl control in joystick.allControls)
        {
            if (!(control is ButtonControl button))
                continue;

            if (currentIndex == buttonIndex)
                return button;

            currentIndex++;
        }

        return null;
    }

    void OnDisable()
    {
        SetAiming(false);
        SetVisibleRayRenderer(false);
        ResetWeaponMechanismPose();
        weaponMechanismAnimating = false;
    }


    void OnDestroy()
    {
        if (runtimeVisibleRayMaterial != null)
        {
            Destroy(runtimeVisibleRayMaterial);
        }
    }

}
