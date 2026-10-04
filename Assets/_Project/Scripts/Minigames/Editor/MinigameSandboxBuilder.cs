using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BariBarista.Minigames.EditorTools
{
    /// <summary>
    /// Tools/BariBarista/Create Minigame Sandbox — 미니게임 3종 테스트 씬·프리팹·정의 에셋을 만든다.
    /// 같은 이름이 있으면 덮어쓰지 않고 새 이름으로 만든다(재질은 있으면 재사용).
    /// 모델 경로는 이 에디터 도구에서만 쓰고, 런타임 스크립트는 인스펙터 참조로만 동작한다.
    /// </summary>
    public static class MinigameSandboxBuilder
    {
        private const string ScenesDir = "Assets/_Project/Scenes";
        private const string PrefabsDir = "Assets/_Project/Prefabs/Minigames";
        private const string MaterialsDir = "Assets/_Project/Prefabs/Minigames/Materials";
        private const string DataDir = "Assets/_Project/Data/Minigames";
        private const string CeramicCupPath = "Assets/_Project/Art/Models/ceramic cup.fbx";
        private const string PaperCupPath = "Assets/_Project/Art/Models/Paper cup.fbx";
        private const float Spacing = 30f;

        private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        // 재생성 중이면 같은 경로를 덮어써서 GUID를 유지한다(새 이름 만들기 없음)
        private static bool rebuilding;

        [MenuItem("Tools/BariBarista/Create Minigame Sandbox")]
        public static void Create() => Build(false);

        /// <summary>
        /// 프리팹·정의 에셋·샌드박스 씬을 코드대로 다시 만든다. 같은 경로를 덮어써서 GUID가 그대로다.
        /// 프리팹을 손으로 고친 내용은 사라지므로, 바꾸고 싶은 값은 이 빌더에 적는다.
        /// </summary>
        [MenuItem("Tools/BariBarista/Rebuild Minigame Prefabs")]
        public static void Rebuild() => Build(true);

        private static string TargetPath(string path) => rebuilding ? path : AssetDatabase.GenerateUniqueAssetPath(path);

        private static void Build(bool rebuild)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[MinigameSandbox] 플레이 중에는 실행할 수 없습니다.");
                return;
            }

            if (rebuild)
            {
                // 샌드박스 씬은 어차피 다시 만들지만, 다른 씬의 저장 안 한 변경은 지키기 위해 취소한다
                string sandboxPath = ScenesDir + "/MinigameSandbox.unity";
                for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                {
                    var open = EditorSceneManager.GetSceneAt(i);
                    if (open.isDirty && open.path != sandboxPath)
                    {
                        Debug.LogWarning($"[MinigameSandbox] 저장하지 않은 씬({open.path})이 있어 취소했습니다.");
                        return;
                    }
                }
            }
            else if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[MinigameSandbox] 저장하지 않은 씬이 있어 취소했습니다.");
                return;
            }

            rebuilding = rebuild;
            try
            {
                BuildAll();
            }
            finally
            {
                rebuilding = false;
            }
        }

        private static void BuildAll()
        {
            EnsureFolder(ScenesDir);
            EnsureFolder(PrefabsDir);
            EnsureFolder(MaterialsDir);
            EnsureFolder(DataDir);
            materials.Clear();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            IcePiece icePrefab = CreateIcePrefab();

            var espresso = BuildEspresso();
            var ice = BuildIce(icePrefab);
            var milk = BuildMilk();

            espresso.transform.position = new Vector3(0f * Spacing, 0f, 0f);
            ice.transform.position = new Vector3(1f * Spacing, 0f, 0f);
            milk.transform.position = new Vector3(2f * Spacing, 0f, 0f);

            var espressoPrefab = SavePrefab(espresso, "EspressoShot");
            var icePrefabRoot = SavePrefab(ice, "IceScoop");
            var milkPrefab = SavePrefab(milk, "MilkPour");

            var espressoDef = CreateDefinition("EspressoShot", "espresso_shot", "샷 내려라!", "espresso", 7f, espressoPrefab);
            var iceDef = CreateDefinition("IceScoop", "ice_scoop", "얼음 퍼라!", "ice", 8f, icePrefabRoot);
            var milkDef = CreateDefinition("MilkPour", "milk_pour", "우유 부어라!", "milk", 7f, milkPrefab);

            // 단독 실행기 (AudioListener는 여기 하나만 둔다. 미니게임 프리팹에는 넣지 않는다)
            var runnerGo = new GameObject("MicrogameRunner");
            runnerGo.AddComponent<AudioListener>();
            var runner = runnerGo.AddComponent<StandaloneMicrogameRunner>();
            var so = new SerializedObject(runner);
            var entries = so.FindProperty("entries");
            entries.arraySize = 3;
            SetEntry(entries.GetArrayElementAtIndex(0), espresso.GetComponent<MicrogameBase>(), espressoDef);
            SetEntry(entries.GetArrayElementAtIndex(1), ice.GetComponent<MicrogameBase>(), iceDef);
            SetEntry(entries.GetArrayElementAtIndex(2), milk.GetComponent<MicrogameBase>(), milkDef);
            so.ApplyModifiedPropertiesWithoutUndo();

            string scenePath = TargetPath(ScenesDir + "/MinigameSandbox.unity");
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[MinigameSandbox] 생성 완료: {scenePath}\n프리팹: {AssetDatabase.GetAssetPath(espressoPrefab)}, {AssetDatabase.GetAssetPath(icePrefabRoot)}, {AssetDatabase.GetAssetPath(milkPrefab)}\n정의: {AssetDatabase.GetAssetPath(espressoDef)}, {AssetDatabase.GetAssetPath(iceDef)}, {AssetDatabase.GetAssetPath(milkDef)}");
        }

        // ───────────────────────── 에스프레소 샷 ─────────────────────────

        private static GameObject BuildEspresso()
        {
            var root = new GameObject("EspressoShot");
            var game = root.AddComponent<EspressoShotMicrogame>();
            game.Id = "espresso_shot";
            Transform r = root.transform;

            var cam = AddCamera(r, new Vector3(0.05f, 0.75f, -1.05f), new Vector3(0.1f, 0.3f, 0.05f));
            var floor = AddFloor(r);

            Prim(PrimitiveType.Cube, "MachineBody", r, new Vector3(0f, 0.6f, 0.4f), new Vector3(0.8f, 1.2f, 0.5f), Mat("Machine", new Color(0.18f, 0.18f, 0.2f)), true);
            Prim(PrimitiveType.Cube, "MachineHead", r, new Vector3(0f, 0.85f, 0.08f), new Vector3(0.5f, 0.2f, 0.25f), Mat("Machine", new Color(0.18f, 0.18f, 0.2f)), true);
            Prim(PrimitiveType.Cylinder, "GroupHead", r, new Vector3(0f, 0.72f, 0.02f), new Vector3(0.14f, 0.04f, 0.14f), Mat("Metal", new Color(0.75f, 0.75f, 0.78f)), false);
            var spout = Empty("Spout", r, new Vector3(0f, 0.68f, 0f));
            var saucer = Prim(PrimitiveType.Cylinder, "DripTray", r, new Vector3(0f, 0.01f, 0f), new Vector3(0.32f, 0.01f, 0.32f), Mat("Metal", new Color(0.75f, 0.75f, 0.78f)), false);
            var button = Prim(PrimitiveType.Cube, "ExtractButton", r, new Vector3(0.18f, 0.97f, -0.04f), new Vector3(0.08f, 0.04f, 0.08f), Mat("Button", new Color(0.85f, 0.2f, 0.15f)), false);

            // 컵: 루트 원점이 컵 바닥
            var cupGo = new GameObject("Cup");
            cupGo.transform.SetParent(r, false);
            cupGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            Vector3 cupSize = AddModel(CeramicCupPath, cupGo.transform, 0.2f);
            var cupCol = cupGo.AddComponent<BoxCollider>();
            cupCol.center = new Vector3(0f, cupSize.y * 0.5f, 0f);
            cupCol.size = cupSize;
            var cupRb = cupGo.AddComponent<Rigidbody>();
            cupRb.isKinematic = true;
            cupRb.useGravity = false;
            cupRb.interpolation = RigidbodyInterpolation.Interpolate;
            var cupFollower = cupGo.AddComponent<MouseSpringFollower>();
            Configure(cupFollower,
                ("viewCamera", cam),
                ("grabMode", (int)MouseSpringFollower.GrabMode.Manual),
                ("maxDistanceFromOrigin", 1f),
                ("springStrength", 120f),
                ("damping", 14f),
                ("maxSpeed", 3f),
                ("tiltSpeed", 0f),
                ("maxTilt", 0f));
            float liquidDiameter = Mathf.Min(cupSize.x, cupSize.z) * 0.62f;
            var cupVisual = AddCupVisual(cupGo, liquidDiameter, cupSize.y * 0.8f, 0.015f);

            var stream = AddStream(r, spout.localPosition, Mat("Espresso", new Color(0.25f, 0.13f, 0.06f)));
            var gauge = AddGauge(r, new Vector3(0.3f, 0.03f, -0.05f), 0.4f);

            Configure(game,
                ("viewCamera", cam),
                ("spout", spout),
                ("saucer", saucer.transform),
                ("cup", cupGo.transform),
                ("cupCollider", cupCol),
                ("cupFollower", cupFollower),
                ("counter", floor.transform),
                ("cupVisual", cupVisual),
                ("gauge", gauge),
                ("stream", stream),
                ("extractButton", button.transform));
            return root;
        }

        // ───────────────────────── 얼음 퍼기 ─────────────────────────

        private static GameObject BuildIce(IcePiece icePrefab)
        {
            var root = new GameObject("IceScoop");
            var game = root.AddComponent<IceScoopMicrogame>();
            game.Id = "ice_scoop";
            Transform r = root.transform;

            var cam = AddCamera(r, new Vector3(-0.05f, 1.45f, -1.15f), new Vector3(-0.05f, 0.3f, 0.05f));
            var floor = AddFloor(r);

            // 제빙기
            var iceMat = Mat("Ice", new Color(0.8f, 0.93f, 1f));
            Prim(PrimitiveType.Cube, "IceMachine", r, new Vector3(-0.5f, 0.25f, 0.1f), new Vector3(0.42f, 0.5f, 0.42f), Mat("Machine", new Color(0.18f, 0.18f, 0.2f)), true);
            Prim(PrimitiveType.Cube, "IceBinVisual", r, new Vector3(-0.5f, 0.505f, 0.1f), new Vector3(0.36f, 0.01f, 0.36f), iceMat, false);
            var bin = new GameObject("IceBinZone");
            bin.transform.SetParent(r, false);
            bin.transform.localPosition = new Vector3(-0.5f, 0.62f, 0.1f);
            bin.layer = 2; // Ignore Raycast
            var binCol = bin.AddComponent<BoxCollider>();
            binCol.isTrigger = true;
            binCol.size = new Vector3(0.38f, 0.26f, 0.38f);

            // 컵 (정적 콜라이더 링으로 얼음을 담는다)
            var cupGo = new GameObject("Cup");
            cupGo.transform.SetParent(r, false);
            cupGo.transform.localPosition = new Vector3(0.3f, 0f, 0.05f);
            Vector3 cupSize = AddModel(PaperCupPath, cupGo.transform, 0.36f, true);
            float radius = Mathf.Min(cupSize.x, cupSize.z) * 0.45f;
            AddCupWalls(cupGo.transform, radius, cupSize.y);
            var zone = new GameObject("CupZone");
            zone.transform.SetParent(cupGo.transform, false);
            zone.transform.localPosition = new Vector3(0f, cupSize.y * 0.55f, 0f);
            zone.layer = 2;
            var zoneCol = zone.AddComponent<BoxCollider>();
            zoneCol.isTrigger = true;
            zoneCol.size = new Vector3(radius * 1.9f, cupSize.y * 1.1f, radius * 1.9f);
            var cupVisual = AddCupVisual(cupGo, radius * 1.8f, cupSize.y * 0.9f, 0.01f);

            // 스쿱: 앞(+X)이 열린 상자. 로컬 -Z축(뒤)으로 기울이면 앞이 내려가 쏟아진다
            var scoopGo = new GameObject("Scoop");
            scoopGo.transform.SetParent(r, false);
            scoopGo.transform.localPosition = new Vector3(-0.1f, 0.62f, -0.1f);
            var metal = Mat("Metal", new Color(0.75f, 0.75f, 0.78f));
            Prim(PrimitiveType.Cube, "Base", scoopGo.transform, new Vector3(0f, -0.05f, 0f), new Vector3(0.26f, 0.025f, 0.22f), metal, true);
            Prim(PrimitiveType.Cube, "BackWall", scoopGo.transform, new Vector3(-0.13f, 0.03f, 0f), new Vector3(0.02f, 0.17f, 0.22f), metal, true);
            Prim(PrimitiveType.Cube, "SideWallL", scoopGo.transform, new Vector3(0f, 0.03f, 0.11f), new Vector3(0.26f, 0.17f, 0.02f), metal, true);
            Prim(PrimitiveType.Cube, "SideWallR", scoopGo.transform, new Vector3(0f, 0.03f, -0.11f), new Vector3(0.26f, 0.17f, 0.02f), metal, true);
            // 낮은 앞턱: 들고 다닐 땐 얼음을 붙잡고, 기울이면 넘어간다
            Prim(PrimitiveType.Cube, "FrontLip", scoopGo.transform, new Vector3(0.13f, -0.025f, 0f), new Vector3(0.02f, 0.03f, 0.22f), metal, true);
            Prim(PrimitiveType.Cube, "Handle", scoopGo.transform, new Vector3(-0.22f, 0.02f, 0f), new Vector3(0.16f, 0.03f, 0.04f), Mat("Handle", new Color(0.2f, 0.2f, 0.22f)), false);
            var fillPoint = Empty("FillPoint", scoopGo.transform, new Vector3(0f, 0.04f, 0f));
            var scoopZoneGo = new GameObject("ScoopZone");
            scoopZoneGo.transform.SetParent(scoopGo.transform, false);
            scoopZoneGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            scoopZoneGo.layer = 2;
            var scoopZone = scoopZoneGo.AddComponent<BoxCollider>();
            scoopZone.isTrigger = true;
            scoopZone.size = new Vector3(0.26f, 0.2f, 0.22f);
            var scoopRb = scoopGo.AddComponent<Rigidbody>();
            scoopRb.mass = 3f;
            scoopRb.useGravity = false;
            scoopRb.interpolation = RigidbodyInterpolation.Interpolate;
            scoopRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var scoop = scoopGo.AddComponent<MouseSpringFollower>();
            Configure(scoop,
                ("viewCamera", cam),
                ("grabMode", (int)MouseSpringFollower.GrabMode.Always),
                ("maxDistanceFromOrigin", 0.9f),
                ("springStrength", 150f),
                ("damping", 16f),
                ("maxSpeed", 5f),
                ("tiltAxisLocal", Vector3.back),
                ("tiltSpeed", 150f),
                ("tiltReturnSpeed", 200f),
                ("maxTilt", 110f));

            var poolGo = new GameObject("IcePool");
            poolGo.transform.SetParent(r, false);
            var pool = poolGo.AddComponent<IcePiecePool>();
            Configure(pool, ("template", icePrefab));

            var gauge = AddGauge(r, new Vector3(0.58f, 0.03f, 0f), 0.4f);

            Configure(game,
                ("viewCamera", cam),
                ("scoop", scoop),
                ("scoopFillPoint", fillPoint),
                ("scoopZone", scoopZone),
                ("binZone", binCol),
                ("cupZone", zoneCol),
                ("floor", floor.transform),
                ("icePool", pool),
                ("cupVisual", cupVisual),
                ("countGauge", gauge));
            return root;
        }

        // ───────────────────────── 우유 붓기 ─────────────────────────

        private static GameObject BuildMilk()
        {
            var root = new GameObject("MilkPour");
            var game = root.AddComponent<MilkPourMicrogame>();
            game.Id = "milk_pour";
            Transform r = root.transform;

            var cam = AddCamera(r, new Vector3(0f, 1.15f, -1.1f), new Vector3(0f, 0.42f, 0f));
            var floor = AddFloor(r);

            var cupGo = new GameObject("Cup");
            cupGo.transform.SetParent(r, false);
            cupGo.transform.localPosition = new Vector3(0.1f, 0f, 0f);
            Vector3 cupSize = AddModel(PaperCupPath, cupGo.transform, 0.36f, true);
            float radius = Mathf.Min(cupSize.x, cupSize.z) * 0.45f;
            var mouthGo = new GameObject("CupMouth");
            mouthGo.transform.SetParent(cupGo.transform, false);
            mouthGo.transform.localPosition = new Vector3(0f, cupSize.y, 0f);
            mouthGo.layer = 2;
            var mouth = mouthGo.AddComponent<BoxCollider>();
            mouth.isTrigger = true;
            mouth.size = new Vector3(radius * 1.9f, 0.02f, radius * 1.9f);
            var cupVisual = AddCupVisual(cupGo, radius * 1.8f, cupSize.y * 0.95f, 0.01f);

            // 우유팩: +X 쪽에 입구. 로컬 -Z축으로 기울이면 입구가 내려간다
            var cartonGo = new GameObject("MilkCarton");
            cartonGo.transform.SetParent(r, false);
            cartonGo.transform.localPosition = new Vector3(-0.2f, 0.72f, 0f);
            var cartonMat = Mat("Carton", new Color(0.95f, 0.95f, 0.97f));
            Prim(PrimitiveType.Cube, "Body", cartonGo.transform, Vector3.zero, new Vector3(0.12f, 0.26f, 0.12f), cartonMat, true);
            Prim(PrimitiveType.Cube, "Label", cartonGo.transform, new Vector3(0f, 0f, -0.061f), new Vector3(0.1f, 0.12f, 0.002f), Mat("CartonLabel", new Color(0.2f, 0.45f, 0.85f)), false);
            Prim(PrimitiveType.Cube, "Cap", cartonGo.transform, new Vector3(0.045f, 0.14f, 0f), new Vector3(0.03f, 0.03f, 0.03f), Mat("Button", new Color(0.85f, 0.2f, 0.15f)), false);
            var spout = Empty("Spout", cartonGo.transform, new Vector3(0.06f, 0.14f, 0f));
            var cartonRb = cartonGo.AddComponent<Rigidbody>();
            cartonRb.mass = 1f;
            cartonRb.useGravity = false;
            cartonRb.interpolation = RigidbodyInterpolation.Interpolate;
            var carton = cartonGo.AddComponent<MouseSpringFollower>();
            Configure(carton,
                ("viewCamera", cam),
                ("grabMode", (int)MouseSpringFollower.GrabMode.WhileGrabHeld),
                ("maxDistanceFromOrigin", 0.8f),
                ("springStrength", 110f),
                ("damping", 13f),
                ("maxSpeed", 4f),
                ("tiltAxisLocal", Vector3.back),
                ("tiltSpeed", 70f),
                ("tiltReturnSpeed", 400f),
                ("maxTilt", 110f),
                ("rotationGain", 9f));

            var milkMat = Mat("Milk", new Color(0.97f, 0.96f, 0.92f));
            var stream = AddStream(r, Vector3.zero, milkMat);
            var particles = AddParticles(spout, milkMat.color);
            var gauge = AddGauge(r, new Vector3(0.38f, 0.03f, 0f), 0.4f);

            Configure(game,
                ("viewCamera", cam),
                ("carton", carton),
                ("spout", spout),
                ("cupMouth", mouth),
                ("floor", floor.transform),
                ("cupVisual", cupVisual),
                ("gauge", gauge),
                ("stream", stream),
                ("pourParticles", particles));
            return root;
        }

        // ───────────────────────── 공용 부품 ─────────────────────────

        private static Camera AddCamera(Transform parent, Vector3 localPos, Vector3 lookAtLocal)
        {
            var go = new GameObject("Camera");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.LookRotation(lookAtLocal - localPos, Vector3.up);
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.02f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.47f, 0.4f);
            return cam;
        }

        private static GameObject AddFloor(Transform parent)
        {
            var floor = Prim(PrimitiveType.Plane, "Floor", parent, Vector3.zero, new Vector3(0.4f, 1f, 0.4f), Mat("Floor", new Color(0.78f, 0.66f, 0.5f)), true);
            return floor;
        }

        /// <summary>모델을 목표 높이로 맞추고 바닥 중앙을 부모 원점에 둔다. 콜라이더는 제거. 반환값은 최종 크기.</summary>
        private static Vector3 AddModel(string assetPath, Transform parent, float targetHeight, bool hideTopCap = false)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            GameObject model;
            if (asset != null)
            {
                model = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                model.transform.SetParent(parent, false);
            }
            else
            {
                Debug.LogWarning($"[MinigameSandbox] 모델을 찾지 못해 원기둥으로 대체: {assetPath}");
                model = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                model.transform.SetParent(parent, false);
                model.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
            }
            model.name = "Model";
            foreach (var c in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

            Bounds b = RendererBounds(model);
            if (hideTopCap)
            {
                // 뚜껑처럼 위쪽 25%에만 걸친 부품은 숨긴다(이름에 의존하지 않음)
                float capLine = b.min.y + b.size.y * 0.75f;
                foreach (var r in model.GetComponentsInChildren<Renderer>())
                    if (r.bounds.min.y > capLine) r.gameObject.SetActive(false);
                b = RendererBounds(model);
            }
            if (b.size.y > 1e-5f) model.transform.localScale *= targetHeight / b.size.y;
            b = RendererBounds(model);
            Vector3 bottomCenter = new Vector3(b.center.x, b.min.y, b.center.z);
            model.transform.position += parent.position - bottomCenter;
            return RendererBounds(model).size;
        }

        private static Bounds RendererBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(false);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.1f);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        /// <summary>얼음이 담기도록 보이지 않는 바닥 + 벽 링 콜라이더를 만든다.</summary>
        private static void AddCupWalls(Transform cup, float radius, float height)
        {
            var walls = new GameObject("Colliders");
            walls.transform.SetParent(cup, false);
            var bottom = walls.AddComponent<BoxCollider>();
            bottom.center = new Vector3(0f, 0.01f, 0f);
            bottom.size = new Vector3(radius * 2f, 0.02f, radius * 2f);
            const int count = 12;
            float width = 2f * Mathf.PI * radius / count * 1.25f;
            for (int i = 0; i < count; i++)
            {
                float a = i * 360f / count;
                var w = new GameObject("Wall" + i);
                w.transform.SetParent(walls.transform, false);
                w.transform.localRotation = Quaternion.Euler(0f, a, 0f);
                w.transform.localPosition = w.transform.localRotation * new Vector3(0f, height * 0.5f, radius + 0.01f);
                var box = w.AddComponent<BoxCollider>();
                box.size = new Vector3(width, height, 0.02f);
            }
        }

        private static CupVisual AddCupVisual(GameObject cup, float diameter, float maxHeight, float bottomY)
        {
            var root = new GameObject("Liquid");
            root.transform.SetParent(cup.transform, false);
            root.transform.localPosition = new Vector3(0f, bottomY, 0f);
            root.transform.localScale = new Vector3(diameter, maxHeight, diameter);
            var pivot = Empty("Pivot", root.transform, Vector3.zero);
            var body = Prim(PrimitiveType.Cylinder, "Surface", pivot, new Vector3(0f, 0.5f, 0f), new Vector3(1f, 0.5f, 1f), Mat("Liquid", Color.white), false);
            pivot.localScale = new Vector3(1f, 0.0001f, 1f);
            pivot.gameObject.SetActive(false);

            var ice = Prim(PrimitiveType.Cube, "IceIndicator", cup.transform, new Vector3(0f, bottomY + maxHeight * 0.6f, 0f), new Vector3(diameter * 0.7f, maxHeight * 0.25f, diameter * 0.7f), Mat("Ice", new Color(0.8f, 0.93f, 1f)), false);
            ice.SetActive(false);

            var visual = cup.AddComponent<CupVisual>();
            Configure(visual,
                ("liquid", pivot),
                ("liquidRenderer", body.GetComponent<Renderer>()),
                ("iceIndicator", ice));
            return visual;
        }

        /// <summary>위쪽이 원점인 줄기 피벗. localScale.y = 길이.</summary>
        private static Transform AddStream(Transform parent, Vector3 localPos, Material mat)
        {
            var pivot = Empty("Stream", parent, localPos);
            Prim(PrimitiveType.Cylinder, "Body", pivot, new Vector3(0f, -0.5f, 0f), new Vector3(1f, 0.5f, 1f), mat, false);
            pivot.localScale = new Vector3(0.02f, 0.1f, 0.02f);
            pivot.gameObject.SetActive(false);
            return pivot;
        }

        private static VerticalGauge AddGauge(Transform parent, Vector3 localPos, float height)
        {
            var root = new GameObject("Gauge");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            root.transform.localScale = new Vector3(0.04f, height, 0.02f);
            Prim(PrimitiveType.Cube, "Background", root.transform, new Vector3(0f, 0.5f, 0f), Vector3.one, Mat("GaugeBg", new Color(0.12f, 0.12f, 0.12f)), false);
            var band = Empty("Band", root.transform, new Vector3(0f, 0f, -0.6f));
            Prim(PrimitiveType.Cube, "Body", band, new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 1f, 0.4f), Mat("GaugeBand", new Color(0.2f, 0.8f, 0.3f)), false);
            var fill = Empty("Fill", root.transform, new Vector3(0f, 0f, -1.1f));
            Prim(PrimitiveType.Cube, "Body", fill, new Vector3(0f, 0.5f, 0f), new Vector3(0.6f, 1f, 0.4f), Mat("GaugeFill", new Color(1f, 0.6f, 0.1f)), false);
            fill.localScale = new Vector3(1f, 0.0001f, 1f);
            fill.gameObject.SetActive(false);

            var gauge = root.AddComponent<VerticalGauge>();
            Configure(gauge, ("fill", fill), ("band", band));
            return gauge;
        }

        private static ParticleSystem AddParticles(Transform spout, Color color)
        {
            var go = new GameObject("PourParticles");
            go.transform.SetParent(spout, false);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 0.5f;
            main.startSpeed = 0.3f;
            main.startSize = 0.025f;
            main.startColor = color;
            main.gravityModifier = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            var emission = ps.emission;
            emission.rateOverTime = 80f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6f;
            shape.radius = 0.008f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = ParticleMat("MilkParticle", color);
            return ps;
        }

        private static Transform Empty(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        // ───────────────────────── 에셋 ─────────────────────────

        private static IcePiece CreateIcePrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "IceCube";
            go.transform.localScale = Vector3.one * 0.085f;
            go.GetComponent<Renderer>().sharedMaterial = Mat("Ice", new Color(0.8f, 0.93f, 1f));
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.05f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            go.AddComponent<IcePiece>();
            string path = TargetPath(PrefabsDir + "/IceCube.prefab");
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<IcePiece>();
        }

        private static GameObject SavePrefab(GameObject root, string name)
        {
            string path = TargetPath($"{PrefabsDir}/{name}.prefab");
            return PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction);
        }

        private static MicrogameDefinition CreateDefinition(string fileName, string id, string instruction, string stationId, float timeLimit, GameObject prefab)
        {
            string path = TargetPath($"{DataDir}/{fileName}.asset");
            // 재생성이면 기존 에셋을 불러와 필드만 갱신한다(GUID 유지)
            var existing = rebuilding ? AssetDatabase.LoadAssetAtPath<MicrogameDefinition>(path) : null;
            var def = existing != null ? existing : ScriptableObject.CreateInstance<MicrogameDefinition>();
            def.id = id;
            def.instruction = instruction;
            def.stationId = stationId;
            def.baseTimeLimit = timeLimit;
            def.prefab = prefab;
            def.successOnTimeout = false;
            def.defaultDifficulty = 1;
            if (existing != null) EditorUtility.SetDirty(def);
            else AssetDatabase.CreateAsset(def, path);
            return def;
        }

        private static Material Mat(string name, Color color)
        {
            if (materials.TryGetValue(name, out var cached)) return cached;
            string path = $"{MaterialsDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader) { name = name };
                SetColor(mat, color);
                AssetDatabase.CreateAsset(mat, path);
            }
            materials[name] = mat;
            return mat;
        }

        private static Material ParticleMat(string name, Color color)
        {
            if (materials.TryGetValue(name, out var cached)) return cached;
            string path = $"{MaterialsDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit");
                mat = new Material(shader) { name = name };
                SetColor(mat, color);
                AssetDatabase.CreateAsset(mat, path);
            }
            materials[name] = mat;
            return mat;
        }

        private static void SetColor(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        // ───────────────────────── 직렬화 필드 연결 ─────────────────────────

        private static void SetEntry(SerializedProperty entry, MicrogameBase game, MicrogameDefinition def)
        {
            entry.FindPropertyRelative("microgame").objectReferenceValue = game;
            entry.FindPropertyRelative("definition").objectReferenceValue = def;
        }

        private static void Configure(Object target, params (string name, object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (name, value) in values)
            {
                var p = so.FindProperty(name);
                if (p == null)
                {
                    Debug.LogError($"[MinigameSandbox] {target.GetType().Name}에 '{name}' 필드가 없습니다.");
                    continue;
                }
                switch (value)
                {
                    case Object o: p.objectReferenceValue = o; break;
                    case int i when p.propertyType == SerializedPropertyType.Enum: p.enumValueIndex = i; break;
                    case int i: p.intValue = i; break;
                    case float f: p.floatValue = f; break;
                    case bool b: p.boolValue = b; break;
                    case Vector3 v: p.vector3Value = v; break;
                    case string s: p.stringValue = s; break;
                    default: Debug.LogError($"[MinigameSandbox] 지원하지 않는 값 형식: {name}"); break;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
