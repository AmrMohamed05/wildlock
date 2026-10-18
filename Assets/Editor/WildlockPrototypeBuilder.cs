#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

namespace Wildlock.Editor
{
    /// <summary>
    /// Automated Scene & Outer Vista Terrain Builder for Wildlock (Assignment 2 Part 3).
    /// Runs automatically once on load (or upon exiting Play Mode) or anytime via top menu:
    /// "Wildlock -> Build or Rebuild Assignment 2 Prototype"
    /// </summary>
    [InitializeOnLoad]
    public static class WildlockPrototypeBuilder
    {
        private const string RootObjectName = "Wildlock_Assignment2_Prototype";
        private const string GeneratedFolder = "Assets/Wildlock_Generated";
        private const string VersionSentinelName = "SetupVersion_v5";

        static WildlockPrototypeBuilder()
        {
            EditorApplication.delayCall += AutoBuildIfNeeded;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += AutoBuildIfNeeded;
            }
        }

        private static void AutoBuildIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            EnsureProjectRenderPipelineAsset();

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.name.Contains("SampleScene")) return;

            GameObject existingRoot = GameObject.Find(RootObjectName);
            if (existingRoot != null && existingRoot.transform.Find(VersionSentinelName) != null)
            {
                return; // Already up to date with SetupVersion_v5
            }

            BuildAssignment2Prototype();
        }

        private static void EnsureProjectRenderPipelineAsset()
        {
            // Prevent AdamCharacterPackSetup from overwriting the project's Unity 6 URP pipeline
            SessionState.SetBool("AdamCharacterPack.SetupAsked", true);
            EditorPrefs.SetBool("AdamCharacterPack.SetupDeclined", true);

            RenderPipelineAsset pcRp = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            if (pcRp == null) return;

            bool changed = false;
            if (GraphicsSettings.defaultRenderPipeline != pcRp)
            {
                GraphicsSettings.defaultRenderPipeline = pcRp;
                changed = true;
            }

            int currentLevel = QualitySettings.GetQualityLevel();
            int levelCount = QualitySettings.names.Length;
            for (int i = 0; i < levelCount; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                if (QualitySettings.renderPipeline != pcRp)
                {
                    QualitySettings.renderPipeline = pcRp;
                    changed = true;
                }
            }
            QualitySettings.SetQualityLevel(currentLevel, false);

            if (changed)
            {
                AssetDatabase.SaveAssets();
            }
        }

        [MenuItem("Wildlock/Build or Rebuild Assignment 2 Prototype", false, 1)]
        public static void BuildAssignment2Prototype()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Please exit Play Mode before rebuilding the Wildlock Prototype.");
                return;
            }

            EnsureProjectRenderPipelineAsset();

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.path.EndsWith("SampleScene.unity"))
            {
                if (File.Exists("Assets/Scenes/SampleScene.unity"))
                {
                    scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
                }
            }

            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Wildlock_Generated");
            }

            // Remove previous generated root if rebuilding
            GameObject oldRoot = GameObject.Find(RootObjectName);
            if (oldRoot != null)
            {
                Object.DestroyImmediate(oldRoot);
            }

            GameObject root = new GameObject(RootObjectName);
            GameObject sentinel = new GameObject(VersionSentinelName);
            sentinel.transform.SetParent(root.transform, false);

            // 1. Upgrade existing Assignment 1 Town Objects with Colliders & Interactive Scripts
            Transform centralCartTransform = UpgradeExistingTownObjects();

            // 2. Build the Non-Playable Outer Canyon & Mountain Vista Terrain (TerrainSampleAssets)
            BuildOuterVistaTerrain(root.transform);

            // 3. Build Lighting System (Day/Night + Floodlights + Torches + Showdown Beacons + Dark Zones)
            WildlockLightingManager lightingMgr = BuildLightingSystem(root.transform);

            // 4. Build Playable Gunslinger Hero (Starter Assets + Adam Character Pack Skins + Lantern)
            WildlockPlayerController playerCtrl = BuildPlayerHero(root.transform, lightingMgr);

            // 5. Build Cinemachine 3 Virtual Cameras (3rd-Person, 1st-Person, Cutscene Zoom, Overview)
            WildlockCameraDirector camDirector = BuildCinemachineCameras(root.transform, playerCtrl, centralCartTransform);

            // 6. Build Interactive Environment Mechanics (Generator Switch, Rising Platform, Saloon Doors, Jump Pads, Crates, Barrels, Pickups)
            WildlockSaloonDoor armoryGate = BuildInteractiveArenaMechanics(root.transform, lightingMgr);

            // 7. Build Animated 2v2 NPCs using Adam Character Pack (Duo Partner Adam + Outlaw Bots Guard & Lu)
            BuildAdamPackNpcs(root.transform);

            // 8. Wire up Central Game Manager
            WildlockGameManager gameMgr = root.AddComponent<WildlockGameManager>();
            gameMgr.centralMineCartTransform = centralCartTransform;
            gameMgr.sheriffArmoryGate = armoryGate;

            // Final pass: strip any residual missing script slots & disable any stray embedded FBX cameras
            StripAllMissingScriptsInScene(scene);
            WildlockCameraDirector.DisableStraySceneCameras();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[Wildlock] Assignment 2 Prototype (v5) successfully built and saved in SampleScene.unity!");
        }

        private static void StripAllMissingScriptsInScene(Scene scene)
        {
            if (!scene.IsValid()) return;
            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject r in roots)
            {
                if (r == null) continue;
                Transform[] allTransforms = r.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in allTransforms)
                {
                    if (t != null && t.gameObject != null)
                    {
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                    }
                }
            }
        }

        private static Transform UpgradeExistingTownObjects()
        {
            EnsureNoEmbeddedFbxCameras();
            StripAllMissingScriptsInScene(SceneManager.GetActiveScene());
            WildlockCameraDirector.DisableStraySceneCameras();

            // Ensure Plane floor has collider
            GameObject plane = GameObject.Find("Plane");
            if (plane != null && plane.GetComponent<Collider>() == null)
            {
                plane.AddComponent<MeshCollider>();
            }

            // Ensure town buildings have colliders so player/bullets interact with them
            string[] buildingNames =
            {
                "saloon_marmoset", "generalstorenoglass", "mine", "Water Tower Water Tanker",
                "lpcarriage2", "Sign_final_attempt"
            };

            foreach (string objName in buildingNames)
            {
                GameObject go = GameObject.Find(objName);
                if (go == null) continue;
                AddMeshCollidersRecursively(go);
            }

            // Remove any MeshColliders from decorative perimeter boulder prefabs (whose child meshes have large local offsets)
            string[] rockNames =
            {
                "SOURCE", "SOURCE (1)", "SOURCE (2)", "SOURCE (3)", "SOURCE (4)",
                "SOURCE (5)", "SOURCE (6)", "SOURCE (7)", "SOURCE (8)",
                "source", "source (1)"
            };
            foreach (string rName in rockNames)
            {
                GameObject rGo = GameObject.Find(rName);
                if (rGo == null) continue;
                Collider[] cols = rGo.GetComponentsInChildren<Collider>(true);
                foreach (Collider c in cols)
                {
                    if (c != null) Object.DestroyImmediate(c);
                }
            }

            // Upgrade existing Wooden Crates ("boxes", "boxes (1)")
            string[] crateNames = { "boxes", "boxes (1)" };
            foreach (string cName in crateNames)
            {
                GameObject crateGo = GameObject.Find(cName);
                if (crateGo != null)
                {
                    EnsureBoxColliderOnBounds(crateGo);
                    WildlockDestructibleProp prop = crateGo.GetComponent<WildlockDestructibleProp>();
                    if (prop == null) prop = crateGo.AddComponent<WildlockDestructibleProp>();
                    prop.propDisplayName = "Frontier Supply Crate";
                    prop.maxHealth = 200f;
                    prop.isExplosiveBarrel = false;
                    prop.dropsGemOnBreak = true;
                }
            }

            // Upgrade existing Barrels ("barrels", "barrels (1)")
            string[] barrelNames = { "barrels", "barrels (1)" };
            foreach (string bName in barrelNames)
            {
                GameObject barrelGo = GameObject.Find(bName);
                if (barrelGo != null)
                {
                    EnsureBoxColliderOnBounds(barrelGo);
                    WildlockDestructibleProp prop = barrelGo.GetComponent<WildlockDestructibleProp>();
                    if (prop == null) prop = barrelGo.AddComponent<WildlockDestructibleProp>();
                    prop.propDisplayName = "Black-Powder Barrel Cluster";
                    prop.maxHealth = 150f;
                    prop.isExplosiveBarrel = true;
                    prop.dropsGemOnBreak = true;
                }
            }

            // Archive the 4 static capsule proxies from Assignment 1 so we replace them with animated 3D characters
            string[] proxyNames = { "player", "player (1)", "player (2)", "player (3)" };
            foreach (string pName in proxyNames)
            {
                GameObject proxy = GameObject.Find(pName);
                if (proxy != null)
                {
                    proxy.SetActive(false);
                }
            }

            // Upgrade Central Ruby Mine Cart ("reg gem")
            GameObject regGem = GameObject.Find("reg gem");
            if (regGem != null)
            {
                EnsureBoxColliderOnBounds(regGem);
                WildlockGemPickup cartPickup = regGem.GetComponent<WildlockGemPickup>();
                if (cartPickup == null) cartPickup = regGem.AddComponent<WildlockGemPickup>();
                cartPickup.isCentralPermanentCart = true;
                cartPickup.rotationSpeed = 0f;

                Transform childGemAnim = regGem.transform.Find("gemanimation");
                if (childGemAnim != null)
                {
                    childGemAnim.localPosition = new Vector3(0f, 80f, 0f);
                }

                return regGem.transform;
            }

            return null;
        }

        private static void AddMeshCollidersRecursively(GameObject root)
        {
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter mf in filters)
            {
                if (mf != null && mf.sharedMesh != null && mf.GetComponent<Collider>() == null)
                {
                    mf.gameObject.AddComponent<MeshCollider>();
                }
            }
        }

        private static void EnsureBoxColliderOnBounds(GameObject root)
        {
            if (root.GetComponent<Collider>() != null) return;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                BoxCollider bc = root.AddComponent<BoxCollider>();
                bc.size = Vector3.one;
                return;
            }

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }

            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = root.transform.InverseTransformPoint(b.center);
            Vector3 lossy = root.transform.lossyScale;
            box.size = new Vector3(
                Mathf.Abs(lossy.x) > 0.0001f ? b.size.x / Mathf.Abs(lossy.x) : 1f,
                Mathf.Abs(lossy.y) > 0.0001f ? b.size.y / Mathf.Abs(lossy.y) : 1f,
                Mathf.Abs(lossy.z) > 0.0001f ? b.size.z / Mathf.Abs(lossy.z) : 1f
            );
        }

        /// <summary>
        /// Builds the surrounding Non-Playable Wild West Canyon & Mountain Vista using TerrainSampleAssets.
        /// Center of playable arena is (-124.8, 0, -10) with radius ~35m.
        /// Outer Terrain spans 900m x 900m around the town, completely flat beneath the town floor and rising
        /// into dramatic sculpted red-rock mesas and mountain peaks outside the sandstone rock boundary.
        /// </summary>
        private static void BuildOuterVistaTerrain(Transform parent)
        {
            string terrainDataPath = GeneratedFolder + "/Wildlock_OuterTerrainData.asset";
            TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainDataPath);
            if (terrainData == null)
            {
                terrainData = new TerrainData();
                AssetDatabase.CreateAsset(terrainData, terrainDataPath);
            }

            int hmRes = 257;
            terrainData.heightmapResolution = hmRes;
            float terrainWidth = 900f;
            float terrainHeight = 135f;
            terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainWidth);

            // Configure PBR TerrainLayers from Assets/TerrainSampleAssets/Textures/Terrain/
            TerrainLayer sandLayer = CreateOrLoadTerrainLayer("Wildlock_Layer_Sand",
                "Assets/TerrainSampleAssets/Textures/Terrain/Sand_BaseColor.tif",
                "Assets/TerrainSampleAssets/Textures/Terrain/Sand_Normal.tif",
                "Assets/TerrainSampleAssets/Textures/Terrain/Sand_MaskMap.tif",
                new Vector2(18f, 18f));

            TerrainLayer soilRocksLayer = CreateOrLoadTerrainLayer("Wildlock_Layer_SoilRocks",
                "Assets/TerrainSampleAssets/Textures/Terrain/Soil_Rocks_BaseColor.tif",
                "Assets/TerrainSampleAssets/Textures/Terrain/Soil_Rocks_Normal.tif",
                "Assets/TerrainSampleAssets/Textures/Terrain/Soil_Rocks_MaskMap.tif",
                new Vector2(24f, 24f));

            TerrainLayer canyonRockLayer = CreateOrLoadTerrainLayer("Wildlock_Layer_CanyonRock",
                "Assets/TerrainSampleAssets/Textures/Terrain/Rock_BaseColor.tif",
                "Assets/TerrainSampleAssets/Textures/Terrain/Rock_Normal.tif",
                "Assets/TerrainSampleAssets/Textures/Terrain/Rock_MaskMap.tif",
                new Vector2(32f, 32f));

            TerrainLayer dryScrubLayer = CreateOrLoadTerrainLayer("Wildlock_Layer_DryGrass",
                "Assets/TerrainSampleAssets/Textures/Terrain/Grass_Dry_BaseColor.tif",
                "Assets/TerrainSampleAssets/Textures/Terrain/Grass_Dry_Normal.tif",
                "Assets/TerrainSampleAssets/Textures/Terrain/Grass_Dry_MaskMap.tif",
                new Vector2(20f, 20f));

            List<TerrainLayer> validLayers = new List<TerrainLayer>();
            if (sandLayer != null) validLayers.Add(sandLayer);
            if (soilRocksLayer != null) validLayers.Add(soilRocksLayer);
            if (canyonRockLayer != null) validLayers.Add(canyonRockLayer);
            if (dryScrubLayer != null) validLayers.Add(dryScrubLayer);

            if (validLayers.Count > 0)
            {
                terrainData.terrainLayers = validLayers.ToArray();
            }

            // Sculpt Canyon Basin & Surrounding Mountain Ranges
            float[,] heights = new float[hmRes, hmRes];
            for (int y = 0; y < hmRes; y++)
            {
                float ny = (float)y / (hmRes - 1);
                float worldOffsetZ = (ny - 0.5f) * terrainWidth;

                for (int x = 0; x < hmRes; x++)
                {
                    float nx = (float)x / (hmRes - 1);
                    float worldOffsetX = (nx - 0.5f) * terrainWidth;

                    float distFromArenaCenter = Mathf.Sqrt(worldOffsetX * worldOffsetX + worldOffsetZ * worldOffsetZ);

                    // Keep inner 58m radius completely flat at 0 so the 60m x 60m playable town floor is completely untouched
                    if (distFromArenaCenter <= 58f)
                    {
                        heights[y, x] = 0f;
                        continue;
                    }

                    // Smooth ramp outside 58m up to 420m
                    float rimT = Mathf.Clamp01((distFromArenaCenter - 58f) / 230f);
                    float smoothRim = rimT * rimT * (3f - 2f * rimT);

                    // Multi-octave canyon ridges & terraced Western mesas
                    float n1 = Mathf.PerlinNoise(nx * 4.5f + 1.7f, ny * 4.5f + 2.3f);
                    float n2 = Mathf.PerlinNoise(nx * 11.0f + 5.1f, ny * 11.0f + 8.4f) * 0.35f;
                    float ridge = 1f - Mathf.Abs(Mathf.PerlinNoise(nx * 3.2f + 0.4f, ny * 3.2f + 0.9f) * 2f - 1f);
                    ridge = Mathf.Pow(ridge, 1.5f);

                    float rawH = (n1 * 0.58f + ridge * 0.62f + n2 * 0.25f) * smoothRim;

                    // Add Western Mesa terracing steps
                    float terraced = Mathf.Floor(rawH * 7f) / 7f;
                    float finalH = Mathf.Lerp(rawH, terraced, 0.38f);

                    // Distant horizon mountain peaks near outer border
                    float outerEdge = Mathf.Clamp01((distFromArenaCenter - 165f) / 210f);
                    finalH += outerEdge * outerEdge * 0.38f * (0.6f + 0.4f * n1);

                    heights[y, x] = Mathf.Clamp01(finalH * 0.88f);
                }
            }
            terrainData.SetHeights(0, 0, heights);

            // Paint Splatmap Alpha Maps based on altitude & slope
            if (validLayers.Count == 4)
            {
                terrainData.alphamapResolution = 128;
                int amRes = terrainData.alphamapResolution;
                float[,,] alphas = new float[amRes, amRes, 4];

                for (int y = 0; y < amRes; y++)
                {
                    float normY = (float)y / (amRes - 1);
                    for (int x = 0; x < amRes; x++)
                    {
                        float normX = (float)x / (amRes - 1);
                        float h = terrainData.GetInterpolatedHeight(normX, normY) / terrainHeight;
                        float steepness = terrainData.GetSteepness(normX, normY) / 90f;

                        float wSand = Mathf.Clamp01(1.0f - h * 5.0f) * (1f - steepness);
                        float wRock = Mathf.Clamp01(steepness * 2.4f + h * 0.6f);
                        float wDryScrub = Mathf.Clamp01(Mathf.Sin(h * Mathf.PI * 2.5f)) * (1f - steepness * 0.8f) * 0.6f;
                        float wSoil = 0.35f;

                        float sum = wSand + wSoil + wRock + wDryScrub + 0.0001f;
                        alphas[y, x, 0] = wSand / sum;
                        alphas[y, x, 1] = wSoil / sum;
                        alphas[y, x, 2] = wRock / sum;
                        alphas[y, x, 3] = wDryScrub / sum;
                    }
                }
                terrainData.SetAlphamaps(0, 0, alphas);
            }

            GameObject terrainGo = Terrain.CreateTerrainGameObject(terrainData);
            terrainGo.name = "Wildlock_OuterVistaTerrain";
            terrainGo.transform.SetParent(parent, false);
            // Center the 900x900 terrain at (-124.8, -0.35, -10) slightly below the town floor
            terrainGo.transform.position = new Vector3(-124.8f - terrainWidth * 0.5f, -0.35f, -10f - terrainWidth * 0.5f);

            Terrain terrainComp = terrainGo.GetComponent<Terrain>();
            Shader urpTerrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (urpTerrainShader != null)
            {
                string matPath = GeneratedFolder + "/Wildlock_URP_TerrainMat.mat";
                Material terrainMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (terrainMat == null)
                {
                    terrainMat = new Material(urpTerrainShader);
                    AssetDatabase.CreateAsset(terrainMat, matPath);
                }
                terrainComp.materialTemplate = terrainMat;
            }

            TerrainCollider tc = terrainGo.GetComponent<TerrainCollider>();
            if (tc != null) tc.terrainData = terrainData;

            // Place Desert Foliage from TerrainSampleAssets along the outer canyon foothills
            PopulateOuterVistaProps(parent, terrainComp, terrainGo.transform.position);
        }

        private static TerrainLayer CreateOrLoadTerrainLayer(string layerName, string diffusePath, string normalPath, string maskPath, Vector2 tileSize)
        {
            string assetPath = $"{GeneratedFolder}/{layerName}.terrainlayer";
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(assetPath);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, assetPath);
            }

            Texture2D diff = AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath);
            Texture2D norm = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);

            if (diff != null) layer.diffuseTexture = diff;
            if (norm != null) layer.normalMapTexture = norm;
            if (mask != null) layer.maskMapTexture = mask;
            layer.tileSize = tileSize;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        private static void PopulateOuterVistaProps(Transform parent, Terrain terrain, Vector3 terrainOrigin)
        {
            GameObject vistaPropsRoot = new GameObject("OuterVista_FoliageAndMountains");
            vistaPropsRoot.transform.SetParent(parent, false);

            // Upgrade TerrainSampleAssets foliage materials to URP AlphaClip if needed so they never render pink
            UpgradeFoliageMaterialsToUrp();

            string[] foliagePrefabPaths =
            {
                "Assets/TerrainSampleAssets/Prefabs/BushDry_A.prefab",
                "Assets/TerrainSampleAssets/Prefabs/BushDry_B.prefab",
                "Assets/TerrainSampleAssets/Prefabs/GrassDry_A.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Heather_A.prefab"
            };

            List<GameObject> foliagePrefabs = new List<GameObject>();
            foreach (string p in foliagePrefabPaths)
            {
                GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (pf != null) foliagePrefabs.Add(pf);
            }

            Vector3 arenaCenter = new Vector3(-124.8f, 0f, -10f);
            System.Random rng = new System.Random(1885);

            // Scatter 75 dry desert shrubs along the visible foothills outside the boundary rocks (radius 60m to 165m)
            if (foliagePrefabs.Count > 0 && terrain != null)
            {
                for (int i = 0; i < 75; i++)
                {
                    float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                    float radius = Mathf.Lerp(60f, 165f, (float)rng.NextDouble());
                    float wx = arenaCenter.x + Mathf.Cos(angle) * radius;
                    float wz = arenaCenter.z + Mathf.Sin(angle) * radius;
                    float wy = terrain.SampleHeight(new Vector3(wx, 0f, wz)) + terrainOrigin.y;

                    GameObject prefab = foliagePrefabs[i % foliagePrefabs.Count];
                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, vistaPropsRoot.transform);
                    inst.transform.position = new Vector3(wx, wy, wz);
                    inst.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f);
                    float scale = Mathf.Lerp(1.3f, 2.6f, (float)rng.NextDouble());
                    inst.transform.localScale = Vector3.one * scale;
                }
            }
        }

        private static void UpgradeFoliageMaterialsToUrp()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;

            string[] matPaths =
            {
                "Assets/TerrainSampleAssets/Materials/BushDry.asset",
                "Assets/TerrainSampleAssets/Materials/GrassDry.asset",
                "Assets/TerrainSampleAssets/Materials/Heather.asset"
            };

            foreach (string mp in matPaths)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (m == null) continue;

                Texture baseTex = m.HasProperty("Base_Map") ? m.GetTexture("Base_Map") : m.mainTexture;
                m.shader = urpLit;
                if (baseTex != null) m.SetTexture("_BaseMap", baseTex);
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", 0.35f);
                m.SetFloat("_Cull", 0f); // Double-sided foliage
                m.EnableKeyword("_ALPHATEST_ON");
                EditorUtility.SetDirty(m);
            }
        }

        private static WildlockLightingManager BuildLightingSystem(Transform parent)
        {
            GameObject lightingRoot = new GameObject("Lighting_And_Atmosphere");
            lightingRoot.transform.SetParent(parent, false);

            WildlockLightingManager mgr = lightingRoot.AddComponent<WildlockLightingManager>();

            GameObject dirLightGo = GameObject.Find("Directional Light");
            if (dirLightGo != null)
            {
                mgr.directionalSunLight = dirLightGo.GetComponent<Light>();
            }

            // 1. Town Generator Floodlights (4 tall street floodlights around the central thoroughfare)
            Vector3[] floodlightPositions =
            {
                new Vector3(-118f, 7.5f, -6f),
                new Vector3(-132f, 7.5f, -6f),
                new Vector3(-118f, 7.5f, -20f),
                new Vector3(-138f, 7.5f, -18f)
            };

            foreach (Vector3 pos in floodlightPositions)
            {
                GameObject flGo = new GameObject("TownGenerator_Floodlight");
                flGo.transform.SetParent(lightingRoot.transform, false);
                flGo.transform.position = pos;
                flGo.transform.rotation = Quaternion.Euler(65f, 0f, 0f);

                Light spot = flGo.AddComponent<Light>();
                spot.type = LightType.Spot;
                spot.color = new Color(1f, 0.92f, 0.72f);
                spot.intensity = 22f;
                spot.range = 28f;
                spot.spotAngle = 82f;
                spot.enabled = false;
                mgr.townGeneratorFloodlights.Add(spot);
            }

            // 2. Warm Flickering Torches at Gold Mine & Saloon
            Vector3[] torchPositions =
            {
                new Vector3(-136.5f, 2.6f, 6.8f),   // Gold Mine Left Torch
                new Vector3(-139.8f, 2.6f, 6.8f),   // Gold Mine Right Torch
                new Vector3(-110.5f, 2.8f, -16.5f), // Saloon Porch Left
                new Vector3(-115.2f, 2.8f, -16.5f), // Saloon Porch Right
                new Vector3(-141.5f, 2.8f, -22.5f)  // General Store Porch
            };

            List<Light> mineLights = new List<Light>();
            List<Light> saloonLights = new List<Light>();

            for (int i = 0; i < torchPositions.Length; i++)
            {
                GameObject torchGo = new GameObject($"Frontier_TorchLight_{i}");
                torchGo.transform.SetParent(lightingRoot.transform, false);
                torchGo.transform.position = torchPositions[i];

                Light pt = torchGo.AddComponent<Light>();
                pt.type = LightType.Point;
                pt.color = new Color(1f, 0.58f, 0.18f);
                pt.intensity = 5.5f;
                pt.range = 12f;
                mgr.mineAndSaloonTorches.Add(pt);

                if (i < 2) mineLights.Add(pt);
                else saloonLights.Add(pt);
            }

            // 3. Showdown Countdown Pulsing Red Alarm Beacons around the Central Mine Cart
            Vector3 cartPos = new Vector3(-134f, 2.4f, 5.1f);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f * Mathf.Deg2Rad;
                GameObject alarmGo = new GameObject($"Showdown_AlarmBeacon_{i}");
                alarmGo.transform.SetParent(lightingRoot.transform, false);
                alarmGo.transform.position = cartPos + new Vector3(Mathf.Cos(a) * 5f, 0f, Mathf.Sin(a) * 5f);

                Light al = alarmGo.AddComponent<Light>();
                al.type = LightType.Point;
                al.color = new Color(1f, 0.08f, 0.12f);
                al.intensity = 10f;
                al.range = 14f;
                al.enabled = false;
                mgr.countdownEmergencyLights.Add(al);
            }

            // 4. Environment Dark Zone Trigger Volumes (Section 3.3: Entering Mine or Saloon darkens exterior & boosts local torches)
            CreateLightZoneTrigger(lightingRoot.transform, "Gold Mine Cavern Entrance", new Vector3(-138.25f, 2.0f, 7.8f), new Vector3(8f, 4.5f, 8f), mineLights.ToArray());
            CreateLightZoneTrigger(lightingRoot.transform, "Saloon Covered Porch & Interior", new Vector3(-112.6f, 2.0f, -19.5f), new Vector3(11f, 4.5f, 9f), saloonLights.ToArray());

            return mgr;
        }

        private static void CreateLightZoneTrigger(Transform parent, string zoneName, Vector3 pos, Vector3 size, Light[] lights)
        {
            GameObject zoneGo = new GameObject("LightZone_" + zoneName);
            zoneGo.transform.SetParent(parent, false);
            zoneGo.transform.position = pos;

            BoxCollider bc = zoneGo.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = size;

            WildlockLightZoneTrigger trigger = zoneGo.AddComponent<WildlockLightZoneTrigger>();
            trigger.zoneDisplayName = zoneName;
            trigger.zoneSpecificLights = lights;
        }

        private static WildlockPlayerController BuildPlayerHero(Transform parent, WildlockLightingManager lightingMgr)
        {
            GameObject playerGo = new GameObject("Wildlock_PlayerHero");
            playerGo.tag = "Player";
            playerGo.transform.SetParent(parent, false);
            // Spawn in the clear central main street looking directly toward the Central Mine Cart, Saloon, and Generator Switch
            playerGo.transform.position = new Vector3(-122.0f, 0.15f, -12.0f);
            playerGo.transform.rotation = Quaternion.Euler(0f, -25f, 0f);

            CharacterController cc = playerGo.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.93f, 0f);
            cc.radius = 0.32f;

            // Camera Targets
            GameObject camRoot = new GameObject("PlayerCameraRoot");
            camRoot.tag = "CinemachineTarget";
            camRoot.transform.SetParent(playerGo.transform, false);
            camRoot.transform.localPosition = new Vector3(0f, 1.48f, 0f);

            GameObject fpsEye = new GameObject("FirstPersonEyeTarget");
            fpsEye.transform.SetParent(playerGo.transform, false);
            fpsEye.transform.localPosition = new Vector3(0f, 1.66f, 0.24f);

            GameObject muzzle = new GameObject("RevolverMuzzlePoint");
            muzzle.transform.SetParent(camRoot.transform, false);
            muzzle.transform.localPosition = new Vector3(0.32f, -0.15f, 0.65f);

            // Attach Player-Controlled Western Oil Lantern & Aim Spotlight (Lighting 3.2) without floating primitive mesh
            GameObject lanternHolder = new GameObject("Player_WesternLantern");
            lanternHolder.transform.SetParent(playerGo.transform, false);
            lanternHolder.transform.localPosition = new Vector3(-0.32f, 1.15f, 0.25f);

            Light ptLight = lanternHolder.AddComponent<Light>();
            ptLight.type = LightType.Point;
            ptLight.color = new Color(1f, 0.72f, 0.32f);
            ptLight.intensity = 6.5f;
            ptLight.range = 15f;

            GameObject spotGo = new GameObject("LanternAimSpotlight");
            spotGo.transform.SetParent(camRoot.transform, false);
            spotGo.transform.localPosition = new Vector3(0f, 0f, 0.2f);
            Light spotLight = spotGo.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.color = new Color(1f, 0.85f, 0.55f);
            spotLight.intensity = 12f;
            spotLight.range = 32f;
            spotLight.spotAngle = 52f;

            if (lightingMgr != null)
            {
                lightingMgr.playerLanternPointLight = ptLight;
                lightingMgr.playerAimSpotLight = spotLight;
                lightingMgr.playerLanternBulbRenderer = null;
            }

            // Instantiate Swappable 3D Character Skins (Adam Character Pack + Starter Assets)
            RuntimeAnimatorController animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller");

            List<GameObject> skins = new List<GameObject>();

            GameObject guardSkin = InstantiateAdamPackCharacter(
                "Assets/UnityTechnologies/Adam Character Pack/Guard/Guard.FBX",
                "HeroSkin_GuardEnforcer", playerGo.transform, animCtrl, false);
            if (guardSkin != null) skins.Add(guardSkin);

            GameObject adamSkin = InstantiateAdamPackCharacter(
                "Assets/UnityTechnologies/Adam Character Pack/Adam/Adam.FBX",
                "HeroSkin_AdamVanguard", playerGo.transform, animCtrl, false);
            if (adamSkin != null) skins.Add(adamSkin);

            GameObject luSkin = InstantiateAdamPackCharacter(
                "Assets/UnityTechnologies/Adam Character Pack/Lu/Lu.FBX",
                "HeroSkin_LuCommander", playerGo.transform, animCtrl, false);
            if (luSkin != null) skins.Add(luSkin);

            GameObject armatureSkin = InstantiateStarterArmatureSkin(
                "HeroSkin_StarterArmature", playerGo.transform, animCtrl);
            if (armatureSkin != null) skins.Add(armatureSkin);

            GameObject kyleSkin = InstantiateCharacterSkin(
                "Assets/Starter Assets/Runtime/SpaceRobotKyle/Prefabs/KyleRobot.prefab",
                "HeroSkin_KyleGunslinger", playerGo.transform, animCtrl);
            if (kyleSkin != null) skins.Add(kyleSkin);

            for (int i = 0; i < skins.Count; i++)
            {
                skins[i].SetActive(i == 0);
            }

            WildlockPlayerController ctrl = playerGo.AddComponent<WildlockPlayerController>();
            ctrl.cameraFollowTarget = camRoot.transform;
            ctrl.firstPersonEyeTarget = fpsEye.transform;
            ctrl.weaponMuzzlePoint = muzzle.transform;
            ctrl.characterSkins = skins.ToArray();
            ctrl.activeSkinIndex = 0;

            return ctrl;
        }

        private static GameObject InstantiateStarterArmatureSkin(string name, Transform parent, RuntimeAnimatorController animCtrl)
        {
            string fbxPath = "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Models/Armature.fbx";
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null) return null;

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, parent);
            inst.name = name;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;

            Material bodyMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Materials/M_Armature_Body.mat");
            Material armsMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Materials/M_Armature_Arms.mat");

            SkinnedMeshRenderer smr = inst.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr != null && bodyMat != null && armsMat != null)
            {
                smr.sharedMaterials = new[] { bodyMat, armsMat };
            }

            Animator anim = inst.GetComponent<Animator>();
            if (anim == null) anim = inst.AddComponent<Animator>();
            Avatar av = AssetDatabase.LoadAssetAtPath<Avatar>(fbxPath);
            if (av != null) anim.avatar = av;
            if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (anim.GetComponent<WildlockAnimationEventReceiver>() == null)
            {
                anim.gameObject.AddComponent<WildlockAnimationEventReceiver>();
            }
            return inst;
        }

        private static GameObject InstantiateCharacterSkin(string prefabPath, string name, Transform parent, RuntimeAnimatorController animCtrl)
        {
            GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (pf == null) return null;

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
            inst.name = name;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;

            Animator anim = inst.GetComponentInChildren<Animator>();
            if (anim == null) anim = inst.AddComponent<Animator>();
            if (anim.avatar == null)
            {
                Avatar kyleAv = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/Starter Assets/Runtime/SpaceRobotKyle/Models/KyleRobot.fbx");
                if (kyleAv != null) anim.avatar = kyleAv;
            }
            if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (anim.GetComponent<WildlockAnimationEventReceiver>() == null)
            {
                anim.gameObject.AddComponent<WildlockAnimationEventReceiver>();
            }
            return inst;
        }

        private static GameObject InstantiateAdamPackCharacter(string fbxPath, string name, Transform parent, RuntimeAnimatorController animCtrl, bool attachWeapon)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null) return null;

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, parent);
            inst.name = name;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;

            // Apply the exact URP PBR materials & submesh active states from Guard_Scene, Lu_Scene, and Adam_Scene
            if (fbxPath.Contains("Guard.FBX"))
            {
                ConfigureGuardCharacter(inst);
            }
            else if (fbxPath.Contains("Lu.FBX"))
            {
                ConfigureLuCharacter(inst);
            }
            else if (fbxPath.Contains("Adam.FBX"))
            {
                ConfigureAdamCharacter(inst);
            }

            Animator anim = inst.GetComponent<Animator>();
            if (anim == null) anim = inst.AddComponent<Animator>();
            Avatar fbxAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(fbxPath);
            if (fbxAvatar != null) anim.avatar = fbxAvatar;
            if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (anim.GetComponent<WildlockAnimationEventReceiver>() == null)
            {
                anim.gameObject.AddComponent<WildlockAnimationEventReceiver>();
            }

            return inst;
        }

        private static void ConfigureGuardCharacter(GameObject inst)
        {
            Material body01 = AssetDatabase.LoadAssetAtPath<Material>("Assets/UnityTechnologies/Adam Character Pack/Guard/Materials/Guard_Body_01.mat");
            Material body02 = AssetDatabase.LoadAssetAtPath<Material>("Assets/UnityTechnologies/Adam Character Pack/Guard/Materials/Guard_Body_02.mat");
            Material headWhite = AssetDatabase.LoadAssetAtPath<Material>("Assets/UnityTechnologies/Adam Character Pack/Guard/Materials/Guard_Head_white.mat");
            Material lashesWhite = AssetDatabase.LoadAssetAtPath<Material>("Assets/UnityTechnologies/Adam Character Pack/Guard/Materials/Guard_eye_lashes_white.mat");
            Material reflEyesWhite = AssetDatabase.LoadAssetAtPath<Material>("Assets/UnityTechnologies/Adam Character Pack/Guard/Materials/Guard_reflection_eyes_white.mat");

            Renderer[] renderers = inst.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                string n = r.gameObject.name;
                switch (n)
                {
                    case "Guard_Body_01":
                        r.gameObject.SetActive(true);
                        if (body01 != null) r.sharedMaterial = body01;
                        break;
                    case "Guard_Body_02":
                        r.gameObject.SetActive(true);
                        if (body02 != null) r.sharedMaterial = body02;
                        break;
                    case "Guard_head_white":
                        r.gameObject.SetActive(true);
                        if (headWhite != null) r.sharedMaterial = headWhite;
                        break;
                    case "Guard_eye_lashes_white":
                        r.gameObject.SetActive(true);
                        if (lashesWhite != null) r.sharedMaterial = lashesWhite;
                        break;
                    case "Guard_reflection_eyes_white":
                        r.gameObject.SetActive(true);
                        if (reflEyesWhite != null) r.sharedMaterial = reflEyesWhite;
                        break;
                    default:
                        // Hide unused duplicate heads/weapons (head_black, head_yellow, migli, M4A1, etc.)
                        r.gameObject.SetActive(false);
                        break;
                }
            }
        }

        private static void ConfigureLuCharacter(GameObject inst)
        {
            Material headMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/UnityTechnologies/Adam Character Pack/Lu/Materials/Lieutenant_Head.mat");
            Material bodyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/UnityTechnologies/Adam Character Pack/Lu/Materials/Lieutenant_body.mat");
            Material jacketMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/UnityTechnologies/Adam Character Pack/Lu/Materials/Lieutenant_Jacket.mat");
            Material gunsMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/UnityTechnologies/Adam Character Pack/Lu/Materials/Lieutenant_guns.mat");
            Material eyesMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/UnityTechnologies/Adam Character Pack/Lu/Materials/Lieutenant_Eyes.mat");
            Material eyeLensMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/UnityTechnologies/Adam Character Pack/Adam/Materials/Eye_Lens.mat");

            Renderer[] renderers = inst.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                string n = r.gameObject.name;
                switch (n)
                {
                    case "Bandages":
                    case "Head":
                        r.gameObject.SetActive(true);
                        if (headMat != null) r.sharedMaterial = headMat;
                        break;
                    case "Body":
                    case "Stilts_clean":
                        r.gameObject.SetActive(true);
                        if (bodyMat != null) r.sharedMaterial = bodyMat;
                        break;
                    case "Jacket_clean":
                    case "Jacket_details_clean":
                        r.gameObject.SetActive(true);
                        if (jacketMat != null) r.sharedMaterial = jacketMat;
                        break;
                    case "Rifle":
                        if (gunsMat != null) r.sharedMaterial = gunsMat;
                        break;
                    case "eyes":
                        // Disable mocap eye-rig proxy mesh (which otherwise renders as two white goggle blobs over Lu's face)
                        r.gameObject.SetActive(false);
                        break;
                }
            }

            // Attach Lieutenant_Eyes.FBX to Head_MoCap_M just like Lu_Scene.unity
            Transform headBone = FindDeepChild(inst.transform, "Head_MoCap_M");
            GameObject eyesFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UnityTechnologies/Adam Character Pack/Lu/Lieutenant_Eyes.FBX");
            if (headBone != null && eyesFbx != null && headBone.Find("Lieutenant_Eyes") == null)
            {
                GameObject eyesInst = (GameObject)PrefabUtility.InstantiatePrefab(eyesFbx, headBone);
                eyesInst.name = "Lieutenant_Eyes";
                eyesInst.transform.localPosition = new Vector3(-0.00046798456f, -0.00011907047f, -0.00065535004f);
                eyesInst.transform.localRotation = new Quaternion(0.0022801857f, -0.0000059132126f, -0.00000010430814f, 0.99999744f);
                eyesInst.transform.localScale = Vector3.one;

                Renderer eyeRenderer = eyesInst.GetComponentInChildren<Renderer>(true);
                if (eyeRenderer != null && eyesMat != null && eyeLensMat != null)
                {
                    eyeRenderer.sharedMaterials = new[] { eyesMat, eyeLensMat };
                }
            }
        }

        private static void ConfigureAdamCharacter(GameObject inst)
        {
            const string matDir = "Assets/UnityTechnologies/Adam Character Pack/Adam/Materials/";
            Material armsMetal = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Adam_Arms_metal.mat");
            Material armsRubber = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Adam_Arms_rubber.mat");
            Material armsPlastic = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Adam_Arms_plastic.mat");
            Material headMetal = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Adam_head_metal.mat");
            Material headPlastic = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Adam_head_plastic.mat");
            Material headRubber = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Adam_head_rubber.mat");
            Material legsMetal = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Adam_Legs_metal.mat");
            Material legsRubber = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Adam_Legs_rubber.mat");
            Material legsPlastic = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Adam_Legs_plastic.mat");
            Material torsoMetal = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Adam_Torso_metal.mat");
            Material torsoRubber = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Adam_Torso_rubber.mat");
            Material torsoPlastic = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Adam_Torso_plastic.mat");
            Material maskMat = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Adam_mask.mat");
            Material maskMetal = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Adam_Mask_metal.mat");
            Material monNeck = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Adam_monitors_neck.mat");
            Material monFront = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Adam_monitors.mat");
            Material eyeBlades = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Eye_blades.mat");
            Material eyeSclera = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Eye_Sclera.mat");
            Material eyeLens = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Eye_Lens.mat");
            Material eyeMetal = AssetDatabase.LoadAssetAtPath<Material>(matDir + "Eye_metal.mat");

            Renderer[] renderers = inst.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                string n = r.gameObject.name;

                // Disable rig helper meshes & unused/damaged/LOD1 submeshes
                if (n.StartsWith("Rig_Trapezius") || n == "RootMotion" ||
                    n.Contains("LOD_01") || n.Contains("Obsolete") ||
                    n.Contains("smashed") || n.Contains("Bloody") ||
                    n == "mesh_Adam_arm_WOSleeve" || n == "spine")
                {
                    r.enabled = false;
                    r.gameObject.SetActive(false);
                    continue;
                }

                switch (n)
                {
                    case "mesh_Adam_mask":
                        r.gameObject.SetActive(true);
                        r.enabled = true;
                        if (maskMat != null && maskMetal != null) r.sharedMaterials = new[] { maskMat, maskMetal };
                        break;
                    case "mesh_Adam_arms":
                        r.gameObject.SetActive(true);
                        if (armsMetal != null && armsRubber != null && armsPlastic != null)
                            r.sharedMaterials = new[] { armsMetal, armsRubber, armsPlastic };
                        break;
                    case "mesh_Adam_head":
                        r.gameObject.SetActive(true);
                        if (headMetal != null && headPlastic != null && headRubber != null)
                            r.sharedMaterials = new[] { headMetal, headPlastic, headRubber };
                        break;
                    case "mesh_Adam_legs":
                        r.gameObject.SetActive(true);
                        if (legsMetal != null && legsRubber != null && legsPlastic != null)
                            r.sharedMaterials = new[] { legsMetal, legsRubber, legsPlastic };
                        break;
                    case "mesh_Adam_LSleeve":
                        r.gameObject.SetActive(true);
                        if (armsPlastic != null && armsMetal != null && armsRubber != null)
                            r.sharedMaterials = new[] { armsPlastic, armsMetal, armsRubber };
                        break;
                    case "mesh_Adam_RSleeve":
                        r.gameObject.SetActive(true);
                        if (armsMetal != null && armsPlastic != null && armsRubber != null)
                            r.sharedMaterials = new[] { armsMetal, armsPlastic, armsRubber };
                        break;
                    case "mesh_Adam_torso":
                        r.gameObject.SetActive(true);
                        if (torsoMetal != null && torsoRubber != null && torsoPlastic != null)
                            r.sharedMaterials = new[] { torsoMetal, torsoRubber, torsoPlastic };
                        break;
                    case "cables":
                        r.gameObject.SetActive(true);
                        if (headPlastic != null) r.sharedMaterial = headPlastic;
                        break;
                    case "mesh_Adam_spine":
                        r.gameObject.SetActive(true);
                        if (torsoMetal != null && torsoPlastic != null)
                            r.sharedMaterials = new[] { torsoMetal, torsoPlastic };
                        break;
                    case "Adam_monitor_back":
                        r.gameObject.SetActive(true);
                        if (monNeck != null) r.sharedMaterial = monNeck;
                        break;
                    case "Adam_monitor_front":
                        r.gameObject.SetActive(true);
                        if (monFront != null) r.sharedMaterial = monFront;
                        break;
                    default:
                        if (n.EndsWith("_Lens"))
                        {
                            r.gameObject.SetActive(true);
                            if (eyeLens != null) r.sharedMaterial = eyeLens;
                        }
                        else if (n.Contains("_BlendBase") || n.Contains("_BlendScrew_"))
                        {
                            if (eyeSclera != null) r.sharedMaterial = eyeSclera;
                        }
                        else if (n.EndsWith("_Sclera") || n.Contains("_Side"))
                        {
                            if (eyeMetal != null) r.sharedMaterial = eyeMetal;
                        }
                        else if (n.StartsWith("Eye_"))
                        {
                            if (eyeBlades != null) r.sharedMaterial = eyeBlades;
                        }
                        break;
                }
            }
        }

        private static Transform FindDeepChild(Transform parent, string targetName)
        {
            if (parent == null) return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform c = parent.GetChild(i);
                if (c.name == targetName) return c;
                Transform found = FindDeepChild(c, targetName);
                if (found != null) return found;
            }
            return null;
        }

        private static void EnsureNoEmbeddedFbxCameras()
        {
            string[] fbxPaths =
            {
                "Assets/wildwest/water-tower-water-tanker-22-mb/source/Water Tower Water Tanker.fbx",
                "Assets/sign-w-wanted-poster/source/Sign_final_attempt.fbx",
                "Assets/wildwest/stylized-gold-mine/source/mine.fbx",
                "Assets/barrel/general-store/source/generalstorenoglass.fbx",
                "Assets/desert/carriage/source 1/model/lpcarriage2.fbx"
            };

            foreach (string path in fbxPaths)
            {
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer != null && (importer.importCameras || importer.importLights))
                {
                    importer.importCameras = false;
                    importer.importLights = false;
                    importer.SaveAndReimport();
                }
            }
        }

        private static WildlockCameraDirector BuildCinemachineCameras(Transform parent, WildlockPlayerController player, Transform centralCart)
        {
            GameObject camRoot = new GameObject("Cinemachine_CameraRig");
            camRoot.transform.SetParent(parent, false);

            // Ensure Main Camera has CinemachineBrain, highest depth (10), and extended FarClipPlane for outer terrain vista
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.enabled = true;
                mainCam.depth = 10f;
                mainCam.clearFlags = CameraClearFlags.Skybox;
                mainCam.cullingMask = ~0;
                mainCam.nearClipPlane = 0.15f;
                mainCam.farClipPlane = 2500f;
                mainCam.transform.position = new Vector3(-120.2f, 2.15f, -15.6f);
                mainCam.transform.rotation = Quaternion.Euler(6f, -25f, 0f);
                CinemachineBrain brain = mainCam.GetComponent<CinemachineBrain>();
                if (brain == null) brain = mainCam.gameObject.AddComponent<CinemachineBrain>();
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.75f);
            }

            // 1. Third-Person Virtual Camera (Clean Cinemachine 3 setup with obstacle clipping disabled)
            GameObject tpGo = new GameObject("VCam_1_ThirdPersonFollow");
            tpGo.transform.SetParent(camRoot.transform, false);
            CinemachineCamera tpVCam = tpGo.AddComponent<CinemachineCamera>();
            var tpFollow = tpGo.AddComponent<CinemachineThirdPersonFollow>();
            tpFollow.Damping = new Vector3(0.08f, 0.2f, 0.2f);
            tpFollow.ShoulderOffset = new Vector3(0.6f, 0.3f, 0f);
            tpFollow.VerticalArmLength = 0.25f;
            tpFollow.CameraSide = 0.75f;
            tpFollow.CameraDistance = 4.2f;
            tpFollow.AvoidObstacles.Enabled = false;

            var tpLens = tpVCam.Lens;
            tpLens.FieldOfView = 48f;
            tpLens.NearClipPlane = 0.15f;
            tpLens.FarClipPlane = 2500f;
            tpVCam.Lens = tpLens;

            if (player != null)
            {
                tpVCam.Follow = player.cameraFollowTarget;
                tpVCam.Priority = new PrioritySettings { Enabled = true, Value = 20 };
            }

            // 2. First-Person POV Virtual Camera
            GameObject fpGo = new GameObject("VCam_2_FirstPersonPOV");
            fpGo.transform.SetParent(camRoot.transform, false);
            CinemachineCamera fpVCam = fpGo.AddComponent<CinemachineCamera>();
            fpGo.AddComponent<CinemachineHardLockToTarget>();
            fpGo.AddComponent<CinemachineRotateWithFollowTarget>();
            var fpLens = fpVCam.Lens;
            fpLens.FieldOfView = 62f;
            fpLens.NearClipPlane = 0.12f;
            fpLens.FarClipPlane = 2500f;
            fpVCam.Lens = fpLens;
            if (player != null)
            {
                fpVCam.Follow = player.firstPersonEyeTarget;
            }
            fpVCam.Priority = new PrioritySettings { Enabled = true, Value = 5 };

            // 3. Cinematic / Event Transition Virtual Camera (Focused on Central Mine Cart & Rising Sniper Platform)
            GameObject cutGo = new GameObject("VCam_3_CutsceneObjectiveTransition");
            cutGo.transform.SetParent(camRoot.transform, false);
            cutGo.transform.position = new Vector3(-119.5f, 6.2f, -2.5f);
            cutGo.transform.rotation = Quaternion.Euler(22f, -62f, 0f);
            CinemachineCamera cutVCam = cutGo.AddComponent<CinemachineCamera>();
            cutGo.AddComponent<CinemachineRotationComposer>();
            var cutLens = cutVCam.Lens;
            cutLens.FieldOfView = 45f;
            cutLens.NearClipPlane = 0.15f;
            cutLens.FarClipPlane = 2500f;
            cutVCam.Lens = cutLens;
            if (centralCart != null)
            {
                cutVCam.LookAt = centralCart;
            }
            cutVCam.Priority = new PrioritySettings { Enabled = true, Value = 5 };

            // 4. Tactical Arena & Outer Vista Overview Camera
            GameObject ovGo = new GameObject("VCam_4_TacticalArenaOverview");
            ovGo.transform.SetParent(camRoot.transform, false);
            ovGo.transform.position = new Vector3(-92f, 26f, -32f);
            ovGo.transform.rotation = Quaternion.Euler(34f, -48f, 0f);
            CinemachineCamera ovVCam = ovGo.AddComponent<CinemachineCamera>();
            var ovLens = ovVCam.Lens;
            ovLens.FieldOfView = 55f;
            ovLens.NearClipPlane = 0.2f;
            ovLens.FarClipPlane = 2500f;
            ovVCam.Lens = ovLens;
            ovVCam.Priority = new PrioritySettings { Enabled = true, Value = 5 };

            WildlockCameraDirector director = camRoot.AddComponent<WildlockCameraDirector>();
            director.thirdPersonVCam = tpVCam;
            director.firstPersonVCam = fpVCam;
            director.cutsceneVCam = cutVCam;
            director.overviewVCam = ovVCam;
            director.thirdPersonFollowTarget = player != null ? player.cameraFollowTarget : null;
            director.firstPersonFollowTarget = player != null ? player.firstPersonEyeTarget : null;
            director.centralMineCartTarget = centralCart;
            director.playerVisualMeshesToHideInFPS = player != null ? player.characterSkins : null;

            return director;
        }

        private static WildlockSaloonDoor BuildInteractiveArenaMechanics(Transform parent, WildlockLightingManager lightingMgr)
        {
            GameObject mechRoot = new GameObject("Interactive_Arena_Mechanics");
            mechRoot.transform.SetParent(parent, false);

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material ironMat = new Material(urpLit) { color = new Color(0.24f, 0.26f, 0.30f) };
            Material brassMat = new Material(urpLit) { color = new Color(0.82f, 0.62f, 0.22f) };
            Material woodMat = new Material(urpLit) { color = new Color(0.48f, 0.32f, 0.18f) };
            Material barrelRedMat = new Material(urpLit) { color = new Color(0.78f, 0.14f, 0.12f) };

            // 1. Rising Central Sniper Platform & 2 Tactical Iron Cover Barriers (Controlled by Generator Switch)
            GameObject sniperPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sniperPlatform.name = "Rising_CentralSniperPlatform";
            sniperPlatform.transform.SetParent(mechRoot.transform, false);
            sniperPlatform.transform.position = new Vector3(-123.5f, -0.35f, -5.5f);
            sniperPlatform.transform.localScale = new Vector3(4.5f, 0.8f, 4.5f);
            sniperPlatform.GetComponent<Renderer>().sharedMaterial = ironMat;

            GameObject barrierEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrierEast.name = "Rising_IronCoverBarrier_East";
            barrierEast.transform.SetParent(mechRoot.transform, false);
            barrierEast.transform.position = new Vector3(-115.5f, -0.8f, -1.5f);
            barrierEast.transform.localScale = new Vector3(0.5f, 1.7f, 4.2f);
            barrierEast.GetComponent<Renderer>().sharedMaterial = ironMat;

            GameObject barrierWest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrierWest.name = "Rising_IronCoverBarrier_West";
            barrierWest.transform.SetParent(mechRoot.transform, false);
            barrierWest.transform.position = new Vector3(-131.5f, -0.8f, -9.5f);
            barrierWest.transform.localScale = new Vector3(0.5f, 1.7f, 4.2f);
            barrierWest.GetComponent<Renderer>().sharedMaterial = ironMat;

            // Generator Lever Switch Station
            GameObject switchStation = new GameObject("Arena_GeneratorLeverSwitch");
            switchStation.transform.SetParent(mechRoot.transform, false);
            switchStation.transform.position = new Vector3(-115.0f, 0f, 3.8f);

            BoxCollider switchCol = switchStation.AddComponent<BoxCollider>();
            switchCol.center = new Vector3(0f, 1.0f, 0f);
            switchCol.size = new Vector3(1.4f, 2.0f, 1.4f);

            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedestal.transform.SetParent(switchStation.transform, false);
            pedestal.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            pedestal.transform.localScale = new Vector3(0.7f, 1.2f, 0.7f);
            pedestal.GetComponent<Renderer>().sharedMaterial = ironMat;

            GameObject leverPivot = new GameObject("LeverArmPivot");
            leverPivot.transform.SetParent(switchStation.transform, false);
            leverPivot.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            leverPivot.transform.localRotation = Quaternion.Euler(42f, 0f, 0f);

            GameObject leverStick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leverStick.transform.SetParent(leverPivot.transform, false);
            leverStick.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            leverStick.transform.localScale = new Vector3(0.1f, 0.45f, 0.1f);
            leverStick.GetComponent<Renderer>().sharedMaterial = brassMat;

            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.transform.SetParent(switchStation.transform, false);
            orb.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            orb.transform.localScale = Vector3.one * 0.32f;
            Material orbMat = new Material(urpLit) { color = Color.red };
            orb.GetComponent<Renderer>().sharedMaterial = orbMat;

            WildlockEnvironmentSwitch envSwitch = switchStation.AddComponent<WildlockEnvironmentSwitch>();
            envSwitch.leverArm = leverPivot.transform;
            envSwitch.indicatorOrbRenderer = orb.GetComponent<Renderer>();
            envSwitch.risingPlatformsAndBarriers = new[] { sniperPlatform.transform, barrierEast.transform, barrierWest.transform };

            // 2. Saloon Interactive Swinging Batwing Doors & Sheriff's 5-Gem Armory Vault Gate
            CreateInteractiveDoor(mechRoot.transform, "Saloon Batwing Doors", new Vector3(-112.6f, 0f, -15.2f), 0f, 0, woodMat, lightingMgr);
            WildlockSaloonDoor armoryGate = CreateInteractiveDoor(mechRoot.transform, "Sheriff Armory Vault Gate (Unlocks at 5 Gems)", new Vector3(-108.5f, 0f, 6.2f), 90f, 5, brassMat, lightingMgr);

            // Place bonus Health Elixir & Ammo inside the Armory Gate
            CreateStatPickup(mechRoot.transform, WildlockStatPickup.PickupKind.HealthElixir, new Vector3(-106.2f, 0.75f, 6.2f));
            CreateStatPickup(mechRoot.transform, WildlockStatPickup.PickupKind.AmmoAndEnergyCrate, new Vector3(-106.2f, 0.75f, 7.6f));

            // 3. Steam-Powered Balcony Jump Pads (Launch onto Saloon Balcony & General Store Roof)
            CreateSteamJumpPad(mechRoot.transform, "Saloon Balcony Steam Jump Pad", new Vector3(-117.5f, 0.08f, -15.5f), 7.5f, brassMat);
            CreateSteamJumpPad(mechRoot.transform, "Mine Overlook Steam Jump Pad", new Vector3(-135.5f, 0.08f, -3.5f), 6.8f, brassMat);

            // 4. Extra Destructible Wooden Crates & Explosive Red Barrels along the Central Arena
            Vector3[] extraCratePositions =
            {
                new Vector3(-118.5f, 0.55f, -4.2f),
                new Vector3(-121.0f, 0.55f, 1.8f),
                new Vector3(-128.8f, 0.55f, -6.5f),
                new Vector3(-133.2f, 0.55f, -15.0f)
            };

            foreach (Vector3 cPos in extraCratePositions)
            {
                GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crate.name = "Destructible_WoodenCrate";
                crate.transform.SetParent(mechRoot.transform, false);
                crate.transform.position = cPos;
                crate.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
                crate.GetComponent<Renderer>().sharedMaterial = woodMat;

                WildlockDestructibleProp dp = crate.AddComponent<WildlockDestructibleProp>();
                dp.propDisplayName = "Destructible Supply Crate";
                dp.maxHealth = 200f;
                dp.isExplosiveBarrel = false;
                dp.dropsGemOnBreak = true;
            }

            Vector3[] extraBarrelPositions =
            {
                new Vector3(-120.2f, 0.65f, -8.5f),
                new Vector3(-130.5f, 0.65f, 2.2f),
                new Vector3(-125.0f, 0.65f, -14.5f)
            };

            foreach (Vector3 bPos in extraBarrelPositions)
            {
                GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                barrel.name = "Explosive_BlackPowderBarrel";
                barrel.transform.SetParent(mechRoot.transform, false);
                barrel.transform.position = bPos;
                barrel.transform.localScale = new Vector3(0.85f, 0.65f, 0.85f);
                barrel.GetComponent<Renderer>().sharedMaterial = barrelRedMat;

                WildlockDestructibleProp dp = barrel.AddComponent<WildlockDestructibleProp>();
                dp.propDisplayName = "Explosive TNT Barrel";
                dp.maxHealth = 150f;
                dp.isExplosiveBarrel = true;
                dp.dropsGemOnBreak = true;
            }

            // 5. Field Health Elixir & Ammo Pickups
            CreateStatPickup(mechRoot.transform, WildlockStatPickup.PickupKind.HealthElixir, new Vector3(-114.0f, 0.75f, -10.5f));
            CreateStatPickup(mechRoot.transform, WildlockStatPickup.PickupKind.AmmoAndEnergyCrate, new Vector3(-129.0f, 0.75f, -18.0f));

            return armoryGate;
        }

        private static WildlockSaloonDoor CreateInteractiveDoor(Transform parent, string title, Vector3 pos, float rotY, int requiredGems, Material mat, WildlockLightingManager lightingMgr)
        {
            GameObject doorRoot = new GameObject("Interactive_" + title);
            doorRoot.transform.SetParent(parent, false);
            doorRoot.transform.position = pos;
            doorRoot.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            BoxCollider triggerCol = doorRoot.AddComponent<BoxCollider>();
            triggerCol.center = new Vector3(0f, 1.2f, 0f);
            triggerCol.size = new Vector3(3.2f, 2.5f, 1.5f);

            GameObject leftPivot = new GameObject("LeftDoorPivot");
            leftPivot.transform.SetParent(doorRoot.transform, false);
            leftPivot.transform.localPosition = new Vector3(-1.1f, 1.1f, 0f);

            GameObject leftWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWing.transform.SetParent(leftPivot.transform, false);
            leftWing.transform.localPosition = new Vector3(0.52f, 0f, 0f);
            leftWing.transform.localScale = new Vector3(1.04f, 1.8f, 0.14f);
            leftWing.GetComponent<Renderer>().sharedMaterial = mat;

            GameObject rightPivot = new GameObject("RightDoorPivot");
            rightPivot.transform.SetParent(doorRoot.transform, false);
            rightPivot.transform.localPosition = new Vector3(1.1f, 1.1f, 0f);

            GameObject rightWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWing.transform.SetParent(rightPivot.transform, false);
            rightWing.transform.localPosition = new Vector3(-0.52f, 0f, 0f);
            rightWing.transform.localScale = new Vector3(1.04f, 1.8f, 0.14f);
            rightWing.GetComponent<Renderer>().sharedMaterial = mat;

            WildlockSaloonDoor door = doorRoot.AddComponent<WildlockSaloonDoor>();
            door.doorTitle = title;
            door.leftDoorWing = leftPivot.transform;
            door.rightDoorWing = rightPivot.transform;
            door.requiredGemsToUnlock = requiredGems;
            if (lightingMgr != null)
            {
                door.interiorLights = lightingMgr.mineAndSaloonTorches.ToArray();
            }

            return door;
        }

        private static void CreateSteamJumpPad(Transform parent, string name, Vector3 pos, float height, Material mat)
        {
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = name;
            pad.transform.SetParent(parent, false);
            pad.transform.position = pos;
            pad.transform.localScale = new Vector3(2.2f, 0.12f, 2.2f);
            pad.GetComponent<Renderer>().sharedMaterial = mat;

            SphereCollider sc = pad.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.7f;

            Light glow = pad.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(0.25f, 0.9f, 1f);
            glow.intensity = 4.5f;
            glow.range = 5.5f;

            WildlockJumpPad jp = pad.AddComponent<WildlockJumpPad>();
            jp.padName = name;
            jp.launchHeight = height;
        }

        private static void CreateStatPickup(Transform parent, WildlockStatPickup.PickupKind kind, Vector3 pos)
        {
            GameObject go = GameObject.CreatePrimitive(kind == WildlockStatPickup.PickupKind.HealthElixir ? PrimitiveType.Sphere : PrimitiveType.Cube);
            go.name = "Pickup_" + kind;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.55f;

            Collider col = go.GetComponent<Collider>();
            col.isTrigger = true;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material m = new Material(urpLit)
            {
                color = kind == WildlockStatPickup.PickupKind.HealthElixir
                    ? new Color(0.15f, 0.95f, 0.38f)
                    : new Color(1f, 0.78f, 0.18f)
            };
            go.GetComponent<Renderer>().sharedMaterial = m;

            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = m.color;
            l.intensity = 3.2f;
            l.range = 4.2f;

            WildlockStatPickup sp = go.AddComponent<WildlockStatPickup>();
            sp.kind = kind;
            sp.amount = 250;
        }

        private static void BuildAdamPackNpcs(Transform parent)
        {
            GameObject npcRoot = new GameObject("Wildlock_2v2_AdamPack_NPCs");
            npcRoot.transform.SetParent(parent, false);

            RuntimeAnimatorController animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller");

            // 1. Friendly Duo Partner: Adam (Team 1) spawned right beside the player in the central street
            CreateNpcInstance(
                npcRoot.transform,
                "Assets/UnityTechnologies/Adam Character Pack/Adam/Adam.FBX",
                "Sheriff_Adam (Duo Partner)",
                false,
                new Vector3(-126.0f, 0.05f, -9.5f),
                -20f,
                WildlockNpcAgent.NpcState.FollowPlayer,
                new[]
                {
                    new Vector3(-126f, 0.05f, -9.5f),
                    new Vector3(-120f, 0.05f, -2.0f),
                    new Vector3(-128f, 0.05f, 2.5f)
                },
                animCtrl);

            // 2. Enemy Outlaw 1: Guard (Team 2 Enforcer)
            CreateNpcInstance(
                npcRoot.transform,
                "Assets/UnityTechnologies/Adam Character Pack/Guard/Guard.FBX",
                "Outlaw_Guard (Enemy Enforcer)",
                true,
                new Vector3(-130.5f, 0.05f, -22.5f),
                25f,
                WildlockNpcAgent.NpcState.Patrol,
                new[]
                {
                    new Vector3(-130.5f, 0.05f, -22.5f),
                    new Vector3(-126.0f, 0.05f, -8.5f),
                    new Vector3(-133.5f, 0.05f, 1.5f),
                    new Vector3(-138.0f, 0.05f, -14.0f)
                },
                animCtrl);

            // 3. Enemy Outlaw 2: Lu (Team 2 Sharpshooter guarding the Central Mine Cart)
            CreateNpcInstance(
                npcRoot.transform,
                "Assets/UnityTechnologies/Adam Character Pack/Lu/Lu.FBX",
                "Outlaw_Lu (Mine Cart Boss)",
                true,
                new Vector3(-135.5f, 0.05f, -6.5f),
                45f,
                WildlockNpcAgent.NpcState.Patrol,
                new[]
                {
                    new Vector3(-135.5f, 0.05f, -6.5f),
                    new Vector3(-131.0f, 0.05f, 6.5f),
                    new Vector3(-122.5f, 0.05f, 4.0f),
                    new Vector3(-126.5f, 0.05f, -6.5f)
                },
                animCtrl);
        }

        private static void CreateNpcInstance(
            Transform parent,
            string fbxPath,
            string displayName,
            bool isEnemy,
            Vector3 spawnPos,
            float rotY,
            WildlockNpcAgent.NpcState initialState,
            Vector3[] waypoints,
            RuntimeAnimatorController animCtrl)
        {
            GameObject holder = new GameObject(displayName);
            holder.transform.SetParent(parent, false);
            holder.transform.position = spawnPos;
            holder.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            CapsuleCollider col = holder.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.height = 1.9f;
            col.radius = 0.42f;

            InstantiateAdamPackCharacter(fbxPath, "CharacterModel", holder.transform, animCtrl, true);

            WildlockNpcAgent agent = holder.AddComponent<WildlockNpcAgent>();
            agent.npcDisplayName = displayName;
            agent.isEnemyOutlaw = isEnemy;
            agent.maxHealth = 600f;
            agent.currentHealth = 600f;
            agent.currentState = initialState;
            agent.patrolWaypoints = waypoints;
        }
    }
}
#endif
