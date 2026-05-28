using UnityEngine;
using UnityEngine.InputSystem;

public class ProvinceLiftTester : MonoBehaviour
{
    [Header("Test edilecek il objesi")]
    public Transform targetProvince;

    [Header("Yükselme ayarları")]
    public float liftAmount = 0.7f;
    public float moveSpeed = 3f;

    private Vector3 originalLocalPosition;
    private Vector3 targetLocalPosition;
    private bool isLifted = false;

    private InputAction toggleLiftAction;

    private void Awake()
    {
        toggleLiftAction = new InputAction(
            name: "ToggleProvinceLift",
            type: InputActionType.Button,
            binding: "<Keyboard>/space"
        );

        toggleLiftAction.performed += OnToggleLiftPerformed;
    }

    private void OnEnable()
    {
        toggleLiftAction.Enable();
    }

    private void OnDisable()
    {
        toggleLiftAction.Disable();
    }

    private void OnDestroy()
    {
        toggleLiftAction.performed -= OnToggleLiftPerformed;
        toggleLiftAction.Dispose();
    }

    private void Start()
    {
        if (targetProvince == null)
        {
            Debug.LogWarning("ProvinceLiftTester: Target Province atanmadı.");
            return;
        }

        originalLocalPosition = targetProvince.localPosition;
        targetLocalPosition = originalLocalPosition;
    }

    private void Update()
    {
        if (targetProvince == null)
            return;

        targetProvince.localPosition = Vector3.Lerp(
            targetProvince.localPosition,
            targetLocalPosition,
            Time.deltaTime * moveSpeed
        );
    }

    private void OnToggleLiftPerformed(InputAction.CallbackContext context)
    {
        ToggleProvinceLift();
    }

    private void ToggleProvinceLift()
    {
        isLifted = !isLifted;

        if (isLifted)
        {
            targetLocalPosition = originalLocalPosition + Vector3.up * liftAmount;
        }
        else
        {
            targetLocalPosition = originalLocalPosition;
        }
    }
}