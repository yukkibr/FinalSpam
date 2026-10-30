using UnityEngine;

[RequireComponent(typeof(ThirdPersonController))]
[DefaultExecutionOrder(80)]
public class ProceduralCharacterAnimator : MonoBehaviour
{
    // ============================================================
    // CONTROLLER
    // ============================================================

    private ThirdPersonController controller;


    // ============================================================
    // ARMATURE
    // ============================================================

    [Header("Armature")]

    public Transform armatureRoot;


    // ============================================================
    // BONES
    // ============================================================

    [Header("Bones")]

    public Transform hipRoot;

    public Transform thighLeft;
    public Transform legLeft;

    public Transform thighRight;
    public Transform legRight;

    public Transform spine;
    public Transform torso;

    public Transform armLeft;
    public Transform forearmLeft;

    public Transform armRight;
    public Transform forearmRight;

    public Transform head;


    // ============================================================
    // EIXOS
    // ============================================================

    [Header("Eixos dos Bones")]

    public RotationAxis legRotationAxis = RotationAxis.Z;

    // Eixo usado SOMENTE para o balanço dos braços durante
    // caminhada/corrida.
    //
    // Normalmente:
    // X = frente/trás
    //
    public RotationAxis armSwingRotationAxis = RotationAxis.X;

    // Eixo usado SOMENTE para levantar os braços durante
    // o pulo/queda.
    //
    // Normalmente:
    // Z = braços abrindo para as laterais.
    //
    public RotationAxis armJumpRotationAxis = RotationAxis.Z;

    public RotationAxis torsoRotationAxis = RotationAxis.X;


    // ============================================================
    // DIREÇÕES
    // ============================================================

    [Header("Direção individual dos Bones")]

    public float leftLegDirection = 1f;

    public float rightLegDirection = -1f;

    public float leftArmDirection = 1f;

    public float rightArmDirection = 1f;


    // ============================================================
    // CAMINHADA / CORRIDA
    // ============================================================

    [Header("Caminhada / Corrida")]

    public float walkCycleSpeed = 7f;

    public float runCycleSpeed = 11f;

    public float legSwing = 50f;

    public float armSwing = 42f;

    public float kneeBend = 10f;


    // ============================================================
    // JOELHOS
    // ============================================================

    [Header("Joelhos")]

    public float kneeLift = 8f;

    public float kneeReturnSpeed = 10f;


    // ============================================================
    // QUADRIL
    // ============================================================

    [Header("Quadril")]

    public float hipSway = 3f;

    public float hipBob = 2f;

    public float hipRotation = 2f;


    // ============================================================
    // TORSO
    // ============================================================

    [Header("Torso")]

    public float torsoLean = 8f;

    public float torsoMovement = 2f;

    public float spineMovement = 1.5f;


    // ============================================================
    // IDLE
    // ============================================================

    [Header("Idle / Respiração")]

    public float idleBreathingSpeed = 1.5f;

    public float idleBreathingAmount = 1.5f;

    public float idleHipMovement = 2f;


    // ============================================================
    // SALTO
    // ============================================================

    [Header("Salto")]

    public float jumpLegBend = 15f;

    // Quanto os braços sobem para as laterais durante o pulo.
    public float jumpArmLift = 35f;

    // Quanto os braços abrem para equilíbrio durante a queda.
    public float fallArmLift = 20f;

    public float fallLegBend = 10f;

    public float landingSquash = 10f;

    public float landingDuration = 0.18f;

    [Header("Double Jump / Cambalhota")]

    public float somersaultDegrees = 360f;
    public float somersaultBodyTuck = 28f;
    public float somersaultRecoverySpeed = 12f;


    // ============================================================
    // DETECÇÃO DE QUEDA
    // ============================================================

    [Header("Detecção de Queda")]

    // Velocidade vertical mínima para considerar que realmente
    // estamos caindo.
    //
    // Evita pequenas oscilações do CharacterController.
    public float fallVelocityThreshold = -1.0f;

    // Tempo mínimo fora do chão antes de considerar queda.
    //
    // Isso ajuda bastante com Terrain e pequenas irregularidades
    // do chão.
    public float fallGraceTime = 0.08f;

    // Tempo mínimo de pulo antes de permitir a animação de queda.
    public float jumpPhaseDuration = 0.08f;


    // ============================================================
    // NATAÇÃO
    // ============================================================

    [Header("Natação")]

    public float swimCycleSpeed = 5f;

    public float swimArmSwing = 55f;

    public float swimLegSwing = 20f;

    public float swimHipPitch = 45f;
    public float swimTorsoTilt = 30f;
    public float swimSpineTilt = 15f;
    public float swimForearmBend = 24f;
    public float swimHeadLift = 12f;

    public float swimIdleCycleMultiplier = 0.25f;
    public float swimVerticalBodyTilt = 10f;


    // ============================================================
    // PISTOLA / POSE DOS BRAÇOS
    // ============================================================

    [Header("Pistola / Pose dos Braços")]

    [Tooltip("Se ficar vazio, o script tenta encontrar uma RaycastPistol nos filhos do personagem.")]
    public RaycastPistol pistol;

    public bool autoFindPistol = true;

    [Tooltip("Offsets locais em graus, relativos à pose original dos bones.")]
    public Vector3 pistolRightArmEuler =
        new Vector3(-55f, 0f, -18f);

    public Vector3 pistolLeftArmEuler =
        new Vector3(-50f, 0f, 18f);

    public Vector3 pistolRightForearmEuler =
        new Vector3(-65f, 0f, 0f);

    public Vector3 pistolLeftForearmEuler =
        new Vector3(-75f, 0f, 0f);

    public Vector3 pistolTorsoEuler =
        new Vector3(-3f, 0f, 0f);

    [Header("Pistola / Primeira Pessoa")]

    [Tooltip("Na primeira pessoa, por padrão somente o braço direito é levantado para mostrar a arma.")]
    public Vector3 firstPersonPistolRightArmEuler =
        new Vector3(-78f, 4f, -10f);

