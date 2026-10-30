using System;
using System.Collections.Generic;
using UnityEngine;

namespace FinalSpam.Enemies
{
    /// <summary>
    /// Anima os ossos dos inimigos em tempo real, sem AnimationClips ou AnimatorController.
    /// Nao move o GameObject principal: a locomocao futura fica com a IA/NavMesh.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(120)]
    [AddComponentMenu("FinalSpam/Inimigos/Animador Procedural")]
    public sealed class EnemyProceduralAnimator : MonoBehaviour
    {
        public enum EnemyKind { TrojanSoldier, TowerVirus, DdosPlane }
        public enum BoneAxis { X, Y, Z }

        // Opcoes independentes para HAND_R (espada) e HAND_L (escudo).
        // O espaco escolhido determina qual Transform fornece o eixo.
        public enum TrojanArmAxis { PositiveX, NegativeX, PositiveY, NegativeY, PositiveZ, NegativeZ }
        public enum TrojanArmSpace { Character, BoneLocal, ParentLocal }

        [Header("Identidade")]
        public EnemyKind enemyKind = EnemyKind.TrojanSoldier;
        [Tooltip("Raiz do modelo FBX. Vazio = este GameObject.")]
        public Transform modelRoot;

        [Header("Movimento (preparado para IA/NavMesh)")]
        [Tooltip("Detecta movimento do GameObject. A animacao nao altera o deslocamento fisico.")]
        public bool autoDetectMovement = true;
        [Min(0.01f)] public float speedForFullWalk = 3.5f;
        [Min(0f)] public float minimumMovementSpeed = 0.08f;
        [Min(0.1f)] public float movementBlendSpeed = 8f;
        [Min(0.1f)] public float walkCycleFrequency = 1.8f;

        [Header("Reacoes")]
        [Min(0.1f)] public float attackDuration = 0.65f;
        [Min(0.1f)] public float defenseDuration = 1.25f;
        [Min(0.1f)] public float shieldRaiseSpeed = 9f;
        [Min(0.1f)] public float hitDuration = 0.32f;
        [Min(0.1f)] public float deathDuration = 0.9f;

        [Header("Teste durante Play")]
        [Tooltip("Simula caminhada sem mover a posicao. Desative quando integrar com IA.")]
        public bool previewWalking;
        [Tooltip("Repete o ataque automaticamente so para testar a animacao.")]
        public bool previewRepeatedAttacks;
        [Tooltip("Mantem escudo levantado durante Play; desligado por padrao.")]
        public bool previewHoldDefense;
        [Min(0.6f)] public float previewAttackInterval = 2.5f;

