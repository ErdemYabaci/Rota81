using UnityEngine;

public class ProvinceControllerAutoSetup : MonoBehaviour
{
    [Header("Genel Province Ayarları")]
    public float liftAmount = 0.35f;
    public float moveSpeed = 5f;

    [Header("İsimlendirme")]
    public bool useObjectNameAsProvinceName = true;

    [ContextMenu("Add ProvinceController To All Pivots")]
    public void AddProvinceControllersToAllPivots()
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);

        int addedCount = 0;
        int updatedCount = 0;

        foreach (Transform child in allChildren)
        {
            if (!child.name.EndsWith("_Pivot"))
                continue;

            ProvinceController controller = child.GetComponent<ProvinceController>();

            if (controller == null)
            {
                controller = child.gameObject.AddComponent<ProvinceController>();
                addedCount++;
            }
            else
            {
                updatedCount++;
            }

            controller.liftAmount = liftAmount;
            controller.moveSpeed = moveSpeed;

            if (useObjectNameAsProvinceName)
            {
                controller.provinceName = child.name.Replace("_Pivot", "");
            }
        }

        Debug.Log($"ProvinceControllerAutoSetup: {addedCount} yeni controller eklendi, {updatedCount} controller güncellendi.");
    }
}