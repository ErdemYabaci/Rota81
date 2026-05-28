using System.Collections.Generic;
using UnityEngine;

public class RandomRouteGenerator : MonoBehaviour
{
    [Header("References")]
    public RouteMapManager routeMapManager;

    [Header("Auto Generate")]
    public bool generateOnStart = true;

    [Header("Players")]
    public PlayerRouteController player1;
    public PlayerRouteController player2;

    [Header("Random Route Settings")]
    public int cityCount = 20;
    public bool giveSameRouteToBothPlayers = true;

    [ContextMenu("Generate Random Route For Players")]

    private void Start()
{
    if (GameState.GameInitialized)
    {
        // Real game flow controls route + bus positions via GameState/MapSceneManager.
        return;
    }

    if (generateOnStart)
    {
        GenerateRandomRouteForPlayers();
    }
}
    public void GenerateRandomRouteForPlayers()
    {
        if (routeMapManager == null)
        {
            Debug.LogWarning("RandomRouteGenerator: RouteMapManager atanmadı.");
            return;
        }

        string[] availableCities = routeMapManager.GetAllProvinceNames();

        if (availableCities == null || availableCities.Length == 0)
        {
            Debug.LogWarning("RandomRouteGenerator: Kullanılabilir şehir bulunamadı.");
            return;
        }

        int actualCityCount = Mathf.Min(cityCount, availableCities.Length);

        string[] routeForPlayer1 = GenerateUniqueRandomRoute(availableCities, actualCityCount);

        if (player1 != null)
        {
            player1.SetRoute(routeForPlayer1);
        }

        if (player2 != null)
        {
            if (giveSameRouteToBothPlayers)
            {
                player2.SetRoute(routeForPlayer1);
            }
            else
            {
                string[] routeForPlayer2 = GenerateUniqueRandomRoute(availableCities, actualCityCount);
                player2.SetRoute(routeForPlayer2);
            }
        }

        Debug.Log($"RandomRouteGenerator: {actualCityCount} şehirlik rastgele rota oluşturuldu.");
    }

    private string[] GenerateUniqueRandomRoute(string[] availableCities, int count)
    {
        List<string> pool = new List<string>(availableCities);
        List<string> route = new List<string>();

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);

            route.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }

        return route.ToArray();
    }
}