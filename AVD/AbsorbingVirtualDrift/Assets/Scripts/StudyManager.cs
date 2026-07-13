using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

public class StudyManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridSnapper gridSnapper;
    [SerializeField] private Transform targetBlock;
    [SerializeField] private PlacementBlock movableBlock;
    [SerializeField] private Transform startPose;
    [SerializeField] private CSVLogger csvLogger;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI conditionText;
    [SerializeField] private TextMeshProUGUI trialText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Trial Counts")]
    [SerializeField] private int practiceTrialsOff = 2;
    [SerializeField] private int recordedTrialsOff = 5;
    [SerializeField] private int practiceTrialsOn = 2;
    [SerializeField] private int recordedTrialsOn = 5;

    [Header("Capture Tolerance")]
    [SerializeField] private float positionToleranceCm = 2.5f;
    [SerializeField] private float rotationToleranceDeg = 45f;

    private static readonly Vector3Int[] TargetCells =
    {
        new Vector3Int(-1, 0, 2),
        new Vector3Int(0, 0, 2),
        new Vector3Int(1, 0, 2),
        new Vector3Int(-1, 1, 2),
        new Vector3Int(0, 1, 2),
        new Vector3Int(1, 1, 2),
        new Vector3Int(0, 0, 3),
        new Vector3Int(0, 1, 3),
    };

    private string sessionId;
    private int targetCellIndex;
    private int recordedTrialCounter;
    private bool waitingForRelease;
    private float trialStartTime;
    private Rigidbody movableRigidbody;

    private Vector3Int currentTargetCell;
    private Vector3 currentTargetPosition;
    private Quaternion currentTargetRotation;
    private StudyCondition currentCondition;
    private bool currentIsPractice;

    private void Awake()
    {
        sessionId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    private void Start()
    {
        movableRigidbody = movableBlock.GetComponent<Rigidbody>();
        movableBlock.OnReleased += HandleBlockReleased;
        csvLogger.BeginSession();
        StartCoroutine(RunStudy());
    }

    private void OnDestroy()
    {
        if (movableBlock != null) movableBlock.OnReleased -= HandleBlockReleased;
    }

    private IEnumerator RunStudy()
    {
        yield return RunBlock(StudyCondition.SnappingOff, practiceTrialsOff, true);
        yield return RunBlock(StudyCondition.SnappingOff, recordedTrialsOff, false);
        yield return RunBlock(StudyCondition.SnappingOn, practiceTrialsOn, true);
        yield return RunBlock(StudyCondition.SnappingOn, recordedTrialsOn, false);

        conditionText.text = "Study complete";
        trialText.text = "Data saved";
        statusText.text = string.Empty;
    }

    private IEnumerator RunBlock(StudyCondition condition, int count, bool isPractice)
    {
        for (int i = 0; i < count; i++)
        {
            yield return RunTrial(condition, isPractice, i + 1, count);
        }
    }

    private IEnumerator RunTrial(StudyCondition condition, bool isPractice, int indexInBlock, int blockCount)
    {
        currentCondition = condition;
        currentIsPractice = isPractice;

        currentTargetCell = TargetCells[targetCellIndex % TargetCells.Length];
        targetCellIndex++;

        currentTargetPosition = gridSnapper.CellToWorldPosition(currentTargetCell);
        currentTargetRotation = Quaternion.identity;

        targetBlock.position = currentTargetPosition;
        targetBlock.rotation = currentTargetRotation;

        movableBlock.transform.position = startPose.position;
        movableBlock.transform.rotation = startPose.rotation;
        if (movableRigidbody != null)
        {
            movableRigidbody.velocity = Vector3.zero;
            movableRigidbody.angularVelocity = Vector3.zero;
        }

        string label = isPractice ? "Practice" : "Recorded";
        string conditionLabel = condition == StudyCondition.SnappingOn ? "Snapping ON" : "Snapping OFF";
        conditionText.text = $"Condition: {conditionLabel}";
        trialText.text = $"{label} Trial {indexInBlock} / {blockCount}";
        statusText.text = "Place the block and release";

        trialStartTime = Time.time;
        waitingForRelease = true;
        movableBlock.SetInteractionEnabled(true);

        while (waitingForRelease)
        {
            yield return null;
        }

        statusText.text = "Trial recorded";
        yield return new WaitForSeconds(1f);
    }

    private void HandleBlockReleased(Vector3 rawPosition, Quaternion rawRotation)
    {
        if (!waitingForRelease) return;
        waitingForRelease = false;

        movableBlock.SetInteractionEnabled(false);

        float trialTime = Time.time - trialStartTime;
        bool snappingEnabled = currentCondition == StudyCondition.SnappingOn;

        Vector3 finalPosition = rawPosition;
        Quaternion finalRotation = rawRotation;

        if (snappingEnabled)
        {
            finalPosition = gridSnapper.SnapPosition(rawPosition);
            finalRotation = gridSnapper.SnapRotation(rawRotation);
            movableBlock.transform.position = finalPosition;
            movableBlock.transform.rotation = finalRotation;
        }

        var data = BuildTrialData(rawPosition, rawRotation, finalPosition, finalRotation, snappingEnabled, trialTime);

        if (!currentIsPractice)
        {
            recordedTrialCounter++;
            data.trialNumber = recordedTrialCounter;
            csvLogger.AppendTrial(data);
        }
    }

    private TrialData BuildTrialData(Vector3 rawPos, Quaternion rawRot, Vector3 finalPos, Quaternion finalRot, bool snappingEnabled, float trialTime)
    {
        var data = new TrialData
        {
            sessionId = sessionId,
            timestamp = System.DateTime.Now.ToString("o", CultureInfo.InvariantCulture),
            condition = currentCondition,
            snappingEnabled = snappingEnabled,
            targetCell = currentTargetCell,
            targetPosition = currentTargetPosition,
            targetRotation = currentTargetRotation,
            rawPosition = rawPos,
            rawRotation = rawRot,
            finalPosition = finalPos,
            finalRotation = finalRot,
            trialTimeSeconds = trialTime
        };

        Vector3 rawDiff = rawPos - currentTargetPosition;
        data.rawXErrorCm = Mathf.Abs(rawDiff.x) * 100f;
        data.rawYErrorCm = Mathf.Abs(rawDiff.y) * 100f;
        data.rawZErrorCm = Mathf.Abs(rawDiff.z) * 100f;
        data.rawPositionErrorCm = Vector3.Distance(rawPos, currentTargetPosition) * 100f;
        data.rawOrientationErrorDeg = Quaternion.Angle(rawRot, currentTargetRotation);
        data.rawPositionSuccess = data.rawXErrorCm <= positionToleranceCm
            && data.rawYErrorCm <= positionToleranceCm
            && data.rawZErrorCm <= positionToleranceCm;
        data.rawRotationSuccess = data.rawOrientationErrorDeg <= rotationToleranceDeg;
        data.rawCombinedSuccess = data.rawPositionSuccess && data.rawRotationSuccess;

        Vector3 finalDiff = finalPos - currentTargetPosition;
        data.finalXErrorCm = Mathf.Abs(finalDiff.x) * 100f;
        data.finalYErrorCm = Mathf.Abs(finalDiff.y) * 100f;
        data.finalZErrorCm = Mathf.Abs(finalDiff.z) * 100f;
        data.finalPositionErrorCm = Vector3.Distance(finalPos, currentTargetPosition) * 100f;
        data.finalOrientationErrorDeg = Quaternion.Angle(finalRot, currentTargetRotation);
        data.finalPositionSuccess = data.finalXErrorCm <= positionToleranceCm
            && data.finalYErrorCm <= positionToleranceCm
            && data.finalZErrorCm <= positionToleranceCm;
        data.finalRotationSuccess = data.finalOrientationErrorDeg <= rotationToleranceDeg;
        data.finalCombinedSuccess = data.finalPositionSuccess && data.finalRotationSuccess;

        data.correctGridCell = gridSnapper.IsSameGridCell(finalPos, currentTargetPosition);

        return data;
    }
}
