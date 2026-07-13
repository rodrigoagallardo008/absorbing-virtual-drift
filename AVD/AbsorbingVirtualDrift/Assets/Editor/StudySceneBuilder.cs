#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One-time setup tool for Study 1 (Quantized AR Placement Accuracy).
// Run "Study/1. Import Required Samples" once after Unity finishes resolving
// packages, then "Study/2. Build ARPlacementStudy Scene". Safe to re-run --
// step 2 deletes and rebuilds Assets/Scenes/ARPlacementStudy.unity from
// scratch each time, and auto-wires StudyManager/CSVLogger/PlacementBlock
// (including their Inspector reference fields) once those scripts exist.
public static class StudySceneBuilder
{
    private const string SourceScenePath = "Assets/Scenes/SampleScene.unity";
    private const string TargetScenePath = "Assets/Scenes/ARPlacementStudy.unity";
    private const string BlockFbxPath = "Assets/Geometry/3_UNIT.fbx";
    private const float BlockSize = 0.05f;

    [MenuItem("Study/1. Import Required Samples")]
    public static void ImportSamples()
    {
        var samples = Sample.FindByPackage("com.unity.xr.interaction.toolkit", null);
        if (samples == null || !samples.Any())
        {
            Debug.LogError("No samples found for com.unity.xr.interaction.toolkit. " +
                "Open Package Manager and confirm the package finished resolving/downloading first.");
            return;
        }

        foreach (var s in samples)
        {
            bool isRelevant =
                s.displayName.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                s.displayName.IndexOf("Starter Assets", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isRelevant) continue;

            bool ok = s.Import(Sample.ImportOptions.OverridePreviousImports);
            Debug.Log($"Imported sample '{s.displayName}': {ok}");
        }

        AssetDatabase.Refresh();
    }

    [MenuItem("Study/2. Build ARPlacementStudy Scene")]
    public static void BuildScene()
    {
        if (!System.IO.File.Exists(BlockFbxPath))
        {
            Debug.LogError($"Block mesh not found at {BlockFbxPath}.");
            return;
        }

        if (System.IO.File.Exists(TargetScenePath))
        {
            AssetDatabase.DeleteAsset(TargetScenePath);
        }

        if (!AssetDatabase.CopyAsset(SourceScenePath, TargetScenePath))
        {
            Debug.LogError($"Failed to copy {SourceScenePath} to {TargetScenePath}.");
            return;
        }

        var scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

        RemoveBuildingBlockObjects(scene);

        var interactionManagerGO = new GameObject("XR Interaction Manager");
        AddComponentByTypeName(interactionManagerGO, "UnityEngine.XR.Interaction.Toolkit.XRInteractionManager");

        InstantiateXROrigin();

        var studySystem = new GameObject("StudySystem");
        var studyManagerGO = new GameObject("StudyManager");
        studyManagerGO.transform.SetParent(studySystem.transform);
        var gridOriginGO = new GameObject("GridOrigin");
        gridOriginGO.transform.SetParent(studySystem.transform);
        gridOriginGO.transform.position = new Vector3(0f, 1.2f, 0.6f);

        var workspaceGO = new GameObject("Workspace");
        workspaceGO.transform.position = gridOriginGO.transform.position;

        var targetBlockGO = BuildBlock("TargetBlock", workspaceGO.transform, new Vector3(0f, 0f, 0.1f), isTarget: true);
        var movableBlockGO = BuildBlock("MovableBlock", workspaceGO.transform, new Vector3(-0.15f, 0f, 0f), isTarget: false);

        var startPoseGO = new GameObject("StartPose");
        startPoseGO.transform.SetParent(workspaceGO.transform);
        startPoseGO.transform.localPosition = new Vector3(-0.15f, 0f, 0f);
        startPoseGO.transform.localRotation = Quaternion.identity;

        var (conditionText, trialText, statusText) = BuildUI();
        BuildEventSystem();

        var gridSnapperComp = AddComponentByTypeName(gridOriginGO, "GridSnapper");
        var placementBlockComp = AddComponentByTypeName(movableBlockGO, "PlacementBlock");
        var csvLoggerComp = AddComponentByTypeName(studyManagerGO, "CSVLogger");
        var studyManagerComp = AddComponentByTypeName(studyManagerGO, "StudyManager");

        if (studyManagerComp != null)
        {
            var so = new SerializedObject(studyManagerComp);
            SetObjectRef(so, "gridSnapper", gridSnapperComp);
            SetObjectRef(so, "targetBlock", targetBlockGO.transform);
            SetObjectRef(so, "movableBlock", placementBlockComp);
            SetObjectRef(so, "startPose", startPoseGO.transform);
            SetObjectRef(so, "csvLogger", csvLoggerComp);
            SetObjectRef(so, "conditionText", conditionText);
            SetObjectRef(so, "trialText", trialText);
            SetObjectRef(so, "statusText", statusText);
            so.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"ARPlacementStudy scene built at {TargetScenePath}. " +
            "Remember: passthrough is NOT set up yet -- add it via Meta > Tools > Building Blocks > Passthrough.");
    }

