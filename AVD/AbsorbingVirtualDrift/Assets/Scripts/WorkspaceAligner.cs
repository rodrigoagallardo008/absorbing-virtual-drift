using UnityEngine;

public class WorkspaceAligner : MonoBehaviour
{
    [SerializeField] private string[] workspaceObjectNames =
    {
        "3_UNIT", "3_UNIT (1)", "3_UNIT (2)", "lines", "steps", "ROBOT"
    };
    [SerializeField] private string cameraAnchorName = "CenterEyeAnchor";
    [SerializeField] private float distanceFromUser = 1.4f;
    [SerializeField] private float floorHeight = 0f;
    [SerializeField] private float delaySeconds = 0.3f;

    private void Start()
    {
        Invoke(nameof(AlignWorkspace), delaySeconds);
    }

    private void AlignWorkspace()
    {
        Transform cameraTransform = FindCamera();
        if (cameraTransform == null) return;

        Vector3 flatForward = cameraTransform.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.forward;
        flatForward.Normalize();

        var pivot = new GameObject("WorkspacePivot").transform;

        int reparented = 0;
        foreach (string objectName in workspaceObjectNames)
        {
            GameObject target = GameObject.Find(objectName);
            if (target == null) continue;
            target.transform.SetParent(pivot, true);
            reparented++;
        }

        if (reparented == 0)
        {
            Destroy(pivot.gameObject);
            return;
        }

        Vector3 desiredPosition = cameraTransform.position + flatForward * distanceFromUser;
        desiredPosition.y = floorHeight;

        pivot.SetPositionAndRotation(desiredPosition, Quaternion.LookRotation(flatForward, Vector3.up));
    }

    private Transform FindCamera()
    {
        GameObject anchor = GameObject.Find(cameraAnchorName);
        if (anchor != null) return anchor.transform;
        return Camera.main != null ? Camera.main.transform : null;
    }
}
