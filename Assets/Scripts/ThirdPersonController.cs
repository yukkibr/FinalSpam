using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System.Text;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    // ============================================================
    // MOVIMENTAÇÃO TERRESTRE
    // ============================================================

    [Header("Movimentação Terrestre")]

    public float walkSpeed = 5f;
    public float runSpeed = 8.5f;

    public float jumpHeight = 1.5f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;

    [Header("Double Jump")]

    public bool enableDoubleJump = true;
    public float doubleJumpHeight = 1.5f;
    public float somersaultDuration = 0.65f;


    // ============================================================
    // NATAÇÃO
    // ============================================================

    [Header("Movimentação na Água")]

    public float swimSpeed = 3.5f;
    public float waterGravity = -2f;
    public float buoyancyForce = 4f;


    // ============================================================
    // CÂMERA
    // ============================================================

    [Header("Câmera")]

    public Transform cameraTransform;

    public float mouseSensitivity = 0.05f;
    public float distanceFromPlayer = 5f;

    [Header("Primeira Pessoa")]

    public float firstPersonHeight = 1.65f;
    public Vector3 firstPersonOffset = new Vector3(0f, 0f, 0.08f);

    [Header("Mira / Over The Shoulder")]

    [Tooltip("Usado somente quando estiver mirando em terceira pessoa. Fora da mira a câmera continua exatamente como antes.")]
    public float aimCameraDistance = 2.4f;

    public float aimCameraShoulderOffset = 0.65f;
    public float aimCameraVerticalOffset = 0.15f;

    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;


    // ============================================================
    // JOYSTICK / CONTROLE
    // ============================================================

    [Header("Joystick / Controle")]

    public bool enableControllerInput = true;
    public float controllerDeadzone = 0.18f;
    public float controllerLookSensitivity = 120f;
    public bool invertControllerLookY = false;

    [Header("Debug de Input")]

    public bool logInputDeviceChanges = true;
    public bool logControllerButtons = true;
    public bool logControllerLayoutOnConnect = true;

    [Header("Joystick Genérico / PS2 USB")]

    // Esses índices são apenas valores iniciais.
    // O console mostra o índice real de cada botão pressionado.
    public int genericJumpButton = 1;
    public int genericRunButton = 10;
    public int genericCameraToggleButton = 11;
    public int genericSwimUpButton = 1;
    public int genericSwimDownButton = 4;

    // Em controles genéricos o segundo analógico não possui um padrão.
    // O layout é mostrado no Console quando o joystick é conectado.
    // Exemplos comuns: z/rz, rx/ry.
    public string genericLookXAxisControl = "z";
    public string genericLookYAxisControl = "rz";
    public bool invertGenericMoveY = false;


    // ============================================================
    // REFERÊNCIAS INTERNAS
    // ============================================================

    private CharacterController controller;

    private float verticalVelocity;

    private bool isUnderwater = false;

    private float cameraRotationX = 0f;
    private float cameraRotationY = 0f;

    private int jumpCount = 0;
    private bool isDoubleJumping = false;
    private float doubleJumpTimer = 0f;

    private bool isFirstPerson = false;
    private bool isWeaponAiming = false;

    private InputDevice activeControllerDevice;


    // ============================================================
    // ESTADO PÚBLICO
    // ============================================================

    public float CurrentSpeed { get; private set; }

    public float HorizontalSpeed { get; private set; }

    public Vector3 Velocity { get; private set; }

    public Vector3 HorizontalVelocity { get; private set; }

    public bool IsMoving { get; private set; }

    public bool IsRunning { get; private set; }

    public bool IsUnderwater => isUnderwater;

    public bool IsDoubleJumping => isDoubleJumping;

    public float DoubleJumpProgress =>
        somersaultDuration > 0f
            ? Mathf.Clamp01(doubleJumpTimer / somersaultDuration)
            : 1f;

    public bool IsFirstPerson => isFirstPerson;

    public bool IsWeaponAiming => isWeaponAiming;

    public InputMethod ActiveInputMethod { get; private set; } =
        InputMethod.KeyboardMouse;

    public string ActiveControllerName =>
        activeControllerDevice != null
            ? activeControllerDevice.displayName
            : string.Empty;

    public bool IsGrounded =>
        controller != null &&
        controller.isGrounded;

    public bool IsJumping =>
        !isUnderwater &&
        !IsGrounded &&
        verticalVelocity > 0.1f;

    public bool IsFalling =>
        !isUnderwater &&
        !IsGrounded &&
        verticalVelocity < -0.1f;

    public float VerticalVelocity =>
        verticalVelocity;

    public float SwimVerticalInput { get; private set; }

    public Vector3 CurrentMoveDirection { get; private set; }


    // ============================================================
    // START
    // ============================================================

    void OnEnable()
    {
        InputSystem.onDeviceChange +=
            HandleInputDeviceChange;
    }


    void OnDisable()
    {
        InputSystem.onDeviceChange -=
            HandleInputDeviceChange;
    }


    void Start()
    {
        controller =
            GetComponent<CharacterController>();

        if (cameraTransform == null &&
            Camera.main != null)
        {
            cameraTransform =
                Camera.main.transform;
        }

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;

        ScanConnectedControllers();
    }


    // ============================================================
    // UPDATE
    // ============================================================

    void Update()
    {
        UpdateInputDeviceDetection();
        LogPressedControllerButtons();

        HandleCameraInput();
        HandleCameraModeToggle();
        UpdateDoubleJumpState();

        if (isUnderwater)
        {
            HandleSwimming();
        }
        else
        {
            HandleNormalMovement();
        }
    }


    // ============================================================
    // CÂMERA
    // ============================================================

    void HandleCameraInput()
    {
        Vector2 lookInput =
            Vector2.zero;

        if (ActiveInputMethod == InputMethod.Controller &&
            enableControllerInput)
        {
            lookInput =
                ReadControllerLookInput();

            cameraRotationY +=
                lookInput.x *
                controllerLookSensitivity *
                Time.deltaTime;

            float verticalLook =
                invertControllerLookY
                    ? -lookInput.y
                    : lookInput.y;

            cameraRotationX -=
                verticalLook *
                controllerLookSensitivity *
                Time.deltaTime;
        }
        else if (Mouse.current != null)
        {
            Vector2 mouseDelta =
                Mouse.current.delta.ReadValue() *
                mouseSensitivity;

            cameraRotationY +=
                mouseDelta.x;

            cameraRotationX -=
                mouseDelta.y;
        }

        cameraRotationX =
            Mathf.Clamp(
                cameraRotationX,
                minVerticalAngle,
                maxVerticalAngle
            );
    }


    void HandleCameraModeToggle()
    {
        bool keyboardToggle =
            Keyboard.current != null &&
            Keyboard.current.vKey.wasPressedThisFrame;

        bool controllerToggle =
            ActiveInputMethod == InputMethod.Controller &&
            WasControllerCameraTogglePressed();

        if (keyboardToggle || controllerToggle)
        {
            isFirstPerson = !isFirstPerson;
        }
    }


    public void SetWeaponAiming(bool aiming)
    {
        isWeaponAiming =
            aiming &&
            !isUnderwater;
    }


    void UpdateDoubleJumpState()
    {
        if (!isDoubleJumping)
            return;

        doubleJumpTimer += Time.deltaTime;

        if (doubleJumpTimer >= somersaultDuration ||
            controller.isGrounded ||
            isUnderwater)
        {
            isDoubleJumping = false;
            doubleJumpTimer = 0f;
        }
    }


    // ============================================================
    // MOVIMENTO TERRESTRE
    // ============================================================

    void HandleNormalMovement()
    {
        Vector2 movementInput =
            ReadMovementInput();

        bool isRunningInput =
            ReadRunInput();

        bool jumpPressed =
            ReadJumpPressed();


        Vector3 inputDir =
            new Vector3(
                movementInput.x,
                0f,
                movementInput.y
            );

        if (inputDir.sqrMagnitude > 1f)
            inputDir.Normalize();


        // --------------------------------------------------------
        // CORRIDA
        // --------------------------------------------------------

        IsRunning =
            isRunningInput &&
            inputDir.sqrMagnitude > 0.01f;


        // --------------------------------------------------------
        // GRAVIDADE / PULO
        // --------------------------------------------------------

        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
            jumpCount = 0;

            if (jumpPressed)
            {
                verticalVelocity =
                    Mathf.Sqrt(
                        jumpHeight *
                        -2f *
                        gravity
                    );

                jumpCount = 1;
            }
        }
        else
        {
            if (jumpPressed &&
                enableDoubleJump &&
                jumpCount == 1)
            {
                verticalVelocity =
                    Mathf.Sqrt(
                        doubleJumpHeight *
                        -2f *
                        gravity
                    );

                jumpCount = 2;
                isDoubleJumping = true;
                doubleJumpTimer = 0f;
            }

            verticalVelocity +=
                gravity *
                Time.deltaTime;
        }


        float speed =
            IsRunning
                ? runSpeed
                : walkSpeed;


        ExecuteMovement(
            inputDir,
            speed
        );
    }


    // ============================================================
    // NATAÇÃO
    // ============================================================

    void HandleSwimming()
    {
        jumpCount = 0;
        isDoubleJumping = false;
        doubleJumpTimer = 0f;

        Vector2 movementInput =
            ReadMovementInput();

        float moveY =
            ReadSwimVerticalInput();


        Vector3 inputDir =
            new Vector3(
                movementInput.x,
                0f,
                movementInput.y
            );

        if (inputDir.sqrMagnitude > 1f)
            inputDir.Normalize();


        SwimVerticalInput =
            moveY;


        verticalVelocity =
            waterGravity +
            (moveY * swimSpeed) +
            buoyancyForce;


        IsRunning = false;


        ExecuteMovement(
            inputDir,
            swimSpeed
        );
    }


    // ============================================================
    // INPUT - TECLADO / JOYSTICK
    // ============================================================

    void UpdateInputDeviceDetection()
    {
        if (!enableControllerInput)
        {
            SetInputMethod(
                InputMethod.KeyboardMouse,
                null
            );

            return;
        }

        bool keyboardButtonPressed =
            Keyboard.current != null &&
            Keyboard.current.anyKey.wasPressedThisFrame;

        bool mouseButtonPressed =
            Mouse.current != null &&
            (
                Mouse.current.leftButton.wasPressedThisFrame ||
                Mouse.current.rightButton.wasPressedThisFrame ||
                Mouse.current.middleButton.wasPressedThisFrame
            );

        bool keyboardDigitalInput =
            keyboardButtonPressed ||
            mouseButtonPressed;

        InputDevice controllerButtonDevice =
            GetControllerWithPressedButton();

        bool controllerDigitalInput =
            controllerButtonDevice != null;


        // Entrada digital possui prioridade sobre analógicos mantidos.
        if (controllerDigitalInput && !keyboardDigitalInput)
        {
            SetInputMethod(
                InputMethod.Controller,
                controllerButtonDevice
            );

            return;
        }

        if (keyboardDigitalInput && !controllerDigitalInput)
        {
            SetInputMethod(
                InputMethod.KeyboardMouse,
                null
            );

            return;
        }


        bool mouseMoved =
            Mouse.current != null &&
            Mouse.current.delta.ReadValue().sqrMagnitude > 0.25f;

        InputDevice controllerAnalogDevice =
            GetControllerWithAnalogActivity();

        bool controllerAnalogInput =
            controllerAnalogDevice != null;

        if (controllerAnalogInput && !mouseMoved)
        {
            SetInputMethod(
                InputMethod.Controller,
                controllerAnalogDevice
            );
        }
        else if (mouseMoved && !controllerAnalogInput)
        {
            SetInputMethod(
                InputMethod.KeyboardMouse,
                null
            );
        }
    }


    void SetInputMethod(
        InputMethod method,
        InputDevice controllerDevice
    )
    {
        if (method == InputMethod.Controller &&
            controllerDevice != null)
        {
            activeControllerDevice =
                controllerDevice;
        }
        else if (method == InputMethod.KeyboardMouse)
        {
            activeControllerDevice = null;
        }

        if (ActiveInputMethod == method)
            return;

        ActiveInputMethod = method;

        if (!logInputDeviceChanges)
            return;

        if (method == InputMethod.Controller)
        {
            string controllerName =
                activeControllerDevice != null
                    ? activeControllerDevice.displayName
                    : "Controle";

            Debug.Log(
                "[Input] Teclado/Mouse -> Controle: " +
                controllerName
            );
        }
        else
        {
            Debug.Log(
                "[Input] Controle -> Teclado/Mouse"
            );
        }
    }


    Vector2 ReadMovementInput()
    {
        if (ActiveInputMethod == InputMethod.Controller &&
            enableControllerInput)
        {
            InputDevice device =
                GetUsableController();

            if (device is Gamepad gamepad)
            {
                Vector2 stickInput =
                    gamepad.leftStick.ReadValue();

                Vector2 dpadInput =
                    gamepad.dpad.ReadValue();

                Vector2 value =
                    dpadInput.sqrMagnitude > stickInput.sqrMagnitude
                        ? dpadInput
                        : stickInput;

                return ApplyControllerDeadzone(
                    value
                );
            }

            if (device is Joystick joystick)
            {
                Vector2 value =
                    joystick.stick.ReadValue();

                if (invertGenericMoveY)
                {
                    value.y *= -1f;
                }

                return ApplyControllerDeadzone(
                    value
                );
            }
        }

        Vector2 keyboardInput =
            Vector2.zero;

        if (Keyboard.current == null)
            return keyboardInput;

        if (Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
        {
            keyboardInput.x += 1f;
        }

        if (Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
        {
            keyboardInput.x -= 1f;
        }

        if (Keyboard.current.wKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed)
        {
            keyboardInput.y += 1f;
        }

        if (Keyboard.current.sKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed)
        {
            keyboardInput.y -= 1f;
        }

        return keyboardInput.normalized;
    }


    Vector2 ReadControllerLookInput()
    {
        InputDevice device =
            GetUsableController();

        if (device is Gamepad gamepad)
        {
            return ApplyControllerDeadzone(
                gamepad.rightStick.ReadValue()
            );
        }

        if (device is Joystick joystick)
        {
            float x =
                ReadGenericJoystickAxis(
                    joystick,
                    genericLookXAxisControl
                );

            float y =
                ReadGenericJoystickAxis(
                    joystick,
                    genericLookYAxisControl
                );

            return ApplyControllerDeadzone(
                new Vector2(x, y)
            );
        }

        return Vector2.zero;
    }


    bool ReadRunInput()
    {
        if (ActiveInputMethod == InputMethod.Controller &&
            enableControllerInput)
        {
            InputDevice device =
                GetUsableController();

            if (device is Gamepad gamepad)
            {
                return
                    gamepad.leftStickButton.isPressed ||
                    gamepad.rightTrigger.ReadValue() > 0.5f;
            }

            if (device is Joystick joystick)
            {
                ButtonControl button =
                    GetGenericJoystickButton(
                        joystick,
                        genericRunButton
                    );

                return button != null &&
                    button.isPressed;
            }
        }

        return
            Keyboard.current != null &&
            (
                Keyboard.current.leftShiftKey.isPressed ||
                Keyboard.current.rightShiftKey.isPressed
            );
    }


    bool ReadJumpPressed()
    {
        if (ActiveInputMethod == InputMethod.Controller &&
            enableControllerInput)
        {
            InputDevice device =
                GetUsableController();

            if (device is Gamepad gamepad)
            {
                return gamepad.buttonSouth.wasPressedThisFrame;
            }

            if (device is Joystick joystick)
            {
                ButtonControl button =
                    GetGenericJoystickButton(
                        joystick,
                        genericJumpButton
                    );

                return button != null &&
                    button.wasPressedThisFrame;
            }
        }

        return
            Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame;
    }


    float ReadSwimVerticalInput()
    {
        if (ActiveInputMethod == InputMethod.Controller &&
            enableControllerInput)
        {
            InputDevice device =
                GetUsableController();

            if (device is Gamepad gamepad)
            {
                float value = 0f;

                if (gamepad.buttonSouth.isPressed)
                    value += 1f;

                if (gamepad.leftShoulder.isPressed)
                    value -= 1f;

                return Mathf.Clamp(
                    value,
                    -1f,
                    1f
                );
            }

            if (device is Joystick joystick)
            {
                float value = 0f;

                ButtonControl upButton =
                    GetGenericJoystickButton(
                        joystick,
                        genericSwimUpButton
                    );

                ButtonControl downButton =
                    GetGenericJoystickButton(
                        joystick,
                        genericSwimDownButton
                    );

                if (upButton != null &&
                    upButton.isPressed)
                {
                    value += 1f;
                }

                if (downButton != null &&
                    downButton.isPressed)
                {
                    value -= 1f;
                }

                return Mathf.Clamp(
                    value,
                    -1f,
                    1f
                );
            }
        }

        float keyboardValue = 0f;

        if (Keyboard.current == null)
            return keyboardValue;

        if (Keyboard.current.spaceKey.isPressed)
            keyboardValue += 1f;

        if (Keyboard.current.leftShiftKey.isPressed ||
            Keyboard.current.rightShiftKey.isPressed)
        {
            keyboardValue -= 1f;
        }

        return Mathf.Clamp(
            keyboardValue,
            -1f,
            1f
        );
    }


    bool WasControllerCameraTogglePressed()
    {
        InputDevice device =
            GetUsableController();

        if (device is Gamepad gamepad)
        {
            return
                gamepad.rightStickButton.wasPressedThisFrame;
        }

        if (device is Joystick joystick)
        {
            ButtonControl button =
                GetGenericJoystickButton(
                    joystick,
                    genericCameraToggleButton
                );

            return button != null &&
                button.wasPressedThisFrame;
        }

        return false;
    }


    Vector2 ApplyControllerDeadzone(
        Vector2 value
    )
    {
        float deadzone =
            Mathf.Clamp(
                controllerDeadzone,
                0f,
                0.95f
            );

        float magnitude =
            value.magnitude;

        if (magnitude <= deadzone)
            return Vector2.zero;

        float normalizedMagnitude =
            Mathf.InverseLerp(
                deadzone,
                1f,
                Mathf.Clamp01(magnitude)
            );

        return value.normalized *
            normalizedMagnitude;
    }


    InputDevice GetUsableController()
    {
        if (activeControllerDevice != null &&
            activeControllerDevice.added &&
            activeControllerDevice.enabled)
        {
            return activeControllerDevice;
        }

        if (Gamepad.current != null &&
            Gamepad.current.enabled)
        {
            activeControllerDevice =
                Gamepad.current;

            return activeControllerDevice;
        }

        if (Joystick.current != null &&
            Joystick.current.enabled)
        {
            activeControllerDevice =
                Joystick.current;

            return activeControllerDevice;
        }

        activeControllerDevice = null;
        return null;
    }


    InputDevice GetControllerWithPressedButton()
    {
        foreach (InputDevice device in InputSystem.devices)
        {
            if (!IsControllerDevice(device) ||
                !device.enabled)
            {
                continue;
            }

            foreach (InputControl control in device.allControls)
            {
                if (control is ButtonControl button &&
                    button.wasPressedThisFrame)
                {
                    return device;
                }
            }
        }

        return null;
    }


    InputDevice GetControllerWithAnalogActivity()
    {
        foreach (InputDevice device in InputSystem.devices)
        {
            if (!IsControllerDevice(device) ||
                !device.enabled)
            {
                continue;
            }

            if (device is Gamepad gamepad)
            {
                if (gamepad.leftStick.ReadValue().magnitude > controllerDeadzone ||
                    gamepad.rightStick.ReadValue().magnitude > controllerDeadzone ||
                    gamepad.leftTrigger.ReadValue() > controllerDeadzone ||
                    gamepad.rightTrigger.ReadValue() > controllerDeadzone)
                {
                    return device;
                }
            }
            else if (device is Joystick joystick)
            {
                if (joystick.stick.ReadValue().magnitude > controllerDeadzone)
                {
                    return device;
                }

                Vector2 lookInput =
                    new Vector2(
                        ReadGenericJoystickAxis(
                            joystick,
                            genericLookXAxisControl
                        ),
                        ReadGenericJoystickAxis(
                            joystick,
                            genericLookYAxisControl
                        )
                    );

                if (lookInput.magnitude > controllerDeadzone)
                {
                    return device;
                }
            }
        }

        return null;
    }


    bool IsControllerDevice(
        InputDevice device
    )
    {
        return
            device is Gamepad ||
            device is Joystick;
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


    float ReadGenericJoystickAxis(
        Joystick joystick,
        string controlName
    )
    {
        if (joystick == null ||
            string.IsNullOrWhiteSpace(controlName))
        {
            return 0f;
        }

        foreach (InputControl control in joystick.allControls)
        {
            if (!(control is AxisControl axis))
                continue;

            if (string.Equals(
                control.name,
                controlName,
                System.StringComparison.OrdinalIgnoreCase
            ))
            {
                return axis.ReadValue();
            }

            if (string.Equals(
                control.displayName,
                controlName,
                System.StringComparison.OrdinalIgnoreCase
            ))
            {
                return axis.ReadValue();
            }
        }

        return 0f;
    }


    // ============================================================
    // DETECÇÃO / DEBUG DO CONTROLE
    // ============================================================

    void ScanConnectedControllers()
    {
        int controllerCount = 0;

        foreach (InputDevice device in InputSystem.devices)
        {
            if (!IsControllerDevice(device))
                continue;

            controllerCount++;

            if (logInputDeviceChanges)
            {
                LogControllerDetected(
                    device,
                    "detectado"
                );
            }

            if (logControllerLayoutOnConnect)
            {
                LogControllerLayout(
                    device
                );
            }
        }

        if (controllerCount == 0 &&
            logInputDeviceChanges)
        {
            Debug.Log(
                "[Input] Nenhum joystick/gamepad detectado no Start."
            );
        }
    }


    void HandleInputDeviceChange(
        InputDevice device,
        InputDeviceChange change
    )
    {
        if (!IsControllerDevice(device))
            return;

        switch (change)
        {
            case InputDeviceChange.Added:
            case InputDeviceChange.Reconnected:
            case InputDeviceChange.Enabled:

                if (logInputDeviceChanges)
                {
                    LogControllerDetected(
                        device,
                        change.ToString()
                    );
                }

                if (logControllerLayoutOnConnect)
                {
                    LogControllerLayout(
                        device
                    );
                }

                break;

            case InputDeviceChange.Removed:
            case InputDeviceChange.Disconnected:
            case InputDeviceChange.Disabled:

                if (logInputDeviceChanges)
                {
                    Debug.Log(
                        "[Input] Controle removido/desconectado: " +
                        device.displayName +
                        " | " +
                        change
                    );
                }

                if (activeControllerDevice == device)
                {
                    activeControllerDevice = null;

                    if (ActiveInputMethod == InputMethod.Controller)
                    {
                        SetInputMethod(
                            InputMethod.KeyboardMouse,
                            null
                        );
                    }
                }

                break;
        }
    }


    void LogControllerDetected(
        InputDevice device,
        string status
    )
    {
        Debug.Log(
            "[Input] Controle " +
            status +
            ": " +
            device.displayName +
            " | Layout: " +
            device.layout +
            " | Interface: " +
            device.description.interfaceName +
            " | Fabricante: " +
            device.description.manufacturer +
            " | Produto: " +
            device.description.product
        );
    }


    void LogControllerLayout(
        InputDevice device
    )
    {
        StringBuilder log =
            new StringBuilder();

        log.AppendLine(
            "[Input] === LAYOUT DO CONTROLE: " +
            device.displayName +
            " ==="
        );

        int buttonIndex = 0;
        int axisIndex = 0;

        foreach (InputControl control in device.allControls)
        {
            if (control is ButtonControl)
            {
                log.AppendLine(
                    "Botão #" +
                    buttonIndex +
                    " -> " +
                    control.name +
                    " | " +
                    control.displayName +
                    " | Path: " +
                    control.path
                );

                buttonIndex++;
            }
            else if (control is AxisControl)
            {
                log.AppendLine(
                    "Eixo #" +
                    axisIndex +
                    " -> " +
                    control.name +
                    " | " +
                    control.displayName +
                    " | Path: " +
                    control.path
                );

                axisIndex++;
            }
        }

        log.Append(
            "[Input] ========================================"
        );

        Debug.Log(
            log.ToString()
        );
    }


    void LogPressedControllerButtons()
    {
        if (!logControllerButtons)
            return;

        foreach (InputDevice device in InputSystem.devices)
        {
            if (!IsControllerDevice(device) ||
                !device.enabled)
            {
                continue;
            }

            int buttonIndex = 0;

            foreach (InputControl control in device.allControls)
            {
                if (!(control is ButtonControl button))
                    continue;

                if (button.wasPressedThisFrame)
                {
                    Debug.Log(
                        "[Input][Controle] " +
                        device.displayName +
                        " | Botão #" +
                        buttonIndex +
                        " | Name: " +
                        control.name +
                        " | Display: " +
                        control.displayName +
                        " | Path: " +
                        control.path
                    );
                }

                buttonIndex++;
            }
        }
    }


    // ============================================================
    // EXECUTAR MOVIMENTO
    // ============================================================

    void ExecuteMovement(
        Vector3 inputDir,
        float speed
    )
    {
        Vector3 moveDirection =
            Vector3.zero;


        // --------------------------------------------------------
        // DIREÇÃO RELATIVA À CÂMERA
        // --------------------------------------------------------

        if (inputDir.magnitude >= 0.1f)
        {
            Vector3 cameraForward =
                cameraTransform.forward;

            Vector3 cameraRight =
                cameraTransform.right;


            cameraForward.y = 0f;
            cameraRight.y = 0f;


            cameraForward.Normalize();
            cameraRight.Normalize();


            moveDirection =
                (
                    cameraForward * inputDir.z +
                    cameraRight * inputDir.x
                ).normalized;


            Quaternion targetRotation =
                Quaternion.LookRotation(
                    moveDirection
                );


            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed *
                    Time.deltaTime
                );
        }


        // --------------------------------------------------------
        // INFORMAÇÕES PARA ANIMAÇÃO
        // --------------------------------------------------------

        CurrentMoveDirection =
            moveDirection;


        IsMoving =
            moveDirection.sqrMagnitude >
            0.001f;


        CurrentSpeed =
            IsMoving
                ? speed
                : 0f;


        // --------------------------------------------------------
        // VELOCIDADE HORIZONTAL REAL
        // --------------------------------------------------------

        HorizontalVelocity =
            moveDirection * speed;


        HorizontalSpeed =
            HorizontalVelocity.magnitude;


        // --------------------------------------------------------
        // VELOCIDADE REAL COMPLETA
        // --------------------------------------------------------

        Velocity =
            HorizontalVelocity +
            Vector3.up *
            verticalVelocity;


        // --------------------------------------------------------
        // MOVIMENTO FINAL
        // --------------------------------------------------------

        Vector3 finalMove =
            Velocity;


        controller.Move(
            finalMove *
            Time.deltaTime
        );


        // --------------------------------------------------------
        // CORRIGE VELOCIDADE DEPOIS DO MOVIMENTO
        // --------------------------------------------------------

        Velocity =
            HorizontalVelocity +
            Vector3.up *
            verticalVelocity;
    }


    // ============================================================
    // CAMERA LATE UPDATE
    // ============================================================

    void LateUpdate()
    {
        if (cameraTransform == null)
            return;


        Quaternion rotation =
            Quaternion.Euler(
                cameraRotationX,
                cameraRotationY,
                0f
            );


        Vector3 targetPosition;

        if (isFirstPerson)
        {
            // Primeira pessoa continua exatamente no comportamento anterior.
            // Ao mirar, apenas a animação do braço muda.
            targetPosition =
                transform.position +
                Vector3.up * firstPersonHeight +
                rotation * firstPersonOffset;
        }
        else if (isWeaponAiming)
        {
            // Over the shoulder SOMENTE durante a mira.
            // Fora da mira usamos o mesmo cálculo da versão anterior.
            Vector3 shoulderOffset =
                new Vector3(
                    aimCameraShoulderOffset,
                    aimCameraVerticalOffset,
                    -aimCameraDistance
                );

            targetPosition =
                transform.position +
                Vector3.up * 1.5f +
                rotation * shoulderOffset;
        }
        else
        {
            targetPosition =
                transform.position +
                Vector3.up * 1.5f -
                (
                    rotation *
                    Vector3.forward *
                    distanceFromPlayer
                );
        }


        cameraTransform.position =
            targetPosition;

        cameraTransform.rotation =
            rotation;
    }


    // ============================================================
    // ÁGUA
    // ============================================================

    private void OnTriggerEnter(
        Collider other
    )
    {
        if (other.CompareTag("Water"))
        {
            isUnderwater = true;
            isWeaponAiming = false;

            verticalVelocity = 0f;
        }
    }


    private void OnTriggerExit(
        Collider other
    )
    {
        if (other.CompareTag("Water"))
        {
            isUnderwater = false;

            verticalVelocity = 2f;

            SwimVerticalInput = 0f;
        }
    }
}


// ================================================================
// TIPO DE INPUT ATIVO
// ================================================================

public enum InputMethod
{
    KeyboardMouse,
    Controller
}