    public Vector3 firstPersonPistolRightForearmEuler =
        new Vector3(-70f, 0f, 0f);

    public bool firstPersonUseLeftArm = false;

    public Vector3 firstPersonPistolLeftArmEuler =
        new Vector3(-58f, 0f, 16f);

    public Vector3 firstPersonPistolLeftForearmEuler =
        new Vector3(-72f, 0f, 0f);

    [Range(0f, 1f)]
    public float pistolAimPitchInfluence = 0.8f;

    [Range(0f, 1f)]
    public float pistolTorsoPitchInfluence = 0.3f;

    public float pistolPoseSmoothness = 18f;

    [Header("Pistola / Recuo")]

    public float pistolRecoilAngle = 8f;

    public float pistolRecoilRecoverySpeed = 18f;


    // ============================================================
    // POSE NEUTRA
    // ============================================================

    [Header("Pose Neutra")]

    public float leftArmRestRotation = 70f;

    public float rightArmRestRotation = -70f;

    public float leftForearmRestRotation = 0f;

    public float rightForearmRestRotation = 0f;


    // ============================================================
    // SUAVIZAÇÃO
    // ============================================================

    [Header("Suavização")]

    public float animationSmoothness = 10f;

    public float locomotionBlendSpeed = 8f;


    // ============================================================
    // DEBUG
    // ============================================================

    [Header("Debug")]

    public bool showDebugMessages = true;


    // ============================================================
    // ROTAÇÕES ORIGINAIS
    // ============================================================

    private Quaternion initialArmatureRootRotation;

    private Quaternion initialHipRotation;

    private Quaternion initialSpineRotation;
    private Quaternion initialTorsoRotation;

    private Quaternion initialThighLeftRotation;
    private Quaternion initialLegLeftRotation;

    private Quaternion initialThighRightRotation;
    private Quaternion initialLegRightRotation;

    private Quaternion initialArmLeftRotation;
    private Quaternion initialForearmLeftRotation;

    private Quaternion initialArmRightRotation;
    private Quaternion initialForearmRightRotation;

    private Quaternion initialHeadRotation;


    // ============================================================
    // POSES NEUTRAS
    // ============================================================

    private Quaternion neutralArmLeftRotation;
    private Quaternion neutralArmRightRotation;

    private Quaternion neutralForearmLeftRotation;
    private Quaternion neutralForearmRightRotation;


    // ============================================================
    // POSIÇÃO ORIGINAL DO QUADRIL
    // ============================================================

    private Vector3 initialHipLocalPosition;


    // ============================================================
    // ANIMAÇÃO
    // ============================================================

    private float animationTime;

    private float swimAnimationTime;

    private float currentAnimationIntensity;

    private float currentSpeed;

    private float previousSpeed;


    // ============================================================
    // ESTADO DO MOVIMENTO AÉREO
    // ============================================================

    private bool wasGrounded;

    private float airborneTimer;

    private float jumpTimer;

    private bool wasJumping;

    private bool wasFalling;


    // ============================================================
    // PISTOLA
    // ============================================================

    private float pistolRecoil;

    // O modelo novo possui HAND.R abaixo de FOREARM.R. A mira e calculada
    // a partir da geometria real dos ossos, nao de Euler fixo do modelo antigo.
    private Transform rightHand;
    private Quaternion initialPistolGripLocalRotation;

    // ============================================================
    // ATERRISSAGEM
    // ============================================================

    private float landingTimer;


    // ============================================================
    // AWAKE
    // ============================================================

    void Awake()
    {
        controller =
            GetComponent<ThirdPersonController>();

        if (pistol == null && autoFindPistol)
        {
            pistol =
                GetComponentInChildren<RaycastPistol>(true);
        }

        AutoFindBones();

        rightHand = armRight != null
            ? FindChildRecursive(armRight, "HAND.R")
            : null;

        if (pistol != null)
            initialPistolGripLocalRotation = pistol.transform.localRotation;

        SaveInitialPose();

        ValidateBones();

        wasGrounded =
            controller.IsGrounded;
    }


    // ============================================================
    // LATE UPDATE
    // ============================================================

    void LateUpdate()
    {
        if (controller == null)
            return;


        currentSpeed =
            controller.CurrentSpeed;


        if (!controller.IsDoubleJumping)
        {
            RestoreSomersaultPose();
        }


        bool grounded =
            controller.IsGrounded;


        float verticalVelocity =
            controller.VerticalVelocity;


        // ========================================================
        // CONTROLE DE TEMPO NO AR
        // ========================================================

        if (!grounded && !controller.IsUnderwater)
        {
            airborneTimer +=
                Time.deltaTime;
        }
        else
        {
            airborneTimer = 0f;
        }


        // ========================================================
        // DETECÇÃO DE ATERRISSAGEM
        // ========================================================

        if (
            grounded &&
            !wasGrounded &&
            !controller.IsUnderwater
        )
        {
            landingTimer =
                landingDuration;
        }


        wasGrounded =
            grounded;


        // ========================================================
        // TIMER DE ATERRISSAGEM
        // ========================================================

        if (landingTimer > 0f)
        {
            landingTimer -=
                Time.deltaTime;
        }


        // ========================================================
        // TIMER DO PULO
        // ========================================================

        if (
            !grounded &&
            !controller.IsUnderwater
        )
        {
            jumpTimer +=
                Time.deltaTime;
        }
        else
        {
            jumpTimer = 0f;
        }


        // ========================================================
        // DOUBLE JUMP / CAMBALHOTA
        // ========================================================

        if (controller.IsDoubleJumping)
        {
            ApplyDoubleJumpAnimation();
            previousSpeed = currentSpeed;
            return;
        }


        // ========================================================
        // NATAÇÃO
        // ========================================================

        if (controller.IsUnderwater)
        {
            ApplySwimmingAnimation();
            return;
        }


        // ========================================================
        // DETECÇÃO MELHORADA DE PULO
        // ========================================================

        bool isJumping =
            !grounded &&
            verticalVelocity > 0.1f &&
            jumpTimer >= 0f;


        // ========================================================
        // DETECÇÃO MELHORADA DE QUEDA
        // ========================================================

        bool isFalling =
            !grounded &&
            airborneTimer >= fallGraceTime &&
            jumpTimer >= jumpPhaseDuration &&
            verticalVelocity <= fallVelocityThreshold;


        // ========================================================
        // SALTO
        // ========================================================

        if (isJumping)
        {
            wasJumping = true;
            wasFalling = false;

            ApplyJumpAnimation();

            ApplyPistolPoseIfNeeded();

            previousSpeed =
                currentSpeed;

            return;
        }


        // ========================================================
        // QUEDA
        // ========================================================

        if (isFalling)
        {
            wasJumping = false;
            wasFalling = true;

            ApplyFallAnimation();

            ApplyPistolPoseIfNeeded();

            previousSpeed =
                currentSpeed;

            return;
        }


        // ========================================================
        // ATERRISSAGEM
        // ========================================================

        if (landingTimer > 0f)
        {
            ApplyLandingAnimation();

            ApplyPistolPoseIfNeeded();

            previousSpeed =
                currentSpeed;

            return;
        }


        // ========================================================
        // LOCOMOÇÃO
        // ========================================================

        if (controller.IsMoving)
        {
            ApplyLocomotionAnimation();
        }
        else
        {
            ApplyIdleAnimation();
        }


        ApplyPistolPoseIfNeeded();


        previousSpeed =
            currentSpeed;
    }