    private static void SetObjectRef(SerializedObject so, string fieldName, UnityEngine.Object value)
    {
        if (value == null) return;
        var prop = so.FindProperty(fieldName);
        if (prop != null) prop.objectReferenceValue = value;
        else Debug.LogWarning($"Field '{fieldName}' not found on {so.targetObject.GetType().Name} -- wire it manually in the Inspector.");
    }

    private static void RemoveBuildingBlockObjects(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root != null && root.name.StartsWith("[BuildingBlock]"))
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }

    private static void InstantiateXROrigin()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Samples/XR Interaction Toolkit" });
        GameObject bestMatch = null;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.IndexOf("Hands Interaction Demo", StringComparison.OrdinalIgnoreCase) < 0) continue;

            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name.IndexOf("XR Origin", StringComparison.OrdinalIgnoreCase) < 0) continue;

            Debug.Log($"XR Origin candidate: {path}");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            bestMatch = prefab;
            if (name.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0) break;
        }

        if (bestMatch == null)
        {
            Debug.LogError("Could not find an XR Origin prefab under the Hands Interaction Demo sample. " +
                "Run 'Study/1. Import Required Samples' first. If it still isn't found, drag the correct " +
                "XR Origin prefab from Assets/Samples/XR Interaction Toolkit/.../Hands Interaction Demo/ " +
                "into the scene manually and rename it 'XR Origin Hands'.");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(bestMatch);
        instance.name = "XR Origin Hands";
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
    }

    private static GameObject BuildBlock(string name, Transform parent, Vector3 localPos, bool isTarget)
    {
        var container = new GameObject(name);
        container.transform.SetParent(parent);
        container.transform.localPosition = localPos;
        container.transform.localRotation = Quaternion.identity;

        var sourceFbx = AssetDatabase.LoadAssetAtPath<GameObject>(BlockFbxPath);
        var meshGO = (GameObject)PrefabUtility.InstantiatePrefab(sourceFbx, container.transform);
        meshGO.name = "Mesh";
        meshGO.transform.localPosition = Vector3.zero;
        meshGO.transform.localRotation = Quaternion.identity;
        meshGO.transform.localScale = Vector3.one;

        var renderers = meshGO.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogError($"{BlockFbxPath} has no Renderer -- cannot compute size for {name}.");
            return container;
        }

        var boundsBefore = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) boundsBefore.Encapsulate(renderers[i].bounds);

        // Uniform scale only -- preserves 3_UNIT's real proportions (no distortion).
        // The largest dimension is capped to fit exactly one 5cm grid cell; the
        // other axes may end up smaller than 5cm if the mesh isn't cubic.
        float largestDimension = Mathf.Max(boundsBefore.size.x, Mathf.Max(boundsBefore.size.y, boundsBefore.size.z));
        float uniformScale = BlockSize / Mathf.Max(largestDimension, 0.0001f);
        meshGO.transform.localScale = Vector3.one * uniformScale;

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        Debug.Log($"{name}: 3_UNIT.fbx scaled to size {bounds.size} (largest axis = {BlockSize}m).");

        var redMat = GetOrCreateMaterial("Mat_FrontMarker_Red", Color.red, transparent: false);
        var blueMat = GetOrCreateMaterial("Mat_TopMarker_Blue", Color.blue, transparent: false);
        AddMarker(container, "FrontMarker", bounds, Vector3.forward, redMat);
        AddMarker(container, "TopMarker", bounds, Vector3.up, blueMat);

        if (isTarget)
        {
            var transparentMat = GetOrCreateMaterial("Mat_TargetBlock", new Color(1f, 0.92f, 0.3f, 0.4f), transparent: true);
            foreach (var r in renderers) r.sharedMaterial = transparentMat;
        }
        else
        {
            var box = container.AddComponent<BoxCollider>();
            box.center = container.transform.InverseTransformPoint(bounds.center);
            box.size = bounds.size;

            var rb = container.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            var grab = AddComponentByTypeName(container,
                "UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable",
                "UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable");
            if (grab != null)
            {
                var so = new SerializedObject(grab);
                var movementTypeProp = so.FindProperty("m_MovementType");
                if (movementTypeProp != null) movementTypeProp.enumValueIndex = 1; // Kinematic
                var throwProp = so.FindProperty("m_ThrowOnDetach");
                if (throwProp != null) throwProp.boolValue = false;
                so.ApplyModifiedProperties();
            }
        }

        return container;
    }

    private static void AddMarker(GameObject container, string name, Bounds worldBounds, Vector3 localDir, Material mat)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = name;
        UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
        marker.transform.SetParent(container.transform);

        var halfSize = worldBounds.extents;
        var localCenter = container.transform.InverseTransformPoint(worldBounds.center);
        var offset = Vector3.Scale(localDir, halfSize) + localDir * 0.003f;
        marker.transform.localPosition = localCenter + offset;
        marker.transform.localRotation = Quaternion.identity;

        const float thickness = 0.005f;
        var faceSize = worldBounds.size * 0.7f;
        marker.transform.localScale = new Vector3(
            Mathf.Abs(localDir.x) > 0.5f ? thickness : faceSize.x,
            Mathf.Abs(localDir.y) > 0.5f ? thickness : faceSize.y,
            Mathf.Abs(localDir.z) > 0.5f ? thickness : faceSize.z);

        marker.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private static Material GetOrCreateMaterial(string name, Color color, bool transparent)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        var path = $"Assets/Materials/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { color = color };

        if (transparent)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static (TextMeshProUGUI condition, TextMeshProUGUI trial, TextMeshProUGUI status) BuildUI()
    {
        var canvasGO = new GameObject("SimpleUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 200);
        rt.position = new Vector3(0f, 1.6f, 1.0f);
        rt.localScale = Vector3.one * 0.001f;

        var condition = CreateLabel(canvasGO.transform, "ConditionText", new Vector2(0, 60), "Condition: --");
        var trial = CreateLabel(canvasGO.transform, "TrialText", new Vector2(0, 0), "Trial 0 / 0");
        var status = CreateLabel(canvasGO.transform, "StatusText", new Vector2(0, -60), "Ready");
        return (condition, trial, status);
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string name, Vector2 anchoredPos, string text)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(380, 50);
        rt.anchoredPosition = anchoredPos;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 36;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    private static void BuildEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem", typeof(EventSystem));
        AddComponentByTypeName(go,
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule",
            "UnityEngine.EventSystems.StandaloneInputModule");
    }

    private static Component AddComponentByTypeName(GameObject go, params string[] candidateTypeNames)
    {
        foreach (var name in candidateTypeNames)
        {
            var type = FindType(name);
            if (type != null) return go.AddComponent(type);
        }
        Debug.LogWarning($"Could not find type among: {string.Join(", ", candidateTypeNames)} on '{go.name}'. " +
            "Skipping -- add manually once available, or re-run this tool after the type exists.");
        return null;
    }

    private static Type FindType(string fullName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type t = null;
            try { t = asm.GetType(fullName); } catch { /* ignore reflection-only or dynamic assemblies */ }
            if (t != null) return t;
        }
        return null;
    }
}
#endif
