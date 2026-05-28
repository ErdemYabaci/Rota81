using UnityEngine;

public class ProvinceMaterialApplier : MonoBehaviour
{
    public Material provinceMaterial;

    [ContextMenu("Apply Material To All Provinces")]
    public void ApplyMaterialToAllProvinces()
    {
        if (provinceMaterial == null)
        {
            Debug.LogWarning("ProvinceMaterialApplier: Province Material atanmadı.");
            return;
        }

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);

        foreach (MeshRenderer renderer in renderers)
        {
            renderer.sharedMaterial = provinceMaterial;
        }

        Debug.Log($"ProvinceMaterialApplier: {renderers.Length} objeye materyal uygulandı.");
    }
}