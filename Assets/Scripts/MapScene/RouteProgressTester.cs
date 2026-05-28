using UnityEngine;
using UnityEngine.InputSystem;

public class RouteProgressTester : MonoBehaviour
{
    [Header("Managers")]
    public RouteMapManager routeMapManager;

    [Header("Player Bus")]
    public BusController playerBus;

    [Header("Test Route")]
    public string[] routeCities = new string[]
    {
        "Ankara",
        "İstanbul",
        "İzmir"
    };

    private int currentCityIndex = 0;

    private InputAction nextCityAction;
    private InputAction resetRouteAction;

    private void Awake()
    {
        nextCityAction = new InputAction(
            name: "NextCity",
            type: InputActionType.Button,
            binding: "<Keyboard>/n"
        );

        resetRouteAction = new InputAction(
            name: "ResetRoute",
            type: InputActionType.Button,
            binding: "<Keyboard>/r"
        );

        nextCityAction.performed += _ => GoToNextCity();
        resetRouteAction.performed += _ => ResetRoute();
    }

    private void OnEnable()
    {
        nextCityAction.Enable();
        resetRouteAction.Enable();
    }

    private void OnDisable()
    {
        nextCityAction.Disable();
        resetRouteAction.Disable();
    }

    private void OnDestroy()
    {
        nextCityAction.Dispose();
        resetRouteAction.Dispose();
    }

    private void Start()
    {
        if (GameState.GameInitialized)
        {
            // Prevent test helper from overriding in-game bus placement.
            return;
        }

        PlaceBusAtCurrentCity();
    }

    public void GoToNextCity()
    {
        if (routeCities == null || routeCities.Length == 0)
        {
            Debug.LogWarning("RouteProgressTester: Rota boş.");
            return;
        }

        if (currentCityIndex >= routeCities.Length - 1)
        {
            Debug.Log("RouteProgressTester: Rota tamamlandı.");
            return;
        }

        currentCityIndex++;

        string nextCity = routeCities[currentCityIndex];

        MoveBusToCity(nextCity);

        Debug.Log($"RouteProgressTester: Sonraki şehir: {nextCity}");
    }

    public void ResetRoute()
    {
        currentCityIndex = 0;
        PlaceBusAtCurrentCity();

        Debug.Log("RouteProgressTester: Rota başa alındı.");
    }

    private void PlaceBusAtCurrentCity()
    {
        if (routeCities == null || routeCities.Length == 0)
            return;

        string currentCity = routeCities[currentCityIndex];

        ProvinceController province = routeMapManager.GetProvince(currentCity);

        if (province == null)
            return;

        routeMapManager.LowerAllProvinces();
        routeMapManager.LiftProvince(currentCity);

        playerBus.SetPositionToProvince(province);
    }

    private void MoveBusToCity(string cityName)
    {
        ProvinceController province = routeMapManager.GetProvince(cityName);

        if (province == null)
            return;

        routeMapManager.LowerAllProvinces();
        routeMapManager.LiftProvince(cityName);

        playerBus.MoveToProvince(province);
    }
}