    // ============================================================
    // CAMINHADA / CORRIDA
    // ============================================================

    void ApplyLocomotionAnimation()
    {
        float normalizedSpeed =
            Mathf.Clamp01(
                currentSpeed /
                controller.runSpeed
            );


        // --------------------------------------------------------
        // TRANSIÇÃO DE INTENSIDADE
        // --------------------------------------------------------

        float targetIntensity =
            Mathf.InverseLerp(
                0f,
                controller.walkSpeed,
                currentSpeed
            );


        currentAnimationIntensity =
            Mathf.Lerp(
                currentAnimationIntensity,
                targetIntensity,
                locomotionBlendSpeed *
                Time.deltaTime
            );


        // --------------------------------------------------------
        // VELOCIDADE DO CICLO
        // --------------------------------------------------------

        float cycleSpeed =
            Mathf.Lerp(
                walkCycleSpeed,
                runCycleSpeed,
                normalizedSpeed
            );


        animationTime +=
            cycleSpeed *
            Time.deltaTime;


        // --------------------------------------------------------
        // ONDA PRINCIPAL
        // --------------------------------------------------------

        float cycle =
            Mathf.Sin(animationTime);


        float secondaryCycle =
            Mathf.Sin(
                animationTime * 2f
            ) * 0.12f;


        float gaitWave =
            cycle +
            secondaryCycle;


        // ========================================================
        // PERNAS
        // ========================================================

        float leftLegAngle =
            gaitWave *
            legSwing *
            currentAnimationIntensity *
            leftLegDirection;


        float rightLegAngle =
            -gaitWave *
            legSwing *
            currentAnimationIntensity *
            rightLegDirection;


        ApplyAxisRotation(
            thighLeft,
            initialThighLeftRotation,
            legRotationAxis,
            leftLegAngle
        );


        ApplyAxisRotation(
            thighRight,
            initialThighRightRotation,
            legRotationAxis,
            rightLegAngle
        );


        // ========================================================
        // JOELHOS
        // ========================================================

        float leftKnee =
            Mathf.Max(
                0f,
                -gaitWave
            ) *
            (
                kneeBend +
                kneeLift
            ) *
            currentAnimationIntensity;


        float rightKnee =
            Mathf.Max(
                0f,
                gaitWave
            ) *
            (
                kneeBend +
                kneeLift
            ) *
            currentAnimationIntensity;


        ApplyAxisRotation(
            legLeft,
            initialLegLeftRotation,
            legRotationAxis,
            -leftKnee
        );


        ApplyAxisRotation(
            legRight,
            initialLegRightRotation,
            legRotationAxis,
            -rightKnee
        );


        // ========================================================
        // BRAÇOS - CAMINHADA
        // ========================================================
        //
        // IMPORTANTE:
        //
        // Aqui usamos armSwingRotationAxis.
        //
        // Portanto, se estiver configurado como X:
        //
        // braço esquerdo -> frente/trás
        // braço direito  -> trás/frente
        //
        // NÃO usamos o eixo de pulo aqui.
        //
        // Isso impede o personagem de parecer um pássaro.
        // ========================================================

        float leftArmAngle =
            -gaitWave *
            armSwing *
            currentAnimationIntensity *
            leftArmDirection;


        float rightArmAngle =
            gaitWave *
            armSwing *
            currentAnimationIntensity *
            rightArmDirection;


        ApplyAxisRotation(
            armLeft,
            neutralArmLeftRotation,
            armSwingRotationAxis,
            leftArmAngle
        );


        ApplyAxisRotation(
            armRight,
            neutralArmRightRotation,
            armSwingRotationAxis,
            rightArmAngle
        );


        // ========================================================
        // COTOVELOS / ANTEBRAÇOS
        // ========================================================

        float leftForearm =
            Mathf.Max(
                0f,
                gaitWave
            ) *
            5f *
            currentAnimationIntensity;


        float rightForearm =
            Mathf.Max(
                0f,
                -gaitWave
            ) *
            5f *
            currentAnimationIntensity;


        ApplyAxisRotation(
            forearmLeft,
            neutralForearmLeftRotation,
            armSwingRotationAxis,
            leftForearm
        );


        ApplyAxisRotation(
            forearmRight,
            neutralForearmRightRotation,
            armSwingRotationAxis,
            rightForearm
        );


        // ========================================================
        // QUADRIL
        // ========================================================

        float weightShift =
            Mathf.Sin(
                animationTime
            );


        float hipSwayAmount =
            weightShift *
            hipSway *
            currentAnimationIntensity;


        float hipRotationAmount =
            weightShift *
            hipRotation *
            currentAnimationIntensity;


        float hipBobAmount =
            Mathf.Abs(
                Mathf.Sin(
                    animationTime
                )
            ) *
            hipBob *
            currentAnimationIntensity;


        if (hipRoot != null)
        {
            Quaternion target =
                initialHipRotation *
                Quaternion.Euler(
                    hipBobAmount,
                    0f,
                    hipSwayAmount
                );


            SmoothRotation(
                hipRoot,
                target
            );


            hipRoot.localPosition =
                Vector3.Lerp(
                    hipRoot.localPosition,
                    initialHipLocalPosition +
                    Vector3.up *
                    (
                        hipBobAmount *
                        0.005f
                    ),
                    animationSmoothness *
                    Time.deltaTime
                );
        }


        // ========================================================
        // TORSO
        // ========================================================

        float targetLean =
            Mathf.Lerp(
                0f,
                torsoLean,
                normalizedSpeed
            );


        float torsoWave =
            gaitWave *
            torsoMovement *
            currentAnimationIntensity;


        if (torso != null)
        {
            Quaternion target =
                initialTorsoRotation;


            target *=
                Quaternion.Euler(
                    GetAxisVector(
                        torsoRotationAxis,
                        -targetLean +
                        torsoWave
                    )
                );


            SmoothRotation(
                torso,
                target
            );
        }


        // ========================================================
        // SPINE
        // ========================================================

        if (spine != null)
        {
            float spineWave =
                gaitWave *
                spineMovement *
                currentAnimationIntensity;


            Quaternion target =
                initialSpineRotation;


            target *=
                Quaternion.Euler(
                    GetAxisVector(
                        torsoRotationAxis,
                        spineWave
                    )
                );


            SmoothRotation(
                spine,
                target
            );
        }


        // ========================================================
        // CABEÇA
        // ========================================================

        if (head != null)
        {
            Quaternion target =
                initialHeadRotation *
                Quaternion.Euler(
                    0f,
                    -weightShift *
                    1.5f *
                    currentAnimationIntensity,
                    0f
                );


            SmoothRotation(
                head,
                target
            );
        }
    }


