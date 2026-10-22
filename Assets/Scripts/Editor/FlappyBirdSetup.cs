#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using FlappyBird.Core;
using FlappyBird.Obstacles;
using FlappyBird.Player;
using FlappyBird.UI;

namespace FlappyBird.Editor
{
    public static class FlappyBirdSetup
    {
        [MenuItem("FlappyBird/Build Everything")]
        public static void BuildEverything()
        {
            Debug.Log("[FlappyBirdSetup] Starting automated build of Flappy Bird...");
            EnsureDirectories();
            ConfigureTagsAndLayers();
            ConfigurePhysics2DMatrix();
            CreateSprites();
            var panelSettings = CreatePanelSettings();
            var pipePrefab = BuildPipePrefab();
            var birdPrefab = BuildBirdPrefab();
            BuildScene(panelSettings, birdPrefab, pipePrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FlappyBirdSetup] Automated build completed successfully!");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = {
                "Assets/Prefabs",
                "Assets/Sprites",
                "Assets/UI/Settings",
                "Assets/UI/Documents",
                "Assets/UI/Stylesheets",
                "Assets/Scenes"
            };

            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
        }

        public static void ConfigureTagsAndLayers()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            // Add Tags
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            string[] tagsToAdd = { "Player", "Obstacle", "Ground", "ScoreZone" };
            foreach (var tag in tagsToAdd)
            {
                bool exists = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                }
            }

            // Set Layers
            // Layer 8: Player, Layer 9: Obstacle, Layer 10: Ground
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            SetLayerName(layersProp, 8, "Player");
            SetLayerName(layersProp, 9, "Obstacle");
            SetLayerName(layersProp, 10, "Ground");

            tagManager.ApplyModifiedProperties();
            Debug.Log("[FlappyBirdSetup] Tags and Layers configured.");
        }

        private static void SetLayerName(SerializedProperty layersProp, int index, string name)
        {
            if (index < layersProp.arraySize)
            {
                SerializedProperty elem = layersProp.GetArrayElementAtIndex(index);
                elem.stringValue = name;
            }
        }

        public static void ConfigurePhysics2DMatrix()
        {
            int playerLayer = 8;
            int obstacleLayer = 9;
            int groundLayer = 10;

            // Player collides with Obstacle and Ground
            Physics2D.IgnoreLayerCollision(playerLayer, obstacleLayer, false);
            Physics2D.IgnoreLayerCollision(playerLayer, groundLayer, false);

            // Obstacle ignores Obstacle and Ground
            Physics2D.IgnoreLayerCollision(obstacleLayer, obstacleLayer, true);
            Physics2D.IgnoreLayerCollision(obstacleLayer, groundLayer, true);

            // Ground ignores Ground
            Physics2D.IgnoreLayerCollision(groundLayer, groundLayer, true);

            Debug.Log("[FlappyBirdSetup] Physics2D Collision Matrix configured.");
        }

        public static void CreateSprites()
        {
            // Bird texture: 64x64 yellow circle
            string birdPath = "Assets/Sprites/bird.png";
            if (!File.Exists(birdPath))
            {
                int size = 64;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color yellow = new Color(1.0f, 0.843f, 0.0f, 1.0f);
                Color dark = new Color(0.2f, 0.2f, 0.2f, 1.0f);
                Color clear = new Color(0, 0, 0, 0);

                Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
                float radius = size * 0.45f;
                float innerRadius = radius - 2f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist <= innerRadius)
                            tex.SetPixel(x, y, yellow);
                        else if (dist <= radius)
                            tex.SetPixel(x, y, dark);
                        else
                            tex.SetPixel(x, y, clear);
                    }
                }
                tex.Apply();
                File.WriteAllBytes(birdPath, tex.EncodeToPNG());
            }

            // Pipe texture: 64x64 green rect with border
            string pipePath = "Assets/Sprites/pipe.png";
            if (!File.Exists(pipePath))
            {
                int w = 64, h = 64;
                Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                Color green = new Color(0.18f, 0.8f, 0.44f, 1.0f);
                Color border = new Color(0.12f, 0.53f, 0.29f, 1.0f);

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (x < 3 || x >= w - 3 || y < 2 || y >= h - 2)
                            tex.SetPixel(x, y, border);
                        else
                            tex.SetPixel(x, y, green);
                    }
                }
                tex.Apply();
                File.WriteAllBytes(pipePath, tex.EncodeToPNG());
            }

            // Ground texture: 64x64 brown/orange rect
            string groundPath = "Assets/Sprites/ground.png";
            if (!File.Exists(groundPath))
            {
                int w = 64, h = 64;
                Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                Color groundTop = new Color(0.85f, 0.45f, 0.15f, 1.0f);
                Color groundBody = new Color(0.72f, 0.35f, 0.1f, 1.0f);

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (y >= h - 8)
                            tex.SetPixel(x, y, groundTop);
                        else
                            tex.SetPixel(x, y, groundBody);
                    }
                }
                tex.Apply();
                File.WriteAllBytes(groundPath, tex.EncodeToPNG());
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            SetTextureAsSprite(birdPath);
            SetTextureAsSprite(pipePath);
            SetTextureAsSprite(groundPath);
        }

        private static void SetTextureAsSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 64;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        public static PanelSettings CreatePanelSettings()
        {
            string path1 = "Assets/UI/FlappyBirdPanelSettings.asset";
            string path2 = "Assets/UI/Settings/FlappyBirdPanelSettings.asset";

            PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path1);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(1080, 1920);
                panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panelSettings.match = 0.5f;

                AssetDatabase.CreateAsset(panelSettings, path1);
                Debug.Log($"[FlappyBirdSetup] Created PanelSettings at {path1}");
            }

            if (!File.Exists(path2))
            {
                AssetDatabase.CopyAsset(path1, path2);
            }

            return panelSettings;
        }

        public static GameObject BuildBirdPrefab()
        {
            string prefabPath = "Assets/Prefabs/Bird.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null)
            {
                return existing;
            }

            GameObject birdGO = new GameObject("Bird");
            birdGO.tag = "Player";
            birdGO.layer = 8; // Player
            birdGO.transform.position = new Vector3(-1.5f, 0f, 0f);

            // Rigidbody2D
            Rigidbody2D rb = birdGO.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 2.5f;

            // CircleCollider2D
            CircleCollider2D col = birdGO.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
            col.isTrigger = false;

            // BirdInputReader
            BirdInputReader inputReader = birdGO.AddComponent<BirdInputReader>();
            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/Input/PlayerInputActions.inputactions");
            if (inputAsset == null)
            {
                inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");
            }
            if (inputAsset != null)
            {
                SerializedObject so = new SerializedObject(inputReader);
                so.FindProperty("_inputActions").objectReferenceValue = inputAsset;
                so.ApplyModifiedProperties();
            }

            // BirdPhysics
            BirdPhysics physics = birdGO.AddComponent<BirdPhysics>();

            // BirdCollisionHandler
            BirdCollisionHandler collisionHandler = birdGO.AddComponent<BirdCollisionHandler>();
            SerializedObject soCol = new SerializedObject(collisionHandler);
            soCol.FindProperty("_obstacleLayerMask").intValue = 1 << 9; // Obstacle
            soCol.FindProperty("_groundLayerMask").intValue = 1 << 10; // Ground
            soCol.ApplyModifiedProperties();

            // Child BirdVisual
            GameObject visualGO = new GameObject("BirdVisual");
            visualGO.transform.SetParent(birdGO.transform, false);
            SpriteRenderer sr = visualGO.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/bird.png");
            sr.color = new Color(1.0f, 0.843f, 0.0f, 1.0f); // Canary Yellow

            BirdView view = visualGO.AddComponent<BirdView>();
            SerializedObject soView = new SerializedObject(view);
            soView.FindProperty("_visualTransform").objectReferenceValue = visualGO.transform;
            soView.FindProperty("_birdPhysics").objectReferenceValue = physics;
            soView.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(birdGO, prefabPath);
            Object.DestroyImmediate(birdGO);
            Debug.Log($"[FlappyBirdSetup] Created Bird prefab at {prefabPath}");
            return prefab;
        }

        public static GameObject BuildPipePrefab()
        {
            string prefabPath = "Assets/Prefabs/PipePair.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null)
            {
                return existing;
            }

            GameObject pipePairGO = new GameObject("PipePair");

            // Mover & Pair components on root
            PipeMover mover = pipePairGO.AddComponent<PipeMover>();
            PipePair pipePair = pipePairGO.AddComponent<PipePair>();

            SerializedObject soMover = new SerializedObject(mover);
            soMover.FindProperty("_scrollSpeed").floatValue = 3.5f;
            soMover.FindProperty("_despawnX").floatValue = -10.0f;
            soMover.FindProperty("_parentPipe").objectReferenceValue = pipePair;
            soMover.ApplyModifiedProperties();

            Sprite pipeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/pipe.png");

            // Top Pipe (opens downward, extends upward)
            GameObject topPipe = new GameObject("TopPipe");
            topPipe.tag = "Obstacle";
            topPipe.layer = 9; // Obstacle
            topPipe.transform.SetParent(pipePairGO.transform, false);

            GameObject topVisual = new GameObject("Visual");
            topVisual.transform.SetParent(topPipe.transform, false);
            topVisual.transform.localPosition = new Vector3(0f, 5.0f, 0f);
            topVisual.transform.localScale = new Vector3(1.3f, 10.0f, 1.0f);
            SpriteRenderer topSr = topVisual.AddComponent<SpriteRenderer>();
            topSr.sprite = pipeSprite;
            topSr.color = new Color(0.18f, 0.8f, 0.44f, 1.0f);

            BoxCollider2D topCol = topPipe.AddComponent<BoxCollider2D>();
            topCol.size = new Vector2(1.3f, 10.0f);
            topCol.offset = new Vector2(0f, 5.0f);

            // Bottom Pipe (opens upward, extends downward)
            GameObject bottomPipe = new GameObject("BottomPipe");
            bottomPipe.tag = "Obstacle";
            bottomPipe.layer = 9; // Obstacle
            bottomPipe.transform.SetParent(pipePairGO.transform, false);

            GameObject bottomVisual = new GameObject("Visual");
            bottomVisual.transform.SetParent(bottomPipe.transform, false);
            bottomVisual.transform.localPosition = new Vector3(0f, -5.0f, 0f);
            bottomVisual.transform.localScale = new Vector3(1.3f, 10.0f, 1.0f);
            SpriteRenderer bottomSr = bottomVisual.AddComponent<SpriteRenderer>();
            bottomSr.sprite = pipeSprite;
            bottomSr.color = new Color(0.18f, 0.8f, 0.44f, 1.0f);

            BoxCollider2D bottomCol = bottomPipe.AddComponent<BoxCollider2D>();
            bottomCol.size = new Vector2(1.3f, 10.0f);
            bottomCol.offset = new Vector2(0f, -5.0f);

            // Score Zone Trigger
            GameObject scoreZone = new GameObject("ScoreZone");
            scoreZone.tag = "ScoreZone";
            scoreZone.transform.SetParent(pipePairGO.transform, false);
            BoxCollider2D triggerCol = scoreZone.AddComponent<BoxCollider2D>();
            triggerCol.isTrigger = true;
            triggerCol.size = new Vector2(0.5f, 3.2f);
            triggerCol.offset = Vector2.zero;

            ScoreTrigger scoreTrigger = scoreZone.AddComponent<ScoreTrigger>();
            SerializedObject soTrigger = new SerializedObject(scoreTrigger);
            soTrigger.FindProperty("_playerLayerMask").intValue = 1 << 8; // Player
            soTrigger.FindProperty("_playerTag").stringValue = "Player";
            soTrigger.FindProperty("_triggerCollider").objectReferenceValue = triggerCol;
            soTrigger.ApplyModifiedProperties();

            // Wire PipePair serialized fields
            SerializedObject soPair = new SerializedObject(pipePair);
            soPair.FindProperty("_topPipe").objectReferenceValue = topPipe.transform;
            soPair.FindProperty("_bottomPipe").objectReferenceValue = bottomPipe.transform;
            soPair.FindProperty("_scoreTrigger").objectReferenceValue = scoreTrigger;
            soPair.FindProperty("_mover").objectReferenceValue = mover;
            soPair.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(pipePairGO, prefabPath);
            Object.DestroyImmediate(pipePairGO);
            Debug.Log($"[FlappyBirdSetup] Created PipePair prefab at {prefabPath}");
            return prefab;
        }

        public static void BuildScene(PanelSettings panelSettings, GameObject birdPrefab, GameObject pipePrefab)
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            GameObject cameraGO = new GameObject("Main Camera");
            cameraGO.tag = "MainCamera";
            Camera cam = cameraGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.0f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.44f, 0.77f, 0.81f, 1.0f); // #70C5CE sky blue
            cameraGO.transform.position = new Vector3(0f, 0f, -10f);
            cameraGO.AddComponent<AudioListener>();

            // 2. Environment
            GameObject envGO = new GameObject("Environment");
            Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/ground.png");

            // Ground
            GameObject groundGO = new GameObject("Ground");
            groundGO.tag = "Ground";
            groundGO.layer = 10; // Ground
            groundGO.transform.SetParent(envGO.transform, false);
            groundGO.transform.position = new Vector3(0f, -4.5f, 0f);

            GameObject groundVisual = new GameObject("Visual");
            groundVisual.transform.SetParent(groundGO.transform, false);
            groundVisual.transform.localScale = new Vector3(20f, 2.0f, 1.0f);
            SpriteRenderer groundSr = groundVisual.AddComponent<SpriteRenderer>();
            groundSr.sprite = groundSprite;

            BoxCollider2D groundCol = groundGO.AddComponent<BoxCollider2D>();
            groundCol.size = new Vector2(20f, 2.0f);

            // Ceiling
            GameObject ceilingGO = new GameObject("Ceiling");
            ceilingGO.tag = "Ground";
            ceilingGO.layer = 10; // Ground
            ceilingGO.transform.SetParent(envGO.transform, false);
            ceilingGO.transform.position = new Vector3(0f, 5.5f, 0f);

            BoxCollider2D ceilingCol = ceilingGO.AddComponent<BoxCollider2D>();
            ceilingCol.size = new Vector2(20f, 1.0f);

            // 3. Player Bird Instance
            GameObject birdInstance = PrefabUtility.InstantiatePrefab(birdPrefab) as GameObject;
            birdInstance.name = "Bird";
            birdInstance.transform.position = new Vector3(-1.5f, 0f, 0f);
            BirdPhysics birdPhysics = birdInstance.GetComponent<BirdPhysics>();
            BirdCollisionHandler birdCollision = birdInstance.GetComponent<BirdCollisionHandler>();
            BirdInputReader birdInput = birdInstance.GetComponent<BirdInputReader>();

            // 4. Managers
            GameObject managersGO = new GameObject("Managers");
            GameManager gameManager = managersGO.AddComponent<GameManager>();
            ScoreManager scoreManager = managersGO.AddComponent<ScoreManager>();
            PipePool pipePool = managersGO.AddComponent<PipePool>();
            PipeSpawner pipeSpawner = managersGO.AddComponent<PipeSpawner>();

            // Wire PipePool
            SerializedObject soPool = new SerializedObject(pipePool);
            soPool.FindProperty("_poolSize").intValue = 8;
            soPool.FindProperty("_prefab").objectReferenceValue = pipePrefab.GetComponent<PipePair>();
            soPool.ApplyModifiedProperties();

            // Wire PipeSpawner
            SerializedObject soSpawner = new SerializedObject(pipeSpawner);
            soSpawner.FindProperty("_pool").objectReferenceValue = pipePool;
            soSpawner.FindProperty("_spawnInterval").floatValue = 1.6f;
            soSpawner.FindProperty("_spawnX").floatValue = 10.0f;
            soSpawner.FindProperty("_minY").floatValue = -1.5f;
            soSpawner.FindProperty("_maxY").floatValue = 2.0f;
            soSpawner.FindProperty("_gapSize").floatValue = 3.2f;
            soSpawner.ApplyModifiedProperties();

            // Wire GameManager
            SerializedObject soGM = new SerializedObject(gameManager);
            soGM.FindProperty("_birdPhysics").objectReferenceValue = birdPhysics;
            soGM.FindProperty("_collisionHandler").objectReferenceValue = birdCollision;
            soGM.FindProperty("_inputReader").objectReferenceValue = birdInput;
            soGM.FindProperty("_spawner").objectReferenceValue = pipeSpawner;
            soGM.FindProperty("_pool").objectReferenceValue = pipePool;
            soGM.FindProperty("_scoreManager").objectReferenceValue = scoreManager;
            soGM.ApplyModifiedProperties();

            // 5. UI Setup
            GameObject uiGO = new GameObject("UI");
            UIDocument uiDoc = uiGO.AddComponent<UIDocument>();
            VisualTreeAsset vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Documents/GameHUD.uxml");
            if (vta == null)
            {
                vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/FlappyBirdUI.uxml");
            }
            uiDoc.visualTreeAsset = vta;
            uiDoc.panelSettings = panelSettings;

            SerializedObject soDoc = new SerializedObject(uiDoc);
            SerializedProperty psProp = soDoc.FindProperty("m_PanelSettings");
            if (psProp != null)
            {
                psProp.objectReferenceValue = panelSettings;
            }
            SerializedProperty vtaProp = soDoc.FindProperty("m_VisualTreeAsset") ?? soDoc.FindProperty("sourceAsset");
            if (vtaProp != null)
            {
                vtaProp.objectReferenceValue = vta;
            }
            soDoc.ApplyModifiedProperties();
            EditorUtility.SetDirty(uiDoc);
            EditorUtility.SetDirty(uiGO);

            GameUIPresenter uiPresenter = uiGO.AddComponent<GameUIPresenter>();
            SerializedObject soUIP = new SerializedObject(uiPresenter);
            soUIP.FindProperty("_uiDocument").objectReferenceValue = uiDoc;
            soUIP.FindProperty("_gameManager").objectReferenceValue = gameManager;
            soUIP.FindProperty("_scoreManager").objectReferenceValue = scoreManager;
            soUIP.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[FlappyBirdSetup] Saved assembled scene to {scenePath}");
        }
    }
}
#endif
