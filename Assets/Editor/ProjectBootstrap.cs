using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 기본 씬(Assets/Scenes/Main.unity)을 생성하는 에디터 유틸리티.
/// 메뉴: Tools > Project > Create Main Scene
/// 배치 모드: -executeMethod ProjectBootstrap.CreateMainScene
/// </summary>
public static class ProjectBootstrap
{
    private const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Tools/Project/Create Main Scene")]
    public static void CreateMainScene()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(3f, 1f, 3f);
        ground.layer = LayerMask.NameToLayer("Default");

        // Player
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1f, 0f);
        var rb = player.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        var groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform, false);
        groundCheck.transform.localPosition = new Vector3(0f, -1f, 0f);

        var controller = player.AddComponent<PlayerController>();
        var so = new SerializedObject(controller);
        so.FindProperty("_groundCheck").objectReferenceValue = groundCheck.transform;
        so.FindProperty("_groundLayer").intValue = LayerMask.GetMask("Default");
        so.ApplyModifiedPropertiesWithoutUndo();

        // GameManager
        var gm = new GameObject("GameManager");
        gm.AddComponent<GameManager>();

        // Camera follows from behind
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 6f, -10f);
            cam.transform.LookAt(player.transform.position);
        }

        EditorSceneManager.SaveScene(scene, ScenePath);

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log($"[ProjectBootstrap] Created {ScenePath} and registered it in Build Settings.");
    }
}