    // ============================================================
    // IDLE
    // ============================================================

    void ApplyIdleAnimation()
    {
        currentAnimationIntensity =
            Mathf.Lerp(
                currentAnimationIntensity,
                0f,
                animationSmoothness *
                Time.deltaTime
            );


        animationTime +=
            idleBreathingSpeed *
            Time.deltaTime;


        float breathing =
            Mathf.Sin(
                animationTime
            ) *
            idleBreathingAmount;


        // --------------------------------------------------------
        // TORSO
        // --------------------------------------------------------

        if (torso != null)
        {
            Quaternion target =
                initialTorsoRotation *
                Quaternion.Euler(
                    GetAxisVector(
                        torsoRotationAxis,
                        breathing
                    )
                );


            SmoothRotation(
                torso,
                target
            );
        }


        // --------------------------------------------------------
        // QUADRIL
        // --------------------------------------------------------

        if (hipRoot != null)
        {
            float idleMovement =
                Mathf.Sin(
                    animationTime * 0.5f
                ) *
                idleHipMovement;


            Quaternion target =
                initialHipRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    idleMovement
                );


            SmoothRotation(
                hipRoot,
                target
            );


            hipRoot.localPosition =
                Vector3.Lerp(
                    hipRoot.localPosition,
                    initialHipLocalPosition,
                    animationSmoothness *
                    Time.deltaTime
                );
        }


        // --------------------------------------------------------
        // RETORNO DOS BONES
        // --------------------------------------------------------

        ReturnToNeutral(
            thighLeft,
            initialThighLeftRotation
        );

        ReturnToNeutral(
            thighRight,
            initialThighRightRotation
        );

        ReturnToNeutral(
            legLeft,
            initialLegLeftRotation
        );

        ReturnToNeutral(
            legRight,
            initialLegRightRotation
        );


        ReturnToNeutral(
            armLeft,
            neutralArmLeftRotation
        );

        ReturnToNeutral(
            armRight,
            neutralArmRightRotation
        );


        ReturnToNeutral(
            forearmLeft,
            neutralForearmLeftRotation
        );

        ReturnToNeutral(
            forearmRight,
            neutralForearmRightRotation
        );


        ReturnToNeutral(
            spine,
            initialSpineRotation
        );


