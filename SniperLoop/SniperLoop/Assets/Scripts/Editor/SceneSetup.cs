using UnityEngine;
using UnityEditor;

// EDITOR SCRIPT — lives in an "Editor" folder so Unity knows it's editor-only.
// This script does NOT run in your game. It adds a menu item that builds
// the entire test scene from code: ground, player, gun, targets, bullet prefab.
//
// WHY BUILD FROM CODE?
// You could place everything manually in the Scene view, and normally you would.
// But for this first setup, doing it from code lets me show you exactly what
// components go on each object, what values they need, and how the hierarchy
// is structured. Once it's built, you can modify everything in the Inspector.
public class SceneSetup : MonoBehaviour
{
    // [MenuItem] adds this method to Unity's top menu bar.
    // After the script compiles, you'll see: SniperLoop > Setup Scene
    [MenuItem("SniperLoop/Setup Scene")]
    static void SetupScene()
    {
        // --- GROUND ---
        // A Cube scaled flat to act as a floor. In 3D, a "ground plane" is just
        // a wide, thin box. We could use a Plane primitive, but Cubes have a
        // BoxCollider by default (Planes use MeshCollider which is more expensive).
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(100f, 1f, 100f);

        // Give the ground a distinct color so it's not default white.
        ground.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Ground", new Color(0.3f, 0.35f, 0.25f));

        // --- PLAYER ---
        // The player is an EMPTY GameObject — no visual mesh.
        // In first-person, you ARE the camera, so you don't need to see a body.
        // The CharacterController component provides the collision capsule.
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 1.5f, 0f);

        // CharacterController: Unity's built-in player movement collider.
        // It's a capsule shape. We set height and center to roughly human-sized.
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.center = new Vector3(0f, 0f, 0f);

        // Add our movement script to the player.
        player.AddComponent<PlayerMovement>();

        // --- CAMERA ---
        // The Main Camera becomes a CHILD of the player.
        // "Child" means it's nested under the player in the hierarchy.
        // Children inherit their parent's position and rotation — so when
        // the player moves or spins, the camera follows automatically.
        //
        // We position it at the player's "eye height" — near the top of the
        // CharacterController capsule.
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.SetParent(player.transform);
            mainCam.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            mainCam.transform.localRotation = Quaternion.identity;

            // Add MouseLook to the camera and wire up the playerBody reference.
            MouseLook mouseLook = mainCam.gameObject.AddComponent<MouseLook>();

