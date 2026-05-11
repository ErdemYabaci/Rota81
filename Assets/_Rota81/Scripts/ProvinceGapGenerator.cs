using UnityEngine;

public class ProvinceGapGenerator : MonoBehaviour
{
    [Header("Boşluk Ayarı")]
    [Range(0.80f, 1.00f)]
    public float shrinkAmount = 0.96f;

    [Header("Dikey eksen")]
    public bool useYAsVerticalAxis = true;

    [ContextMenu("Create Gaps Between Provinces")]
    public void CreateGapsBetweenProvinces()
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);

        int processedCount = 0;

        foreach (MeshRenderer renderer in renderers)
        {
            Transform province = renderer.transform;

            if (province == transform)
                continue;

            if (province.parent != null && province.parent.name.EndsWith("_Pivot"))
                continue;

            Bounds bounds = renderer.bounds;

            GameObject pivotObject = new GameObject(province.name + "_Pivot");
            Transform pivot = pivotObject.transform;

            Transform oldParent = province.parent;
            int oldSiblingIndex = province.GetSiblingIndex();

            pivot.SetParent(oldParent, true);
            pivot.position = bounds.center;
            pivot.rotation = Quaternion.identity;
            pivot.localScale = Vector3.one;
            pivot.SetSiblingIndex(oldSiblingIndex);

            province.SetParent(pivot, true);

            if (useYAsVerticalAxis)
            {
                pivot.localScale = new Vector3(shrinkAmount, 1f, shrinkAmount);
            }
            else
            {
                pivot.localScale = new Vector3(shrinkAmount, shrinkAmount, 1f);
            }

            processedCount++;
        }

        Debug.Log($"ProvinceGapGenerator: {processedCount} il parçası küçültüldü ve aralara boşluk açıldı.");
    }
}