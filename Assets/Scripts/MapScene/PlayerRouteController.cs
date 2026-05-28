using UnityEngine;

public class PlayerRouteController : MonoBehaviour
{
    [Header("Player Info")]
    public string playerName = "Player";

    [Header("References")]
    public RouteMapManager routeMapManager;
    public BusController busController;

    [Header("Default Route Data")]
    public RouteData routeData;

    [Header("State")]
    [SerializeField] private int currentCityIndex = 0;

    private string[] runtimeRouteCities;

    public int CurrentCityIndex => currentCityIndex;

    private string[] ActiveRouteCities
    {
        get
        {
            if (runtimeRouteCities != null && runtimeRouteCities.Length > 0)
                return runtimeRouteCities;

            if (routeData != null)
                return routeData.cityNames;

            return null;
        }
    }

    public string CurrentCityName
    {
        get
        {
            string[] activeRoute = ActiveRouteCities;

            if (activeRoute == null || activeRoute.Length == 0)
                return "";

            return activeRoute[currentCityIndex];
        }
    }

    public bool HasFinishedRoute
    {
        get
        {
            string[] activeRoute = ActiveRouteCities;

            if (activeRoute == null || activeRoute.Length == 0)
                return false;

            return currentCityIndex >= activeRoute.Length - 1;
        }
    }

    private void Start()
    {
        PlaceAtCurrentCity();
    }

    public void SetRoute(string[] newRouteCities, bool placeAtStart = true)
    {
        if (newRouteCities == null || newRouteCities.Length == 0)
        {
            Debug.LogWarning($"{playerName}: Yeni rota boş geldi.");
            return;
        }

        runtimeRouteCities = newRouteCities;
        currentCityIndex = 0;

        if (placeAtStart)
        {
            PlaceAtCurrentCity();
        }

        Debug.Log($"{playerName}: Yeni rota atandı. Şehir sayısı: {runtimeRouteCities.Length}");
    }

    public void ClearRuntimeRoute()
    {
        runtimeRouteCities = null;
        currentCityIndex = 0;

        PlaceAtCurrentCity();

        Debug.Log($"{playerName}: Runtime rota temizlendi, default RouteData kullanılacak.");
    }

    public void PlaceAtCurrentCity()
    {
        if (!HasValidSetup())
            return;

        string[] activeRoute = ActiveRouteCities;
        string cityName = activeRoute[currentCityIndex];

        ProvinceController province = routeMapManager.GetProvince(cityName);

        if (province == null)
            return;

        busController.SetPositionToProvince(province);

        Debug.Log($"{playerName}: Başlangıç/mevcut şehir: {cityName}");
    }

    public void MoveToNextCity()
    {
        if (!HasValidSetup())
            return;

        if (HasFinishedRoute)
        {
            Debug.Log($"{playerName}: Rotayı zaten tamamladı.");
            return;
        }

        string[] activeRoute = ActiveRouteCities;

        currentCityIndex++;

        string nextCityName = activeRoute[currentCityIndex];
        ProvinceController nextProvince = routeMapManager.GetProvince(nextCityName);

        if (nextProvince == null)
            return;

        routeMapManager.LowerAllProvinces();
        routeMapManager.LiftProvince(nextCityName);

        busController.MoveToProvince(nextProvince);

        Debug.Log($"{playerName}: {nextCityName} şehrine ilerliyor.");
    }

    public void ResetRoute()
    {
        currentCityIndex = 0;
        PlaceAtCurrentCity();

        Debug.Log($"{playerName}: Rota sıfırlandı.");
    }

    private bool HasRoute()
    {
        string[] activeRoute = ActiveRouteCities;

        return activeRoute != null && activeRoute.Length > 0;
    }

    private bool HasValidSetup()
    {
        if (routeMapManager == null)
        {
            Debug.LogWarning($"{playerName}: RouteMapManager atanmadı.");
            return false;
        }

        if (busController == null)
        {
            Debug.LogWarning($"{playerName}: BusController atanmadı.");
            return false;
        }

        if (!HasRoute())
        {
            Debug.LogWarning($"{playerName}: RouteData veya runtime rota boş.");
            return false;
        }

        return true;
    }
}