            // SerializeField fields can be set from code using SerializedObject.
            // This is how Editor scripts programmatically fill in Inspector slots.
            SerializedObject so = new SerializedObject(mouseLook);
            so.FindProperty("playerBody").objectReferenceValue = player.transform;
            so.ApplyModifiedProperties();
        }

        // --- GUN (visual) ---
        // A stretched Cube to represent the gun barrel.
        // It's a child of the camera so it moves with your view — like holding a gun.
        //
        // localPosition places it in the bottom-right of the view.
        // X = right, Y = down from center, Z = forward (in front of camera).
        GameObject gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gun.name = "Gun";
        gun.transform.SetParent(mainCam.transform);
        gun.transform.localPosition = new Vector3(0.3f, -0.3f, 0.7f);
        gun.transform.localScale = new Vector3(0.1f, 0.1f, 0.5f);
        gun.transform.localRotation = Quaternion.identity;
        gun.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Gun", new Color(0.2f, 0.2f, 0.25f));

        // Disable the gun's collider — we don't want it participating in physics.
        // Without this, the gun's BoxCollider could block bullets or bump into walls.
        gun.GetComponent<Collider>().enabled = false;

        // --- MUZZLE POINT ---
        // An empty GameObject at the tip of the gun barrel.
        // This is where bullets spawn from. Its .forward direction determines
        // which way bullets fly. Because it's a child of the gun (which is a
        // child of the camera), it always points where you're looking.
        GameObject muzzlePoint = new GameObject("MuzzlePoint");
        muzzlePoint.transform.SetParent(gun.transform);
        muzzlePoint.transform.localPosition = new Vector3(0f, 0f, 0.5f);
        muzzlePoint.transform.localRotation = Quaternion.identity;

        // --- BULLET PREFAB ---
        // Create a small sphere, configure it as our bullet, then save it as a prefab.
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "Bullet";
        bullet.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        bullet.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Bullet", Color.yellow);

        // Tag it as "Bullet" so targets can identify hits.
        // We need to make sure the tag exists first.
        bullet.tag = "Bullet";

        // Add Rigidbody — this makes it a physics object.
        // useGravity = true means it'll arc downward over distance.
        // This is the foundation of SniperLoop's ballistic system.
        Rigidbody rb = bullet.AddComponent<Rigidbody>();
        rb.useGravity = false; // Bullet.cs applies custom gravity via gravityMultiplier
        rb.mass = 0.1f;
        // Continuous collision detection prevents fast-moving bullets from
        // passing through thin objects (like walls). Without this, a fast
        // bullet can teleport past a wall between physics frames.
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Add our Bullet script for auto-cleanup on hit/timeout.
        bullet.AddComponent<Bullet>();

        // Save as a Prefab in Assets/Prefabs/.
        // PrefabUtility.SaveAsPrefabAsset is an Editor-only API.
        string prefabPath = "Assets/Prefabs/Bullet.prefab";
        GameObject bulletPrefab = PrefabUtility.SaveAsPrefabAsset(bullet, prefabPath);

        // Destroy the scene instance — we only need the saved prefab asset.
        DestroyImmediate(bullet);

        // --- WIRE UP SHOOTING ---
        // Add PlayerShooting to the camera and connect the prefab + muzzle point.
        if (mainCam != null)
        {
            PlayerShooting shooting = mainCam.gameObject.AddComponent<PlayerShooting>();
            SerializedObject shootingSO = new SerializedObject(shooting);
            shootingSO.FindProperty("bulletPrefab").objectReferenceValue = bulletPrefab;
            shootingSO.FindProperty("muzzlePoint").objectReferenceValue = muzzlePoint.transform;
            shootingSO.ApplyModifiedProperties();
        }

        // --- TARGETS ---
        // A few Cubes placed at various distances to shoot at.
        // Each gets a Target script and a BoxCollider (already on Cubes by default).
        CreateTarget("Target_Close", new Vector3(0f, 1f, 10f), new Color(0.8f, 0.3f, 0.3f));
        CreateTarget("Target_Mid", new Vector3(5f, 1.5f, 25f), new Color(0.3f, 0.8f, 0.3f));
        CreateTarget("Target_Far", new Vector3(-3f, 2f, 50f), new Color(0.3f, 0.3f, 0.8f));

        // --- WALLS (something to give the scene depth) ---
        CreateWall("Wall_Left", new Vector3(-8f, 2.5f, 15f), new Vector3(1f, 5f, 10f));
        CreateWall("Wall_Right", new Vector3(10f, 2.5f, 20f), new Vector3(1f, 5f, 8f));

        // Clean up the default MyCube if it exists.
        GameObject myCube = GameObject.Find("MyCube");
        if (myCube != null)
            DestroyImmediate(myCube);

        Debug.Log("Scene setup complete! Press Play to test.");
    }

    static void CreateTarget(string name, Vector3 position, Color color)
    {
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name = name;
        target.transform.position = position;
        target.transform.localScale = new Vector3(1.5f, 2f, 0.3f);
        target.GetComponent<Renderer>().sharedMaterial = CreateMaterial(name, color);
        target.AddComponent<Target>();
    }

    static void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = CreateMaterial(name, new Color(0.5f, 0.45f, 0.4f));
    }

    // Helper: creates a simple colored material using URP's Lit shader.
    // Each material is saved as an asset so it persists after play mode.
    static Material CreateMaterial(string name, Color color)
    {
        // "Universal Render Pipeline/Lit" is URP's standard shader.
        // In the built-in render pipeline you'd use "Standard" instead.
        // URP uses different shader property names — _BaseColor instead of _Color.
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", 0.2f);

        string matPath = $"Assets/Prefabs/{name}_Mat.mat";
        AssetDatabase.CreateAsset(mat, matPath);
        return mat;
    }
}