        [Header("Troiano - bones reais do FBX")]
        public Transform trojanRoot;
        public Transform trojanBody;
        public Transform trojanLeftArm;   // HAND_L: escudo
        public Transform trojanRightArm;  // HAND_R: espada
        public Transform trojanLeftLeg;   // FEET_L
        public Transform trojanRightLeg;  // FEET_R
        [Range(0f, 65f)] public float trojanWalkSwing = 32f;
        [Range(0f, 20f)] public float trojanBodyBob = 3f;
        [Range(0f, 140f)] public float trojanSwordSwing = 100f;
        [Header("Troiano - amplitude do ataque")]
        [Range(0.20f, 1f)]
        [Tooltip("Multiplica o angulo do golpe sem mudar o eixo escolhido da espada. 0.50 = metade do arco original.")]
        public float trojanSwordAttackArc = 0.50f;
        [Header("Troiano - defesa com escudo")]
        [Tooltip("Angulos da defesa em eixos do personagem: X = inclinar frente/tras, Y = giro horizontal, Z = inclinacao lateral. Os eixos locais dos bones FBX sao ignorados.")]
        public Vector3 trojanShieldGuardEuler = new Vector3(-62f, -8f, 18f);
        [Tooltip("Angulos da espada em eixos do personagem, em vez dos eixos locais do HAND_R.")]
        public Vector3 trojanSwordReadyEuler = new Vector3(-12f, 0f, -8f);
        [Header("Troiano - bloqueio em duas etapas")]
        [Range(0.18f, 1.50f)]
        [Tooltip("Tempo para completar: primeiro estende o escudo para frente, depois cobre o peito.")]
        public float trojanShieldTransitionTime = 0.46f;
        [Range(0.20f, 0.80f)]
        [Tooltip("Parte da animacao dedicada a avancar o escudo antes de fecha-lo na frente do corpo.")]
        public float trojanShieldReachPhase = 0.44f;
        [Range(-75f, 75f)]
        [Tooltip("Fase final: giro para trazer o escudo do lado esquerdo ate o centro do torso. Inverta o sinal se fechar para o lado errado.")]
        [HideInInspector] public float trojanShieldCoverYaw = 28f;
        [Range(0f, 70f)]
        [Tooltip("Rotacao adicional para levar a malha do escudo para FRENTE na etapa 1. O sentido e detectado automaticamente a partir da geometria.")]
        [HideInInspector] public float trojanShieldForwardReachAngle = 58f;
        [Range(-55f, 55f)]
        [Tooltip("Inclinacao extra para trazer a borda do escudo em direcao ao centro do peito, sem deslocar o pivot do braco.")]
        [HideInInspector] public float trojanShieldCoverRoll = 24f;
        [Range(0f, 0.50f)]
        [Tooltip("Distancia EXATA que o braco esquerdo avanca na primeira etapa (metros).")]
        public float trojanShieldForwardPush = 0.322f;
        [Range(0f, 0.50f)]
        [Tooltip("Distancia EXATA que o braco esquerdo se move para a direita na segunda etapa (metros).")]
        public float trojanShieldRightPush = 0.306f;
        // Campos antigos mantidos para nao quebrar a serializacao de
        // prefabs/cenas. Nao influenciam mais os movimentos.
        [HideInInspector] public float trojanShieldCoverLift = 0.12f;
        [HideInInspector] public float trojanShieldCoverInward = 0.045f;

        [Header("Troiano - editor de eixos dos bracos")]
        [Tooltip("Eixo PRINCIPAL do movimento da espada (HAND_R).")]
        public TrojanArmAxis trojanSwordAxis = TrojanArmAxis.PositiveX;
        [Tooltip("Onde o eixo da espada e interpretado: personagem, osso, ou pai do osso.")]
        public TrojanArmSpace trojanSwordAxisSpace = TrojanArmSpace.Character;
        [Tooltip("Eixo PRINCIPAL do movimento do escudo (HAND_L).")]
        public TrojanArmAxis trojanShieldAxis = TrojanArmAxis.PositiveX;
        [Tooltip("Onde o eixo do escudo e interpretado: personagem, osso, ou pai do osso.")]
        public TrojanArmSpace trojanShieldAxisSpace = TrojanArmSpace.Character;
        [Range(0f, 1f)]
        [Tooltip("0 = girar somente no eixo escolhido; 1 = aplicar tambem inclinacao diagonal Y/Z das animacoes anteriores.")]
        public float trojanSecondaryAxisWeight = 0f;

        [Header("Troiano - laboratorio de eixos (Play)")]
        [Tooltip("Substitui temporariamente as animacoes dos bracos por dois angulos manuais. Nao altera as posicoes da espada e do escudo.")]
        public bool previewTrojanAxes;
        [Range(-180f, 180f)] public float previewSwordAxisAngle;
        [Range(-180f, 180f)] public float previewShieldAxisAngle;

        [Header("Virus Torre - seis pernas, 3 pares")]
        public Transform virusRoot;
        public Transform virusBody;
        [Tooltip("Ordem: L1, L2, L3, R1, R2, R3.")]
        public Transform[] virusUpperLegs = new Transform[6];
        [Tooltip("Ordem: L1, L2, L3, R1, R2, R3.")]
        public Transform[] virusLowerLegs = new Transform[6];
        [Range(0f, 60f)] public float virusUpperSwing = 24f;
        [Range(0f, 60f)] public float virusLowerBend = 20f;
        [Range(0f, 15f)] public float virusBodyTilt = 5f;

        [Header("Aviao DDoS - voo / turbina")]
        public Transform planeBody;
        public Transform planeTurbine;
        public BoneAxis turbineSpinAxis = BoneAxis.Y;
        [Min(0f)] public float turbineDegreesPerSecond = 1000f;
        [Range(0f, 45f)] public float planeBankAngle = 12f;
        [Range(0f, 20f)] public float planePitchAngle = 5f;
        [Range(0f, 1f)] public float planeHoverHeight = 0.13f;

