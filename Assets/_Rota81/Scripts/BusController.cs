using UnityEngine;

public class BusController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 8f;
    public float stoppingDistance = 0.05f;

    [Header("Model Rotation Offset")]
    public Vector3 rotationOffsetEuler = Vector3.zero;

    private Vector3 targetPosition;
    private bool hasTarget;

    private void Update()
    {
        if (!hasTarget)
            return;

        MoveToTarget();
        RotateToTarget();
    }

    public void MoveToProvince(ProvinceController province)
    {
        if (province == null)
        {
            Debug.LogWarning("BusController: Province null geldi.");
            return;
        }

        targetPosition = province.GetBusStopPosition();
        hasTarget = true;
    }

    public void SetPositionToProvince(ProvinceController province)
    {
        if (province == null)
            return;

        transform.position = province.GetBusStopPosition();
        targetPosition = transform.position;
        hasTarget = false;
    }

    private void MoveToTarget()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance <= stoppingDistance)
        {
            transform.position = targetPosition;
            hasTarget = false;
        }
    }

    private void RotateToTarget()
    {
        Vector3 direction = targetPosition - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);
        Quaternion offsetRotation = Quaternion.Euler(rotationOffsetEuler);
        Quaternion finalRotation = lookRotation * offsetRotation;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            finalRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}