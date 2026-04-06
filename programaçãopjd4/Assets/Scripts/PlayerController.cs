using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Arraste aqui o asset InputAction (InputSystem_Actions)")]
    public InputActionAsset actions;
    public string actionMapName = "Player";
    public string moveActionName = "Move";

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
    private Vector3 _moveDir = Vector3.zero;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null)
        {
            // fallback: try to find any Camera in the scene
            var cam = Object.FindObjectOfType<Camera>();
            if (cam != null)
            {
                mainCamera = cam;
                Debug.LogWarning("PlayerController: Camera.main was null; falling back to first Camera found in scene.");
            }
            else
            {
                Debug.LogWarning("PlayerController: no Camera found in scene. Camera-relative movement will be disabled.");
                useCameraRelative = false;
            }
        }
    }

    void OnEnable()
    {
        if (actions == null)
        {
            Debug.LogWarning("PlayerController: no InputActionAsset assigned.");
            return;
        }
        var map = actions.FindActionMap(actionMapName, throwIfNotFound: false);
        if (map == null)
        {
            Debug.LogWarning($"PlayerController: action map '{actionMapName}' not found.");
            return;
        }

        _moveAction = map.FindAction(moveActionName, throwIfNotFound: false);
        if (_moveAction == null)
        {
            Debug.LogWarning($"PlayerController: action '{moveActionName}' not found in map '{actionMapName}'.");
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

        Vector3 input = new Vector3(_moveInput.x, 0f, _moveInput.y);
        if (input.sqrMagnitude < 1e-6f) return; // nothing to do

        if (useCameraRelative && mainCamera != null)
        {
            Vector3 camForward = mainCamera.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();
            Vector3 camRight = mainCamera.transform.right;
            camRight.y = 0f;
            camRight.Normalize();
            _moveDir = camRight * input.x + camForward * input.z;
        }
        else
        {
            _moveDir = input;
        }

        if (_moveDir.sqrMagnitude > 1f) _moveDir.Normalize();

        // apply acceleration-based movement
        _rb.AddForce(_moveDir * speed, ForceMode.Acceleration);

        // clamp horizontal speed
        if (maxSpeed > 0f)
        {
            // use linearVelocity to avoid obsolete velocity usage
            Vector3 vel = _rb.linearVelocity;
            Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);
            Vector3 limited = Vector3.ClampMagnitude(horizontal, maxSpeed);
            _rb.linearVelocity = new Vector3(limited.x, vel.y, limited.z);
        }
    }

    void OnValidate()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (speed < 0f) speed = 0f;
        if (maxSpeed < 0f) maxSpeed = 0f;
    }
}

