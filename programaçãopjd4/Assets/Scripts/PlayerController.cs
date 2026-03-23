using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Arraste aqui o asset InputAction (InputSystem_Actions)")]
    public InputActionAsset actions;

    [Header("Movement")]
    [Tooltip("Força aplicada para movimentar a bola (aceleração)")]
    public float speed = 10f;
    [Tooltip("Velocidade máxima (magnitude horizontal). 0 = sem limite")]
    public float maxSpeed = 7f;
    [Tooltip("Se verdadeiro, o movimento será relativo à rotação da câmera")]
    public bool useCameraRelative = true;
    [Tooltip("Referência à câmera principal (deixe vazio para obter Camera.main)")]
    public Camera mainCamera;

    // runtime
    private Rigidbody _rb;
    private InputAction _moveAction;
    private Vector2 _moveInput = Vector2.zero;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void OnEnable()
    {
        if (actions == null)
        {
            Debug.LogWarning("PlayerController: nenhum InputActionAsset atribuído (campo 'actions').");
            return;
        }

        var map = actions.FindActionMap("Player", throwIfNotFound: false);
        if (map == null)
        {
            Debug.LogWarning("PlayerController: action map 'Player' não encontrado no InputActionAsset.");
            return;
        }

        _moveAction = map.FindAction("Move", throwIfNotFound: false);
        if (_moveAction == null)
        {
            Debug.LogWarning("PlayerController: action 'Move' não encontrada no action map 'Player'.");
            return;
        }

        _moveAction.performed += OnMovePerformed;
        _moveAction.canceled += OnMoveCanceled;
        _moveAction.Enable();
    }

    void OnDisable()
    {
        if (_moveAction != null)
        {
            _moveAction.performed -= OnMovePerformed;
            _moveAction.canceled -= OnMoveCanceled;
            _moveAction.Disable();
        }
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        _moveInput = Vector2.zero;
    }

    void FixedUpdate()
    {
        if (_rb == null) return;

        // leitura do input em Vector3 (x,z)
        Vector3 input = new Vector3(_moveInput.x, 0f, _moveInput.y);
        if (input.sqrMagnitude < 1e-6f) return; // nada pra fazer

        Vector3 moveDir;
        if (useCameraRelative && mainCamera != null)
        {
            // projetar forward e right da câmera no plano XZ
            Vector3 camForward = mainCamera.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();
            Vector3 camRight = mainCamera.transform.right;
            camRight.y = 0f;
            camRight.Normalize();
            moveDir = camRight * _moveInput.x + camForward * _moveInput.y;
        }
        else
        {
            moveDir = input;
        }

        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        // aplicar força usando aceleração (independente de massa)
        _rb.AddForce(moveDir * speed, ForceMode.Acceleration);

        // limitar velocidade horizontal (manter componente Y para gravidade/salto)
        if (maxSpeed > 0f)
        {
            Vector3 vel = _rb.linearVelocity;
            Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);
            Vector3 limited = Vector3.ClampMagnitude(horizontal, maxSpeed);
            _rb.linearVelocity = new Vector3(limited.x, vel.y, limited.z);
        }
    }

    void OnValidate()
    {
        // facilita configuração no editor
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (speed < 0f) speed = 0f;
        if (maxSpeed < 0f) maxSpeed = 0f;
    }
}

