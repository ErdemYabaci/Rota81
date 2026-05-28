using UnityEngine;

public class BusController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 8f;
    public float stoppingDistance = 0.05f;

    [Header("Position Offset")]
    public Vector3 positionOffset = Vector3.zero;

    [Header("Model Rotation Offset")]
    public Vector3 rotationOffsetEuler = Vector3.zero;

    [Header("Penalty Spin")]
    public float minSpinDelay = 0.05f;
    public float maxSpinDelay = 0.2f;
    public float minSpinDuration = 0.45f;
    public float maxSpinDuration = 1.25f;
    public float spinDegreesPerSecond = 540f;

    private Vector3 targetPosition;
    private bool hasTarget;
    private Coroutine spinCoroutine;

    /// <summary>True while the bus is travelling toward its target.</summary>
    public bool IsMoving => hasTarget;

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

        targetPosition = province.GetBusStopPosition() + positionOffset;
        hasTarget = true;
    }

    public void SetPositionToProvince(ProvinceController province)
    {
        if (province == null)
            return;

        transform.position = province.GetBusStopPosition() + positionOffset;
        targetPosition = transform.position;
        hasTarget = false;
    }

    public void StartPenaltySpin()
    {
        if (!isActiveAndEnabled)
            return;

        if (spinCoroutine != null)
            StopCoroutine(spinCoroutine);

        spinCoroutine = StartCoroutine(PenaltySpinRoutine());
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

    private System.Collections.IEnumerator PenaltySpinRoutine()
    {
        float delay = Mathf.Max(minSpinDelay, maxSpinDelay);
        if (maxSpinDelay > minSpinDelay)
            delay = Random.Range(minSpinDelay, maxSpinDelay);

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float duration = Mathf.Max(minSpinDuration, maxSpinDuration);
        if (maxSpinDuration > minSpinDuration)
            duration = Random.Range(minSpinDuration, maxSpinDuration);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float frameSpin = spinDegreesPerSecond * Time.deltaTime;
            transform.Rotate(Vector3.up, frameSpin, Space.World);
            elapsed += Time.deltaTime;
            yield return null;
        }

        spinCoroutine = null;
    }
}