        [Header("Aviao DDoS - pontos de disparo e bomba")]
        [Tooltip("Origem do tiro da torreta ESQUERDA (esquerda do proprio aviao). " +
            "O eixo azul Z/forward representa a direcao inicial de tiro.")]
        public Transform ddosMuzzleLeft;
        [Tooltip("Origem do tiro da torreta DIREITA (direita do proprio aviao).")]
        public Transform ddosMuzzleRight;
        [Tooltip("Local exato de onde a Mini Nuke sera solta. " +
            "O eixo azul Z/forward aponta para a direcao de queda.")]
        public Transform ddosMiniNukeDropPoint;

        public bool IsDead { get { return dead; } }
        public float MovementBlend { get { return movementBlend; } }
        public bool IsAttacking { get { return !dead && attackElapsed >= 0f && attackElapsed < attackDuration; } }
        public bool IsDefending { get { return !dead &&
            (holdDefense || previewHoldDefense || (defenseElapsed >= 0f && defenseElapsed < defenseDuration)); } }
        public float DefenseBlend { get { return defenseBlend; } }

        private struct RestPose
        {
            public Quaternion rotation;
            public Vector3 position;
            public RestPose(Transform transform)
            {
                rotation = transform.localRotation;
                position = transform.localPosition;
            }
        }

        private readonly Dictionary<Transform, RestPose> rest = new Dictionary<Transform, RestPose>(24);
        private Vector3 previousPosition;
        private Vector3 previousVelocity;
        private float movementBlend;
        private float cycle;
        private float attackElapsed = -1f;
        private float defenseElapsed = -1f;
        private float defenseBlend;
        private bool holdDefense;
        private float hitElapsed = -1f;
        private float deathElapsed;
        private float previewTimer;
        private float turbineDegrees;
        private bool dead;
        private bool initialized;
        private void Awake() { Initialize(); }

        private void OnEnable()
        {
            if (!initialized) Initialize();
            previousPosition = transform.position;
        }

        private void Initialize()
        {
            if (modelRoot == null) modelRoot = transform;
            AutoAssignBones();

            rest.Clear();
            Store(trojanRoot, trojanBody, trojanLeftArm, trojanRightArm, trojanLeftLeg, trojanRightLeg);
            Store(virusRoot, virusBody);
            Store(planeBody, planeTurbine);
            if (virusUpperLegs != null) Store(virusUpperLegs);
            if (virusLowerLegs != null) Store(virusLowerLegs);

            previousPosition = transform.position;
            initialized = true;
        }

        // Apenas a Mao ESQUERDA: alcance e fechamento sao posicoes
        // deterministicas relativas a pose original, sem usar pivot do FBX,
        // sem rolar a mao em outros tres eixos e sem acumular rotacoes.
        // A mao direita/espada nao e alterada por esta funcao.
        private void ApplyTrojanShieldGuardPose(float reach, float cover, float death)
        {
            if (trojanLeftArm == null) return;
            RestPose pose;
            if (!rest.TryGetValue(trojanLeftArm, out pose)) return;

            Vector3 offsetWorld = Vector3.zero;
            if (!previewTrojanAxes && !dead && death < 1f)
            {
                // Fase 1: o antebraco avanca para fora do torso.
                float forward = Mathf.Max(0f, trojanShieldForwardPush) *
                    reach * (1f - death);
                // Fase 2: DESLOCAMENTO DIRETO para a direita, sem somar
                // ou subtrair o antigo offset de centralizacao.
                // 0.075 = 7.5 cm, exatamente o movimento da ultima versao.
                float rightward = Mathf.Max(0f, trojanShieldRightPush) *
                    cover * (1f - death);

                offsetWorld = transform.forward * forward +
                    transform.right * rightward;
            }

            // Posicao restabelecida a cada frame: sem deriva acumulada.
            trojanLeftArm.localPosition = pose.position +
                (trojanLeftArm.parent != null
                    ? trojanLeftArm.parent.InverseTransformVector(offsetWorld)
                    : offsetWorld);
        }

