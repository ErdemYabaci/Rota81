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

    [Header("Province List")]
    [SerializeField] private List<ProvinceEntry> provinces = new List<ProvinceEntry>();

    private Dictionary<string, ProvinceController> provinceDictionary;

    private void Awake()
    {
        BuildDictionary();
    }

    private void BuildDictionary()
    {
        provinceDictionary = new Dictionary<string, ProvinceController>();

        foreach (ProvinceEntry entry in provinces)
        {
            if (entry == null || entry.provinceController == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.provinceName))
                continue;

            if (provinceDictionary.ContainsKey(entry.provinceName))
            {
                Debug.LogWarning($"RouteMapManager: Aynı il adı tekrar eklenmiş: {entry.provinceName}");
                continue;
            }

            provinceDictionary.Add(entry.provinceName, entry.provinceController);
        }

        Debug.Log($"RouteMapManager: {provinceDictionary.Count} il dictionary içine alındı.");
    }

    public ProvinceController GetProvince(string provinceName)
    {
        if (provinceDictionary == null)
            BuildDictionary();

        if (provinceDictionary.TryGetValue(provinceName, out ProvinceController province))
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