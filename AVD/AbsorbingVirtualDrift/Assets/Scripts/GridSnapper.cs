using System.Collections.Generic;
using UnityEngine;

public class GridSnapper : MonoBehaviour
{
    [SerializeField] private Transform gridOrigin;
    [SerializeField] private float gridSize = 0.05f;

    private static readonly List<Quaternion> ValidRotations = BuildValidRotations();

    private void Awake()
    {
        if (gridOrigin == null) gridOrigin = transform;
    }

    public Vector3 CellToWorldPosition(Vector3Int cell)
    {
        return gridOrigin.position + new Vector3(cell.x, cell.y, cell.z) * gridSize;
    }

    public Vector3Int WorldPositionToCell(Vector3 worldPosition)
    {
        Vector3 offset = (worldPosition - gridOrigin.position) / gridSize;
        return new Vector3Int(
            Mathf.RoundToInt(offset.x),
            Mathf.RoundToInt(offset.y),
            Mathf.RoundToInt(offset.z));
    }

    private static float SnapCoordinate(float value, float origin, float gridSize)
    {
        return origin + Mathf.Round((value - origin) / gridSize) * gridSize;
    }

    public Vector3 SnapPosition(Vector3 rawPosition)
    {
        Vector3 origin = gridOrigin.position;
        return new Vector3(
            SnapCoordinate(rawPosition.x, origin.x, gridSize),
            SnapCoordinate(rawPosition.y, origin.y, gridSize),
            SnapCoordinate(rawPosition.z, origin.z, gridSize));
    }

    public Quaternion SnapRotation(Quaternion rawRotation)
    {
        Quaternion best = ValidRotations[0];
        float bestAngle = Quaternion.Angle(rawRotation, best);

        for (int i = 1; i < ValidRotations.Count; i++)
        {
            float angle = Quaternion.Angle(rawRotation, ValidRotations[i]);
            if (angle < bestAngle)
            {
                bestAngle = angle;
                best = ValidRotations[i];
            }
        }

        return best;
    }

    public bool IsSameGridCell(Vector3 positionA, Vector3 positionB)
    {
        return WorldPositionToCell(positionA) == WorldPositionToCell(positionB);
    }

    private static List<Quaternion> BuildValidRotations()
    {
        var forwardDirections = new[]
        {
            Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up, Vector3.down
        };

        var rotations = new List<Quaternion>(24);

        foreach (var forward in forwardDirections)
        {
            Vector3 up = (forward == Vector3.up || forward == Vector3.down) ? Vector3.forward : Vector3.up;
            Quaternion baseRotation = Quaternion.LookRotation(forward, up);

            for (int i = 0; i < 4; i++)
            {
                rotations.Add(baseRotation * Quaternion.Euler(0f, 0f, i * 90f));
            }
        }

        return rotations;
    }
}
