using System;
using System.Collections.Generic;
using UnityEngine;

public class RouteMapManager : MonoBehaviour
{
    [Serializable]
    public class ProvinceEntry
    {
        public string provinceName;
        public ProvinceController provinceController;
    }

    [Header("Province Root")]
    [SerializeField] private Transform provincesRoot;

    [Header("Province List")]
    [SerializeField] private List<ProvinceEntry> provinces = new List<ProvinceEntry>();

    private Dictionary<string, ProvinceController> provinceDictionary;
    private Dictionary<string, ProvinceController> normalizedProvinceDictionary;

    private void Awake()
    {
        BuildDictionary();
    }

    private void BuildDictionary()
    {
        provinceDictionary = new Dictionary<string, ProvinceController>();
        normalizedProvinceDictionary = new Dictionary<string, ProvinceController>();

        foreach (ProvinceEntry entry in provinces)
        {
            if (entry == null || entry.provinceController == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.provinceName))
                continue;

            if (!provinceDictionary.ContainsKey(entry.provinceName))
            {
                provinceDictionary.Add(entry.provinceName, entry.provinceController);
            }

            string norm = NormalizeName(entry.provinceName);
            if (!string.IsNullOrEmpty(norm) && !normalizedProvinceDictionary.ContainsKey(norm))
            {
                normalizedProvinceDictionary.Add(norm, entry.provinceController);
            }
        }

        Debug.Log($"RouteMapManager: {provinceDictionary.Count} il dictionary içine alındı.");
    }

    private string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "";

        name = name.ToLowerInvariant();
        
        // Remove spaces, dots, hyphens
        name = name.Replace(" ", "").Replace(".", "").Replace("-", "");

        // Convert Turkish characters to English equivalents for loose comparison
        name = name.Replace("ı", "i")
                   .Replace("ş", "s")
                   .Replace("ğ", "g")
                   .Replace("ü", "u")
                   .Replace("ö", "o")
                   .Replace("ç", "c");

        // Specific GeoJSON spelling oddities
        if (name == "kmaras") return "kahramanmaras";
        if (name == "kinkkale") return "kirikkale";
        if (name == "zinguldak") return "zonguldak";

        return name;
    }

    [ContextMenu("Rebuild Province List From Children")]
    public void RebuildProvinceListFromChildren()
    {
        if (provincesRoot == null)
        {
            Debug.LogWarning("RouteMapManager: Provinces Root atanmadı.");
            return;
        }

        provinces.Clear();

        ProvinceController[] controllers = provincesRoot.GetComponentsInChildren<ProvinceController>(true);

        foreach (ProvinceController controller in controllers)
        {
            if (controller == null)
                continue;

            if (string.IsNullOrWhiteSpace(controller.provinceName))
                continue;

            ProvinceEntry entry = new ProvinceEntry
            {
                provinceName = controller.provinceName,
                provinceController = controller
            };

            provinces.Add(entry);
        }

        BuildDictionary();

        Debug.Log($"RouteMapManager: {provinces.Count} il child objelerden listeye eklendi.");
    }

    public string[] GetAllProvinceNames()
    {
        List<string> names = new List<string>();

        foreach (ProvinceEntry entry in provinces)
        {
            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.provinceName))
                continue;

            if (entry.provinceController == null)
                continue;

            names.Add(entry.provinceName);
        }

        return names.ToArray();
    }

    public ProvinceController GetProvince(string provinceName)
    {
        if (provinceDictionary == null || normalizedProvinceDictionary == null)
            BuildDictionary();

        if (string.IsNullOrWhiteSpace(provinceName))
            return null;

        // Try exact match first
        if (provinceDictionary.TryGetValue(provinceName, out ProvinceController province))
        {
            return province;
        }

        // Try normalized lookup
        string norm = NormalizeName(provinceName);
        if (normalizedProvinceDictionary.TryGetValue(norm, out province))
        {
            return province;
        }

        Debug.LogWarning($"RouteMapManager: İl bulunamadı: {provinceName}");
        return null;
    }

    public void LiftProvince(string provinceName)
    {
        ProvinceController province = GetProvince(provinceName);

        if (province == null)
            return;

        province.Lift();
    }

    public void LowerProvince(string provinceName)
    {
        ProvinceController province = GetProvince(provinceName);

        if (province == null)
            return;

        province.Lower();
    }

    public void LowerAllProvinces()
    {
        foreach (ProvinceEntry entry in provinces)
        {
            if (entry.provinceController != null)
            {
                entry.provinceController.Lower();
            }
        }
    }
}