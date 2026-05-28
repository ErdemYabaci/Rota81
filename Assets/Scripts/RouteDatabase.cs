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
        },
        {
            "Akdeniz",
            new string[]
            {
                "Antalya",
                "Burdur",
                "Isparta",
                "Mersin",
                "Adana",
                "Osmaniye",
                "Hatay",
                "Kahramanmaraş"
            }
        },
        {
            "Ege",
            new string[]
            {
                "Muğla",
                "Aydın",
                "İzmir",
                "Manisa",
                "Denizli",
                "Uşak",
                "Kütahya",
                "Afyonkarahisar"
            }
        },
        {
            "Marmara",
            new string[]
            {
                "Çanakkale",
                "Balıkesir",
                "Bursa",
                "Bilecik",
                "Sakarya",
                "Kocaeli",
                "İstanbul",
                "Tekirdağ",
                "Edirne",
                "Kırklareli"
            }
        },
        {
            "Doğu Anadolu",
            new string[]
            {
                "Şırnak",
                "Hakkâri",
                "Van",
                "Bitlis",
                "Muş",
                "Bingöl",
                "Tunceli",
                "Elazığ",
                "Malatya"
            }
        },
        {
            "İç Anadolu",
            new string[]
            {
                "Eskişehir",
                "Ankara",
                "Kırıkkale",
                "Kırşehir",
                "Yozgat",
                "Sivas",
                "Kayseri",
                "Nevşehir",
                "Aksaray",
                "Niğde",
                "Konya"
            }
        }
    };
}
