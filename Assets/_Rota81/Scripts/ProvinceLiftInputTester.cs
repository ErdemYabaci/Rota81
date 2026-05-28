using UnityEngine;
using UnityEngine.InputSystem;

public class ProvinceLiftInputTester : MonoBehaviour
{
    [Header("Test edilecek province controller")]
    public ProvinceController targetProvince;

    private InputAction toggleAction;

    private void Awake()
    {
        toggleAction = new InputAction(
            name: "ToggleProvinceLift",
            type: InputActionType.Button,
            binding: "<Keyboard>/space"
        );

        toggleAction.performed += OnTogglePerformed;
    }

    private void OnEnable()
    {
        toggleAction.Enable();
    }

    private void OnDisable()
    {
        toggleAction.Disable();
    }

    private void OnDestroy()
    {
        toggleAction.performed -= OnTogglePerformed;
        toggleAction.Dispose();
    }

    private void OnTogglePerformed(InputAction.CallbackContext context)
    {
        if (targetProvince == null)
        {
            Debug.LogWarning("ProvinceLiftInputTester: Target Province atanmadı.");
            return;
        }

        targetProvince.ToggleLift();
    }
}