using System.Collections.Generic;

/// <summary>
/// Hard-coded route catalogue.  Add new routes here as the game grows.
/// No MonoBehaviour needed — just a static dictionary.
/// </summary>
public static class RouteDatabase
{
    /// <summary>Key = route display name, Value = ordered city names.</summary>
    public static readonly Dictionary<string, string[]> Routes =
        new Dictionary<string, string[]>
    {
        {
            "Karadeniz",
            new string[]
            {
                "Zonguldak",
                "Kastamonu",
                "Sinop",
                "Samsun",
                "Ordu",
                "Giresun",
                "Trabzon",
                "Rize",
                "Artvin"
            }
        }
    };
}
