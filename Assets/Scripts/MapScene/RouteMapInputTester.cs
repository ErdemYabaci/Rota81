using UnityEngine;
using UnityEngine.InputSystem;

public class RouteMapInputTester : MonoBehaviour
{
    [Header("Route Map Manager")]
    public RouteMapManager routeMapManager;

    [Header("Bus Controller")]
    public BusController busController;

    [Header("Test Province Names")]
    public string province1 = "Ankara";
    public string province2 = "İstanbul";
    public string province3 = "İzmir";

    private InputAction press1Action;
    private InputAction press2Action;
    private InputAction press3Action;
    private InputAction lowerAllAction;

    private void Awake()
    {
        press1Action = new InputAction("GoProvince1", InputActionType.Button, "<Keyboard>/1");
        press2Action = new InputAction("GoProvince2", InputActionType.Button, "<Keyboard>/2");
        press3Action = new InputAction("GoProvince3", InputActionType.Button, "<Keyboard>/3");
        lowerAllAction = new InputAction("LowerAllProvinces", InputActionType.Button, "<Keyboard>/0");

        press1Action.performed += _ => GoToProvince(province1);
        press2Action.performed += _ => GoToProvince(province2);
        press3Action.performed += _ => GoToProvince(province3);
        lowerAllAction.performed += _ => LowerAll();
    }

    private void OnEnable()
    {
        press1Action.Enable();
        press2Action.Enable();
        press3Action.Enable();
        lowerAllAction.Enable();
    }

    private void OnDisable()
    {
        press1Action.Disable();
        press2Action.Disable();
        press3Action.Disable();
        lowerAllAction.Disable();
    }

    private void OnDestroy()
    {
        press1Action.Dispose();
        press2Action.Dispose();
        press3Action.Dispose();
        lowerAllAction.Dispose();
    }

    private void GoToProvince(string provinceName)
    {
        if (routeMapManager == null)
        {
            Debug.LogWarning("RouteMapInputTester: RouteMapManager atanmadı.");
            return;
        }

        if (busController == null)
        {
            Debug.LogWarning("RouteMapInputTester: BusController atanmadı.");
            return;
        }

        ProvinceController province = routeMapManager.GetProvince(provinceName);

        if (province == null)
            return;

        routeMapManager.LowerAllProvinces();
        routeMapManager.LiftProvince(provinceName);

        busController.MoveToProvince(province);

        Debug.Log($"RouteMapInputTester: Otobüs {provinceName} iline gidiyor.");
    }

    private void LowerAll()
    {
        if (routeMapManager == null)
            return;

        routeMapManager.LowerAllProvinces();

        Debug.Log("RouteMapInputTester: Tüm iller indirildi.");
    }
}