        private void Store(params Transform[] bones)
        {
            if (bones == null) return;
            foreach (Transform bone in bones)
            {
                if (bone != null && !rest.ContainsKey(bone))
                    rest.Add(bone, new RestPose(bone));
            }
        }

        /// <summary>Preenche referencias pelo nome dos ossos do FBX. Nao precisa de rig Humanoid.</summary>
        public void AutoAssignBones()
        {
            if (modelRoot == null) modelRoot = transform;

            if (enemyKind == EnemyKind.TrojanSoldier)
            {
                if (trojanRoot == null) trojanRoot = FindBone("ROOT");
                if (trojanBody == null) trojanBody = FindBone("BODY");
                if (trojanLeftArm == null) trojanLeftArm = FindBone("HAND_L");
                if (trojanRightArm == null) trojanRightArm = FindBone("HAND_R");
                if (trojanLeftLeg == null) trojanLeftLeg = FindBone("FEET_L");
                if (trojanRightLeg == null) trojanRightLeg = FindBone("FEET_R");
            }
            else if (enemyKind == EnemyKind.TowerVirus)
            {
                if (virusRoot == null) virusRoot = FindBone("ROOT_BONE");
                if (virusBody == null) virusBody = FindBone("BODY");
                if (virusUpperLegs == null || virusUpperLegs.Length != 6)
                    Array.Resize(ref virusUpperLegs, 6);
                if (virusLowerLegs == null || virusLowerLegs.Length != 6)
                    Array.Resize(ref virusLowerLegs, 6);

                for (int side = 0; side < 2; side++)
                {
                    string suffix = side == 0 ? "_L" : "_R";
                    for (int leg = 1; leg <= 3; leg++)
                    {
                        int index = side * 3 + leg - 1;
                        if (virusUpperLegs[index] == null)
                            virusUpperLegs[index] = FindBone("UPPER_LEG" + leg + suffix);
                        if (virusLowerLegs[index] == null)
                            virusLowerLegs[index] = FindBone("LOWER_LEG" + leg + suffix);
                    }
                }
            }
            else if (enemyKind == EnemyKind.DdosPlane)
            {
                if (planeBody == null) planeBody = FindBone("BODY");
                if (planeTurbine == null) planeTurbine = FindBone("Turbina");
            }
        }

        private Transform FindBone(string name)
        {
            if (modelRoot == null) return null;
            foreach (Transform t in modelRoot.GetComponentsInChildren<Transform>(true))
            {
                string candidate = t.name;
                int namespaceEnd = candidate.LastIndexOf(':');
                if (namespaceEnd >= 0) candidate = candidate.Substring(namespaceEnd + 1);
                if (string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase))
                    return t;
            }
            return null;
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private void SetRotation(Transform bone, float x, float y, float z)
        {
            RestPose pose;
            if (bone == null || !rest.TryGetValue(bone, out pose)) return;
            bone.localRotation = pose.rotation * Quaternion.Euler(x, y, z);
        }

        // Converter um eixo X/Y/Z assinado em direcao no espaco escolhido.
        // Isso permite selecionar os eixos REAIS do novo FBX no Inspector,
        // sem precisar mudar o encaixe da espada ou do escudo.
        private static Vector3 TrojanAxisVector(TrojanArmAxis axis)
        {
            switch (axis)
            {
                case TrojanArmAxis.NegativeX: return Vector3.left;
                case TrojanArmAxis.PositiveY: return Vector3.up;
                case TrojanArmAxis.NegativeY: return Vector3.down;
                case TrojanArmAxis.PositiveZ: return Vector3.forward;
                case TrojanArmAxis.NegativeZ: return Vector3.back;
                default: return Vector3.right;
            }
        }

