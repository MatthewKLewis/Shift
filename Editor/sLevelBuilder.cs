using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

//Shift > Build Test Level: generates a one-corridor greybox that needs all three modes.
//
//  z 0-15   START      door_1 blocks the way          -> console: "open door_1"
//  z 15-35  TURRET     turret_a covers door_2         -> console: "disable turret_a", or shoot it
//  z 35-55  DRONES     two drones rush you            -> FPS: shoot them (they can't be hacked)
//  z 52     EXIT pad
public static class sLevelBuilder
{
    const string ScenePath = "Assets/Scenes/TestLevel.unity";
    const string MaterialFolder = "Assets/TestLevel";

    const float Width = 20f;
    const float Length = 58f;
    const float WallHeight = 3f;

    [MenuItem("Shift/Build Test Level")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
            !EditorUtility.DisplayDialog("Build Test Level", $"Overwrite {ScenePath}?", "Overwrite", "Cancel"))
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        //The player prefab brings its own camera
        Object.DestroyImmediate(Object.FindAnyObjectByType<Camera>().gameObject);

        var floorMat = MakeMaterial("Floor", new Color(0.22f, 0.24f, 0.28f));
        var wallMat = MakeMaterial("Wall", new Color(0.45f, 0.48f, 0.55f));
        var doorMat = MakeMaterial("Door", new Color(0.9f, 0.55f, 0.15f));
        var enemyMat = MakeMaterial("Enemy", new Color(0.85f, 0.15f, 0.15f));
        var exitMat = MakeMaterial("Exit", new Color(0.2f, 0.9f, 0.4f));

        var level = new GameObject("===TEST LEVEL===").transform;

        //Floor and outer walls
        Box("Floor", level, new Vector3(0f, -0.5f, Length / 2f - 2f), new Vector3(Width, 1f, Length), floorMat);
        Box("Wall_W", level, new Vector3(-Width / 2f, WallHeight / 2f, Length / 2f - 2f), new Vector3(0.5f, WallHeight, Length), wallMat);
        Box("Wall_E", level, new Vector3(Width / 2f, WallHeight / 2f, Length / 2f - 2f), new Vector3(0.5f, WallHeight, Length), wallMat);
        Box("Wall_S", level, new Vector3(0f, WallHeight / 2f, -2f), new Vector3(Width, WallHeight, 0.5f), wallMat);
        Box("Wall_N", level, new Vector3(0f, WallHeight / 2f, Length - 2f), new Vector3(Width, WallHeight, 0.5f), wallMat);

        //Section 1: door_1
        DoorWall("door_1", level, 15f, wallMat, doorMat);

        //Section 2: a turret watching door_2, plus some cover
        Box("Cover", level, new Vector3(-4f, 0.75f, 24f), new Vector3(3f, 1.5f, 1f), wallMat);
        Turret("turret_a", level, new Vector3(6f, 0f, 31f), enemyMat);
        DoorWall("door_2", level, 35f, wallMat, doorMat);

        //Section 3: drones and the exit
        Drone(level, new Vector3(-5f, 0f, 46f), enemyMat);
        Drone(level, new Vector3(5f, 0f, 48f), enemyMat);

        var exit = Box("Exit", level, new Vector3(0f, 0.05f, 52f), new Vector3(4f, 0.1f, 4f), exitMat);
        exit.GetComponent<Collider>().isTrigger = true;
        exit.AddComponent<sExit>();

        //Player, HUD, GameManager prefabs
        var player = Spawn("Assets/Player.prefab");
        if (player != null) player.transform.position = new Vector3(0f, 0.1f, 2f);
        Spawn("Assets/HUD.prefab");
        Spawn("Assets/GameManager.prefab");

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        Debug.Log($"Built {ScenePath}");
    }

    static void DoorWall(string id, Transform parent, float z, Material wallMat, Material doorMat)
    {
        const float doorWidth = 4f;
        float side = (Width - doorWidth) / 2f;
        float sideX = doorWidth / 2f + side / 2f;

        Box($"Wall_{id}_L", parent, new Vector3(-sideX, WallHeight / 2f, z), new Vector3(side, WallHeight, 0.5f), wallMat);
        Box($"Wall_{id}_R", parent, new Vector3(sideX, WallHeight / 2f, z), new Vector3(side, WallHeight, 0.5f), wallMat);

        var door = Box(id, parent, new Vector3(0f, WallHeight / 2f, z), new Vector3(doorWidth, WallHeight, 0.4f), doorMat);
        door.AddComponent<sDoor>().id = id;
    }

    static void Turret(string id, Transform parent, Vector3 position, Material mat)
    {
        var root = new GameObject(id);
        root.transform.SetParent(parent);
        root.transform.position = position;

        var stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stand.name = "Stand";
        stand.transform.SetParent(root.transform, false);
        stand.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        stand.transform.localScale = new Vector3(0.8f, 0.6f, 0.8f);
        stand.GetComponent<Renderer>().sharedMaterial = mat;

        var head = Box("Head", root.transform, Vector3.zero, new Vector3(0.9f, 0.6f, 0.9f), mat);
        head.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        var barrel = Box("Barrel", head.transform, Vector3.zero, new Vector3(0.2f, 0.2f, 1f), mat);
        barrel.transform.localPosition = new Vector3(0f, 0f, 0.8f);
        barrel.transform.localScale = new Vector3(0.2f / 0.9f, 0.2f / 0.6f, 1f / 0.9f); //undo head scale

        root.AddComponent<sHealth>().maxHealth = 4f;
        var turret = root.AddComponent<sTurret>();
        turret.id = id;
        var so = new SerializedObject(turret);
        so.FindProperty("head").objectReferenceValue = head.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Drone(Transform parent, Vector3 position, Material mat)
    {
        var root = new GameObject("Drone");
        root.transform.SetParent(parent);
        root.transform.position = position;

        var cc = root.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.75f, 0f);
        cc.radius = 0.5f;
        cc.height = 1.5f;

        var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = cc.center;
        Object.DestroyImmediate(body.GetComponent<Collider>()); //the CharacterController is the hitbox
        body.GetComponent<Renderer>().sharedMaterial = mat;

        root.AddComponent<sHealth>().maxHealth = 2f;
        root.AddComponent<sDrone>();
    }

    static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material mat)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = mat;
        return box;
    }

    static GameObject Spawn(string prefabPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"Level builder: missing {prefabPath}");
            return null;
        }
        return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
    }

    static Material MakeMaterial(string name, Color color)
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets", "TestLevel");

        string path = $"{MaterialFolder}/m{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void AddToBuildSettings(string path)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == path)) return;
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
