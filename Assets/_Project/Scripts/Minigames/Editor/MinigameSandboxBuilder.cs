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
        private const string ToonDir = "Assets/_Project/Art/Materials/Toon";
        private const string CafeMaterialsDir = "Assets/_Project/Art/Cafe/Materials";
        private const string ToonShaderName = "BariBarista/Toon";
        private const float Spacing = 30f;

        private static readonly Color MilkColor = new Color(0.97f, 0.96f, 0.92f);
        private static readonly Color EspressoColor = new Color(0.25f, 0.13f, 0.06f);
        private static readonly Color IceColor = new Color(0.78f, 0.93f, 1f);
        private const float IceCubeSize = 0.085f;

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

            // 샌드박스 씬을 손으로 고친 채 저장하지 않았다면 다시 만들 때 사라지므로 어느 방식이든 취소한다
            string sandboxPath = ScenesDir + "/MinigameSandbox.unity";
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var open = EditorSceneManager.GetSceneAt(i);
                if (open.isDirty && open.path == sandboxPath)
                {
                    Debug.LogWarning("[MinigameSandbox] 샌드박스 씬에 저장하지 않은 변경이 있어 취소했습니다. 저장하거나 되돌린 뒤 다시 실행하세요.");
                    return;
                }
            }

            if (rebuild)
            {
                // 다른 씬의 저장 안 한 변경은 지키기 위해 취소한다
                for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                {
                    var open = EditorSceneManager.GetSceneAt(i);
                    if (open.isDirty)
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
            EnsureFolder(ToonDir);
            materials.Clear();
            MinigameUiBuilder.EnsureAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 주광·환경광은 GameScene 값에 맞춘다(GameScene은 읽기만 했다)
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.86f, 0.7f);
            light.intensity = 1.5f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.85f;
            lightGo.transform.rotation = new Quaternion(0.23367861f, 0.251131f, -0.062613994f, 0.9372337f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.32f, 0.36f, 0.42f);
            RenderSettings.ambientEquatorColor = new Color(0.27f, 0.24f, 0.2f);
            RenderSettings.ambientGroundColor = new Color(0.1f, 0.09f, 0.08f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = false;

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

            var espressoDef = CreateDefinition("EspressoShot", "espresso_shot", PresentationRules.InstructionShot, "espresso", 7f, espressoPrefab);
            var iceDef = CreateDefinition("IceScoop", "ice_scoop", PresentationRules.InstructionIce, "ice", 8f, icePrefabRoot);
            var milkDef = CreateDefinition("MilkPour", "milk_pour", PresentationRules.InstructionPour, "milk", 7f, milkPrefab);

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

            var cam = AddCamera(r, new Vector3(0.05f, 0.9f, -1.2f), new Vector3(0.1f, 0.32f, 0.05f));
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
            var cupPivot = Empty("CupPivot", cupGo.transform, Vector3.zero);
            Vector3 cupSize = AddModel(CeramicCupPath, cupPivot, 0.2f);
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
            var cupVisual = AddCupVisual(cupGo, cupPivot, liquidDiameter, cupSize.y * 0.8f, 0.015f);

            var stream = AddStream(r, spout.localPosition, ToonMat("Toon_Stream_Espresso", EspressoColor, new Color(0.07f, 0.035f, 0.02f), 0.005f, false, 0.14f, 70f, 0.1f));
            AddCafeBackdrop(r);
            var hud = MinigameUiBuilder.BuildEspressoUi(r);

            Configure(game,
                ("viewCamera", cam),
                ("spout", spout),
                ("saucer", saucer.transform),
                ("cup", cupGo.transform),
                ("cupCollider", cupCol),
                ("cupFollower", cupFollower),
                ("counter", floor.transform),
                ("cupVisual", cupVisual),
                ("hud", hud),
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

            var cam = AddCamera(r, new Vector3(-0.05f, 1.0f, -1.5f), new Vector3(-0.05f, 0.35f, 0.05f));
            var floor = AddFloor(r);

            // 제빙기
            var iceMat = IceMat();
            Prim(PrimitiveType.Cube, "IceMachine", r, new Vector3(-0.5f, 0.25f, 0.05f), new Vector3(0.42f, 0.5f, 0.42f), Mat("Machine", new Color(0.18f, 0.18f, 0.2f)), true);
            Prim(PrimitiveType.Cube, "IceBinVisual", r, new Vector3(-0.5f, 0.505f, 0.05f), new Vector3(0.36f, 0.01f, 0.36f), iceMat, false);
            AddIceMound(r, iceMat, new Vector3(-0.5f, 0.51f, 0.05f), 0.32f);
            var bin = new GameObject("IceBinZone");
            bin.transform.SetParent(r, false);
            bin.transform.localPosition = new Vector3(-0.5f, 0.62f, 0.05f);
            bin.layer = 2; // Ignore Raycast
            var binCol = bin.AddComponent<BoxCollider>();
            binCol.isTrigger = true;
            binCol.size = new Vector3(0.38f, 0.26f, 0.38f);

            // 컵 (정적 콜라이더 링으로 얼음을 담는다)
            var cupGo = new GameObject("Cup");
            cupGo.transform.SetParent(r, false);
            cupGo.transform.localPosition = new Vector3(0.3f, 0f, 0.05f);
            var cupPivot = Empty("CupPivot", cupGo.transform, Vector3.zero);
            Vector3 cupSize = AddModel(PaperCupPath, cupPivot, 0.36f, true);
            float radius = Mathf.Min(cupSize.x, cupSize.z) * 0.45f;
            AddCupWalls(cupGo.transform, radius, cupSize.y);
            var zone = new GameObject("CupZone");
            zone.transform.SetParent(cupGo.transform, false);
            zone.transform.localPosition = new Vector3(0f, cupSize.y * 0.55f, 0f);
            zone.layer = 2;
            var zoneCol = zone.AddComponent<BoxCollider>();
            zoneCol.isTrigger = true;
            zoneCol.size = new Vector3(radius * 1.9f, cupSize.y * 1.1f, radius * 1.9f);
            var cupVisual = AddCupVisual(cupGo, cupPivot, radius * 1.8f, cupSize.y * 0.9f, 0.01f);
            AddCafeBackdrop(r);

            // 스쿱: 앞(+X)이 열린 상자. 로컬 -Z축(뒤)으로 기울이면 앞이 내려가 쏟아진다
            var scoopGo = new GameObject("Scoop");
            scoopGo.transform.SetParent(r, false);
            scoopGo.transform.localPosition = new Vector3(-0.1f, 0.62f, 0.05f);
            var metal = Mat("Metal", new Color(0.75f, 0.75f, 0.78f));
            Prim(PrimitiveType.Cube, "Base", scoopGo.transform, new Vector3(0f, -0.05f, 0f), new Vector3(0.26f, 0.025f, 0.22f), metal, true);
            Prim(PrimitiveType.Cube, "BackWall", scoopGo.transform, new Vector3(-0.13f, 0.03f, 0f), new Vector3(0.02f, 0.17f, 0.22f), metal, true);
            Prim(PrimitiveType.Cube, "SideWallL", scoopGo.transform, new Vector3(0f, 0.03f, 0.11f), new Vector3(0.26f, 0.17f, 0.02f), metal, true);
            Prim(PrimitiveType.Cube, "SideWallR", scoopGo.transform, new Vector3(0f, 0.03f, -0.11f), new Vector3(0.26f, 0.17f, 0.02f), metal, true);
            // 낮은 앞턱: 들고 다닐 땐 얼음을 붙잡고, 기울이면 넘어간다
            Prim(PrimitiveType.Cube, "FrontLip", scoopGo.transform, new Vector3(0.13f, -0.025f, 0f), new Vector3(0.02f, 0.03f, 0.22f), metal, true);
            Prim(PrimitiveType.Cube, "Handle", scoopGo.transform, new Vector3(-0.22f, 0.02f, 0f), new Vector3(0.16f, 0.03f, 0.04f), Mat("Handle", new Color(0.2f, 0.2f, 0.22f)), false);
            var fillPoint = Empty("FillPoint", scoopGo.transform, new Vector3(0f, 0.015f, 0f)); // 스쿱 바닥 위 오목한 안쪽
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
                // 시작 Z(컵 가운데 선)에 고정. X는 제빙기 왼쪽 끝~컵 오른쪽을 덮고 화면 밖으로는 안 나가는 범위
                ("lockWorldZ", true),
                ("limitWorldX", true),
                ("minXFromOrigin", -0.55f),
                ("maxXFromOrigin", 0.5f),
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

            var hud = MinigameUiBuilder.BuildIceUi(r);

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
                ("hud", hud));
            return root;
        }

        // ───────────────────────── 우유 붓기 ─────────────────────────

        /// <summary>제빙기 판 위의 장식 얼음 더미. 콜라이더 없이 같은 재질을 써서 배칭을 유지한다.</summary>
        private static void AddIceMound(Transform parent, Material mat, Vector3 center, float width)
        {
            var mound = Empty("IceMound", parent, center);
            var rng = new System.Random(7);
            // 층마다 칸 수와 높이를 줄여 가운데가 솟은 더미 모양을 만든다 (4x4 + 3x3 + 2x2 = 29개)
            int[] grid = { 4, 3, 2 };
            float y = 0.035f;
            for (int layer = 0; layer < grid.Length; layer++)
            {
                int n = grid[layer];
                float step = width / 4f;
                for (int ix = 0; ix < n; ix++)
                {
                    for (int iz = 0; iz < n; iz++)
                    {
                        float x = (ix - (n - 1) * 0.5f) * step + ((float)rng.NextDouble() - 0.5f) * 0.02f;
                        float z = (iz - (n - 1) * 0.5f) * step + ((float)rng.NextDouble() - 0.5f) * 0.02f;
                        float size = IceCubeSize * (0.85f + (float)rng.NextDouble() * 0.3f);
                        var cube = Prim(PrimitiveType.Cube, "DecoIce", mound.transform,
                            new Vector3(x, y + ((float)rng.NextDouble() - 0.5f) * 0.01f, z), Vector3.one * size, mat, false);
                        cube.transform.localRotation = Quaternion.Euler((float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 360f);
                    }
                }
                y += 0.055f;
            }
        }

        private static GameObject BuildMilk()
        {
            var root = new GameObject("MilkPour");
            var game = root.AddComponent<MilkPourMicrogame>();
            game.Id = "milk_pour";
            Transform r = root.transform;

            var cam = AddCamera(r, new Vector3(0f, 1.15f, -1.3f), new Vector3(0f, 0.42f, 0f));
            var floor = AddFloor(r);

            var cupGo = new GameObject("Cup");
            cupGo.transform.SetParent(r, false);
            cupGo.transform.localPosition = new Vector3(0.1f, 0f, 0f);
            var cupPivot = Empty("CupPivot", cupGo.transform, Vector3.zero);
            Vector3 cupSize = AddModel(PaperCupPath, cupPivot, 0.36f, true);
            float radius = Mathf.Min(cupSize.x, cupSize.z) * 0.45f;
            var mouthGo = new GameObject("CupMouth");
            mouthGo.transform.SetParent(cupGo.transform, false);
            mouthGo.transform.localPosition = new Vector3(0f, cupSize.y, 0f);
            mouthGo.layer = 2;
            var mouth = mouthGo.AddComponent<BoxCollider>();
            mouth.isTrigger = true;
            mouth.size = new Vector3(radius * 1.9f, 0.02f, radius * 1.9f);
            var cupVisual = AddCupVisual(cupGo, cupPivot, radius * 1.8f, cupSize.y * 0.95f, 0.01f);
            AddCafeBackdrop(r);

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

            var streamMat = ToonMat("Toon_Stream_Milk", MilkColor, new Color(0.35f, 0.3f, 0.28f), 0.005f, false, 0.22f, 40f, 0.14f);
            var stream = AddStream(r, Vector3.zero, streamMat);
            var hud = MinigameUiBuilder.BuildMilkUi(r);

            Configure(game,
                ("viewCamera", cam),
                ("carton", carton),
                ("spout", spout),
                ("cupMouth", mouth),
                ("floor", floor.transform),
                ("cupVisual", cupVisual),
                ("hud", hud),
                ("stream", stream));
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
            // 배경 벽이 화면을 덮으므로 빈 곳이 보여도 옛 단색이 아닌 어두운 실내색이 되게 한다
            cam.backgroundColor = new Color(0.1f, 0.075f, 0.06f);
            return cam;
        }

        /// <summary>조리대 상판. 판정용 바닥 평면(콜라이더 유지)이고 재질은 카페 상판 돌.</summary>
        private static GameObject AddFloor(Transform parent)
        {
            var floor = Prim(PrimitiveType.Plane, "Floor", parent, Vector3.zero, new Vector3(0.4f, 1f, 0.4f), CafeMat("Stone_Tan", new Color(0.78f, 0.66f, 0.5f)), true);
            return floor;
        }

        /// <summary>
        /// 뒷벽·조리대 앞판·선반·펜던트 등·따뜻한 점광원. 콜라이더 없음.
        /// 재질은 카페 재질 중 WP6b 뒤에도 남는 이름(Wall_Cream, Wood_Oak, Stone_Tan, Floor_A, Ceramic, Brass, Shade_Glow)만 참조한다.
        /// </summary>
        private static void AddCafeBackdrop(Transform parent)
        {
            var root = Empty("CafeBackdrop", parent, Vector3.zero);
            var wall = CafeMat("Wall_Cream", new Color(0.93f, 0.88f, 0.78f));
            var oak = CafeMat("Wood_Oak", new Color(0.55f, 0.38f, 0.22f));
            var stone = CafeMat("Stone_Tan", new Color(0.78f, 0.66f, 0.5f));
            var floorMat = CafeMat("Floor_A", new Color(0.5f, 0.38f, 0.28f));
            var ceramic = CafeMat("Ceramic", new Color(0.95f, 0.93f, 0.9f));
            var brass = CafeMat("Brass", new Color(0.8f, 0.6f, 0.25f));
            var glow = CafeMat("Shade_Glow", new Color(1f, 0.85f, 0.55f));

            // 뒷벽과 카페 바닥
            Prim(PrimitiveType.Cube, "BackWall", root, new Vector3(0f, 1.4f, 1.35f), new Vector3(6f, 2.8f, 0.1f), wall, false);
            Prim(PrimitiveType.Cube, "SideWallL", root, new Vector3(-2.2f, 1.4f, 0f), new Vector3(0.1f, 2.8f, 5f), wall, false);
            Prim(PrimitiveType.Cube, "SideWallR", root, new Vector3(2.2f, 1.4f, 0f), new Vector3(0.1f, 2.8f, 5f), wall, false);
            Prim(PrimitiveType.Plane, "CafeFloor", root, new Vector3(0f, -0.9f, 0f), new Vector3(0.6f, 1f, 0.6f), floorMat, false);

            // 뒤쪽 낮은 찬장(나무) + 상판(돌)
            Prim(PrimitiveType.Cube, "BackCabinet", root, new Vector3(0f, 0.3f, 1.12f), new Vector3(4.2f, 0.6f, 0.42f), oak, false);
            Prim(PrimitiveType.Cube, "BackCounterTop", root, new Vector3(0f, 0.615f, 1.12f), new Vector3(4.3f, 0.03f, 0.48f), stone, false);

            // 벽 선반과 컵
            Prim(PrimitiveType.Cube, "Shelf", root, new Vector3(0.1f, 1.15f, 1.2f), new Vector3(2.6f, 0.04f, 0.22f), oak, false);
            float[] cupX = { -0.9f, -0.5f, -0.1f, 0.3f, 0.7f, 1.05f };
            for (int i = 0; i < cupX.Length; i++)
            {
                float h = (i % 2 == 0) ? 0.11f : 0.09f;
                Prim(PrimitiveType.Cylinder, "ShelfCup" + i, root, new Vector3(0.1f + cupX[i], 1.17f + h * 0.5f, 1.2f), new Vector3(0.09f, h * 0.5f, 0.09f), ceramic, false);
            }
            // 조리대 위 컵 더미
            Prim(PrimitiveType.Cylinder, "StackCup0", root, new Vector3(-1.3f, 0.66f, 1.1f), new Vector3(0.12f, 0.05f, 0.12f), ceramic, false);
            Prim(PrimitiveType.Cylinder, "StackCup1", root, new Vector3(-1.3f, 0.76f, 1.1f), new Vector3(0.12f, 0.05f, 0.12f), ceramic, false);

            // 펜던트 등(황동 줄 + 빛나는 갓)
            float[] lampX = { -0.8f, 0.9f };
            for (int i = 0; i < lampX.Length; i++)
            {
                Prim(PrimitiveType.Cylinder, "LampCord" + i, root, new Vector3(lampX[i], 2.2f, 0.9f), new Vector3(0.012f, 0.45f, 0.012f), brass, false);
                Prim(PrimitiveType.Cylinder, "LampCap" + i, root, new Vector3(lampX[i], 1.74f, 0.9f), new Vector3(0.05f, 0.025f, 0.05f), brass, false);
                Prim(PrimitiveType.Sphere, "LampShade" + i, root, new Vector3(lampX[i], 1.62f, 0.9f), new Vector3(0.24f, 0.22f, 0.24f), glow, false);
            }

            // 따뜻한 점광원 1개: 그림자 없음
            var lightGo = new GameObject("WarmPointLight");
            lightGo.transform.SetParent(root, false);
            lightGo.transform.localPosition = new Vector3(0.1f, 1.2f, 0.3f);
            var pl = lightGo.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(1f, 0.78f, 0.5f);
            pl.intensity = 1.0f;
            pl.range = 2.5f;
            pl.shadows = LightShadows.None;
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

        /// <summary>컵 액체·얼음 표시를 보이는 피벗(CupPivot) 아래에 만든다. CupVisual은 컵 루트에 붙는다.</summary>
        private static CupVisual AddCupVisual(GameObject cup, Transform cupPivot, float diameter, float maxHeight, float bottomY)
        {
            var root = new GameObject("Liquid");
            root.transform.SetParent(cupPivot, false);
            root.transform.localPosition = new Vector3(0f, bottomY, 0f);
            root.transform.localScale = new Vector3(diameter, maxHeight, diameter);
            var pivot = Empty("Pivot", root.transform, Vector3.zero);
            var liquidMat = ToonMat("Toon_Liquid", Color.white, new Color(0.2f, 0.1f, 0.06f), 0.004f, false, 0.16f, 50f, 0.1f);
            var body = Prim(PrimitiveType.Cylinder, "Surface", pivot, new Vector3(0f, 0.5f, 0f), new Vector3(1f, 0.5f, 1f), liquidMat, false);
            pivot.localScale = new Vector3(1f, 0.0001f, 1f);
            pivot.gameObject.SetActive(false);

            // 크레마·거품 원판. 액체 루트 아래에 두어 높이를 따라 올라온다
            var cremaMat = ToonMat("Toon_Crema", new Color(0.72f, 0.45f, 0.2f), new Color(0.25f, 0.12f, 0.05f), 0.004f, false, 0.3f, 25f, 0.2f);
            var foamMat = ToonMat("Toon_Foam", new Color(1f, 0.98f, 0.92f), new Color(0.4f, 0.32f, 0.26f), 0.004f, false, 0.35f, 20f, 0.25f);
            var layer = Prim(PrimitiveType.Cylinder, "TopLayer", root.transform, Vector3.zero, new Vector3(1f, 0.01f, 1f), cremaMat, false);
            layer.SetActive(false);

            var ice = Prim(PrimitiveType.Cube, "IceIndicator", cupPivot, new Vector3(0f, bottomY + maxHeight * 0.6f, 0f), new Vector3(diameter * 0.7f, maxHeight * 0.25f, diameter * 0.7f), IceMat(), false);
            ice.SetActive(false);

            var visual = cup.AddComponent<CupVisual>();
            Configure(visual,
                ("liquid", pivot),
                ("liquidRenderer", body.GetComponent<Renderer>()),
                ("iceIndicator", ice),
                ("topLayer", layer.transform),
                ("topLayerRenderer", layer.GetComponent<Renderer>()),
                ("cremaMaterial", cremaMat),
                ("foamMaterial", foamMat));
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
            // 콜라이더는 루트에 최종 크기로 두고, 보이는 모델만 자식으로 두어 커지게 한다
            var go = new GameObject("IceCube");
            var col = go.AddComponent<BoxCollider>();
            col.size = Vector3.one * IceCubeSize;
            var model = Prim(PrimitiveType.Cube, "Model", go.transform, Vector3.zero, Vector3.one * IceCubeSize, IceMat(), false);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.05f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var piece = go.AddComponent<IcePiece>();
            Configure(piece, ("visual", model.transform));
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
            else
            {
                // 다시 만들 때 빌더에 적은 색이 기존 재질에도 반영되게 한다
                SetColor(mat, color);
                EditorUtility.SetDirty(mat);
            }
            materials[name] = mat;
            return mat;
        }

        /// <summary>
        /// 카페 재질을 이름으로 참조만 한다(수정하지 않는다). 없으면 경고하고 같은 색의 임시 재질로 대체한다.
        /// </summary>
        private static Material CafeMat(string name, Color fallback)
        {
            string key = "cafe:" + name;
            if (materials.TryGetValue(key, out var cached)) return cached;
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{CafeMaterialsDir}/{name}.mat");
            if (mat == null)
            {
                Debug.LogWarning($"[MinigameSandbox] 카페 재질을 찾지 못해 대체 재질을 씁니다: {name}");
                mat = Mat("Backdrop_" + name, fallback);
            }
            materials[key] = mat;
            return mat;
        }

        /// <summary>얼음 공용 재질. 개별 색 변경(MaterialPropertyBlock) 없이 모든 얼음이 같은 재질을 써서 SRP Batcher로 묶인다.</summary>
        private static Material IceMat()
        {
            return ToonMat("Toon_Ice", IceColor, new Color(0.12f, 0.28f, 0.42f), 0.0035f, true, 0.38f, 18f, 0.22f, new Color(0.55f, 0.72f, 1f));
        }

        /// <summary>BariToon 재질을 Art/Materials/Toon에 만든다. 다시 만들 때도 같은 경로에 값을 덮어써 GUID를 유지한다.</summary>
        private static Material ToonMat(string name, Color baseColor, Color outlineColor, float outlineWidth, bool outlineFromPosition,
            float highlightSize, float highlightGloss, float rimAmount, Color? shadeTint = null)
        {
            string key = "toon:" + name;
            if (materials.TryGetValue(key, out var cached)) return cached;
            EnsureFolder(ToonDir);
            string path = $"{ToonDir}/{name}.mat";
            var shader = Shader.Find(ToonShaderName);
            if (shader == null) Debug.LogError($"[MinigameSandbox] 셰이더 '{ToonShaderName}'를 찾지 못했습니다.");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = mat == null;
            if (isNew) mat = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            else if (shader != null) mat.shader = shader;

            mat.SetColor("_BaseColor", baseColor);
            mat.SetColor("_ShadeTint", shadeTint ?? new Color(0.95f, 0.85f, 0.8f));
            mat.SetColor("_OutlineColor", outlineColor);
            mat.SetFloat("_OutlineWidth", outlineWidth);
            mat.SetFloat("_OutlineUsePosition", outlineFromPosition ? 1f : 0f);
            mat.SetFloat("_HighlightSize", highlightSize);
            mat.SetFloat("_HighlightGloss", highlightGloss);
            mat.SetFloat("_RimAmount", rimAmount);
            if (isNew) AssetDatabase.CreateAsset(mat, path);
            else EditorUtility.SetDirty(mat);
            materials[key] = mat;
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

        internal static void Configure(Object target, params (string name, object value)[] values)
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
                    case Object[] arr:
                        p.arraySize = arr.Length;
                        for (int k = 0; k < arr.Length; k++) p.GetArrayElementAtIndex(k).objectReferenceValue = arr[k];
                        break;
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
