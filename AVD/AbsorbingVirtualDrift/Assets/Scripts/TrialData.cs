using System;
using UnityEngine;

public enum StudyCondition
{
    SnappingOff,
    SnappingOn
}

[Serializable]
public class TrialData
{
    public string sessionId;
    public string timestamp;
    public int trialNumber;
    public StudyCondition condition;
    public bool snappingEnabled;

    public Vector3Int targetCell;
    public Vector3 targetPosition;
    public Quaternion targetRotation;

    public Vector3 rawPosition;
    public Quaternion rawRotation;

    public Vector3 finalPosition;
    public Quaternion finalRotation;

    public float rawXErrorCm;
    public float rawYErrorCm;
    public float rawZErrorCm;
    public float rawPositionErrorCm;
    public float rawOrientationErrorDeg;
    public bool rawPositionSuccess;
    public bool rawRotationSuccess;
    public bool rawCombinedSuccess;

    public float finalXErrorCm;
    public float finalYErrorCm;
    public float finalZErrorCm;
    public float finalPositionErrorCm;
    public float finalOrientationErrorDeg;
    public bool finalPositionSuccess;
    public bool finalRotationSuccess;
    public bool finalCombinedSuccess;

    public bool correctGridCell;
    public float trialTimeSeconds;
}
