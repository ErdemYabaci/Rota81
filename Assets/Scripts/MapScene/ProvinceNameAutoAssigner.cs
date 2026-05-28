using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class ProvinceNameAutoAssigner : MonoBehaviour
{
    [Header("References")]
    public Transform provincesRoot;
    public TextAsset geoJsonFile;
    public RouteMapManager routeMapManager;

    [Header("GeoJSON Settings")]
    public string primaryNameField = "name";

    [Header("Rename Options")]
    public bool renamePivotObjects = true;

    [ContextMenu("Assign Province Names From GeoJSON Order")]
    public void AssignProvinceNamesFromGeoJsonOrder()
    {
        if (provincesRoot == null)
        {
            Debug.LogWarning("ProvinceNameAutoAssigner: Provinces Root atanmadı.");
            return;
        }

        if (geoJsonFile == null)
        {
            Debug.LogWarning("ProvinceNameAutoAssigner: GeoJSON dosyası atanmadı.");
            return;
        }

        List<string> provinceNames = ExtractProvinceNames(geoJsonFile.text);

        if (provinceNames.Count == 0)
        {
            Debug.LogWarning("ProvinceNameAutoAssigner: GeoJSON içinden il adı okunamadı.");
            return;
        }

        List<ProvinceController> controllers = provincesRoot
            .GetComponentsInChildren<ProvinceController>(true)
            .OrderBy(c => ExtractLeadingNumber(c.gameObject.name))
            .ToList();

        if (controllers.Count == 0)
        {
            Debug.LogWarning("ProvinceNameAutoAssigner: ProvinceController bulunamadı.");
            return;
        }

        int assignCount = Mathf.Min(provinceNames.Count, controllers.Count);

        for (int i = 0; i < assignCount; i++)
        {
            string provinceName = provinceNames[i];
            ProvinceController controller = controllers[i];

            controller.provinceName = provinceName;

            if (renamePivotObjects)
            {
                controller.gameObject.name = SanitizeObjectName(provinceName) + "_Pivot";
            }
        }

        if (routeMapManager != null)
        {
            routeMapManager.RebuildProvinceListFromChildren();
        }

        Debug.Log($"ProvinceNameAutoAssigner: {assignCount} il ismi otomatik atandı.");
    }

    private List<string> ExtractProvinceNames(string json)
{
    List<string> names = new List<string>();

    MatchCollection propertyMatches = Regex.Matches(
        json,
        "\"properties\"\\s*:\\s*\\{(.*?)\\}",
        RegexOptions.Singleline
    );

    foreach (Match propertyMatch in propertyMatches)
    {
        string propertiesBlock = propertyMatch.Groups[1].Value;

        string nameValue;

        if (TryGetJsonStringField(propertiesBlock, primaryNameField, out nameValue) ||
            TryGetJsonStringField(propertiesBlock, "name", out nameValue) ||
            TryGetJsonStringField(propertiesBlock, "NAME_1", out nameValue) ||
            TryGetJsonStringField(propertiesBlock, "NAME", out nameValue) ||
            TryGetJsonStringField(propertiesBlock, "shapeName", out nameValue) ||
            TryGetJsonStringField(propertiesBlock, "province", out nameValue) ||
            TryGetJsonStringField(propertiesBlock, "il_adi", out nameValue))
        {
            names.Add(FixProvinceName(nameValue));
        }
    }

    Debug.Log($"ProvinceNameAutoAssigner: GeoJSON içinden {names.Count} il adı okundu.");

    if (names.Count > 0)
    {
        Debug.Log("İlk okunan iller: " + string.Join(", ", names.GetRange(0, Mathf.Min(10, names.Count))));
    }

    return names;
}

private string FixProvinceName(string name)
{
    return name
        .Replace("Sirnak", "Şırnak")
        .Replace("Iğdir", "Iğdır")
        .Replace("Istanbul", "İstanbul")
        .Replace("Izmir", "İzmir")
        .Replace("Afyonkarahisar", "Afyonkarahisar");
}

    private bool TryGetJsonStringField(string jsonBlock, string fieldName, out string value)
    {
        value = "";

        string pattern = "\"" + Regex.Escape(fieldName) + "\"\\s*:\\s*\"([^\"]+)\"";
        Match match = Regex.Match(jsonBlock, pattern);

        if (!match.Success)
            return false;

        value = Regex.Unescape(match.Groups[1].Value);
        return true;
    }

    private int ExtractLeadingNumber(string objectName)
    {
        Match match = Regex.Match(objectName, "^(\\d+)");

        if (!match.Success)
            return int.MaxValue;

        int number;

        if (int.TryParse(match.Groups[1].Value, out number))
            return number;

        return int.MaxValue;
    }

    private string SanitizeObjectName(string name)
    {
        return name
            .Replace(" ", "_")
            .Replace("İ", "I")
            .Replace("ı", "i")
            .Replace("Ş", "S")
            .Replace("ş", "s")
            .Replace("Ğ", "G")
            .Replace("ğ", "g")
            .Replace("Ü", "U")
            .Replace("ü", "u")
            .Replace("Ö", "O")
            .Replace("ö", "o")
            .Replace("Ç", "C")
            .Replace("ç", "c");
    }
}