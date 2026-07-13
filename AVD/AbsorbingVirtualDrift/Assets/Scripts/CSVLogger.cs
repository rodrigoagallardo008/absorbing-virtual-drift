using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public class CSVLogger : MonoBehaviour
{
    private static readonly string[] Header =
    {
        "session_id", "timestamp", "trial_number", "condition", "snapping_enabled",
        "target_cell_x", "target_cell_y", "target_cell_z",
        "target_position_x", "target_position_y", "target_position_z",
        "target_rotation_x", "target_rotation_y", "target_rotation_z", "target_rotation_w",
        "raw_position_x", "raw_position_y", "raw_position_z",
        "raw_rotation_x", "raw_rotation_y", "raw_rotation_z", "raw_rotation_w",
        "final_position_x", "final_position_y", "final_position_z",
        "final_rotation_x", "final_rotation_y", "final_rotation_z", "final_rotation_w",
        "raw_x_error_cm", "raw_y_error_cm", "raw_z_error_cm", "raw_position_error_cm", "raw_orientation_error_deg",
        "raw_position_success", "raw_rotation_success", "raw_combined_success",
        "final_x_error_cm", "final_y_error_cm", "final_z_error_cm", "final_position_error_cm", "final_orientation_error_deg",
        "final_position_success", "final_rotation_success", "final_combined_success",
        "correct_grid_cell", "trial_time_seconds"
    };

    private string filePath;
    private bool headerWritten;

    public string FilePath => filePath;

    public void BeginSession()
    {
        if (headerWritten) return;

        string safeTimestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
        string fileName = $"ARPlacement_{safeTimestamp}.csv";
        filePath = Path.Combine(Application.persistentDataPath, fileName);

        File.WriteAllText(filePath, string.Join(",", Header) + "\n", Encoding.UTF8);
        headerWritten = true;

        Debug.Log(filePath);
    }

    public void AppendTrial(TrialData data)
    {
        if (!headerWritten) BeginSession();

        string[] fields =
        {
            data.sessionId,
            data.timestamp,
            data.trialNumber.ToString(CultureInfo.InvariantCulture),
            data.condition.ToString(),
            data.snappingEnabled.ToString(CultureInfo.InvariantCulture),
            data.targetCell.x.ToString(CultureInfo.InvariantCulture),
            data.targetCell.y.ToString(CultureInfo.InvariantCulture),
            data.targetCell.z.ToString(CultureInfo.InvariantCulture),
            F(data.targetPosition.x), F(data.targetPosition.y), F(data.targetPosition.z),
            F(data.targetRotation.x), F(data.targetRotation.y), F(data.targetRotation.z), F(data.targetRotation.w),
            F(data.rawPosition.x), F(data.rawPosition.y), F(data.rawPosition.z),
            F(data.rawRotation.x), F(data.rawRotation.y), F(data.rawRotation.z), F(data.rawRotation.w),
            F(data.finalPosition.x), F(data.finalPosition.y), F(data.finalPosition.z),
            F(data.finalRotation.x), F(data.finalRotation.y), F(data.finalRotation.z), F(data.finalRotation.w),
            F(data.rawXErrorCm), F(data.rawYErrorCm), F(data.rawZErrorCm), F(data.rawPositionErrorCm), F(data.rawOrientationErrorDeg),
            data.rawPositionSuccess.ToString(CultureInfo.InvariantCulture),
            data.rawRotationSuccess.ToString(CultureInfo.InvariantCulture),
            data.rawCombinedSuccess.ToString(CultureInfo.InvariantCulture),
            F(data.finalXErrorCm), F(data.finalYErrorCm), F(data.finalZErrorCm), F(data.finalPositionErrorCm), F(data.finalOrientationErrorDeg),
            data.finalPositionSuccess.ToString(CultureInfo.InvariantCulture),
            data.finalRotationSuccess.ToString(CultureInfo.InvariantCulture),
            data.finalCombinedSuccess.ToString(CultureInfo.InvariantCulture),
            data.correctGridCell.ToString(CultureInfo.InvariantCulture),
            F(data.trialTimeSeconds)
        };

        File.AppendAllText(filePath, string.Join(",", fields) + "\n", Encoding.UTF8);
    }

    private static string F(float value)
    {
        return value.ToString("F6", CultureInfo.InvariantCulture);
    }
}