        ReturnToNeutral(
            head,
            initialHeadRotation
        );
    }


    // ============================================================
    // SALTO
    // ============================================================

    void ApplyJumpAnimation()
    {
        currentAnimationIntensity =
            Mathf.Lerp(
                currentAnimationIntensity,
                0f,
                animationSmoothness *
                Time.deltaTime
            );


        // ========================================================
        // PERNAS
        // ========================================================

        ApplyAxisRotation(
            thighLeft,
            initialThighLeftRotation,
            legRotationAxis,
            -jumpLegBend
        );


        ApplyAxisRotation(
            thighRight,
            initialThighRightRotation,
            legRotationAxis,
            jumpLegBend
        );


        // ========================================================
        // BRAÇOS
        // ========================================================
        //
        // Aqui NÃO usamos X.
        //
        // Usamos armJumpRotationAxis.
        //
        // Se estiver em Z, os braços sobem para as laterais.
        //
        // Isso é completamente separado do movimento da caminhada.
        // ========================================================

        ApplyAxisRotation(
            armLeft,
            neutralArmLeftRotation,
            armJumpRotationAxis,
            jumpArmLift
        );


        ApplyAxisRotation(
            armRight,
            neutralArmRightRotation,
            armJumpRotationAxis,
            -jumpArmLift
        );


        // ========================================================
        // ANTEBRAÇOS
        // ========================================================

        ApplyAxisRotation(
            forearmLeft,
            neutralForearmLeftRotation,
            armJumpRotationAxis,
            -5f
        );


        ApplyAxisRotation(
            forearmRight,
            neutralForearmRightRotation,
            armJumpRotationAxis,
            5f
        );


        // ========================================================
        // TORSO
        // ========================================================

        if (torso != null)
        {
            Quaternion target =
                initialTorsoRotation *
                Quaternion.Euler(
                    GetAxisVector(
                        torsoRotationAxis,
                        -2f
                    )
                );


            SmoothRotation(
                torso,
                target
            );
        }
    }


    // ============================================================
    // DOUBLE JUMP / CAMBALHOTA
    // ============================================================

    void ApplyDoubleJumpAnimation()
    {
        currentAnimationIntensity =
            Mathf.Lerp(
                currentAnimationIntensity,
                0f,
                animationSmoothness *
                Time.deltaTime
            );

        float progress =
            controller.DoubleJumpProgress;

        // Curva suave para a rotação começar e terminar sem tranco.
        float easedProgress =
            progress * progress * (3f - 2f * progress);

        float tuck =
            Mathf.Sin(progress * Mathf.PI) *
            somersaultBodyTuck;

        // IMPORTANTE:
        // A cambalhota agora gira pelo quadril, e não mais pelo
        // armatureRoot. Isso evita que o pivot fique no pé.
        if (hipRoot != null)
        {
            Quaternion target =
                initialHipRotation *
                Quaternion.Euler(
                    somersaultDegrees * easedProgress,
                    0f,
                    0f
                );

            SmoothRotation(
                hipRoot,
                target
            );

            hipRoot.localPosition =
                Vector3.Lerp(
                    hipRoot.localPosition,
                    initialHipLocalPosition +
                    Vector3.up *
                    (Mathf.Sin(progress * Mathf.PI) * 0.03f),
                    animationSmoothness *
                    Time.deltaTime
                );
        }

        // Encolhe o corpo para dar leitura de cambalhota,
        // e não apenas de um modelo girando rígido.
        ApplyAxisRotation(
            thighLeft,
            initialThighLeftRotation,
            legRotationAxis,
            -tuck
        );

        ApplyAxisRotation(
            thighRight,
            initialThighRightRotation,
            legRotationAxis,
            tuck
        );

        ApplyAxisRotation(
            legLeft,
            initialLegLeftRotation,
            legRotationAxis,
            -tuck * 0.85f
        );

        ApplyAxisRotation(
            legRight,
            initialLegRightRotation,
            legRotationAxis,
            tuck * 0.85f
        );

        ApplyAxisRotation(
            armLeft,
            neutralArmLeftRotation,
            armSwingRotationAxis,
            -tuck * leftArmDirection
        );

        ApplyAxisRotation(
            armRight,
            neutralArmRightRotation,
            armSwingRotationAxis,
            tuck * rightArmDirection
        );

        ApplyAxisRotation(
            forearmLeft,
            neutralForearmLeftRotation,
            armSwingRotationAxis,
            -tuck * 0.65f
        );

        ApplyAxisRotation(
            forearmRight,
            neutralForearmRightRotation,
            armSwingRotationAxis,
            tuck * 0.65f
        );

        if (torso != null)
        {
            Quaternion target =
                initialTorsoRotation *
                Quaternion.Euler(
                    GetAxisVector(
                        torsoRotationAxis,
                        -tuck * 0.45f
                    )
                );

            SmoothRotation(
                torso,
                target
            );
        }

        if (spine != null)
        {
            Quaternion target =
                initialSpineRotation *
                Quaternion.Euler(
                    GetAxisVector(
                        torsoRotationAxis,
                        -tuck * 0.25f
                    )
                );

            SmoothRotation(
                spine,
                target
            );
        }
    }


    // ============================================================
    // QUEDA
    // ============================================================

    void ApplyFallAnimation()
    {
        currentAnimationIntensity =
            Mathf.Lerp(
                currentAnimationIntensity,
                0f,
                animationSmoothness *
                Time.deltaTime
            );


        // ========================================================
        // PERNAS
        // ========================================================

        ApplyAxisRotation(
            thighLeft,
            initialThighLeftRotation,
            legRotationAxis,
            -fallLegBend
        );


        ApplyAxisRotation(
            thighRight,
            initialThighRightRotation,
            legRotationAxis,
            fallLegBend
        );


        // ========================================================
        // BRAÇOS
        // ========================================================
        //
        // Durante a queda os braços ficam abertos para equilíbrio.
        //
        // Também usamos o eixo lateral, não o eixo de caminhada.
        // ========================================================

        ApplyAxisRotation(
            armLeft,
            neutralArmLeftRotation,
            armJumpRotationAxis,
            fallArmLift
        );


        ApplyAxisRotation(
            armRight,
            neutralArmRightRotation,
            armJumpRotationAxis,
            -fallArmLift
        );


        // ========================================================
        // ANTEBRAÇOS
        // ========================================================

        ApplyAxisRotation(
            forearmLeft,
            neutralForearmLeftRotation,
            armJumpRotationAxis,
            -8f
        );


        ApplyAxisRotation(
            forearmRight,
            neutralForearmRightRotation,
            armJumpRotationAxis,
            8f
        );


        // ========================================================
        // TORSO
        // ========================================================

        if (torso != null)
        {
            Quaternion target =
                initialTorsoRotation *
                Quaternion.Euler(
                    GetAxisVector(
                        torsoRotationAxis,
                        3f
                    )
                );


            SmoothRotation(
                torso,
                target
            );
        }
    }


    // ============================================================
    // ATERRISSAGEM
    // ============================================================

    void ApplyLandingAnimation()
    {
        float normalized =
            Mathf.Clamp01(
                landingTimer /
                landingDuration
            );


        float impact =
            Mathf.Sin(
                normalized *
                Mathf.PI
            );


        float amount =
            impact *
            landingSquash;


        // ========================================================
        // PERNAS
        // ========================================================

        ApplyAxisRotation(
            thighLeft,
            initialThighLeftRotation,
            legRotationAxis,
            -amount
        );


        ApplyAxisRotation(
            thighRight,
            initialThighRightRotation,
            legRotationAxis,
            amount
        );


        // ========================================================
        // TORSO
        // ========================================================

        if (torso != null)
        {
            Quaternion target =
                initialTorsoRotation *
                Quaternion.Euler(
                    GetAxisVector(
                        torsoRotationAxis,
                        amount * 0.5f
                    )
                );


            SmoothRotation(
                torso,
                target
            );
        }


        // ========================================================
        // BRAÇOS
        // ========================================================

        // Os braços retornam à posição normal depois do impacto.

        ReturnToNeutral(
            armLeft,
            neutralArmLeftRotation
        );

        ReturnToNeutral(
            armRight,
            neutralArmRightRotation
        );


        ReturnToNeutral(
            forearmLeft,
            neutralForearmLeftRotation
        );

        ReturnToNeutral(
            forearmRight,
            neutralForearmRightRotation
        );
    }


    // ============================================================
    // NATAÇÃO
    // ============================================================

    void ApplySwimmingAnimation()
    {
        bool activelySwimming =
            controller.IsMoving ||
            Mathf.Abs(controller.SwimVerticalInput) > 0.01f;

        float cycleMultiplier =
            activelySwimming
                ? 1f
                : swimIdleCycleMultiplier;

        swimAnimationTime +=
            swimCycleSpeed *
            cycleMultiplier *
            Time.deltaTime;


        float armCycle =
            Mathf.Sin(swimAnimationTime);

        float legCycle =
            Mathf.Sin(swimAnimationTime * 2f);

        float strokeIntensity =
            activelySwimming
                ? 1f
                : 0.35f;

        float verticalTilt =
            -controller.SwimVerticalInput *
            swimVerticalBodyTilt;


        // --------------------------------------------------------
        // CORPO - agora o personagem realmente inclina para uma
        // postura de nado em vez de ficar quase em pé.
        // --------------------------------------------------------

        if (hipRoot != null)
        {
            Quaternion target =
                initialHipRotation *
                Quaternion.Euler(
                    swimHipPitch + (verticalTilt * 0.45f),
                    0f,
                    0f
                );

            SmoothRotation(
                hipRoot,
                target
            );

            hipRoot.localPosition =
                Vector3.Lerp(
                    hipRoot.localPosition,
                    initialHipLocalPosition,
                    animationSmoothness *
                    Time.deltaTime
                );
        }

        if (torso != null)
        {
            Quaternion target =
                initialTorsoRotation *
                Quaternion.Euler(
                    GetAxisVector(
                        torsoRotationAxis,
                        swimTorsoTilt + verticalTilt
                    )
                );

            SmoothRotation(
                torso,
                target
            );
        }

        if (spine != null)
        {
            Quaternion target =
                initialSpineRotation *
                Quaternion.Euler(
                    GetAxisVector(
                        torsoRotationAxis,
                        swimSpineTilt + (verticalTilt * 0.35f)
                    )
                );

            SmoothRotation(
                spine,
                target
            );
        }

        if (head != null)
        {
            Quaternion target =
                initialHeadRotation *
                Quaternion.Euler(
                    -swimHeadLift,
                    0f,
                    0f
                );

            SmoothRotation(
                head,
                target
            );
        }


        // --------------------------------------------------------
        // BRAÇOS - braçada de crawl simplificada.
        // --------------------------------------------------------

        float leftArm =
            armCycle *
            swimArmSwing *
            strokeIntensity;

        float rightArm =
            -armCycle *
            swimArmSwing *
            strokeIntensity;

        ApplyAxisRotation(
            armLeft,
            neutralArmLeftRotation,
            armSwingRotationAxis,
            leftArm
        );

        ApplyAxisRotation(
            armRight,
            neutralArmRightRotation,
            armSwingRotationAxis,
            rightArm
        );

        float leftForearm =
            Mathf.Max(0f, -armCycle) *
            swimForearmBend *
            strokeIntensity;

        float rightForearm =
            Mathf.Max(0f, armCycle) *
            swimForearmBend *
            strokeIntensity;

        ApplyAxisRotation(
            forearmLeft,
            neutralForearmLeftRotation,
            armSwingRotationAxis,
            leftForearm
        );

        ApplyAxisRotation(
            forearmRight,
            neutralForearmRightRotation,
            armSwingRotationAxis,
            rightForearm
        );


        // --------------------------------------------------------
        // PERNAS - flutter kick.
        // --------------------------------------------------------

        float leftLeg =
            legCycle *
            swimLegSwing *
            strokeIntensity;

        float rightLeg =
            -legCycle *
            swimLegSwing *
            strokeIntensity;

        ApplyAxisRotation(
            thighLeft,
            initialThighLeftRotation,
            legRotationAxis,
            leftLeg
        );

        ApplyAxisRotation(
            thighRight,
            initialThighRightRotation,
            legRotationAxis,
            rightLeg
        );

        ApplyAxisRotation(
            legLeft,
            initialLegLeftRotation,
            legRotationAxis,
            -Mathf.Abs(legCycle) * swimLegSwing * 0.4f * strokeIntensity
        );

        ApplyAxisRotation(
            legRight,
            initialLegRightRotation,
            legRotationAxis,
            -Mathf.Abs(legCycle) * swimLegSwing * 0.4f * strokeIntensity
        );
    }


    void RestoreSomersaultPose()
    {
        if (hipRoot == null)
            return;

        float t =
            Mathf.Clamp01(
                somersaultRecoverySpeed *
                Time.deltaTime
            );

        hipRoot.localRotation =
            Quaternion.Slerp(
                hipRoot.localRotation,
                initialHipRotation,
                t
            );

        hipRoot.localPosition =
            Vector3.Lerp(
                hipRoot.localPosition,
                initialHipLocalPosition,
                t
            );
    }


    // ============================================================
    // PISTOLA / POSE DOS BRAÇOS
    // ============================================================

    // Mira de UMA MAO: somente o braco direito segura a pistola.
    // Nao altera os bracos esquerdos, que continuam com Idle/Walk/Run normais.
    // Ao usar vetores entre os bones (ombro->cotovelo->mao), a pose funciona
    // mesmo se os eixos importados do FBX mudarem.
    void ApplyPistolPoseIfNeeded()
    {
        if (pistol == null && autoFindPistol)
            pistol = GetComponentInChildren<RaycastPistol>(true);

        bool shouldAim = pistol != null &&
            pistol.IsEquipped && pistol.IsAiming &&
            controller != null && !controller.IsUnderwater &&
            !controller.IsDoubleJumping;

        if (pistol == null || !shouldAim)
        {
            pistolRecoil = Mathf.MoveTowards(
                pistolRecoil, 0f,
                pistolRecoilRecoverySpeed * Time.deltaTime);

            // A arma volta ao encaixe neutro quando o jogador solta a mira.
            if (pistol != null)
            {
                float returnBlend = 1f - Mathf.Exp(
                    -Mathf.Max(0.1f, pistolPoseSmoothness) * Time.deltaTime);
                pistol.transform.localRotation = Quaternion.Slerp(
                    pistol.transform.localRotation,
                    initialPistolGripLocalRotation, returnBlend);
            }
            return;
        }

        if (armRight == null || forearmRight == null)
            return;

        if (pistol.IsFiringThisFrame)
            pistolRecoil = 1f;
        else
            pistolRecoil = Mathf.MoveTowards(
                pistolRecoil, 0f, pistolRecoilRecoverySpeed * Time.deltaTime);

        Transform camera = controller.cameraTransform;
        Vector3 forward = camera != null
            ? camera.forward.normalized
            : transform.forward.normalized;

        // A mao deve ficar a frente e um pouco a direita do peito.
        Vector3 desiredUpperDirection = (
            forward + transform.right * 0.14f -
            transform.up * 0.055f).normalized;

        // Recuo visual discreto, sem levantar o braco esquerdo.
        Vector3 desiredForearmDirection = (
            forward + transform.right * 0.025f +
            transform.up * (pistolRecoil * 0.12f)).normalized;

        float blend = 1f - Mathf.Exp(
            -Mathf.Max(0.1f, pistolPoseSmoothness) * Time.deltaTime);

        Vector3 elbowDirection =
            forearmRight.position - armRight.position;

        if (elbowDirection.sqrMagnitude > 0.00001f)
        {
            Quaternion shoulderTarget =
                Quaternion.FromToRotation(
                    elbowDirection.normalized,
                    desiredUpperDirection) * armRight.rotation;

            armRight.rotation = Quaternion.Slerp(
                armRight.rotation, shoulderTarget, blend);
        }

        // Alinha tambem o pulso (em vez de somente o ombro),
        // preservando a hierarquia HAND.R da arma.
        if (rightHand != null)
        {
            Vector3 wristDirection =
                rightHand.position - forearmRight.position;

            if (wristDirection.sqrMagnitude > 0.00001f)
            {
                Quaternion elbowTarget =
                    Quaternion.FromToRotation(
                        wristDirection.normalized,
                        desiredForearmDirection) * forearmRight.rotation;

                forearmRight.rotation = Quaternion.Slerp(
                    forearmRight.rotation, elbowTarget, blend);
            }
        }

        // O modelo HandyMan tem cano em +Z apos ser importado pela Unity.
        // Alinhar a ARMA com a camera e independente de seu novo rig FBX.
        // A origem da arma continua presa a HAND.R: nao ha arma flutuante.
        Vector3 muzzleForward = (
            forward + transform.up * (pistolRecoil * 0.035f)).normalized;
        Quaternion gunTarget = Quaternion.LookRotation(
            muzzleForward, Vector3.up);
        pistol.transform.rotation = Quaternion.Slerp(
            pistol.transform.rotation, gunTarget, blend);
    }

    void SmoothPistolRotation(
        Transform bone,
        Quaternion target
    )
    {
        if (bone == null)
            return;

        float t =
            1f -
            Mathf.Exp(
                -Mathf.Max(0f, pistolPoseSmoothness) *
                Time.deltaTime
            );

        bone.localRotation =
            Quaternion.Slerp(
                bone.localRotation,
                target,
                t
            );
    }


    // ============================================================
    // APLICA ROTAÇÃO POR EIXO
    // ============================================================

    void ApplyAxisRotation(
        Transform bone,
        Quaternion baseRotation,
        RotationAxis axis,
        float angle
    )
    {
        if (bone == null)
            return;


        Vector3 rotation =
            GetAxisVector(
                axis,
                angle
            );


        Quaternion target =
            baseRotation *
            Quaternion.Euler(
                rotation
            );


        SmoothRotation(
            bone,
            target
        );
    }


    // ============================================================
    // RETORNAR PARA POSE
    // ============================================================

    void ReturnToNeutral(
        Transform bone,
        Quaternion target
    )
    {
        if (bone == null)
            return;


        SmoothRotation(
            bone,
            target
        );
    }


    // ============================================================
    // EIXOS
    // ============================================================

    Vector3 GetAxisVector(
        RotationAxis axis,
        float angle
    )
    {
        switch (axis)
        {
            case RotationAxis.X:

                return new Vector3(
                    angle,
                    0f,
                    0f
                );


            case RotationAxis.Y:

                return new Vector3(
                    0f,
                    angle,
                    0f
                );


            case RotationAxis.Z:

                return new Vector3(
                    0f,
                    0f,
                    angle
                );
        }


        return Vector3.zero;
    }


    // ============================================================
    // SUAVIZAÇÃO
    // ============================================================

    void SmoothRotation(
        Transform bone,
        Quaternion target
    )
    {
        if (bone == null)
            return;


        float t =
            animationSmoothness *
            Time.deltaTime;


        t =
            Mathf.Clamp01(t);


        bone.localRotation =
            Quaternion.Slerp(
                bone.localRotation,
                target,
                t
            );
    }


    // ============================================================
    // ENCONTRAR BONES
    // ============================================================

    void AutoFindBones()
    {
        if (armatureRoot == null)
        {
            armatureRoot =
                FindChildRecursive(
                    transform,
                    "Armature"
                );
        }


        if (armatureRoot == null)
        {
            Debug.LogError(
                "ProceduralCharacterAnimator: " +
                "Armature não encontrado."
            );

            return;
        }


        hipRoot =
            FindChildRecursive(
                armatureRoot,
                "HIPROOT"
            );


        thighLeft =
            FindChildRecursive(
                armatureRoot,
                "COXA.L"
            );


        // O novo FBX cruzou os nomes das canelas: COXA.L -> PERNA.R,
        // COXA.R -> PERNA.L. Encontrar pelo pai evita animar o joelho errado.
        legLeft = thighLeft != null
            ? FindChildRecursive(thighLeft, "PERNA.R")
            : null;
        if (legLeft == null && thighLeft != null)
            legLeft = FindChildRecursive(thighLeft, "PERNA.L");


        thighRight =
            FindChildRecursive(
                armatureRoot,
                "COXA.R"
            );


        legRight = thighRight != null
            ? FindChildRecursive(thighRight, "PERNA.L")
            : null;
        if (legRight == null && thighRight != null)
            legRight = FindChildRecursive(thighRight, "PERNA.R");


        spine =
            FindChildRecursive(
                armatureRoot,
                "SPINE"
            );


        torso =
            FindChildRecursive(
                armatureRoot,
                "TORSO"
            );


        armLeft =
            FindChildRecursive(
                armatureRoot,
                "ARM.L"
            );


        forearmLeft =
            FindChildRecursive(
                armatureRoot,
                "FOREARM.L"
            );


        armRight =
            FindChildRecursive(
                armatureRoot,
                "ARM.R"
            );


        forearmRight =
            FindChildRecursive(
                armatureRoot,
                "FOREARM.R"
            );


        head =
            FindChildRecursive(
                armatureRoot,
                "CABEÇA"
            );
    }


    // ============================================================
    // BUSCA RECURSIVA
    // ============================================================

    Transform FindChildRecursive(
        Transform parent,
        string childName
    )
    {
        if (parent == null)
            return null;


        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;


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


    // ============================================================
    // SALVAR POSE ORIGINAL
    // ============================================================

    void SaveInitialPose()
    {
        if (armatureRoot != null)
        {
            initialArmatureRootRotation =
                armatureRoot.localRotation;
        }


        if (hipRoot != null)
        {
            initialHipRotation =
                hipRoot.localRotation;

            initialHipLocalPosition =
                hipRoot.localPosition;
        }


        if (spine != null)
            initialSpineRotation =
                spine.localRotation;


        if (torso != null)
            initialTorsoRotation =
                torso.localRotation;


        if (thighLeft != null)
            initialThighLeftRotation =
                thighLeft.localRotation;


        if (legLeft != null)
            initialLegLeftRotation =
                legLeft.localRotation;


        if (thighRight != null)
            initialThighRightRotation =
                thighRight.localRotation;


        if (legRight != null)
            initialLegRightRotation =
                legRight.localRotation;


        if (armLeft != null)
            initialArmLeftRotation =
                armLeft.localRotation;


        if (forearmLeft != null)
            initialForearmLeftRotation =
                forearmLeft.localRotation;


        if (armRight != null)
            initialArmRightRotation =
                armRight.localRotation;


        if (forearmRight != null)
            initialForearmRightRotation =
                forearmRight.localRotation;


        if (head != null)
            initialHeadRotation =
                head.localRotation;


        // ========================================================
        // POSE NEUTRA DOS BRAÇOS
        // ========================================================

        if (armLeft != null)
        {
            neutralArmLeftRotation =
                initialArmLeftRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    leftArmRestRotation
                );
        }


        if (armRight != null)
        {
            neutralArmRightRotation =
                initialArmRightRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    rightArmRestRotation
                );
        }


        if (forearmLeft != null)
        {
            neutralForearmLeftRotation =
                initialForearmLeftRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    leftForearmRestRotation
                );
        }


        if (forearmRight != null)
        {
            neutralForearmRightRotation =
                initialForearmRightRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    rightForearmRestRotation
                );
        }
    }


    // ============================================================
    // VALIDAÇÃO
    // ============================================================

    void ValidateBones()
    {
        if (!showDebugMessages)
            return;


        Debug.Log(
            "=== PROCEDURAL CHARACTER ==="
        );


        Debug.Log(
            "Armature: " +
            Status(armatureRoot)
        );


        Debug.Log(
            "HIPROOT: " +
            Status(hipRoot)
        );


        Debug.Log(
            "COXA.L: " +
            Status(thighLeft)
        );


        Debug.Log(
            "PERNA.L: " +
            Status(legLeft)
        );


        Debug.Log(
            "COXA.R: " +
            Status(thighRight)
        );


        Debug.Log(
            "PERNA.R: " +
            Status(legRight)
        );


        Debug.Log(
            "SPINE: " +
            Status(spine)
        );


        Debug.Log(
            "TORSO: " +
            Status(torso)
        );


        Debug.Log(
            "ARM.L: " +
            Status(armLeft)
        );


        Debug.Log(
            "ARM.R: " +
            Status(armRight)
        );


        Debug.Log(
            "FOREARM.L: " +
            Status(forearmLeft)
        );


        Debug.Log(
            "FOREARM.R: " +
            Status(forearmRight)
        );


        Debug.Log(
            "CABEÇA: " +
            Status(head)
        );


        Debug.Log(
            "============================="
        );
    }


    string Status(
        Transform bone
    )
    {
        return bone != null
            ? "OK"
            : "NÃO ENCONTRADO";
    }
}


// ================================================================
// ENUM
// ================================================================

public enum RotationAxis
{
    X,
    Y,
    Z
}
