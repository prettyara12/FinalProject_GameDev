#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.Level2
{
    [InitializeOnLoad]
    public static class Level2AirportBuilder
    {
        public const string DemoScenePath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleAirport/_Demo/Demo.unity";
        public const string TargetScenePath = "Assets/Scenes/Level 2 saya/Level 2 saya.unity";
        public const string QuizScenePath = "Assets/Develop 3D Mobile Games in Unity/Assets/QuizAdventure/Scenes/Level 2 saya/Level 2 saya.unity";

        static Level2AirportBuilder()
        {
            // Tidak ada listener otomatis agar karakter tidak dibuat kembali secara otomatis
        }

        private static bool IsLevel2Scene(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return path.Contains("Level 2 saya") || path.Contains("Level 2");
        }

        [MenuItem("Tools/✈️ Pulihkan Ukuran Asli Level 2 (Skala Normal 1.0)", priority = 0)]
        public static void MenuRestoreOriginalScales()
        {
            RestoreOriginalScalesInLevel2();
        }

        public static void RestoreOriginalScalesInLevel2()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (!IsLevel2Scene(activeScene.path))
            {
                if (System.IO.File.Exists(QuizScenePath))
                {
                    activeScene = EditorSceneManager.OpenScene(QuizScenePath, OpenSceneMode.Single);
                }
                else if (System.IO.File.Exists(TargetScenePath))
                {
                    activeScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                }
            }

            if (activeScene.IsValid())
            {
                Transform[] allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                int count = 0;
                foreach (var t in allTransforms)
                {
                    if (t == null) continue;
                    t.localScale = Vector3.one;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                    EditorUtility.SetDirty(t);
                    count++;
                }

                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);

                if (activeScene.path == QuizScenePath && System.IO.File.Exists(TargetScenePath))
                {
                    System.IO.File.Copy(QuizScenePath, TargetScenePath, true);
                    AssetDatabase.ImportAsset(TargetScenePath, ImportAssetOptions.ForceUpdate);
                }
                else if (activeScene.path == TargetScenePath && System.IO.File.Exists(QuizScenePath))
                {
                    System.IO.File.Copy(TargetScenePath, QuizScenePath, true);
                    AssetDatabase.ImportAsset(QuizScenePath, ImportAssetOptions.ForceUpdate);
                }

                Debug.Log($"<color=green>[Level2AirportBuilder] SUKSES:</color> Seluruh {count} objek dalam scene Level 2 telah dipulihkan ke ukuran aslinya (Skala 1.0)!");
            }
        }

        [MenuItem("Tools/✈️ Hapus Semua Karakter di Level 2 Bandara", priority = 1)]
        public static void MenuRemoveAllCharacters()
        {
            RemoveAllCharactersFromLevel2();
        }

        public static void RemoveAllCharactersFromLevel2()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (!IsLevel2Scene(activeScene.path))
            {
                if (System.IO.File.Exists(QuizScenePath))
                {
                    activeScene = EditorSceneManager.OpenScene(QuizScenePath, OpenSceneMode.Single);
                }
                else if (System.IO.File.Exists(TargetScenePath))
                {
                    activeScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                }
            }

            if (activeScene.IsValid())
            {
                PurgeAllCharactersInScene(activeScene);
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);

                if (activeScene.path == QuizScenePath && System.IO.File.Exists(TargetScenePath))
                {
                    System.IO.File.Copy(QuizScenePath, TargetScenePath, true);
                    AssetDatabase.ImportAsset(TargetScenePath, ImportAssetOptions.ForceUpdate);
                }
                else if (activeScene.path == TargetScenePath && System.IO.File.Exists(QuizScenePath))
                {
                    System.IO.File.Copy(TargetScenePath, QuizScenePath, true);
                    AssetDatabase.ImportAsset(QuizScenePath, ImportAssetOptions.ForceUpdate);
                }

                Debug.Log("<color=green>[Level2AirportBuilder] SUKSES:</color> Seluruh karakter (Player & NPC) telah dihapus bersih dari scene Level 2!");
            }
        }

        public static void PurgeAllCharactersInScene(Scene scene)
        {
            GameObject[] allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            int count = 0;
            foreach (var g in allGo)
            {
                if (g == null) continue;
                string lower = g.name.ToLower();
                if (lower.StartsWith("npc_") || lower.Contains("pilot") || lower.Contains("pramugari") ||
                    lower.Contains("security") || lower.Contains("petugas") || lower.Contains("wisatawan") ||
                    lower.Contains("trucker") || lower.Contains("rian") || lower.Contains("people") ||
                    lower.Contains("citizen") || lower.Contains("tourist") || lower.Contains("worker") ||
                    lower.Contains("character") || g.CompareTag("Player"))
                {
                    Object.DestroyImmediate(g);
                    count++;
                }
            }

            // Atur kamera ke sudut pandang overview bandara
            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                ThirdPersonCamera tpc = cam.GetComponent<ThirdPersonCamera>();
                if (tpc != null)
                {
                    tpc.target = null;
                    tpc.viewMode = ThirdPersonCamera.CameraViewMode.CityOverview;
                    tpc.cityOverviewPosition = new Vector3(20f, 18f, 45f);
                    tpc.cityOverviewRotationEuler = new Vector3(25f, 45f, 0f);
                    EditorUtility.SetDirty(tpc);
                }
                cam.transform.position = new Vector3(20f, 18f, 45f);
                cam.transform.rotation = Quaternion.Euler(25f, 45f, 0f);
                EditorUtility.SetDirty(cam.gameObject);
            }

            Debug.Log($"[Level2AirportBuilder] Berhasil menghapus {count} objek karakter dari scene.");
        }

        [MenuItem("Tools/✈️ Setup Rian (Karakter Topi Putih-Biru) & Kamera Mengikuti dari Belakang", priority = 0)]
        public static void MenuSetupPlayerOnly()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (!IsLevel2Scene(activeScene.path))
            {
                if (System.IO.File.Exists(QuizScenePath))
                {
                    activeScene = EditorSceneManager.OpenScene(QuizScenePath, OpenSceneMode.Single);
                }
                else if (System.IO.File.Exists(TargetScenePath))
                {
                    activeScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                }
            }

            if (activeScene.IsValid())
            {
                SetupAirportScenePlayer(activeScene);
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);

                if (activeScene.path == QuizScenePath && System.IO.File.Exists(TargetScenePath))
                {
                    System.IO.File.Copy(QuizScenePath, TargetScenePath, true);
                    AssetDatabase.ImportAsset(TargetScenePath, ImportAssetOptions.ForceUpdate);
                }
                else if (activeScene.path == TargetScenePath && System.IO.File.Exists(QuizScenePath))
                {
                    System.IO.File.Copy(TargetScenePath, QuizScenePath, true);
                    AssetDatabase.ImportAsset(QuizScenePath, ImportAssetOptions.ForceUpdate);
                }

                Debug.Log("<color=green>[Level2AirportBuilder] SUKSES:</color> Pemeran Utama Rian (Model Kaos Putih & Topi Biru) dan Kamera Belakang berhasil dikonfigurasi!");
            }
        }

        [MenuItem("Tools/✈️ Pulihkan & Lengkapi Bandara (Pesawat + Rian Saja)", priority = 1)]
        public static void MenuRestoreFullAirport()
        {
            DeployFullAirport();
        }

        [MenuItem("Tools/✈️ Ubah Lantai Bandara Menjadi Aspal (Hitam/Abu Gelap)", priority = 2)]
        public static void MenuApplyAsphaltFloor()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (!IsLevel2Scene(activeScene.path))
            {
                activeScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            }
            if (activeScene.IsValid())
            {
                ApplyAsphaltFloorToScene(activeScene);
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log("<color=green>[Level2AirportBuilder] SUKSES:</color> Seluruh lantai bandara berhasil diubah menjadi Aspal Gelap!");
            }
        }

        public static void DeployFullAirport()
        {
            Debug.Log("[Level2AirportBuilder] Menyalin dan menyusun bandara lengkap dengan armada pesawat & Rian...");

            // 1. Pastikan folder tujuan ada
            if (!AssetDatabase.IsValidFolder("Assets/Scenes/Level 2 saya"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                {
                    AssetDatabase.CreateFolder("Assets", "Scenes");
                }
                AssetDatabase.CreateFolder("Assets/Scenes", "Level 2 saya");
            }

            string quizFolder = "Assets/Develop 3D Mobile Games in Unity/Assets/QuizAdventure/Scenes/Level 2 saya";
            if (!System.IO.Directory.Exists(quizFolder))
            {
                System.IO.Directory.CreateDirectory(quizFolder);
            }

            // 2. Salin scene bandara master lengkap jika perlu
            if (System.IO.File.Exists(DemoScenePath))
            {
                System.IO.File.Copy(DemoScenePath, TargetScenePath, true);
                System.IO.File.Copy(DemoScenePath, QuizScenePath, true);
                AssetDatabase.Refresh();
            }

            // 3. Konfigurasi Player Rian dan Aspal pada scene
            Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (scene.IsValid())
            {
                SetupAirportScenePlayer(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Scene quizScene = EditorSceneManager.OpenScene(QuizScenePath, OpenSceneMode.Single);
            if (quizScene.IsValid())
            {
                SetupAirportScenePlayer(quizScene);
                EditorSceneManager.MarkSceneDirty(quizScene);
                EditorSceneManager.SaveScene(quizScene);
            }

            RegisterSceneInBuildSettings(TargetScenePath);
            RegisterSceneInBuildSettings(QuizScenePath);
        }

        public static void SetupAirportScenePlayer(Scene scene)
        {
            // 1. Bersihkan seluruh NPC tambahan dan model karakter lama
            CleanupAllNPCs();

            // 2. Solid Ground Collider
            GameObject groundCol = GameObject.Find("Airport_Ground_Solid_Collider");
            if (groundCol == null)
            {
                groundCol = new GameObject("Airport_Ground_Solid_Collider");
                groundCol.transform.position = new Vector3(0f, -0.05f, 0f);
                BoxCollider col = groundCol.AddComponent<BoxCollider>();
                col.size = new Vector3(500f, 0.1f, 500f);
            }

            // 3. Setup Pemeran Utama Rian (Model Kaos Putih & Topi Biru - SimplePeople_Trucker_Black)
            GameObject pref = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Trucker_Black.prefab");
            GameObject playerGo = null;
            if (pref != null)
            {
                playerGo = (GameObject)PrefabUtility.InstantiatePrefab(pref);
            }
            if (playerGo == null) playerGo = new GameObject("Rian (Kurir)");

            playerGo.name = "Rian (Kurir)";
            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(35.0f, 0.05f, 147.0f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            playerGo.transform.localScale = Vector3.one;

            // Pasang CapsuleCollider
            CapsuleCollider pCol = playerGo.GetComponent<CapsuleCollider>();
            if (pCol == null) pCol = playerGo.AddComponent<CapsuleCollider>();
            pCol.center = new Vector3(0f, 0.9f, 0f);
            pCol.height = 1.8f;
            pCol.radius = 0.35f;

            // Pasang Rigidbody Kinematic
            Rigidbody rb = playerGo.GetComponent<Rigidbody>();
            if (rb == null) rb = playerGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Pasang Animator dengan SimpleCitizens.controller agar animasi idle/jalan berfungsi
            RuntimeAnimatorController citizenCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleCitizens/Models/SimpleCitizens.controller");
            Animator anim = playerGo.GetComponent<Animator>();
            if (anim == null) anim = playerGo.AddComponent<Animator>();
            anim.applyRootMotion = false;
            anim.enabled = true;
            if (citizenCtrl != null) anim.runtimeAnimatorController = citizenCtrl;

            // Pasang PlayerController untuk kendali WASD
            PlayerController pc = playerGo.GetComponent<PlayerController>();
            if (pc == null) pc = playerGo.AddComponent<PlayerController>();
            pc.walkSpeed = 4.5f;
            pc.sprintSpeed = 8.5f;
            pc.rotationSpeed = 12f;
            pc.jumpHeight = 1.2f;

            // Material Trucker Black asli (Topi biru-putih, kaos singlet putih & suspender)
            Material truckerMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_Trucker_Black.mat");
            if (truckerMat != null)
            {
                var rends = playerGo.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends) if (r != null) r.sharedMaterial = truckerMat;
            }

            EditorUtility.SetDirty(playerGo);

            // 4. Setup Main Camera Mengikuti Rian dari belakang (Zoom out diperkecil)
            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                cam.tag = "MainCamera";
                Vector3 camOffset = -playerGo.transform.forward * 6.5f + Vector3.up * 2.0f;
                cam.transform.position = playerGo.transform.position + camOffset;
                cam.transform.rotation = Quaternion.Euler(14f, 0f, 0f);

                ThirdPersonCamera tpc = cam.GetComponent<ThirdPersonCamera>();
                if (tpc == null) tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();
                tpc.target = playerGo.transform;
                tpc.viewMode = ThirdPersonCamera.CameraViewMode.ThirdPersonFollow;
                tpc.distance = 6.5f;
                tpc.height = 2.0f;
                tpc.targetHeight = 1.5f;
                tpc.followSpeed = 12f;
                tpc.enableMouseLook = true;
                tpc.followRearAngle = true;

                pc.playerCamera = cam;

                EditorUtility.SetDirty(cam.gameObject);
                EditorUtility.SetDirty(tpc);
            }

            // 5. Pastikan material lantai apron/tarmac adalah aspal gelap
            ApplyAsphaltFloorToScene(scene);
        }

        private static void CleanupAllNPCs()
        {
            GameObject[] allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var g in allGo)
            {
                if (g == null) continue;
                string lower = g.name.ToLower();
                // Hapus seluruh NPC, model trucker kaos putih (T-pose), serta objek Rian lama
                if (lower.StartsWith("npc_") || lower.Contains("pilot") || lower.Contains("pramugari") ||
                    lower.Contains("security") || lower.Contains("petugas") || lower.Contains("wisatawan") ||
                    lower.Contains("trucker_white") || lower.Contains("trucker") || lower.Contains("rian"))
                {
                    Object.DestroyImmediate(g);
                }
            }
        }

        public static void ApplyAsphaltFloorToScene(Scene scene)
        {
            Material asphaltMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/QuizAdventure/Materials/Airport_Asphalt.mat");
            if (asphaltMat == null) return;

            int count = 0;
            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var r in renderers)
                {
                    if (r != null && r.gameObject.name.StartsWith("Ground_Concrete"))
                    {
                        if (r.sharedMaterial != asphaltMat)
                        {
                            r.sharedMaterial = asphaltMat;
                            EditorUtility.SetDirty(r);
                            count++;
                        }
                    }
                }
            }
            if (count > 0)
            {
                Debug.Log($"[Level2AirportBuilder] Berhasil memperbarui {count} ubin Ground_Concrete ke material aspal gelap (Airport_Asphalt).");
            }
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool exists = false;
            foreach (var s in scenes)
            {
                if (s.path == scenePath)
                {
                    exists = true;
                    s.enabled = true;
                    break;
                }
            }

            if (!exists)
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }
}
#endif