        // Pitch e a oscilacao PRINCIPAL; Y/Z sao opcionais (peso 0 padrao).
        // Character: eixos do soldado. ParentLocal: eixos de BODY/pai do osso.
        // BoneLocal: eixos locais do proprio HAND_R/HAND_L na POSE ORIGINAL.
        private void SetTrojanArmRotation(
            Transform arm, float pitch, float yaw, float roll,
            TrojanArmAxis axis, TrojanArmSpace space)
        {
            RestPose pose;
            if (arm == null || !rest.TryGetValue(arm, out pose)) return;

            Quaternion parentWorld = arm.parent != null
                ? arm.parent.rotation : Quaternion.identity;
            Vector3 motionAxis = TrojanAxisVector(axis);
            Quaternion delta = Quaternion.AngleAxis(pitch, motionAxis);

            switch (space)
            {
                case TrojanArmSpace.BoneLocal:
                    arm.localRotation = pose.rotation * delta;
                    break;
                case TrojanArmSpace.ParentLocal:
                    arm.localRotation = delta * pose.rotation;
                    break;
                default:
                    arm.rotation = transform.rotation * delta *
                        Quaternion.Inverse(transform.rotation) *
                        (parentWorld * pose.rotation);
                    break;
            }

            // Opcional: dar diagonalidade ao golpe somente quando solicitado.
            float weight = Mathf.Clamp01(trojanSecondaryAxisWeight);
            if (weight > 0.0001f)
            {
                Quaternion extra =
                    Quaternion.AngleAxis(yaw * weight, transform.up) *
                    Quaternion.AngleAxis(roll * weight, transform.forward);
                arm.rotation = extra * arm.rotation;
            }
        }

        private void SetPosition(Transform bone, Vector3 offset)
        {
            RestPose pose;
            if (bone == null || !rest.TryGetValue(bone, out pose)) return;
            bone.localPosition = pose.position + offset;
        }

        private void LateUpdate()
        {
            if (!initialized) Initialize();
            float dt = Mathf.Max(Time.deltaTime, 0.00001f);

            Vector3 displacement = transform.position - previousPosition;
            previousPosition = transform.position;
            Vector3 velocity = displacement / dt;
            // Teleportes e reposicionamentos nao devem tocar a animacao de corrida.
            if (displacement.sqrMagnitude > 4f) velocity = Vector3.zero;
            previousVelocity = Vector3.Lerp(previousVelocity, velocity, Mathf.Clamp01(dt * 8f));

            float horizontalSpeed = new Vector2(previousVelocity.x, previousVelocity.z).magnitude;
            float targetBlend = autoDetectMovement && horizontalSpeed > minimumMovementSpeed
                ? Mathf.Clamp01(horizontalSpeed / Mathf.Max(0.01f, speedForFullWalk))
                : 0f;
            if (previewWalking && !dead) targetBlend = 1f;
            if (dead) targetBlend = 0f;
            movementBlend = Mathf.MoveTowards(movementBlend, targetBlend, dt * movementBlendSpeed);

            if (!dead)
            {
                cycle += dt * walkCycleFrequency * 2f * Mathf.PI
                    * Mathf.Lerp(0.35f, 1.5f, movementBlend);
            }
            if (attackElapsed >= 0f) attackElapsed += dt;
            if (attackElapsed > attackDuration) attackElapsed = -1f;
            if (defenseElapsed >= 0f) defenseElapsed += dt;
            if (defenseElapsed > defenseDuration) defenseElapsed = -1f;
            if (enemyKind == EnemyKind.TrojanSoldier)
            {
                // A defesa precisa de tempo visivel para mostrar as duas fases:
                // estender primeiro, fechar o escudo sobre o peito em seguida.
                float rate = Mathf.Min(
                    Mathf.Max(0.1f, shieldRaiseSpeed),
                    1f / Mathf.Max(0.18f, trojanShieldTransitionTime));
                defenseBlend = Mathf.MoveTowards(defenseBlend,
                    IsDefending ? 1f : 0f, dt * rate);
            }
            if (hitElapsed >= 0f) hitElapsed += dt;
            if (hitElapsed > hitDuration) hitElapsed = -1f;
            if (dead) deathElapsed += dt;

            if (previewRepeatedAttacks && !dead)
            {
                previewTimer += dt;
                if (previewTimer >= previewAttackInterval)
                {
                    previewTimer = 0f;
                    TriggerAttack();
                }
            }

            switch (enemyKind)
            {
                case EnemyKind.TrojanSoldier: AnimateTrojan(); break;
                case EnemyKind.TowerVirus: AnimateVirus(); break;
                case EnemyKind.DdosPlane: AnimatePlane(dt); break;
            }
        }

