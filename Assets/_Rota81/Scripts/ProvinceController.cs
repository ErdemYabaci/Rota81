using UnityEngine;

public class ProvinceController : MonoBehaviour
{
    [Header("Province Info")]
    public string provinceName;

    [Header("Lift Settings")]
    public float liftAmount = 0.35f;
    public float moveSpeed = 5f;

    [Header("Auto Bus Stop Settings")]
    public float busStopHeightOffset = 0.35f;

    private Vector3 originalLocalPosition;
    private Vector3 targetLocalPosition;
    private bool isLifted;

    private MeshRenderer provinceRenderer;

    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
        targetLocalPosition = originalLocalPosition;

        provinceRenderer = GetComponentInChildren<MeshRenderer>();
    }

    private void Update()
    {
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetLocalPosition,
            Time.deltaTime * moveSpeed
        );
    }

    public void Lift()
    {
        isLifted = true;
        targetLocalPosition = originalLocalPosition + Vector3.up * liftAmount;
    }

    public void Lower()
    {
        isLifted = false;
        targetLocalPosition = originalLocalPosition;
    }

    public void ToggleLift()
    {
        if (isLifted)
        {
            Lower();
        }
        else
        {
            Lift();
        }
    }

    public Vector3 GetBusStopPosition()
    {
        if (provinceRenderer == null)
        {
            provinceRenderer = GetComponentInChildren<MeshRenderer>();
        }

        if (provinceRenderer == null)
        {
            return transform.position;
        }

        Bounds bounds = provinceRenderer.bounds;

        return new Vector3(
            bounds.center.x,
            bounds.max.y + busStopHeightOffset,
            bounds.center.z
        );
    }
}