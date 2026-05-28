using UnityEngine;

[CreateAssetMenu(fileName = "NewRouteData", menuName = "Rota81/Route Data")]
public class RouteData : ScriptableObject
{
    [Header("Route Info")]
    public string routeName;

    [Header("Cities In Route")]
    public string[] cityNames;
}