        private float AttackPulse()
        {
            if (!IsAttacking) return 0f;
            float t = attackElapsed / Mathf.Max(0.01f, attackDuration);
            // Preparacao, golpe rapido, recuperacao.
            if (t < 0.24f) return -0.30f * Smooth01(t / 0.24f);
            if (t < 0.48f) return Mathf.Lerp(-0.30f, 1f, Smooth01((t - 0.24f) / 0.24f));
            return 1f - Smooth01((t - 0.48f) / 0.52f);
        }

        private float HitPulse()
        {
            if (hitElapsed < 0f || hitDuration <= 0f) return 0f;
            float t = Mathf.Clamp01(hitElapsed / hitDuration);
            return Mathf.Sin(t * Mathf.PI) * (1f - t * 0.35f);
        }

        private float DeathBlend()
        {
            return dead ? Smooth01(deathElapsed / Mathf.Max(0.1f, deathDuration)) : 0f;
        }

        private void AnimateTrojan()
        {
            // Ossos: HAND_R = espada, HAND_L = escudo.
            // Ambos permanecem ancorados nos respectivos ossos, inclusive
            // durante caminhada, golpe, defesa e recuperacao.
            float moving = movementBlend;
            float step = Mathf.Sin(cycle) * moving;
            float bob = Mathf.Abs(Mathf.Sin(cycle)) * moving;
            float breath = Mathf.Sin(Time.time * 1.75f);
            float attack = AttackPulse(); // antecipacao -> golpe -> retorno
            float guarding = defenseBlend;
            float hit = HitPulse();
            float death = DeathBlend();

            SetRotation(trojanRoot, -70f * death, 0f, 8f * death);
            SetRotation(trojanBody,
                (3f * breath + trojanBodyBob * bob - 13f * hit)
                    * (1f - death),
                1.6f * Mathf.Sin(cycle) * moving
                    * (1f - death),
                (2f * Mathf.Sin(cycle * 0.5f) - 9f * hit)
                    * (1f - death));
            SetPosition(trojanBody, new Vector3(0f, bob * 0.025f, 0f));

            SetRotation(trojanLeftLeg,
                Mathf.Lerp(-step * trojanWalkSwing, 15f, death), 0f, 0f);
            SetRotation(trojanRightLeg,
                Mathf.Lerp(step * trojanWalkSwing, -15f, death), 0f, 0f);

            // ATAQUE NAO MEXE NO ESCUDO. Nada de misturar AttackPulse
            // com a defesa. O escudo so entra em guarda por TriggerDefense
            // ou SetDefense. O torso tambem nao e rotacionado pelo ataque.
            float shield = guarding;
            float phase = Mathf.Clamp(trojanShieldReachPhase, 0.20f, 0.80f);
            float shieldReach = Smooth01(Mathf.Clamp01(shield / phase));
            float shieldCover = Smooth01(Mathf.Clamp01(
                (shield - phase) / Mathf.Max(0.001f, 1f - phase)));

            // Camada 1: avancar girando no eixo principal ja acertado.
            // Camada 2: parar de girar e deslizar para a esquerda
            // (ApplyTrojanShieldGuardPose). Nunca levantar o braco novamente.
            float shieldWalk = IsAttacking ? 0f :
                step * trojanWalkSwing * 0.12f * (1f - shield);
            float shieldPitch = Mathf.Lerp(
                shieldWalk + trojanShieldGuardEuler.x * (0.48f * shieldReach),
                35f, death);

            // Reduz pela metade o arco do corte, mantendo o eixo, o sinal
            // e o referencial que o usuario definiu no seletor.
            float swordWalk = -step * trojanWalkSwing * 0.32f *
                (1f - Mathf.Clamp01(Mathf.Abs(attack)));
            float swordPitch = Mathf.Lerp(
                trojanSwordReadyEuler.x +
                    trojanSwordSwing * trojanSwordAttackArc * attack + swordWalk,
                25f, death);

            // Laboratorio de eixos: esta previa continua isolada,
            // sem fechamento automatico do escudo nem reducao de amplitude.
            if (previewTrojanAxes && !dead)
            {
                swordPitch = previewSwordAxisAngle;
                shieldPitch = previewShieldAxisAngle;
            }

            SetTrojanArmRotation(trojanLeftArm,
                shieldPitch,
                previewTrojanAxes ? 0f : trojanShieldGuardEuler.y * shield * (1f - death),
                previewTrojanAxes ? 0f : trojanShieldGuardEuler.z * shield * (1f - death),
                trojanShieldAxis, trojanShieldAxisSpace);

            // Apenas alcance/centralizacao suaves do braco esquerdo.
            // Sem rotacoes extras automaticas que distorcem o escudo.
            ApplyTrojanShieldGuardPose(shieldReach, shieldCover, death);

            SetTrojanArmRotation(trojanRightArm,
                swordPitch,
                previewTrojanAxes ? 0f : (trojanSwordReadyEuler.y + 6f * attack) * (1f - death),
                previewTrojanAxes ? 0f : (trojanSwordReadyEuler.z - 16f * attack) * (1f - death),
                trojanSwordAxis, trojanSwordAxisSpace);
        }

        private void AnimateVirus()
        {
            float moving = movementBlend;
            float pulse = Mathf.Sin(Time.time * 2.0f);
            float attack = AttackPulse();
            float hit = HitPulse();
            float death = DeathBlend();
            float bob = (Mathf.Sin(cycle * 2f) * 0.035f * moving) + pulse * 0.015f;

            SetRotation(virusRoot, -65f * death, 0f, 15f * death);
            SetRotation(virusBody,
                (virusBodyTilt * pulse * 0.30f + 12f * attack - 15f * hit) * (1f - death),
                (2f * Mathf.Sin(cycle * 0.5f)) * (1f - death),
                (virusBodyTilt * Mathf.Sin(cycle) * moving + 6f * hit) * (1f - death));
            SetPosition(virusBody, new Vector3(0f, bob * (1f - death), 0f));

            if (virusUpperLegs == null || virusLowerLegs == null) return;

            for (int i = 0; i < Mathf.Min(6, virusUpperLegs.Length, virusLowerLegs.Length); i++)
            {
                int pair = i % 3;
                bool left = i < 3;
                // Marcha em tripode: L1+L3+R2 opostos a L2+R1+R3.
                float phase = ((pair == 1) ^ left) ? Mathf.PI : 0f;
                float stride = Mathf.Sin(cycle + phase);
                float lift = Mathf.Max(0f, Mathf.Cos(cycle + phase));
                float outward = left ? -1f : 1f;

                SetRotation(virusUpperLegs[i],
                    (stride * virusUpperSwing * moving + 18f * attack) * (1f - death),
                    0f,
                    (lift * 7f * moving + 10f * death) * outward);
                SetRotation(virusLowerLegs[i],
                    (lift * virusLowerBend * moving + 14f * attack) * (1f - death),
                    0f,
                    -outward * 9f * death);
            }
        }

        private void AnimatePlane(float dt)
        {
            // O DDoS nao reutiliza golpe/animacao de espada do soldado.
            // Futuros disparos e bombas usam os hardpoints, sem inclinar
            // o aviao ao chamar TriggerAttack().
            float hit = HitPulse();
            float death = DeathBlend();
            float sway = Mathf.Sin(Time.time * 1.4f);
            float bankDirection = Vector3.Dot(transform.right, previousVelocity);
            bankDirection = Mathf.Clamp(bankDirection / Mathf.Max(speedForFullWalk, 0.1f), -1f, 1f);
            float bank = planeBankAngle * (bankDirection + 0.24f * sway);

            SetRotation(planeBody,
                (planePitchAngle * Mathf.Sin(Time.time * 1.1f) + 12f * hit)
                    * (1f - death),
                0f,
                Mathf.Lerp(bank + 9f * hit, 80f, death));
            SetPosition(planeBody, new Vector3(0f,
                planeHoverHeight * Mathf.Sin(Time.time * 1.7f) * (1f - death)
                    - 0.30f * death, 0f));

            turbineDegrees = Mathf.Repeat(turbineDegrees + dt *
                turbineDegreesPerSecond * (1f - death), 360f);
            Vector3 spinAxis = turbineSpinAxis == BoneAxis.X
                ? Vector3.right : turbineSpinAxis == BoneAxis.Y ? Vector3.up : Vector3.forward;
            RestPose pose;
            if (planeTurbine != null && rest.TryGetValue(planeTurbine, out pose))
                planeTurbine.localRotation = pose.rotation *
                    Quaternion.AngleAxis(turbineDegrees, spinAxis);
        }

        // Conexoes de origem para o sistema futuro de disparo/drop:
        // a IA pode ler os transforms e calcular direcao ate o jogador.
        // Na cena, os gizmos mostram onde cada tiro e bomba nascem.
        private void OnDrawGizmosSelected()
        {
            if (enemyKind != EnemyKind.DdosPlane) return;
            DrawDdosHardpoint(ddosMuzzleLeft, new Color(0.1f, 0.9f, 1f), "TORRETA E");
            DrawDdosHardpoint(ddosMuzzleRight, new Color(1f, 0.5f, 0.1f), "TORRETA D");
            DrawDdosHardpoint(ddosMiniNukeDropPoint, new Color(1f, 0.25f, 0.3f),
                "MINI NUKE");
        }

        private static void DrawDdosHardpoint(Transform point, Color color, string caption)
        {
            if (point == null) return;
            Gizmos.color = color;
            Gizmos.DrawWireSphere(point.position, 0.075f);
            Vector3 tip = point.position + point.forward * 0.45f;
            Gizmos.DrawLine(point.position, tip);
            Vector3 side = point.right * 0.07f;
            Gizmos.DrawLine(tip, tip - point.forward * 0.12f + side);
            Gizmos.DrawLine(tip, tip - point.forward * 0.12f - side);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(point.position + point.up * 0.12f, caption);
#endif
        }

        /// <summary>Integrar com a IA quando o inimigo comecar um ataque.</summary>
        [ContextMenu("FinalSpam/Testar ataque (Play)")]
        public void TriggerAttack()
        {
            if (dead) return;
            attackElapsed = 0f;
        }

        /// <summary>Levanta o escudo por defenseDuration segundos. A IA pode chamar no inicio do bloqueio.</summary>
        [ContextMenu("FinalSpam/Testar defesa (Play)")]
        public void TriggerDefense()
        {
            if (dead || enemyKind != EnemyKind.TrojanSoldier) return;
            defenseElapsed = 0f;
        }

        /// <summary>Protecao continua; a IA deve chamar false ao terminar.</summary>
        public void SetDefense(bool enabled)
        {
            if (dead || enemyKind != EnemyKind.TrojanSoldier) return;
            holdDefense = enabled;
            if (enabled) defenseElapsed = -1f;
        }

        /// <summary>Integrar com o sistema de vida ao receber um disparo.</summary>
        [ContextMenu("FinalSpam/Testar dano (Play)")]
        public void TriggerHit()
        {
            if (dead) return;
            hitElapsed = 0f;
        }

        /// <summary>Pose de derrota; destruicao e loot serao tratados pela IA/vida futuramente.</summary>
        [ContextMenu("FinalSpam/Testar morte (Play)")]
        public void TriggerDeath()
        {
            if (dead) return;
            dead = true;
            deathElapsed = 0f;
            attackElapsed = -1f;
            defenseElapsed = -1f;
            defenseBlend = 0f;
            holdDefense = false;
            previewHoldDefense = false;
            hitElapsed = -1f;
        }

        [ContextMenu("FinalSpam/Recuperar pose (Play)")]
        public void ResetAnimationState()
        {
            dead = false;
            deathElapsed = 0f;
            attackElapsed = -1f;
            defenseElapsed = -1f;
            defenseBlend = 0f;
            holdDefense = false;
            previewHoldDefense = false;
            hitElapsed = -1f;
            previewTimer = 0f;
            foreach (KeyValuePair<Transform, RestPose> pair in rest)
            {
                if (pair.Key == null) continue;
                pair.Key.localRotation = pair.Value.rotation;
                pair.Key.localPosition = pair.Value.position;
            }
        }

        private void OnDisable()
        {
            if (!initialized) return;
            foreach (KeyValuePair<Transform, RestPose> pair in rest)
            {
                if (pair.Key == null) continue;
                pair.Key.localRotation = pair.Value.rotation;
                pair.Key.localPosition = pair.Value.position;
            }
        }
    }
}
