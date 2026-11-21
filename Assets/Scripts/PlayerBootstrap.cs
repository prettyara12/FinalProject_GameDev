using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Project Setup Bootstrap:
/// 1. Kota Kecil (Lingkungan manual tidak disentuh).
/// 2. Player yang bisa digerakkan dengan keyboard (WASD, Shift lari, Spasi lompat, C duduk).
/// 3. Minimal 2 NPC di dalam kota yang berjalan mandiri dengan anti-tabrakan.
/// </summary>
public static class PlayerBootstrap
{
    private const string AnimatorControllerPath = 
        "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Animations/SimpleCharacter_5.0.controller";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitializeSceneLifecycleListener()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoadedRuntime;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedRuntime;
    }

    private static void OnSceneLoadedRuntime(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name.Contains("Level 2") || scene.name == "Level 2 saya")
        {
            SetupAirportLevel2Saya();
        }
        else if (scene.name == "Module 2" || scene.name == "Module 2_Stage2")
        {
            SetupGameProject();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AutoSetupOnPlay()
    {
        // PlayerBootstrap dikhususkan untuk scene kota (Module 2 dan Module 2_Stage2) serta Level 2 Bandara
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.name != "Module 2" && activeScene.name != "Module 2_Stage2" && !activeScene.name.Contains("Level 2"))
        {
            return;
        }

        SetupGameProject();
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    public static void SyncBuildSettingsOnLoad()
    {
        EnsureAllScenesInBuild();

        EditorApplication.delayCall += () =>
        {
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (activeScene.name.Contains("Level 2") || activeScene.name == "Level 2 saya")
            {
                SetupAirportLevel2Saya();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
                Debug.Log("[PlayerBootstrap] Level 2 Bandara berhasil dikonfigurasi & disimpan otomatis ke file scene!");
            }
            else
            {
                string airportPath = "Assets/Scenes/Level 2 saya/Level 2 saya.unity";
                if (System.IO.File.Exists(airportPath))
                {
                    string sceneContent = System.IO.File.ReadAllText(airportPath);
                    if (!sceneContent.Contains("AirportFlyingAirplane"))
                    {
                        var airportScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(airportPath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
                        SetupAirportLevel2Saya();
                        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(airportScene);
                        UnityEditor.SceneManagement.EditorSceneManager.CloseScene(airportScene, true);
                        Debug.Log("[PlayerBootstrap] Level 2 Bandara berhasil dibake secara additive & disimpan!");
                    }
                }
            }
        };

        UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += (scene, mode) =>
        {
            if (scene.name.Contains("Level 2") || scene.name == "Level 2 saya")
            {
                SetupAirportLevel2Saya();
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                Debug.Log("[PlayerBootstrap] Level 2 Bandara berhasil dikonfigurasi saat scene dibuka & disimpan!");
            }
        };
    }

    public static void EnsureAllScenesInBuild()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        string[] subScenePaths = new string[]
        {
            "Assets/Scenes/Level 2 saya/Level 2 saya.unity",
            "Assets/Scenes/SubScenes/SubScene_0_0.unity",
            "Assets/Scenes/SubScenes/SubScene_0_1.unity",
            "Assets/Scenes/SubScenes/SubScene_0_2.unity",
            "Assets/Scenes/SubScenes/SubScene_1_0.unity",
            "Assets/Scenes/SubScenes/SubScene_1_1.unity",
            "Assets/Scenes/SubScenes/SubScene_1_2.unity",
            "Assets/Scenes/SubScenes/SubScene_2_0.unity",
            "Assets/Scenes/SubScenes/SubScene_2_1.unity",
            "Assets/Scenes/SubScenes/SubScene_2_2.unity",
            "Assets/Scenes/Module8_Cinematics.unity",
        };

        bool modified = false;

        // Bersihkan scene usang / duplikat Level 2 QuizAdventure jika ada
        int obsoleteIdx = scenes.FindIndex(s => s.path.Contains("QuizAdventure") && s.path.Contains("Level 2 saya"));
        if (obsoleteIdx >= 0)
        {
            scenes.RemoveAt(obsoleteIdx);
            modified = true;
        }

        foreach (var path in subScenePaths)
        {
            GUID guid = AssetDatabase.GUIDFromAssetPath(path);
            int idx = scenes.FindIndex(s => s.path == path);
            if (idx < 0)
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                modified = true;
            }
            else if (scenes[idx].guid != guid || !scenes[idx].enabled)
            {
                scenes[idx] = new EditorBuildSettingsScene(path, true);
                modified = true;
            }
        }

        if (modified)
        {
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[PlayerBootstrap] EditorBuildSettings.scenes berhasil disinkronkan untuk Level 2 Bandara & SubScenes!");
        }
    }

    private static void MarkSceneDirtySafe()
    {
        if (!Application.isPlaying)
        {
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            }
        }
    }

    [MenuItem("Tools/🏆 Debug: Trigger Win Level 1 (Kurir Terpercaya)", false, -2)]
    public static void MenuDebugTriggerWinLevel1()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Fitur Khusus Play Mode",
                "Silakan klik tombol Play (▶) di Unity Editor terlebih dahulu!\n\n" +
                "Saat game sedang berjalan, Anda bisa menjalankan menu ini ATAU menekan tombol [F9] pada keyboard untuk langsung memicu kemenangan Level 1 dan memunculkan tombol 'Lanjut ke Level 2 (Bandara)'.",
                "OK");
            return;
        }

        if (DeliveryGameManager.Instance != null)
        {
            DeliveryGameManager.Instance.DebugTriggerInstantWin();
        }
        else
        {
            Debug.LogWarning("[DEBUG] DeliveryGameManager tidak ditemukan di scene saat ini.");
        }
    }

    [MenuItem("Tools/✈️ Setup & Simpan Level 2 Bandara (Bake Colliders & Pesawat Terbang)", false, -1)]
    public static void MenuSetupAirportLevel2()
    {
        var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("Level 2") && activeScene.name != "Level 2 saya")
        {
            string airportScenePath = "Assets/Scenes/Level 2 saya/Level 2 saya.unity";
            if (System.IO.File.Exists(airportScenePath))
            {
                activeScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(airportScenePath);
            }
        }

        SetupAirportLevel2Saya();
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
        EditorUtility.DisplayDialog("Setup Level 2 Bandara Sukses!",
            "Level 2 Bandara telah berhasil dikonfigurasi dan disimpan permanen ke file scene:\n\n" +
            "✓ Pesawat terbang (Plane01) bergerak terbang maju dan memutari langit bandara.\n" +
            "✓ Seluruh kendaraan, tangga, gedung, hanggar, dan pagar memiliki BoxCollider solid.\n" +
            "✓ Seluruh karakter & NPC memiliki CapsuleCollider solid anti-tembus.\n" +
            "✓ Langit biru dan sistem misi siap dimainkan!",
            "OK");
    }

    [MenuItem("Tools/⭐ Setup Game Kurir Terakhir (Final Project)", false, 0)]
    public static void MenuSetupKurirTerakhir()
    {
        SetupGameProject();
        MarkSceneDirtySafe();

        EditorUtility.DisplayDialog("Game Kurir Terakhir Siap Dimainkan!",
            "Game 'Kurir Terakhir' berhasil dikonfigurasi lengkap:\n\n" +
            "1. RIAN (PLAYER): Memulai di depan Mall (WASD, Shift Sprint, Spasi Lompat, [E] Interaksi, [M] Peta Kota).\n" +
            "2. 6 NPC PENERIMA / PETUNJUK: Pak Beni, Clara, Pak Joko, Dimas, Petugas Parkir, dan Naya.\n" +
            "3. COUNTDOWN TIMER: 10 Menit dengan penalti salah kirim (-20s) dan senggolan mobil (-5s).\n" +
            "4. PAKET JATUH: Kotak kardus di jalan aspal dekat pembatas beton pada Misi 4.\n" +
            "5. 3 ENDING: Kurir Terpercaya, Pengiriman Terlambat, dan Gagal Bertugas.\n\n" +
            "Klik tombol Play (▶) untuk langsung memulai petualangan pengiriman!",
            "OK");
    }

    [MenuItem("Tools/⭐ Setup Game (1 Player WASD + Minimal 2 NPC)", false, 1)]
    public static void MenuSetupGameProject()
    {
        SetupGameProject();
        MarkSceneDirtySafe();

        EditorUtility.DisplayDialog("Setup Proyek Selesai!",
            "Proyek game kota berhasil dikonfigurasi sesuai spesifikasi:\n\n" +
            "1. KOTA KECIL: Lingkungan bangunan, jalan, dan rumah Anda tetap 100% terjaga rapi.\n" +
            "2. PLAYER: Dikontrol penuh dengan Keyboard (W, A, S, D / Panah).\n" +
            "   • Shift: Berlari cepat\n" +
            "   • Spasi: Melompat\n" +
            "   • C / X: Duduk santai / berdiri\n" +
            "   • Tekan V untuk beralih antara Kamera Pemandangan Kota & Kamera Mengikuti Player.\n" +
            "3. 3 NPC MANDIRI:\n" +
            "   • NPC Wanita Baju Kuning: Berdiri dan melompat di tempat di trotoar timur, menyapa ramah saat Player mendekat.\n" +
            "   • NPC Pria: Berjalan kaki di trotoar barat melawan arah dari Player, dilengkapi sensor anti-tabrakan.\n" +
            "   • NPC Barber Shop: Berdiri diam di depan Barber Shop menengok kiri-kanan tanpa berjalan.\n" +
            "   • Semuanya bergerak mandiri tanpa kontrol user.\n\n" +
            "Klik tombol Play (▶) untuk langsung mencoba!",
            "OK");
    }

    [MenuItem("Tools/💈 Setup NPC Barber Shop (Menengok Kiri-Kanan)", false, 4)]
    public static void MenuSetupBarberShopNPC()
    {
        RemoveGiantNPCs();
        EnsureEnvironmentColliders();
        GameObject playerGo = EnsurePlayerCharacter();
        EnsureBarberShopNPC(playerGo);

        MarkSceneDirtySafe();

        EditorUtility.DisplayDialog("Setup NPC Barber Shop Selesai!",
            "Karakter NPC di depan Barber Shop berhasil dikonfigurasi:\n\n" +
            "• Berdiri diam di tempat (tanpa berjalan/berpindah posisi).\n" +
            "• Menengok kiri dan kanan secara halus dan alami.\n" +
            "• Dilengkapi animasi kepala dan badan realistis.\n" +
            "• Menyapa dan melambaikan tangan saat Player mendekat.\n\n" +
            "Klik tombol Play (▶) untuk melihat aksinya!",
            "OK");
    }

    [MenuItem("Tools/🌀 Setup Steve & Portal Transition (Modul 7)", false, 2)]
    public static void MenuSetupSteveAndPortal()
    {
        EnsureEnvironmentColliders();
        GameObject playerGo = EnsurePlayerCharacter();
        SetupAllNPCs(playerGo);
        EnsureSteveAndPortal();

        MarkSceneDirtySafe();

        EditorUtility.DisplayDialog("Setup Steve & Portal Selesai!",
            "Quizmaster Steve dan ExitPortal berhasil dikonfigurasi:\n\n" +
            "1. QUIZMASTER STEVE: Berdiri di trotoar depan Coffee Shop.\n" +
            "   • Kanvas miring raksasa lama dinonaktifkan.\n" +
            "   • Balon dialog & subtitle otomatis muncul saat Player mendekat.\n" +
            "   • Memberi instruksi langsung ke portal.\n\n" +
            "2. EXIT PORTAL: Terpasang di trotoar kanan depan halte/billboard.\n" +
            "   • Tersembunyi di awal, otomatis aktif saat bicara dengan Steve.\n" +
            "   • Memicu transisi Fade to Black saat dimasuki dan memuat Level 1 (Build index 1).\n\n" +
            "Klik tombol Play (▶) untuk langsung mencoba!",
            "OK");
    }

    [MenuItem("Tools/👥 Setup 2 NPC (Wanita Melompat di Tempat & Pria Berjalan Melawan Arah)", false, 3)]
    public static void MenuSetupTwoNPCs()
    {
        RemoveGiantNPCs();
        EnsureEnvironmentColliders();
        GameObject playerGo = EnsurePlayerCharacter();
        SetupAllNPCs(playerGo);

        MarkSceneDirtySafe();

        EditorUtility.DisplayDialog("Setup 2 NPC Berhasil!",
            "Dua NPC mandiri berhasil disiapkan:\n\n" +
            "1. WANITA BAJU KUNING: Berdiri dan melompat di tempat di trotoar timur, menyapa Player saat didekati.\n" +
            "2. PRIA BERJALAN MELAWAN ARAH: Berjalan kaki di trotoar barat menghadap selatan (melawan arah dari pergerakan Player), dilengkapi sensor anti-tabrakan dan dialog sapaan.\n\n" +
            "Keduanya mandiri (NPC) tanpa ada kontrol keyboard/user.\nKlik tombol Play (▶) untuk melihat aksinya!",
            "OK");
    }

    [MenuItem("Tools/🛍️ Pindahkan Player ke Area Pertokoan (Shopping Street)", false, 2)]
    public static void MenuTeleportToShoppingArea()
    {
        GameObject playerGo = EnsurePlayerCharacter();
        if (playerGo != null)
        {
            playerGo.transform.position = new Vector3(320.5f, 0.02f, 4.8f);
            playerGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            SetupCamera(playerGo);
            EnsureEnvironmentColliders();

            if (Application.isPlaying)
            {
                SubSceneLoader loader = Object.FindAnyObjectByType<SubSceneLoader>();
                if (loader != null) loader.ForceUpdatePosition();
            }

            MarkSceneDirtySafe();

            EditorUtility.DisplayDialog("Player di Area Pertokoan!",
                "Player berhasil diposisikan di Area Pertokoan (Shopping Street)!\n\n" +
                "• Posisi: Menghadap ke arah barat menelusuri jalan pertokoan (Drug Store, Pawn Shop, dll).\n" +
                "• Bebas Hambatan: Semua garis marka jalan, ubin aspal, dan ornamen lantai telah dibersihkan dari collider rintangan sehingga karakter bisa bebas masuk dan berjalan di area ini.\n\n" +
                "Klik Play (▶) untuk menjelajahi area!",
                "OK");
        }
    }

    [MenuItem("Tools/Hapus Semua NPC Raksasa (Delete Giant NPCs)")]
    public static void MenuRemoveGiantNPCs()
    {
        int deleted = RemoveGiantNPCs();
        MarkSceneDirtySafe();
        EditorUtility.DisplayDialog("NPC Raksasa Dihapus!",
            deleted > 0 
                ? $"Berhasil menghapus {deleted} karakter raksasa dari kota!"
                : "Semua karakter sudah berukuran proporsional (skala 0.1).",
            "OK");
    }

    [MenuItem("Tools/🌐 Setup Multi-Scene Manager (Modul 6)", false, 5)]
    public static void MenuSetupSubSceneManager()
    {
        EnsureAllScenesInBuild();
        GameObject playerGo = EnsurePlayerCharacter();
        EnsureSubSceneManager(playerGo);

        MarkSceneDirtySafe();

        EditorUtility.DisplayDialog("Setup Multi-Scene Manager Berhasil!",
            "Sistem Managing Multiple Scenes (Modul 6) berhasil dikonfigurasi:\n\n" +
            "1. SUB-SCENE LOADER: Terpasang pada GameObject 'SubSceneManager'.\n" +
            "2. SUB-SCENES: 4 Sub-scenes (SubScene_0_0 s/d SubScene_1_1) dalam grid 2x2.\n" +
            "3. SCRIPTABLE OBJECTS: SubSceneData dengan properti Color & Player Shape (Challenge).\n" +
            "4. ADDITIVE STREAMING: Sub-scene otomatis dimuat/dilepas saat Player mendekat/menjauh.\n\n" +
            "Gunakan menu 'Tools/🚀 Teleport ke Grid Multi-Scene' untuk mencoba langsung di area sub-scene!",
            "OK");
    }

    [MenuItem("Tools/🚀 Teleport ke Grid Multi-Scene (Modul 6 Test Area)", false, 6)]
    public static void MenuTeleportToSubSceneGrid()
    {
        EnsureAllScenesInBuild();
        GameObject playerGo = EnsurePlayerCharacter();
        if (playerGo != null)
        {
            EnsureSubSceneManager(playerGo);
            playerGo.transform.position = new Vector3(0f, 0.2f, 0f);
            playerGo.transform.rotation = Quaternion.identity;

            // Atur kamera dengan sudut elevated / luas agar streaming kotak terlihat jelas
            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                ThirdPersonCamera tpc = cam.GetComponent<ThirdPersonCamera>();
                if (tpc == null) tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();
                tpc.viewMode = ThirdPersonCamera.CameraViewMode.ThirdPersonFollow;
                tpc.target = playerGo.transform;
                tpc.targetHeight = 0.18f;
                tpc.distance = 3.5f;
                tpc.height = 2.5f;
                tpc.followSpeed = 10f;
                tpc.rotationFollowSpeed = 6f;
                tpc.showControlsUI = false;
                tpc.SnapToTarget();
            }

            if (!Application.isPlaying)
            {
                // Buka SubScene_0_0 secara aditif di editor agar platform langsung terlihat di Scene view
                bool alreadyOpen = false;
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name == "SubScene_0_0")
                    {
                        alreadyOpen = true;
                        break;
                    }
                }
                if (!alreadyOpen)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                        "Assets/Scenes/SubScenes/SubScene_0_0.unity",
                        UnityEditor.SceneManagement.OpenSceneMode.Additive
                    );
                }
            }
            else
            {
                SubSceneLoader loader = Object.FindAnyObjectByType<SubSceneLoader>();
                if (loader != null) loader.ForceUpdatePosition();
            }

            MarkSceneDirtySafe();
            if (!Application.isPlaying)
            {
                var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                if (activeScene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
                }
            }

            EditorUtility.DisplayDialog("Teleport ke Grid Multi-Scene Berhasil!",
                "Player dipindahkan ke kotak SubScene_0_0 (0, 0.2, 0) pada Grid 3x3 (Ukuran Kotak 1.5 x 1.5):\n\n" +
                "• Kotak Baru Muncul: Saat Player melangkah ke kotak warna lain, platform di depan otomatis dimuat (load) dan terlihat.\n" +
                "• Kotak Lama Terlepas: Platform di belakang otomatis dilepas (unload) saat Player menjauh.\n" +
                "• Karakter Player: Tampilan dan warna Player tetap normal (tidak berubah warna).\n\n" +
                "Silakan berjalan untuk melihat kotak-kotak baru bermunculan secara dinamis!",
                "OK");
        }
    }
    [MenuItem("Tools/🎬 Setup Cinematography & Cutscene (Modul 8)", false, 7)]
    public static void MenuSetupCutscene()
    {
        EnsureEnvironmentColliders();
        GameObject playerGo = EnsurePlayerCharacter();
        EnsureCutsceneSystem(playerGo);
        MarkSceneDirtySafe();

        EditorUtility.DisplayDialog("Setup Cutscene & Cinematography Selesai!",
            @"Sistem Cinematography & Cutscene (Modul 8) berhasil diimplementasikan ke dalam kota:

1. WAITRESS SARAH: Berada di konter Donut Shop dengan animasi ramah & sapaan dialog.
2. VIRTUAL CAMERAS: 3 Kamera Cinemachine (Wide, Dolly Pan menelusuri trotoar, Waitress Medium Shot).
3. TIMELINE: Cut & Blend sinematik otomatis terintegrasi dengan Banner UI dan Jingle Audio.
4. TRIGGER ZONE: Terpasang di trotoar depan Donut Shop.
   • Kontrol WASD pemain dan ThirdPersonCamera dinonaktifkan secara mulus selama cutscene dan dipulihkan kembali setelahnya.

Gunakan menu 'Tools/🍩 Teleport ke Donut Shop' untuk langsung menguji di depan toko!",
            "OK");
    }

    [MenuItem("Tools/🍩 Teleport ke Donut Shop (Modul 8 Cutscene)", false, 8)]
    public static void MenuTeleportToDonutShop()
    {
        GameObject playerGo = EnsurePlayerCharacter();
        if (playerGo != null)
        {
            EnsureCutsceneSystem(playerGo);
            // Posisikan player di trotoar tepat sebelum trigger zone Donut Shop
            playerGo.transform.position = new Vector3(317.0f, 0.02f, 13.5f);
            playerGo.transform.rotation = Quaternion.Euler(0f, -40f, 0f); // Menghadap langsung ke arah Toko Donut

            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                ThirdPersonCamera tpc = cam.GetComponent<ThirdPersonCamera>();
                if (tpc != null)
                {
                    tpc.SnapToTarget();
                }
            }

            MarkSceneDirtySafe();

            EditorUtility.DisplayDialog("Player di Depan Donut Shop!",
                @"Player berhasil dipindahkan ke trotoar depan Toko Donut (317.0, 0.02, 13.5):

• Menghadap lurus ke arah Donut Shop dan Waitress Sarah.
• Melangkahlah maju 1-2 langkah dengan tombol [W] untuk masuk ke Trigger Zone dan menyaksikan Cutscene Sinematik!

Klik Play (▶) lalu tekan [W] untuk mencoba!",
                "OK");
        }
    }

    /// <summary>
    /// Digunakan untuk menjalankan setup otomatis via Unity batchmode di command line.
    /// </summary>
    public static void BatchSetupKurirTerakhir()
    {
        Debug.Log("[PlayerBootstrap] Memulai BatchSetupKurirTerakhir...");
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Module 2.unity");
        if (scene.IsValid())
        {
            SetupGameProject();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log("[PlayerBootstrap] BatchSetupKurirTerakhir sukses dan scene Module 2 berhasil disimpan!");
        }
        else
        {
            Debug.LogError("[PlayerBootstrap] Gagal membuka scene Assets/Module 2.unity!");
        }
    }

    public static void BatchSetupCutscene()
    {
        Debug.Log("[PlayerBootstrap] Memulai BatchSetupCutscene...");
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Module 2.unity");
        if (scene.IsValid())
        {
            SetupGameProject();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log("[PlayerBootstrap] BatchSetupCutscene sukses dan scene Module 2 berhasil disimpan!");
        }
        else
        {
            Debug.LogError("[PlayerBootstrap] Gagal membuka scene Assets/Module 2.unity!");
        }
    }

#endif

    /// <summary>
    /// Menyiapkan 1 Player WASD + Minimal 2 NPC pejalan kaki di trotoar kota.
    /// Lingkungan kota kecil buatan pengguna sama sekali tidak disentuh/diubah.
    /// </summary>
    public static void SetupGameProject()
    {
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.name.Contains("Level 2") || activeScene.name == "Level 2 saya")
        {
            SetupAirportLevel2Saya();
            return;
        }

        // 1. Sesuaikan skala karakter jika ada yang berskala raksasa (skala > 0.2f -> 0.1f)
        RemoveGiantNPCs();

        // 2. Pastikan rintangan/gedung memiliki collider dasar agar tidak tembus
        EnsureEnvironmentColliders();

        // 3. Pastikan seluruh karakter (orang) memiliki collider solid agar tidak bisa ditembus
        EnsureAllCharacterColliders();

        // 4. Setup PLAYER (Bisa digerakkan dengan Keyboard WASD)
        GameObject playerGo = EnsurePlayerCharacter();

        // 4. Setup KAMERA (Pemandangan Kota Luas & Third-Person Follow)
        SetupCamera(playerGo);

        // 5. Pastikan seluruh karakter di scene memiliki material & tekstur URP yang valid (tidak pink/putih/magenta)
        GameObject[] allChars = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
        foreach (var go in allChars)
        {
            if (go == null) continue;
            bool isChar = go.GetComponent<Animator>() != null ||
                          go.name.StartsWith("SimplePeople") ||
                          go.name.StartsWith("SimpleCitizens") ||
                          go.name.StartsWith("NPC_") ||
                          go.name == "Derryl" ||
                          go.name == "Icha" ||
                          go.GetComponent<DeliveryRecipient>() != null ||
                          go.GetComponent<NPCWalker>() != null;

            if (isChar && !go.name.Contains("Camera") && !go.name.Contains("Light"))
            {
                EnsureCorrectCharacterMesh(go);
                AutoEnsureCharacterMaterial(go);
            }
        }

        // 6. Bersihkan objek kanvas WorldSpace lama yang mungkin tertinggal di scene & inisialisasi Screen-Space Dialogue
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allObjects)
        {
            if (go != null && (go.name == "SpeechBubbleCanvas" || go.name == "BubbleBackground"))
            {
                DestroyGameObjectSafe(go);
            }
        }
        NPCDialogueOverlay.EnsureInstance();

        // 7. Setup Game Inti Kurir Terakhir (Final Project: Level 1 & Level 2)
        if (activeScene.name == "Module 2_Stage2")
        {
            SetupCityLevel2(playerGo);
        }
        else
        {
            SetupKurirTerakhirGame(playerGo);
        }

        Debug.Log("[PlayerBootstrap] Berhasil mengonfigurasi 1 Player (WASD) dan Game 'Kurir Terakhir'!");
    }

    /// <summary>
    /// Konfigurasi Utama Game "Kurir Terakhir" (Final Project):
    /// - Menempatkan Rian di depan Mall (310.18, 0.02, 4.34) menghadap ke arah jalan/mall.
    /// - Mengonfigurasi 6 Karakter Penerima/Pemberi Petunjuk (Pak Beni, Clara, Pak Joko, Dimas, Petugas Parkir, Naya).
    /// - Memasang paket kardus fisik yang terjatuh di jalan aspal dekat pembatas beton (301.4, 0.02, 6.8).
    /// - Memasang CarHazardTrigger pada mobil-mobil berjalan di kota.
    /// - Mengaktifkan DeliveryGameManager dan DeliveryHUD.
    /// - Membersihkan/menonaktifkan objek tugas modul lama dan karakter tersembunyi.
    /// </summary>
    public static void SetupKurirTerakhirGame(GameObject playerGo)
    {
        // 1. Bersihkan elemen tugas modul lama dan karakter tersembunyi
        CleanupLegacyModuleObjects();

        // 2. Pertahankan posisi dan rotasi Rian (Player) persis seperti yang diatur pengguna di Scene
        if (playerGo != null)
        {
            PlayerController pc = playerGo.GetComponent<PlayerController>();
            if (pc != null)
            {
                // Kunci gerakan pemain saat awal permainan karena Main Menu aktif
                pc.SetMovementFrozen(true);
            }

            // Arahkan kamera langsung ke belakang Rian sesuai posisi & rotasinya di scene
            SetupCamera(playerGo);
        }

        // 3. Setup 6 Karakter Utama dengan DeliveryRecipient
        SetupKurirRecipients(playerGo);
        EnsureAllCharacterColliders();

        // 3b. Pria berbaju merah (No. 05) di tepi jalan ikut berjalan menyusuri trotoar
        EnsureWanderingStreetMan(playerGo);

        // 3c. Perbaiki material rusak (tampil ungu di URP) pada objek kota
        AirportColorizer.FixBrokenMaterials();

        // 4. Setup Objek Paket Jatuh di Area Parkir (Misi 4)
        SetupDroppedPackageItem();

        // 5. Setup Mobil Berjalan & Hazard Tabrakan
        try
        {
            CityCarManager.SetupUserManualCars();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[PlayerBootstrap] Peringatan saat setup mobil: {ex.Message}");
        }

        // 6. Setup DeliveryGameManager & DeliveryHUD
        GameObject gmGo = GameObject.Find("DeliveryGameManager");
        if (gmGo == null)
        {
            gmGo = new GameObject("DeliveryGameManager");
        }
        DeliveryGameManager gm = gmGo.GetComponent<DeliveryGameManager>();
        if (gm == null) gm = gmGo.AddComponent<DeliveryGameManager>();
        if (gm != null) gm.totalTimeLimit = 360f; // 6 menit (Scene 1 & Scene 2)

        DeliveryHUD.EnsureInstance();
        NPCDialogueOverlay.EnsureInstance();
        GameAudioManager.EnsureInstance();
        // Cutscene dihapus: EnsureRianDeliveryCutsceneSystem tidak lagi dijalankan

        Debug.Log("[PlayerBootstrap] ⭐ Game 'Kurir Terakhir' Berhasil Dikonfigurasi Lengkap!");
    }

    /// <summary>
    /// Mencari GameObject karakter yang sudah ada di scene (termasuk inactive) berdasarkan nama persis atau kata kunci.
    /// Tidak pernah membuat atau menduplikasi GameObject karakter baru.
    /// </summary>
    public static GameObject FindExistingCharacter(string exactName, string fallbackKeyword = null)
    {
        // 1. Pencarian langsung berdasarkan nama persis
        GameObject exact = GameObject.Find(exactName);
        if (exact != null) return exact;

        // 2. Pencarian di seluruh objek scene (termasuk inactive)
        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in all)
        {
            if (go == null) continue;
            if (string.Equals(go.name.Trim(), exactName.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                return go;
            }
        }

        // 3. Fallback pencarian parsial jika nama sedikit berbeda
        if (!string.IsNullOrEmpty(fallbackKeyword))
        {
            string kw = fallbackKeyword.ToLowerInvariant();
            foreach (var go in all)
            {
                if (go == null) continue;
                string lower = go.name.ToLowerInvariant();
                if (lower.Contains(kw) &&
                    !lower.Contains("camera") &&
                    !lower.Contains("light") &&
                    !lower.Contains("canvas") &&
                    !lower.Contains("subscene"))
                {
                    return go;
                }
            }
        }

        return null;
    }

    public static void SafeSetAnimatorIdle(Animator anim)
    {
        if (anim == null) return;
        anim.applyRootMotion = false;
        if (anim.runtimeAnimatorController == null) return;

        foreach (var p in anim.parameters)
        {
            if (p.name == "Speed_f") anim.SetFloat(p.nameHash, 0f);
            else if (p.name == "Animation_int") anim.SetInteger(p.nameHash, 0);
            else if (p.name == "Static_b") anim.SetBool(p.nameHash, false);
            else if (p.name == "Grounded" || p.name == "Grounded_b") anim.SetBool(p.nameHash, true);
            else if (p.name == "Jump_b" || p.name == "Crouch_b" || p.name == "Death_b") anim.SetBool(p.nameHash, false);
        }
    }

    /// <summary>
    /// Setup Khusus Level 2 Bandara ("Level 2 saya"):
    /// - Mengonfigurasi Rian (kurir) dan 6 NPC penerima paket di area bandara internasional.
    /// - Alur Misi: Rian -> Pak Beni 2 -> Naya 2 -> Dimas 2 -> Tukang Parkir 2 -> Pak Joko 2 -> Clara 2 -> Pak Beni 2 -> Tamat!
    /// - Menjamin seluruh 6 NPC dan Rian hadir di scene (menemukan atau menginstansiasi dari prefab jika belum ada).
    /// - Menghilangkan T-pose dengan mengaitkan Animator Controller dan mengevaluasi pose Idle.
    /// - Terhubung sebagai kelanjutan misi resmi dari Scene 1 (Module 2).
    /// </summary>
    public static void SetupAirportLevel2Saya()
    {
        // 1. Setup Rian (Player)
        GameObject playerGo = FindExistingCharacter("Rian (kurir)", "Rian");
        if (playerGo == null) playerGo = FindExistingCharacter("Rian (Kurir)", "Trucker");
        if (playerGo == null) playerGo = GameObject.FindWithTag("Player");

#if UNITY_EDITOR
        if (playerGo == null)
        {
            GameObject truckerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Trucker_Black.prefab");
            if (truckerPrefab != null)
            {
                playerGo = (GameObject)PrefabUtility.InstantiatePrefab(truckerPrefab);
                playerGo.name = "Rian (kurir)";
                playerGo.transform.position = new Vector3(35f, 0.05f, 138.27f);
                playerGo.transform.rotation = Quaternion.Euler(0f, 40f, 0f);
                playerGo.transform.localScale = Vector3.one;
            }
        }
#endif
        if (playerGo == null)
        {
            playerGo = new GameObject("Rian (kurir)");
            playerGo.transform.position = new Vector3(35f, 0.05f, 138.27f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 40f, 0f);
        }

        playerGo.tag = "Player";
        playerGo.name = "Rian (kurir)";
        playerGo.SetActive(true);

        // Bersihkan komponen lama yang bentrok
        CharacterController cc = playerGo.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        NPCWalker walker = playerGo.GetComponent<NPCWalker>();
        if (walker != null) DestroyComponentSafe(walker);

        // Capsule Collider
        CapsuleCollider col = playerGo.GetComponent<CapsuleCollider>();
        if (col == null) col = playerGo.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.center = new Vector3(0f, 0.9f, 0f);
        col.radius = 0.35f;

        // Rigidbody
        Rigidbody rb = playerGo.GetComponent<Rigidbody>();
        if (rb == null) rb = playerGo.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Animator
        Animator anim = playerGo.GetComponent<Animator>();
        if (anim == null) anim = playerGo.AddComponent<Animator>();
        anim.applyRootMotion = false;
        anim.enabled = true;

#if UNITY_EDITOR
        if (anim.runtimeAnimatorController == null)
        {
            RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleCitizens/Models/SimpleCitizens.controller");
            if (ctrl == null)
            {
                ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            }
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        }
#endif
        SafeSetAnimatorIdle(anim);

#if UNITY_EDITOR
        if (!Application.isPlaying && anim != null && anim.runtimeAnimatorController != null)
        {
            anim.Update(0f);
        }
#endif

        // PlayerController
        PlayerController pc = playerGo.GetComponent<PlayerController>();
        if (pc == null) pc = playerGo.AddComponent<PlayerController>();
        pc.walkSpeed = 4.5f;
        pc.sprintSpeed = 8.5f;
        pc.rotationSpeed = 12f;
        pc.jumpHeight = 1.2f;
        pc.bodyRadius = 0.35f;
        pc.bodyHeight = 1.8f;
        pc.maxStepHeight = 0.35f;
        pc.skinWidth = 0.02f;

        EnsureCorrectCharacterMesh(playerGo);
        AutoEnsureCharacterMaterial(playerGo);

        // Setup ThirdPersonCamera
        Camera cam = Camera.main;
        if (cam != null)
        {
            ThirdPersonCamera tpc = cam.GetComponent<ThirdPersonCamera>();
            if (tpc == null) tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();
            tpc.target = playerGo.transform;
            tpc.targetHeight = 1.6f;
            tpc.distance = 13.5f;
            tpc.height = 4.5f;
            tpc.followRearAngle = true;
            tpc.collisionRadius = 0.25f;
            tpc.viewMode = ThirdPersonCamera.CameraViewMode.ThirdPersonFollow;
            pc.playerCamera = cam;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = new Color(0.192f, 0.302f, 0.475f, 1f);
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
        }

        // 2. Setup 6 NPC Penerima Paket Bandara (Prolog -> 5 Paket -> Ending)
        SetupAirportRecipientNPC(
            "Pak Beni 2", "Beni",
            "Pak Beni 2",
            DeliveryRecipient.RecipientType.PakBeni,
            DeliveryMissionState.Prolog,
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Sheriff_Black.prefab",
            new Vector3(41.197f, 0.05f, 146.568f),
            Quaternion.Euler(0f, 200f, 0f),
            new string[]
            {
                "Rian: Halo Pak Beni! Saya sudah tiba di Bandara Internasional. Apa tugas pengantaran paket berikutnya untuk saya?",
                "Pak Beni 2: Halo Rian! Kerja luar biasa telah menyelesaikan seluruh pengantaran di kawasan kota pada Scene 1 tadi!",
                "Pak Beni 2: Tapi tugas Kurir Terpercaya kita belum usai. Hari ini Bandara Internasional membutuhkan bantuan logistik darurat.",
                "Rian: Siap Pak Beni! Barang apa saja yang harus saya antarkan dan siapa saja penerimanya di bandara ini?",
                "Pak Beni 2: Ada 5 paket penting yang harus kau antarkan secara berurutan sesuai manifes penerbangan:",
                "Pak Beni 2: 1. Naya 2 (Dokumen Gate), 2. Dimas 2 (Suku Cadang), 3. Tukang Parkir 2 (Rompi Apron), 4. Pak Joko 2 (Kargo VIP), 5. Clara 2 (Lounge VIP).",
                "Rian: Baik Pak, bagaimana cara saya menemukan lokasi mereka di area bandara yang luas ini?",
                "Pak Beni 2: Gunakan indikator radar emas di layarmu. Titik radar akan memandu arahmu ke tiap kru bandara satu per satu.",
                "Pak Beni 2: Setelah kelima paket selesai kau antarkan, segera kembali lapor ke saya di sini untuk menyelesaikan giliran kerjamu!",
                "Rian: Siap Pak Beni! Saya mengerti dan segera berangkat mengantarkan paket pertama ke Naya 2!"
            },
            new string[]
            {
                "Rian: Lapor Pak Beni! Seluruh 5 paket bandara telah berhasil saya antarkan ke semua penerima!",
                "Pak Beni 2: Luar biasa, Rian! Semua 5 paket bandara sampai dengan selamat dan tepat waktu!",
                "Pak Beni 2: Dari jalanan kota hingga landasan bandara internasional, dedikasimu membuktikan bahwa kau adalah Kurir Terpercaya sejati!",
                "Rian: Terima kasih banyak atas bimbingan dan kepercayaannya, Pak Beni! Senang bisa membantu kelancaran operasional bandara.",
                "Pak Beni 2: Selamat Rian! Tugas Level 2 Bandara telah selesai dengan gemilang! Kamu resmi menamatkan seluruh petualangan kurir!"
            },
            new string[]
            {
                "Rian: Pak Beni, apakah saya sudah boleh menyelesaikan tugas sekarang?",
                "Pak Beni 2: Belum Rian, kamu harus menyelesaikan pengantaran ke 5 petugas bandara terlebih dahulu sebelum melapor kembali kemari!"
            }
        );

        SetupAirportRecipientNPC(
            "Naya 2", "Naya",
            "Naya 2",
            DeliveryRecipient.RecipientType.Naya,
            DeliveryMissionState.Package1_CoffeeShop,
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_BeachBabe_Black.prefab",
            new Vector3(34.0f, 0.05f, 168.0f),
            Quaternion.Euler(0f, 180f, 0f),
            new string[]
            {
                "Rian: Halo Naya 2! Apakah benar Anda yang sedang menunggu paket dari Pak Beni?",
                "Naya 2: Halo Rian! Benar sekali, saya sedang menunggu dokumen manifes penerbangan untuk kru kokpit!"
            },
            new string[]
            {
                "Rian: Ini dokumen manifes penerbangan resmi dari Pak Beni, silakan dicek Naya!",
                "Naya 2: Terima kasih banyak Rian! Dokumen ini tepat seperti yang dibutuhkan kokpit. Sekarang tolong lekas antarkan suku cadang ke Dimas 2 di dekat troli bagasi sebelah timur!",
                "Rian: Baik Naya, saya segera meluncur ke Dimas 2!"
            },
            new string[]
            {
                "Rian: Naya 2, apakah paket ini pesanan Anda?",
                "Naya 2: Maaf Mas Rian, paket ini bukan untuk bagian gate penerbangan. Coba periksa petunjuk pengantaranmu!"
            }
        );

        SetupAirportRecipientNPC(
            "Dimas 2", "Dimas",
            "Dimas 2",
            DeliveryRecipient.RecipientType.Dimas,
            DeliveryMissionState.Package2_DrugStore,
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_StreetMan_Brown.prefab",
            new Vector3(58.0f, 0.05f, 154.0f),
            Quaternion.Euler(0f, -90f, 0f),
            new string[]
            {
                "Rian: Halo Dimas 2! Apakah Anda teknisi yang membutuhkan suku cadang konveyor?",
                "Dimas 2: Halo Rian! Iya betul, mesin pemilah bagasi sedang macet dan butuh suku cadang darurat!"
            },
            new string[]
            {
                "Rian: Ini paket suku cadang konveyor dari bagian logistik untukmu, Dimas!",
                "Dimas 2: Wah mantap sekali, terima kasih Rian! Mesin bagasi bisa segera normal kembali. Berikutnya, tolong antarkan rompi & perlengkapan ke Tukang Parkir 2 di apron barat!",
                "Rian: Siap Dimas, saya langsung berangkat ke Tukang Parkir 2!"
            },
            new string[]
            {
                "Rian: Dimas, apa paket ini yang kau cari?",
                "Dimas 2: Maaf Rian, paket ini bukan suku cadang teknisi bagasi. Coba periksa penerima lain!"
            }
        );

        SetupAirportRecipientNPC(
            "Tukang Parkir 2", "Parkir",
            "Tukang Parkir 2",
            DeliveryRecipient.RecipientType.PetugasParkir,
            DeliveryMissionState.Package3_HardwareStore,
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_RoadWorker_White.prefab",
            new Vector3(18.0f, 0.05f, 154.0f),
            Quaternion.Euler(0f, 90f, 0f),
            new string[]
            {
                "Rian: Permisi, apa Anda Tukang Parkir 2 yang bertugas memandu pesawat di apron?",
                "Tukang Parkir 2: Iya betul Mas Rian! Pesawat sebentar lagi merapat, saya butuh rompi sinyal dan bendera pemandu!"
            },
            new string[]
            {
                "Rian: Ini paket perlengkapan rompi reflektor dan bendera marshaller untukmu!",
                "Tukang Parkir 2: Terima kasih banyak Mas Rian! Dengan perlengkapan ini, pesawat bisa dipandu parkir dengan aman. Lekas antarkan paket keempat ke Pak Joko 2 di terminal kargo selatan!",
                "Rian: Siap Mas! Saya segera ke terminal kargo menemui Pak Joko 2!"
            },
            new string[]
            {
                "Rian: Apakah ini paket perlengkapan marshaller Anda?",
                "Tukang Parkir 2: Bukan ini paket pesanan saya Mas Rian. Saya butuh perlengkapan rompi dan bendera apron marshaller!"
            }
        );

        SetupAirportRecipientNPC(
            "Pak Joko 2", "Joko",
            "Pak Joko 2",
            DeliveryRecipient.RecipientType.PakJoko,
            DeliveryMissionState.Package4_DroppedPackage,
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_BusinessMan_Black.prefab",
            new Vector3(18.0f, 0.05f, 126.0f),
            Quaternion.Euler(0f, 45f, 0f),
            new string[]
            {
                "Rian: Selamat siang Pak Joko 2! Apa ini terminal logistik kargo internasional?",
                "Pak Joko 2: Selamat siang kurir Rian! Benar, saya sedang menunggu dokumen ekspor kargo VIP!"
            },
            new string[]
            {
                "Rian: Ini dokumen kargo ekspor VIP yang dipersiapkan Pak Beni untuk Anda, Pak Joko!",
                "Pak Joko 2: Terima kasih kurir Rian! Manifes kargo udara ini sudah lengkap dan siap diberangkatkan. Tinggal satu paket terakhir, lekas antarkan ke Clara 2 di area lounge eksekutif timur!",
                "Rian: Baik Pak Joko, saya menuju ke Clara 2 sekarang!"
            },
            new string[]
            {
                "Rian: Pak Joko, apa dokumen ini yang Anda tunggu?",
                "Pak Joko 2: Maaf Rian, ini bukan dokumen logistik kargo ekspor VIP."
            }
        );

        SetupAirportRecipientNPC(
            "Clara 2", "Clara",
            "Clara 2",
            DeliveryRecipient.RecipientType.Clara,
            DeliveryMissionState.Package5_Apartment,
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Waitress_White.prefab",
            new Vector3(52.0f, 0.05f, 133.0f),
            Quaternion.Euler(0f, -45f, 0f),
            new string[]
            {
                "Rian: Halo Clara 2! Apakah tamu lounge eksekutif sedang menanti paket cinderamata?",
                "Clara 2: Halo Rian! Benar sekali, para tamu VIP di lounge sudah menantikan bingkisan selamat datang!"
            },
            new string[]
            {
                "Rian: Ini paket bingkisan cinderamata penerbangan khusus untuk lounge eksekutif, Clara!",
                "Clara 2: Wah tepat waktu sekali Rian, terima kasih banyak! Semua 5 paket sekarang sudah tuntas kau antarkan. Cepat temui Pak Beni 2 di area truk pengiriman untuk lapor tugas akhir!",
                "Rian: Terima kasih Clara! Saya segera kembali menemui Pak Beni 2!"
            },
            new string[]
            {
                "Rian: Clara, apa ini bingkisan untuk lounge VIP?",
                "Clara 2: Maaf Rian, saya Clara 2, tapi ini bukan paket cinderamata untuk lounge VIP."
            }
        );

        // 3. Setup Dialog Overlay & Game Managers
        NPCDialogueOverlay.EnsureInstance();

        // DeliveryGameManager
        GameObject gmGo = GameObject.Find("DeliveryGameManager");
        if (gmGo == null) gmGo = new GameObject("DeliveryGameManager");
        DeliveryGameManager gm = gmGo.GetComponent<DeliveryGameManager>();
        if (gm == null) gm = gmGo.AddComponent<DeliveryGameManager>();
        if (gm != null) gm.totalTimeLimit = 360f; // 6 menit (Scene 1 & Scene 2)
        if (gm != null)
        {
            gm.totalTimeLimit = 360f; // 6 menit (Scene 1 & Scene 2)
        }

        // Bersihkan objek radar usang di root scene jika ada
        GameObject staleRootRadar = GameObject.Find("DeliveryRadarHUD");
        if (staleRootRadar != null && staleRootRadar.transform.parent == null)
        {
            if (Application.isPlaying) Object.Destroy(staleRootRadar);
            else Object.DestroyImmediate(staleRootRadar);
        }

        // Setup DeliveryHUD (Radar otomatis dibuat & dikonfigurasi di dalam Canvas HUD)
        DeliveryHUD.EnsureInstance();

        DeliveryRadarHUD radar = Object.FindAnyObjectByType<DeliveryRadarHUD>();
        if (radar == null)
        {
            DeliveryHUD hud = DeliveryHUD.Instance;
            if (hud != null)
            {
                radar = DeliveryRadarHUD.Ensure(hud.transform, null);
            }
        }
        if (radar != null)
        {
            radar.radarWorldRange = 60f;
            if (radar.gameObject != null)
            {
                radar.gameObject.SetActive(true);
            }
        }

        // 4. Setup Animasi 1 Pesawat Terbang Melintas di Langit Bandara (Plane01 Looping Clockwise Flyover)
        EnsureAirportFlyingAirplane();

        // 4b. Beri warna (livery) pada pesawat & perbaiki objek tanpa tekstur (Editor saja)
        AirportColorizer.Apply();

        // 5. Setup Rintangan Fisik Solid Bandara (BoxCollider pada seluruh kendaraan, gedung, props, pagar)
        int colliderCount = EnsureAirportEnvironmentColliders();
        EnsureAllCharacterColliders();

        // 6. Setup Langit Biru Cerah Bandara (Skybox & Camera Background)
        EnsureAirportSky();

        // 7. Setup 3 NPC Pejalan Kaki Bandara (Wandering & Menyapa Rian saat berpapasan)
        EnsureAirportWanderingNPCs(playerGo);

        GameAudioManager.EnsureInstance();

        Debug.Log($"[PlayerBootstrap] Level 2 Bandara berhasil dikonfigurasi! Rian, 6 NPC Misi, 3 NPC Pejalan Kaki, {colliderCount} solid colliders, Pesawat Terbang, dan Langit Biru siap!");
    }

    private static void SetupAirportRecipientNPC(
        string exactName, string fallbackKeyword,
        string characterName,
        DeliveryRecipient.RecipientType recipientType,
        DeliveryMissionState targetMission,
        string prefabPath,
        Vector3 defaultPos,
        Quaternion defaultRot,
        string[] introLines,
        string[] successLines,
        string[] wrongLines)
    {
        GameObject npcGo = FindExistingCharacter(exactName, fallbackKeyword);

#if UNITY_EDITOR
        if (npcGo == null && !string.IsNullOrEmpty(prefabPath))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                npcGo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                npcGo.name = exactName;
                npcGo.transform.position = defaultPos;
                npcGo.transform.rotation = defaultRot;
                npcGo.transform.localScale = Vector3.one;
            }
        }
#endif
        if (npcGo == null)
        {
            npcGo = new GameObject(exactName);
            npcGo.transform.position = defaultPos;
            npcGo.transform.rotation = defaultRot;
            npcGo.transform.localScale = Vector3.one;
        }

        // Pastikan nama dan status aktif
        npcGo.name = exactName;
        npcGo.SetActive(true);

        // Pastikan berlabel tag NPC agar PlayerController mendeteksi sebagai rintangan padat
        npcGo.tag = "NPC";
        PlayerController pc = npcGo.GetComponent<PlayerController>();
        if (pc != null) DestroyComponentSafe(pc);
        CharacterController cc = npcGo.GetComponent<CharacterController>();
        if (cc != null) DestroyComponentSafe(cc);
        NPCWalker walker = npcGo.GetComponent<NPCWalker>();
        if (walker != null) DestroyComponentSafe(walker);

        // CapsuleCollider padat (solid, non-trigger) agar karakter Rian tidak bisa menembus NPC
        CapsuleCollider col = npcGo.GetComponent<CapsuleCollider>();
        if (col == null) col = npcGo.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.center = new Vector3(0f, 0.9f, 0f);
        col.radius = 0.38f;
        col.isTrigger = false;

        // Rigidbody Kinematic agar stabil di posisinya
        Rigidbody rb = npcGo.GetComponent<Rigidbody>();
        if (rb == null) rb = npcGo.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Animator
        Animator anim = npcGo.GetComponent<Animator>();
        if (anim == null) anim = npcGo.AddComponent<Animator>();
        anim.applyRootMotion = false;
        anim.enabled = true;

#if UNITY_EDITOR
        if (anim.runtimeAnimatorController == null)
        {
            RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleCitizens/Models/SimpleCitizens.controller");
            if (ctrl == null)
            {
                ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            }
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        }
#endif
        SafeSetAnimatorIdle(anim);
        if (anim.runtimeAnimatorController != null)
        {
            anim.Play("Idle", 0, 0f);
        }

#if UNITY_EDITOR
        if (!Application.isPlaying && anim != null && anim.runtimeAnimatorController != null)
        {
            anim.Update(0f);
        }
#endif

        EnsureCorrectCharacterMesh(npcGo);
        AutoEnsureCharacterMaterial(npcGo);

        // DeliveryRecipient
        DeliveryRecipient recipient = npcGo.GetComponent<DeliveryRecipient>();
        if (recipient == null) recipient = npcGo.AddComponent<DeliveryRecipient>();
        recipient.characterName = characterName;
        recipient.recipientType = recipientType;
        recipient.targetForMission = targetMission;
        recipient.interactionRadius = 4.0f;
        recipient.introDialogueLines = introLines;
        recipient.successDialogueLines = successLines;
        recipient.wrongDeliveryLines = wrongLines;
    }

    /// <summary>
    /// Konfigurasi Khusus Kota Level 2 (Module 2_Stage2):
    /// - Mengatur tata letak baru untuk seluruh 6 Karakter Penerima (Clara, Pak Joko, Dimas, Petugas Parkir, Naya).
    /// - Menempatkan rintangan/barikade jalan baru di jalur utama untuk menantang ingatan rute pemain.
    /// - Menyiapkan paket kardus yang terjatuh di lokasi baru (dekat deretan toko donat).
    /// </summary>
    public static void SetupCityLevel2(GameObject playerGo)
    {
        // 1. Bersihkan elemen tugas modul lama dan karakter tersembunyi
        CleanupLegacyModuleObjects();

        // 2. Posisikan Rian (Player) di depan Mega Mall di Center Plaza
        if (playerGo != null)
        {
            playerGo.transform.position = new Vector3(0f, 0.02f, -2f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            PlayerController pc = playerGo.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.SetMovementFrozen(false);
            }
            SetupCamera(playerGo);
        }

        // 3. Setup 6 Karakter Utama dengan Tata Letak Baru (Level 2)
        SetupKurirRecipientsLevel2(playerGo);
        EnsureAllCharacterColliders();

        // 4. Setup Objek Paket Jatuh di Lokasi Baru Level 2 (Dekat Donut Shop)
        SetupDroppedPackageItemLevel2();

        // 5. Setup Barikade / Rintangan Jalan Level 2
        SetupLevel2RoadObstacles();

        // 6. Setup DeliveryGameManager & DeliveryHUD
        GameObject gmGo = GameObject.Find("DeliveryGameManager");
        if (gmGo == null) gmGo = new GameObject("DeliveryGameManager");
        DeliveryGameManager gm = gmGo.GetComponent<DeliveryGameManager>();
        if (gm == null) gm = gmGo.AddComponent<DeliveryGameManager>();
        if (gm != null) gm.totalTimeLimit = 360f; // 6 menit (Scene 1 & Scene 2)

        DeliveryHUD.EnsureInstance();
        NPCDialogueOverlay.EnsureInstance();
        GameAudioManager.EnsureInstance();

        Debug.Log("[PlayerBootstrap] ⭐ Kota Level 2 (Tata Letak Baru & Rintangan) Berhasil Dikonfigurasi Lengkap!");
    }

    private static void SetupKurirRecipientsLevel2(GameObject playerGo)
    {
        // 1. PAK BENI (Center Plaza - Depan Mega Mall)
        EnsureRecipientNPCLevel2(
            "NPC_PakBeni",
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Sheriff_Black.prefab",
            "SimplePeople_Sheriff_Black",
            new Vector3(0f, 0.02f, -3.5f),
            Quaternion.Euler(0f, 0f, 0f),
            "Pak Beni (Pusat Pengiriman)",
            DeliveryRecipient.RecipientType.PakBeni,
            DeliveryMissionState.Prolog,
            new string[]
            {
                "Rian! Luar biasa kamu berhasil melewati pengiriman pertama.",
                "Tapi sekarang giliran tantangan sesungguhnya: Kota ini memiliki tata letak jalan koridor baru dan toko-toko berada di lokasi berbeda!",
                "Kamu harus mengantarkan 5 paket baru ke pelanggan di lokasi yang berbeda. Waktumu 6 menit! Mulailah dengan mengantar paket pertama ke Clara di Coffee Shop Koridor Utara sebelah barat."
            },
            new string[]
            {
                "Luar biasa Rian! Meskipun tata letak kota berubah dan rintangan menghalangi, kamu berhasil mengantarkan seluruh paket tepat waktu!",
                "Kamu telah membuktikan dirimu sebagai Master Kurir sejati!"
            },
            new string[]
            {
                "Rian, cepat selesaikan pengiriman paket Level 2 sebelum batas waktu 6 menitmu habis!"
            },
            playerGo
        );

        // 2. CLARA (Coffee Shop - Koridor Utara Barat)
        EnsureRecipientNPCLevel2(
            "NPC_Clara",
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Waitress_White.prefab",
            "SimplePeople_Waitress_White",
            new Vector3(-25f, 0.02f, 17f),
            Quaternion.Euler(0f, 180f, 0f),
            "Clara (Coffee Shop Koridor Utara)",
            DeliveryRecipient.RecipientType.Clara,
            DeliveryMissionState.Package1_CoffeeShop,
            new string[]
            {
                "Selamat datang di kedai kopi Clara cabang Koridor Utara! Apakah kamu membawa paket biji kopi segar pesanan kami?"
            },
            new string[]
            {
                "Terima kasih banyak Mas Rian! Kedai kopi kami di Koridor Utara Barat ini sangat terbantu pasokan ini.",
                "Tadi Pak Joko dari Drug Store mengabari kalau tokonya sekarang berada di Koridor Selatan sebelah barat!"
            },
            new string[]
            {
                "Maaf mas, ini kedai kopi Clara di Koridor Utara. Saya tidak memesan paket ini! Coba periksa alamat toko lain."
            },
            playerGo
        );

        // 3. PAK JOKO (Drug Store - Koridor Selatan Barat)
        EnsureRecipientNPCLevel2(
            "NPC_PakJoko",
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Prostitute_Brown.prefab",
            "SimplePeople_Prostitute_Brown",
            new Vector3(-25f, 0.02f, -17f),
            Quaternion.Euler(0f, 0f, 0f),
            "Pak Joko (Drug Store Koridor Selatan)",
            DeliveryRecipient.RecipientType.PakJoko,
            DeliveryMissionState.Package2_DrugStore,
            new string[]
            {
                "Halo anak muda! Saya Pak Joko. Sedang mengantar pasokan obat darurat ke apotek cabang Koridor Selatan ini?"
            },
            new string[]
            {
                "Terima kasih banyak Mas Rian! Pasokan obat ini sangat dibutuhkan warga kawasan selatan.",
                "Kalau kamu mencari Dimas, pos pemadam kebakarannya berada di Koridor Utara sebelah timur!"
            },
            new string[]
            {
                "Maaf mas, ini Drug Store Koridor Selatan. Saya tidak memesan perkakas atau paket ini! Jangan sampai salah alamat."
            },
            playerGo
        );

        // 4. DIMAS (Fire Station - Koridor Utara Timur)
        EnsureRecipientNPCLevel2(
            "NPC_Dimas",
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_StreetMan_Brown.prefab",
            "SimplePeople_StreetMan_Brown",
            new Vector3(25f, 0.02f, 17f),
            Quaternion.Euler(0f, 180f, 0f),
            "Dimas (Fire Station Koridor Utara)",
            DeliveryRecipient.RecipientType.Dimas,
            DeliveryMissionState.Package3_HardwareStore,
            new string[]
            {
                "Halo kurir! Ini pos pemadam dan siaga darurat Koridor Utara Timur. Ada paket perlengkapan darurat untuk tim kami?"
            },
            new string[]
            {
                "Sip, perlengkapan darurat tim pos utara sudah tiba! Terima kasih kurir!",
                "Gawat Rian! Tadi kulihat ada satu kotak paketmu yang terjatuh di jalan aspal dekat toko donat gang tengah saat kamu melintas cepat!",
                "Coba cepat tanyakan ke Petugas Parkir yang berjaga di persimpangan tengah untuk mengetahui lokasi persisnya!"
            },
            new string[]
            {
                "Maaf mas, ini pos siaga utara. Kami hanya menerima kiriman paket darurat tim!"
            },
            playerGo
        );

        // 5. PETUGAS PARKIR (Persimpangan Tengah)
        EnsureRecipientNPCLevel2(
            "NPC_PetugasParkir",
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_RoadWorker_White.prefab",
            "SimplePeople_RoadWorker_White",
            new Vector3(0f, 0.02f, 10f),
            Quaternion.Euler(0f, 180f, 0f),
            "Petugas Parkir (Persimpangan Tengah)",
            DeliveryRecipient.RecipientType.PetugasParkir,
            DeliveryMissionState.Package4_DroppedPackage,
            new string[]
            {
                "Halo Rian! Hati-hati ya, kedua koridor jalan raya dilintasi mobil-mobil yang melaju kencang.",
                "Gunakan trotoar pejalan kaki dan gang liku tengah untuk rute yang lebih aman!"
            },
            new string[]
            {
                "Hei Rian! Tadi kulihat satu kotak paket kardusmu terjatuh di aspal gang tengah dekat toko donat saat kamu berbelok!",
                "Cepat lari ke gang tengah dekat toko donat dan ambil paket kardus itu dengan tombol [E]!"
            },
            new string[]
            {
                "Saya petugas pengatur lalu lintas persimpangan jalan utama. Banyak mobil melintas, hati-hati menyeberang jalan!"
            },
            playerGo
        );

        // 6. NAYA (Apartemen Merah - Koridor Selatan Timur)
        EnsureRecipientNPCLevel2(
            "NPC_Naya",
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_BeachBabe_Black.prefab",
            "SimplePeople_BeachBabe_Black",
            new Vector3(25f, 0.02f, -17f),
            Quaternion.Euler(0f, 0f, 0f),
            "Naya (Apartemen Koridor Selatan)",
            DeliveryRecipient.RecipientType.Naya,
            DeliveryMissionState.Package5_Apartment,
            new string[]
            {
                "Halo, aku Naya. Apakah kamu membawa dokumen penting yang dikirimkan keluargaku ke apartemen merah Koridor Selatan Timur ini?"
            },
            new string[]
            {
                "Syukurlah dokumen penting ini sampai juga tepat waktu di apartemen kami! Terima kasih banyak Mas Rian!",
                "Semua tugas paketmu sudah selesai! Lekas kembali ke Pak Beni di depan Mega Mall untuk menyelesaikan tugas malam ini!"
            },
            new string[]
            {
                "Maaf mas, saya Naya di apartemen selatan timur. Saya hanya menunggu kiriman dokumen penting keluarga saya!"
            },
            playerGo
        );
    }

    private static GameObject EnsureRecipientNPCLevel2(
        string objectName,
        string prefabPath,
        string sceneSearchName,
        Vector3 position,
        Quaternion rotation,
        string displayName,
        DeliveryRecipient.RecipientType type,
        DeliveryMissionState missionState,
        string[] introLines,
        string[] successLines,
        string[] wrongLines,
        GameObject playerGo)
    {
        GameObject npcGo = FindManualRecipientNPC(type, objectName, sceneSearchName, position);
        if (npcGo == null)
        {
#if UNITY_EDITOR
            GameObject pref = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (pref == null)
            {
                pref = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_StreetMan_Brown.prefab");
            }
            if (pref != null)
            {
                npcGo = (GameObject)PrefabUtility.InstantiatePrefab(pref);
                if (npcGo == null) npcGo = Object.Instantiate(pref);
            }
#endif
        }

        if (npcGo == null) return null;

        npcGo.SetActive(true);
        npcGo.tag = "Untagged";
        npcGo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        npcGo.transform.position = position;
        npcGo.transform.rotation = rotation;

        PlayerController pc = npcGo.GetComponent<PlayerController>();
        if (pc != null) DestroyComponentSafe(pc);
        CharacterController cc = npcGo.GetComponent<CharacterController>();
        if (cc != null) DestroyComponentSafe(cc);

        NPCWalker walker = npcGo.GetComponent<NPCWalker>();
        if (walker != null) DestroyComponentSafe(walker);

        EnsureCorrectCharacterMesh(npcGo);
        AutoEnsureCharacterMaterial(npcGo);

        Rigidbody rb = npcGo.GetComponent<Rigidbody>();
        if (rb == null) rb = npcGo.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        CapsuleCollider col = npcGo.GetComponent<CapsuleCollider>();
        if (col == null) col = npcGo.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.center = new Vector3(0f, 0.9f, 0f);
        col.radius = 0.35f;

        Animator anim = npcGo.GetComponent<Animator>();
        if (anim == null) anim = npcGo.AddComponent<Animator>();
        anim.applyRootMotion = false;
#if UNITY_EDITOR
        if (anim.runtimeAnimatorController == null)
        {
            RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        }
#endif
        SafeSetAnimatorIdle(anim);
        if (anim.runtimeAnimatorController != null)
        {
            anim.Play("Idle", 0, 0f);
            anim.Update(0f);
        }

        DeliveryRecipient recipient = npcGo.GetComponent<DeliveryRecipient>();
        if (recipient == null) recipient = npcGo.AddComponent<DeliveryRecipient>();
        recipient.characterName = displayName;
        recipient.recipientType = type;
        recipient.targetForMission = missionState;
        recipient.introDialogueLines = introLines;
        recipient.successDialogueLines = successLines;
        recipient.wrongDeliveryLines = wrongLines;
        recipient.interactionRadius = (type == DeliveryRecipient.RecipientType.PetugasParkir) ? 2.0f : 3.5f;

        return npcGo;
    }

    private static void SetupDroppedPackageItemLevel2()
    {
        GameObject pkgGo = null;
        GameObject[] allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allGo)
        {
            if (go == null) continue;
            if (go.name.Equals("CardboardBox_02") || go.name.Contains("CardboardBox_02"))
            {
                pkgGo = go;
                break;
            }
        }

        if (pkgGo == null)
        {
#if UNITY_EDITOR
            GameObject boxPref = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleProps/_Prefabs/CardboardBox_02.prefab");
            if (boxPref != null)
            {
                pkgGo = (GameObject)PrefabUtility.InstantiatePrefab(boxPref);
                if (pkgGo == null) pkgGo = Object.Instantiate(boxPref);
            }
#endif
            if (pkgGo == null)
            {
                pkgGo = new GameObject("CardboardBox_02");
            }
        }

        // Posisi baru di Level 2: di jalan aspal gang tengah dekat toko donat (10, 0.026, 3)
        pkgGo.transform.position = new Vector3(10f, 0.026f, 3f);
        pkgGo.transform.rotation = Quaternion.identity;

        MeshRenderer rend = pkgGo.GetComponentInChildren<MeshRenderer>();
#if UNITY_EDITOR
        if (rend != null)
        {
            Material boxMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleProps/_Materials/CardboardBox.mat");
            if (boxMat != null)
            {
                rend.sharedMaterial = boxMat;
            }
        }
#endif

        BoxCollider bc = pkgGo.GetComponent<BoxCollider>();
        if (bc == null) bc = pkgGo.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(1.5f, 1.5f, 1.5f);
        bc.center = new Vector3(0f, 0.25f, 0f);

        DroppedPackageItem drop = pkgGo.GetComponent<DroppedPackageItem>();
        if (drop == null) drop = pkgGo.AddComponent<DroppedPackageItem>();
        drop.interactionRadius = 2.5f;

        DeliveryGameManager gm = DeliveryGameManager.Instance;
        bool isRevealed = (gm != null && gm.IsDroppedPackageRevealed && !gm.IsDroppedPackagePickedUp);
        drop.SetPackageVisible(isRevealed);
    }

    private static void SetupLevel2RoadObstacles()
    {
        GameObject obstaclesRoot = GameObject.Find("Level2_RoadObstacles");
        if (obstaclesRoot == null)
        {
            obstaclesRoot = new GameObject("Level2_RoadObstacles");
        }

        // Barikade 1: Menutup jalur lurus utara (North Avenue)
        CreateRoadBarricade(obstaclesRoot, new Vector3(-8f, 0.02f, 20f), Quaternion.Euler(0f, 0f, 0f), "Barikade Jalur Utara");

        // Barikade 2: Menutup jalur lurus selatan (South Avenue)
        CreateRoadBarricade(obstaclesRoot, new Vector3(8f, 0.02f, -20f), Quaternion.Euler(0f, 0f, 0f), "Barikade Jalur Selatan");
    }

    private static void CreateRoadBarricade(GameObject parent, Vector3 pos, Quaternion rot, string name)
    {
        Transform existing = parent.transform.Find(name);
        if (existing != null) return;

        GameObject barGo = new GameObject(name);
        barGo.transform.SetParent(parent.transform);
        barGo.transform.position = pos;
        barGo.transform.rotation = rot;

        BoxCollider bc = barGo.AddComponent<BoxCollider>();
        bc.size = new Vector3(3.5f, 1.5f, 0.8f);
        bc.center = new Vector3(0f, 0.75f, 0f);

#if UNITY_EDITOR
        GameObject signPref = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleProps/_Prefabs/SignFolding_01.prefab");
        if (signPref != null)
        {
            GameObject signInstance = (GameObject)PrefabUtility.InstantiatePrefab(signPref, barGo.transform);
            if (signInstance != null)
            {
                signInstance.transform.localPosition = Vector3.zero;
                signInstance.transform.localRotation = Quaternion.identity;
                signInstance.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
            }
        }
#endif
    }

    private static void CleanupLegacyModuleObjects()
    {
        string[] oldNames = new string[]
        {
            "Quizmaster Steve",
            "ExitPortal",
            "CutsceneTriggerZone_DonutShop",
            "Cutscene_Manager",
            "Canvas_CutsceneIntro",
            "Canvas_FadeTransition",
            "UI_CutsceneBanner",
            "DollyTrack_DonutShop",
            "CM_vcam_Wide",
            "CM_vcam_Dolly",
            "CM_vcam_Waitress",
            "RianDeliveryCutscene_System",
            "[MODULE8_CUTSCENE]",
            "CM_Delivery_Shot1_PakBeni",
            "CM_Delivery_Shot2_ClaraCoffee",
            "CM_Delivery_Shot3_CityStreet"
        };

        foreach (var name in oldNames)
        {
            GameObject go = GameObject.Find(name);
            while (go != null)
            {
                go.SetActive(false);
                go.name = "_LegacyDisabled_" + name;
                go = GameObject.Find(name);
            }
        }

        GameObject ssm = GameObject.Find("SubSceneManager");
        if (ssm != null)
        {
            ssm.SetActive(false);
        }

        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = Object.FindAnyObjectByType<Camera>();
        if (mainCam != null)
        {
            var brain = mainCam.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            if (brain != null) brain.enabled = false;
        }

        // Bersihkan karakter tersembunyi di antara gedung atau duplikat Tourist (JANGAN PERNAH menyentuh Player / Rian)
        GameObject[] allObjs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var obj in allObjs)
        {
            if (obj == null) continue;

            // Proteksi mutlak: Jangan pernah menghapus karakter pemain Rian
            if (obj.CompareTag("Player") || obj.GetComponent<PlayerController>() != null ||
                obj.name == "Rian" || obj.name.StartsWith("Rian") || obj.name.Contains("Trucker"))
            {
                continue;
            }

            string n = obj.name;
            if (n.Contains("Tourist"))
            {
                DestroyGameObjectSafe(obj);
                continue;
            }
            Vector3 pos = obj.transform.position;
            if (pos.x >= 299f && pos.x <= 305f && pos.z >= -15f && pos.z <= -9f)
            {
                if (n.Contains("Tourist") || n.Contains("Citizen"))
                {
                    DestroyGameObjectSafe(obj);
                }
            }
        }
    }

    private static void SetupKurirRecipients(GameObject playerGo)
    {
        // 1. PAK BENI (Gambar 1: SimplePeople_Sheriff_Black - Pusat Pengiriman di Depan Mall)
        EnsureRecipientNPC(
            "NPC_PakBeni",
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Sheriff_Black.prefab",
            "SimplePeople_Sheriff_Black",
            new Vector3(310.05f, 0.02f, 6.06f),
            Quaternion.Euler(0f, 180f, 0f),
            "Pak Beni (Pusat Pengiriman)",
            DeliveryRecipient.RecipientType.PakBeni,
            DeliveryMissionState.Prolog,
            new string[]
            {
                "Rian! Ini lima paket terakhir untuk malam ini. Semua toko dan apartemen akan segera tutup.",
                "Label alamat beberapa paket agak basah terkena air hujan. Kamu harus membaca papan nama toko dan bertanya pada warga sekitar.",
                "Waktumu 6 menit! Paket pertama harus sampai ke pegawai kedai kopi (Coffee Shop) di trotoar timur sebelum mereka tutup."
            },
            new string[]
            {
                "Luar biasa Rian! Semua lima paket telah berhasil diantarkan tepat waktu.",
                "Kamu telah membuktikan kerja keras, kecepatan, dan tanggung jawabmu malam ini.",
                "Mulai besok, kamu resmi dipercaya menjadi kurir tetap kami!"
            },
            new string[]
            {
                "Rian, cepat selesaikan pengiriman paket-paket itu sebelum batas waktumu habis!"
            },
            playerGo
        );

        // 2. CLARA (Gambar 2: SimplePeople_Waitress_White - Coffee Shop Trotoar Timur)
        EnsureRecipientNPC(
            "NPC_Clara",
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Waitress_White.prefab",
            "SimplePeople_Waitress_White",
            new Vector3(315.35f, 0.02f, 9.01f),
            Quaternion.Euler(0f, -140f, 0f),
            "Clara (Coffee Shop)",
            DeliveryRecipient.RecipientType.Clara,
            DeliveryMissionState.Package1_CoffeeShop,
            new string[]
            {
                "Selamat datang di Coffee Shop! Apakah kamu membawa paket biji kopi pesanan kami?"
            },
            new string[]
            {
                "Halo! Akhirnya paket biji kopi pesananku sampai juga tepat sebelum kedai kami tutup. Terima kasih banyak ya, Mas Rian!",
                "Oh ya, tadi kulihat Pak Joko pemilik Drug Store sedang menunggu kiriman obat di deretan pertokoan barat jalan ini."
            },
            new string[]
            {
                "Maaf mas, ini kedai kopi (Coffee Shop). Saya tidak memesan paket ini! Coba periksa papan nama toko lain."
            },
            playerGo
        );

        // 3. PAK JOKO (Gambar 3: SimplePeople_Prostitute_Brown - Drug Store Sudut Pertokoan Barat)
        EnsureRecipientNPC(
            "NPC_PakJoko",
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Prostitute_Brown.prefab",
            "SimplePeople_Prostitute_Brown",
            new Vector3(315.69f, 0.02f, 3.82f),
            Quaternion.Euler(0f, -90f, 0f),
            "Pak Joko (Drug Store)",
            DeliveryRecipient.RecipientType.PakJoko,
            DeliveryMissionState.Package2_DrugStore,
            new string[]
            {
                "Halo anak muda, saya Pak Joko pemilik Drug Store. Sedang mencari toko obat?"
            },
            new string[]
            {
                "Terima kasih banyak, Mas Rian! Pasokan obat dan vitamin ini sangat ditunggu warga untuk besok pagi.",
                "Kalau kamu mencari Dimas petugas pemadam kebakaran, ia sedang berjaga di pos Fire Station di seberang jalan sebelah barat ini."
            },
            new string[]
            {
                "Maaf mas, ini Drug Store. Saya tidak memesan perkakas atau paket ini! Jangan sampai salah alamat."
            },
            playerGo
        );

        // 4. DIMAS (Gambar 4: SimplePeople_StreetMan_Brown - Pegawai Fire Station)
        EnsureRecipientNPC(
            "NPC_Dimas",
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_StreetMan_Brown.prefab",
            "SimplePeople_StreetMan_Brown",
            new Vector3(301.37f, 0.02f, 5.96f),
            Quaternion.Euler(0f, 280f, 0f),
            "Dimas (Fire Station)",
            DeliveryRecipient.RecipientType.Dimas,
            DeliveryMissionState.Package3_HardwareStore,
            new string[]
            {
                "Halo kurir! Ini pos pemadam kebakaran (Fire Station). Ada kiriman perlengkapan darurat untuk tim kami?"
            },
            new string[]
            {
                "Wah pas sekali, perlengkapan darurat dan suku cadang pesanan tim pemadam sudah tiba. Terima kasih kurir!",
                "Gawat Rian! Tadi kulihat ada satu kotak paketmu yang terjatuh di jalan saat kamu terburu-buru!",
                "Coba cepat tanya Petugas Parkir di area parkir sana, dia yang mengawasi jalan dan tahu persis di mana paketmu terjatuh!"
            },
            new string[]
            {
                "Maaf mas, ini pos pemadam kebakaran (Fire Station). Kami hanya menerima kiriman paket darurat pesanan tim!"
            },
            playerGo
        );

        // 5. PETUGAS PARKIR (Gambar 6: SimplePeople_RoadWorker_White - Area Parkir)
        EnsureRecipientNPC(
            "NPC_PetugasParkir",
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_RoadWorker_White.prefab",
            "SimplePeople_RoadWorker_White",
            new Vector3(298.0f, 0.02f, -5.0f),
            Quaternion.Euler(0f, 45f, 0f),
            "Petugas Parkir",
            DeliveryRecipient.RecipientType.PetugasParkir,
            DeliveryMissionState.Package4_DroppedPackage,
            new string[]
            {
                "Halo Rian! Jalur utama kendaraan sedang padat, hati-hati saat melintas di jalan raya.",
                "Gunakan trotoar pejalan kaki dan lorong antar gedung untuk rute yang lebih cepat dan aman!",
                "Waspadalah saat menyeberang jalan raya, kendaraan bergerak kencang di sekitar sini!"
            },
            new string[]
            {
                "Hei kurir! Tadi kulihat kotak paket kardusmu terjatuh di jalan aspal dekat pembatas beton sebelah pos pemadam kebakaran (Fire Station) saat kamu melintas.",
                "Cepat kembali ke sana dan ambil paket kardus itu dengan tombol [E] sebelum terlindas kendaraan!"
            },
            new string[]
            {
                "Saya petugas parkir yang mengatur lalu lintas kendaraan di sini. Rute trotoar dan lorong pintas lebih aman untuk dilewati!"
            },
            playerGo
        );

        // 6. NAYA (Gambar 5: SimplePeople_BeachBabe_Black - Penghuni Apartemen)
        EnsureRecipientNPC(
            "NPC_Naya",
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_BeachBabe_Black.prefab",
            "SimplePeople_BeachBabe_Black",
            new Vector3(311.15f, 0.02f, -4.42f),
            Quaternion.Euler(0f, 90f, 0f),
            "Naya (Penghuni Apartemen)",
            DeliveryRecipient.RecipientType.Naya,
            DeliveryMissionState.Package5_Apartment,
            new string[]
            {
                "Halo, ada yang bisa kubantu? Aku sedang menunggu paket kiriman penting di depan apartemen ini."
            },
            new string[]
            {
                "Halo Mas Rian! Syukurlah paket dokumen pentingku sampai malam ini juga. Terima kasih banyak atas kerja kerasmu!",
                "Semua paketmu sudah selesai terkirim! Sekarang cepatlah kembali ke Pak Beni di area parkir sebelum batas waktu 6 menitmu habis!"
            },
            new string[]
            {
                "Maaf mas, saya Naya penghuni apartemen. Saya hanya menunggu kiriman dokumen penting keluarga saya!"
            },
            playerGo
        );
    }

    private static GameObject FindManualRecipientNPC(
        DeliveryRecipient.RecipientType type,
        string objectName,
        string sceneSearchName,
        Vector3 defaultPosition)
    {
        // 1. Cari di antara semua GameObject di scene (termasuk yang sedang nonaktif)
        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);

        // a. Prioritas 0: Cocokkan nama persis sceneSearchName atau objectName pada root scene
        foreach (var go in all)
        {
            if (go == null || go.transform.parent != null) continue;
            if (go.CompareTag("Player") || go.GetComponent<PlayerController>() != null) continue;
            string n = go.name;
            if ((!string.IsNullOrEmpty(sceneSearchName) && n.Equals(sceneSearchName, System.StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(objectName) && n.Equals(objectName, System.StringComparison.OrdinalIgnoreCase)))
            {
                return go;
            }
        }

        // b. Prioritas 1: Cocokkan nama spesifik yang diberikan atau dinamai manual oleh pengguna
        // Jika ada beberapa yang cocok, pilih yang paling dekat dengan defaultPosition
        GameObject bestNamed = null;
        float bestNamedDist = float.MaxValue;

        for (int pass = 0; pass < 2; pass++)
        {
            foreach (var go in all)
            {
                if (go == null) continue;
                if (go.name.StartsWith("CH_") || go.name.Contains("_jnt")) continue;
                if (go.CompareTag("Player") || go.GetComponent<PlayerController>() != null) continue;
                if (go.transform.parent != null && (go.transform.parent.name.StartsWith("SimplePeople") || go.transform.parent.name.StartsWith("SimpleCitizens"))) continue;

                if (pass == 0 && go.transform.parent != null) continue;

                string n = go.name;
                bool match = false;

                switch (type)
                {
                    case DeliveryRecipient.RecipientType.Clara:
                        match = (n.Equals("Clara", System.StringComparison.OrdinalIgnoreCase) ||
                                n.Contains("Clara") ||
                                n.Equals("SimplePeople_Waitress_White", System.StringComparison.OrdinalIgnoreCase) ||
                                n.Contains("Waitress_White") ||
                                (n.Contains("Waitress") && !n.Contains("Sarah")));
                        break;
                    case DeliveryRecipient.RecipientType.PakBeni:
                        match = n.Equals("Pak Beni", System.StringComparison.OrdinalIgnoreCase) ||
                                n.Equals("PakBeni", System.StringComparison.OrdinalIgnoreCase) ||
                                n.Contains("Beni") ||
                                n.Contains("Sheriff");
                        break;
                    case DeliveryRecipient.RecipientType.PakJoko:
                        match = n.Equals("Pak Joko", System.StringComparison.OrdinalIgnoreCase) ||
                                n.Equals("PakJoko", System.StringComparison.OrdinalIgnoreCase) ||
                                n.Contains("Joko") ||
                                n.Contains("Prostitute_Brown");
                        break;
                    case DeliveryRecipient.RecipientType.Dimas:
                        match = n.Equals("Dimas", System.StringComparison.OrdinalIgnoreCase) ||
                                n.Contains("Dimas") ||
                                n.Contains("StreetMan_Brown");
                        break;
                    case DeliveryRecipient.RecipientType.PetugasParkir:
                        match = n.Equals("Petugas Parkir", System.StringComparison.OrdinalIgnoreCase) ||
                                n.Equals("PetugasParkir", System.StringComparison.OrdinalIgnoreCase) ||
                                n.Contains("Parkir") ||
                                n.Contains("RoadWorker");
                        break;
                    case DeliveryRecipient.RecipientType.Naya:
                        match = n.Equals("Naya", System.StringComparison.OrdinalIgnoreCase) ||
                                n.Contains("Naya") ||
                                n.Contains("BeachBabe");
                        break;
                }

                if (match)
                {
                    float d = Vector3.Distance(go.transform.position, defaultPosition);
                    if (d < bestNamedDist)
                    {
                        bestNamedDist = d;
                        bestNamed = go;
                    }
                }
            }

            if (bestNamed != null) return bestNamed;
        }

        // c. Prioritas 2: Cari objek karakter di dekat posisi target (radius 5.0m)
        GameObject closest = null;
        float minDist = 5.0f;
        foreach (var go in all)
        {
            if (go == null) continue;
            if (go.name.StartsWith("CH_") || go.name.Contains("_jnt")) continue;
            if (go.CompareTag("Player") || go.GetComponent<PlayerController>() != null) continue;
            if (go.transform.parent != null && (go.transform.parent.name.StartsWith("SimplePeople") || go.transform.parent.name.StartsWith("SimpleCitizens"))) continue;

            bool isChar = go.GetComponent<Animator>() != null || 
                          go.name.StartsWith("SimplePeople") || 
                          go.name.StartsWith("SimpleCitizens") ||
                          go.GetComponent<DeliveryRecipient>() != null;

            if (isChar)
            {
                float d = Vector3.Distance(go.transform.position, defaultPosition);
                if (d < minDist)
                {
                    minDist = d;
                    closest = go;
                }
            }
        }

        if (closest != null) return closest;

        // d. Fallback ke objectName atau sceneSearchName
        GameObject fallback = GameObject.Find(objectName);
        if (fallback == null && !string.IsNullOrEmpty(sceneSearchName))
        {
            fallback = GameObject.Find(sceneSearchName);
        }
        return fallback;
    }

    private static GameObject EnsureRecipientNPC(
        string objectName,
        string prefabPath,
        string sceneSearchName,
        Vector3 position,
        Quaternion rotation,
        string displayName,
        DeliveryRecipient.RecipientType type,
        DeliveryMissionState missionState,
        string[] introLines,
        string[] successLines,
        string[] wrongLines,
        GameObject playerGo)
    {
        GameObject npcGo = FindManualRecipientNPC(type, objectName, sceneSearchName, position);
        bool wasAlreadyInScene = (npcGo != null);

        if (npcGo == null)
        {
#if UNITY_EDITOR
            GameObject pref = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (pref == null)
            {
                pref = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_StreetMan_Brown.prefab");
            }
            if (pref != null)
            {
                npcGo = (GameObject)PrefabUtility.InstantiatePrefab(pref);
                if (npcGo == null) npcGo = Object.Instantiate(pref);
            }
#endif
        }

        if (npcGo == null) return null;

        npcGo.SetActive(true);
        npcGo.tag = "Untagged";
        npcGo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        if (!wasAlreadyInScene)
        {
            npcGo.transform.position = position;
            npcGo.transform.rotation = rotation;
            npcGo.name = objectName;
        }
        else
        {
            // PERTAHANKAN POSISI DAN ROTASI YANG SUDAH DITENTUKAN MANUAL OLEH PENGGUNA DI SCENE
            Vector3 curPos = npcGo.transform.position;
            if (curPos.y < 0.0f || float.IsNaN(curPos.y)) curPos.y = 0.02f;
            npcGo.transform.position = curPos;

            // Bersihkan instansiasi duplikat bernama sama jika ada
            if (!string.IsNullOrEmpty(objectName) && npcGo.name != objectName)
            {
                GameObject staleDup = GameObject.Find(objectName);
                if (staleDup != null && staleDup != npcGo)
                {
                    DestroyGameObjectSafe(staleDup);
                }
            }
        }

        PlayerController pc = npcGo.GetComponent<PlayerController>();
        if (pc != null) DestroyComponentSafe(pc);
        CharacterController cc = npcGo.GetComponent<CharacterController>();
        if (cc != null) DestroyComponentSafe(cc);

        // Hapus NPCWalker agar penerima misi tidak patroli atau menggerakkan kaki
        NPCWalker walker = npcGo.GetComponent<NPCWalker>();
        if (walker != null) DestroyComponentSafe(walker);

        EnsureCorrectCharacterMesh(npcGo);
        AutoEnsureCharacterMaterial(npcGo);

        Rigidbody rb = npcGo.GetComponent<Rigidbody>();
        if (rb == null) rb = npcGo.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        CapsuleCollider col = npcGo.GetComponent<CapsuleCollider>();
        if (col == null) col = npcGo.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.center = new Vector3(0f, 0.9f, 0f);
        col.radius = 0.35f;

        Animator anim = npcGo.GetComponent<Animator>();
        if (anim == null) anim = npcGo.AddComponent<Animator>();
        anim.applyRootMotion = false;
#if UNITY_EDITOR
        if (anim.runtimeAnimatorController == null)
        {
            RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        }
#endif
        SafeSetAnimatorIdle(anim);
        if (anim.runtimeAnimatorController != null)
        {
            anim.Play("Idle", 0, 0f);
            anim.Update(0f);
        }

        // Bersihkan DeliveryRecipient yang salah pasang pada objek lain untuk tipe penerima yang sama
        DeliveryRecipient[] allRecipients = Object.FindObjectsByType<DeliveryRecipient>(FindObjectsInactive.Include);
        foreach (var r in allRecipients)
        {
            if (r != null && r.gameObject != npcGo && r.recipientType == type)
            {
                DestroyComponentSafe(r);
            }
        }

        DeliveryRecipient recipient = npcGo.GetComponent<DeliveryRecipient>();
        if (recipient == null) recipient = npcGo.AddComponent<DeliveryRecipient>();
        recipient.characterName = displayName;
        recipient.recipientType = type;
        recipient.targetForMission = missionState;
        recipient.introDialogueLines = introLines;
        recipient.successDialogueLines = successLines;
        recipient.wrongDeliveryLines = wrongLines;
        recipient.interactionRadius = (type == DeliveryRecipient.RecipientType.PetugasParkir) ? 2.0f : 3.5f;

        return npcGo;
    }

    private static void SetupDroppedPackageItem()
    {
        // 1. Hapus paket lama / objek duplikat jika ada
        GameObject[] allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allGo)
        {
            if (go == null) continue;
            if (go.name == "DroppedPackage_Mission4")
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(go);
                }
                else
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        // 2. Dapatkan objek paket kardus (CardboardBox_02) yang sudah diletakkan pengguna di scene
        bool wasAlreadyInScene = false;
        GameObject pkgGo = null;
        allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allGo)
        {
            if (go == null) continue;
            if (go.name.Equals("CardboardBox_02") || go.name.Contains("CardboardBox_02"))
            {
                pkgGo = go;
                wasAlreadyInScene = true;
                break;
            }
        }

        // Fallback jika tidak ditemukan dengan nama persis CardboardBox_02
        if (pkgGo == null)
        {
            foreach (var go in allGo)
            {
                if (go == null) continue;
                if (go.name.Contains("CardboardBox"))
                {
                    pkgGo = go;
                    wasAlreadyInScene = true;
                    break;
                }
            }
        }

        if (pkgGo == null)
        {
#if UNITY_EDITOR
            GameObject boxPref = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleProps/_Prefabs/CardboardBox_02.prefab");
            if (boxPref == null)
            {
                boxPref = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleProps/_Prefabs/CardboardBox_01.prefab");
            }
            if (boxPref != null)
            {
                pkgGo = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(boxPref);
                if (pkgGo == null) pkgGo = Object.Instantiate(boxPref);
            }
#endif
            if (pkgGo == null)
            {
                pkgGo = new GameObject("CardboardBox_02");
            }
            // Posisi default sesuai lokasi pada gambar referensi pengguna di scene Module 2
            pkgGo.transform.position = new Vector3(300.478f, 0.026f, 2.246f);
            pkgGo.transform.rotation = Quaternion.identity;
        }

        // PERTAHANKAN POSISI DAN ROTASI YANG SUDAH DITENTUKAN MANUAL OLEH PENGGUNA DI SCENE!
        // Jangan pernah mengubah atau menimpa lokasi paket yang sudah ditaruh pengguna di scene.
        if (wasAlreadyInScene)
        {
            Debug.Log($"[PlayerBootstrap] Mempertahankan lokasi paket manual di scene: {pkgGo.transform.position}");
        }

        // Pastikan material CardboardBox URP terpasang
        MeshRenderer rend = pkgGo.GetComponentInChildren<MeshRenderer>();
#if UNITY_EDITOR
        if (rend != null)
        {
            Material boxMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleProps/_Materials/CardboardBox.mat");
            if (boxMat != null)
            {
                rend.sharedMaterial = boxMat;
            }
        }
#endif

        BoxCollider bc = pkgGo.GetComponent<BoxCollider>();
        if (bc == null) bc = pkgGo.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(1.5f, 1.5f, 1.5f);
        bc.center = new Vector3(0f, 0.25f, 0f);

        DroppedPackageItem drop = pkgGo.GetComponent<DroppedPackageItem>();
        if (drop == null) drop = pkgGo.AddComponent<DroppedPackageItem>();
        drop.interactionRadius = 2.5f;

        // Paket baru boleh terlihat setelah Rian selesai percakapan dengan Petugas Parkir (Misi 4),
        // dan harus tetap sembunyi/hilang jika sudah pernah diambil
        DeliveryGameManager gm = DeliveryGameManager.Instance;
        bool isRevealed = (gm != null && gm.IsDroppedPackageRevealed && !gm.IsDroppedPackagePickedUp);
        drop.SetPackageVisible(isRevealed);
    }

    /// <summary>
    /// Menyiapkan karakter Player yang dikendalikan keyboard (W, A, S, D).
    /// Karakter utama adalah Rian (Kurir), menggunakan model SimplePeople_Trucker_Black
    /// (topi trucker biru-putih, singlet putih dengan suspender merah, celana robek)
    /// persis sesuai foto karakter yang diunggah pengguna.
    /// </summary>
    private static GameObject EnsurePlayerCharacter()
    {
        GameObject playerGo = null;

        // 1. Prioritas Utama: Cari karakter Trucker / Rian yang sudah ada di scene (aktif maupun inaktif)
        GameObject[] allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allGo)
        {
            if (go == null) continue;
            if (go.name == "SimplePeople_Trucker_Black" || go.name == "Rian (Kurir)" || go.name == "Rian" || 
                (go.CompareTag("Player") && !go.name.Contains("NPC")))
            {
                playerGo = go;
                break;
            }
        }

        // 2. Jika belum ada di scene, muat dari Prefab SimplePeople_Trucker_Black
        if (playerGo == null)
        {
#if UNITY_EDITOR
            GameObject truckerPref = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Trucker_Black.prefab");
            if (truckerPref == null)
            {
                truckerPref = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Trucker_White.prefab");
            }
            if (truckerPref != null)
            {
                playerGo = (GameObject)PrefabUtility.InstantiatePrefab(truckerPref);
                if (playerGo == null) playerGo = Object.Instantiate(truckerPref);
            }
#endif
        }

        // 3. Fallback jika prefab gagal dimuat
        if (playerGo == null)
        {
            GameObject existingTaggedPlayer = GameObject.FindWithTag("Player");
            if (existingTaggedPlayer != null) playerGo = existingTaggedPlayer;
        }

        if (playerGo == null)
        {
            GameObject existingNamedPlayer = GameObject.Find("Player");
            if (existingNamedPlayer != null) playerGo = existingNamedPlayer;
        }

        if (playerGo == null)
        {
            playerGo = new GameObject("Rian");
            playerGo.transform.position = new Vector3(311.26f, 0.02f, 4.94f);
            playerGo.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
        }

        if (string.IsNullOrEmpty(playerGo.name))
        {
            playerGo.name = "Rian";
        }
        playerGo.tag = "Player";
        playerGo.SetActive(true);

        // Hapus karakter tersembunyi di antara gedung atau sisa Tourist
        foreach (var go in allGo)
        {
            if (go == null || go == playerGo) continue;
            if (go.CompareTag("Player") || go.name == "Rian" || go.name.StartsWith("Rian") || go.name.Contains("Trucker")) continue;
            if (go.name.Contains("Tourist"))
            {
                DestroyGameObjectSafe(go);
                continue;
            }
            Vector3 p = go.transform.position;
            if (p.x >= 299f && p.x <= 305f && p.z >= -15f && p.z <= -9f)
            {
                if (go.name.Contains("Tourist") || go.name.Contains("Citizen"))
                {
                    DestroyGameObjectSafe(go);
                }
            }
        }

        // Pastikan SEMUA karakter selain Player tidak ber-tag Player dan tidak memiliki PlayerController / CharacterController
        GameObject[] allCharacters = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var c in allCharacters)
        {
            if (c != null && c != playerGo)
            {
                if (c.CompareTag("Player")) c.tag = "Untagged";
                PlayerController oldPc = c.GetComponent<PlayerController>();
                if (oldPc != null) DestroyComponentSafe(oldPc);
                CharacterController oldCc = c.GetComponent<CharacterController>();
                if (oldCc != null) DestroyComponentSafe(oldCc);
            }
        }

        // Pertahankan posisi & rotasi Rian 100% persis sesuai dengan yang diatur pengguna di Scene
        Vector3 currentPos = playerGo.transform.position;
        if (float.IsNaN(currentPos.x) || float.IsNaN(currentPos.y) || float.IsNaN(currentPos.z))
        {
            currentPos = new Vector3(311.26f, 0.02f, 4.94f);
            playerGo.transform.position = currentPos;
        }
        playerGo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        // Pastikan hanya sub-mesh Trucker yang aktif (sesuai foto referensi)
        EnsureCorrectCharacterMesh(playerGo);

        // Pasang material URP bertekstur asli SimplePeople_Trucker_Black
        EnsureCharacterMaterial(playerGo, "SimplePeople_Redneck_Black", 
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_Trucker_Black.mat");

        // Hapus NPCWalker dari Player jika ada
        NPCWalker walker = playerGo.GetComponent<NPCWalker>();
        if (walker != null) DestroyComponentSafe(walker);

        // Pasang / Konfigurasi Animator
        Animator anim = playerGo.GetComponent<Animator>();
        if (anim == null) anim = playerGo.AddComponent<Animator>();
        anim.applyRootMotion = false;
        anim.enabled = true;

#if UNITY_EDITOR
        if (anim.runtimeAnimatorController == null)
        {
            RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleCitizens/Models/SimpleCitizens.controller");
            if (ctrl == null)
            {
                ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            }
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        }
#endif

        // Pasang Capsule Collider
        CapsuleCollider col = playerGo.GetComponent<CapsuleCollider>();
        if (col == null) col = playerGo.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.center = new Vector3(0f, 0.9f, 0f);
        col.radius = 0.35f;

        // Pasang Rigidbody Kinematic
        Rigidbody rb = playerGo.GetComponent<Rigidbody>();
        if (rb == null) rb = playerGo.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Pasang PlayerController (WASD Keyboard Control)
        PlayerController pc = playerGo.GetComponent<PlayerController>();
        if (pc == null) pc = playerGo.AddComponent<PlayerController>();
        pc.walkSpeed = 0.45f;
        pc.sprintSpeed = 0.95f;
        pc.rotationSpeed = 12f;
        pc.jumpHeight = 0.25f;
        pc.playerCamera = Camera.main;

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(playerGo);
#endif

        SetupCamera(playerGo);

        return playerGo;
    }

    /// <summary>
    /// Menyiapkan kamera: Fokus pada 1 Pemain Inti (Third-Person Follow).
    /// </summary>
    private static void SetupCamera(GameObject playerGo)
    {
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
        if (cam == null) return;

        ThirdPersonCamera tpc = cam.GetComponent<ThirdPersonCamera>();
        if (tpc == null) tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();

        tpc.viewMode = ThirdPersonCamera.CameraViewMode.ThirdPersonFollow; // Fokus pada Pemain Inti
        tpc.target = playerGo.transform;
        tpc.targetHeight = 0.18f;
        tpc.distance = 1.35f;
        tpc.height = 0.45f;
        tpc.followSpeed = 12f;
        tpc.followRearAngle = true;
        tpc.rotationFollowSpeed = 6f;
        tpc.enableMouseLook = true;
        tpc.showControlsUI = false;
        tpc.SnapToTarget();

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(cam.gameObject);
#endif
    }

    /// <summary>
    /// Menyiapkan 2 NPC mandiri sesuai permintaan pengguna:
    /// 1. NPC Wanita baju kuning di trotoar timur: Melompat di tempat terus-menerus, menyapa saat Player mendekat.
    /// 2. NPC Pria: Berjalan kaki di trotoar barat melawan arah pergerakan Player (berjalan ke arah selatan).
    /// Keduanya otonom tanpa kontrol keyboard / user.
    /// </summary>
    public static void SetupAllNPCs(GameObject playerGo)
    {
        GameObject[] allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);

        // 1. CARI LANGSUNG KARAKTER DI SCENE BERDASARKAN NAMA (Mendukung Objek Aktif Maupun Nonaktif)
        GameObject femaleNpc = null;
        GameObject maleNpc = null;

        foreach (var go in allGo)
        {
            if (go == playerGo) continue;
            if (go.name.StartsWith("CH_") || go.name.Contains("_jnt")) continue;
            if (go.name.Contains("Steve") || go.name.Contains("Quiz")) continue;

            if (femaleNpc == null && (go.name == "SimplePeople_Waitress_White" || 
                                     go.name == "NPC_Wanita_Melompat" || 
                                     go.name == "NPC_Wanita_Duduk"))
            {
                femaleNpc = go;
            }

            if (maleNpc == null && (go.name == "SimplePeople_Pimp_Brown" || 
                                   go.name == "NPC_Pria_PejalanKaki"))
            {
                maleNpc = go;
            }
        }

        // 2. PENCARIAN FLEKSIBEL JIKA MENGGUNAKAN NAMA / PREFAB LAIN
        if (femaleNpc == null || maleNpc == null)
        {
            foreach (var go in allGo)
            {
                if (go == playerGo) continue;
                if (go == femaleNpc || go == maleNpc) continue;
                if (go.name.Contains("Steve") || go.name.Contains("Quiz")) continue;
                // Abaikan sub-mesh CH_ atau objek anak
                if (go.name.StartsWith("CH_") || go.name.Contains("_jnt")) continue;
                if (go.transform.parent != null && (go.transform.parent.name.StartsWith("SimplePeople") || 
                                                   go.transform.parent.name.StartsWith("SimpleCitizens"))) continue;
                if (go.GetComponent<Animator>() == null && !go.name.StartsWith("SimplePeople") && !go.name.StartsWith("SimpleCitizens")) continue;

                Vector3 pos = go.transform.position;
                // Cek apakah di koridor jalan utama kota (X: 315 - 322, Z: -25 - +15)
                if (pos.x >= 315.0f && pos.x <= 322.0f && pos.z >= -25.0f && pos.z <= 15.0f)
                {
                    if (femaleNpc == null && pos.x >= 318.0f)
                    {
                        femaleNpc = go;
                    }
                    else if (maleNpc == null && pos.x < 318.0f)
                    {
                        maleNpc = go;
                    }
                }
            }
        }

        // 3. Fallback jika salah satu belum ada di scene
        if (femaleNpc == null)
        {
#if UNITY_EDITOR
            string[] femalePrefabs = new string[]
            {
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Waitress_White.prefab",
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_HouseWife_White.prefab"
            };
            foreach (var p in femalePrefabs)
            {
                GameObject pref = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (pref != null)
                {
                    femaleNpc = Object.Instantiate(pref);
                    femaleNpc.name = "SimplePeople_Waitress_White";
                    break;
                }
            }
#endif
        }

        if (maleNpc == null)
        {
#if UNITY_EDITOR
            string[] malePrefabs = new string[]
            {
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Pimp_Brown.prefab",
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_StreetMan_White.prefab"
            };
            foreach (var p in malePrefabs)
            {
                GameObject pref = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (pref != null)
                {
                    maleNpc = Object.Instantiate(pref);
                    maleNpc.name = "SimplePeople_Pimp_Brown";
                    break;
                }
            }
#endif
        }

        // 4. KONFIGURASI SEMUA NPC
        if (femaleNpc != null)
        {
            ConfigureFemaleJumpingNPC(femaleNpc, playerGo);
        }

        if (maleNpc != null)
        {
            ConfigureMaleWalkingNPC(maleNpc, playerGo);
        }

        // 5. KONFIGURASI NPC DI DEPAN BARBER SHOP (MENENGOK KIRI-KANAN TANPA BERJALAN)
        EnsureBarberShopNPC(playerGo);
    }

    /// <summary>
    /// Membersihkan semua joint/tulang rig karakter dari komponen yang salah dipasang (NPCWalker, Collider, dll)
    /// dan mengembalikan skala lokal tulang ke normal (1, 1, 1) agar kepala dan anggota tubuh tidak hilang/mengecil.
    /// </summary>
    public static void CleanAllJointBones()
    {
        GameObject[] allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allGo)
        {
            if (go.name.EndsWith("_jnt") || go.name.Contains("_jnt"))
            {
                // Kembalikan skala lokal tulang ke normal (1, 1, 1)
                go.transform.localScale = Vector3.one;

                // Hapus komponen yang salah dipasang di tulang rig
                NPCWalker w = go.GetComponent<NPCWalker>();
                if (w != null) DestroyComponentSafe(w);

                Animator a = go.GetComponent<Animator>();
                if (a != null) DestroyComponentSafe(a);

                Rigidbody rb = go.GetComponent<Rigidbody>();
                if (rb != null) DestroyComponentSafe(rb);

                Collider col = go.GetComponent<Collider>();
                if (col != null) DestroyComponentSafe(col);

                PlayerController pc = go.GetComponent<PlayerController>();
                if (pc != null) DestroyComponentSafe(pc);
            }
        }
    }

    /// <summary>
    /// Menyiapkan NPC di depan Barber Shop agar berdiri diam dan menengok kiri-kanan secara alami (tanpa berjalan).
    /// </summary>
    public static GameObject EnsureBarberShopNPC(GameObject playerGo)
    {
        // Bersihkan tulang rig terlebih dahulu agar kepala tidak hilang
        CleanAllJointBones();

        GameObject[] allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);

        // 1. Cari karakter di depan Barber Shop berdasarkan nama (aktif atau non-aktif)
        GameObject barberNpc = null;
        foreach (var go in allGo)
        {
            if (go == playerGo) continue;
            if (go.name.StartsWith("CH_") || go.name.Contains("_jnt")) continue;

            if (go.name == "SimplePeople_Prostitute_White" ||
                go.name == "SimplePeople_Punk_White" ||
                go.name == "NPC_BarberShop" ||
                go.name == "NPC_Barber_Customer")
            {
                barberNpc = go;
                break;
            }
        }

        if (barberNpc == null)
        {
            foreach (var go in allGo)
            {
                if (go == playerGo) continue;
                // JANGAN PERNAH memilih tulang/joint atau submesh CH_
                if (go.name.Contains("_jnt") || go.name.StartsWith("CH_")) continue;
                // Karakter harus root GameObject atau anak dari People
                if (go.transform.parent != null && go.transform.parent.name != "People") continue;
                if (go.name.Contains("Building") || go.name.Contains("BaberShop")) continue;

                Vector3 pos = go.transform.position;
                // Area depan Barber Shop (X: 303 - 312, Z: 4.5 - 11.5)
                if (pos.x >= 303.0f && pos.x <= 312.0f && pos.z >= 4.5f && pos.z <= 11.5f)
                {
                    barberNpc = go;
                    break;
                }
            }
        }

        // Fallback: Jika belum ada karakter di depan Barber Shop, buatkan satu dari prefab
        if (barberNpc == null)
        {
#if UNITY_EDITOR
            string[] prefabs = new string[]
            {
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Prostitute_White.prefab",
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Punk_White.prefab",
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleCitizens/Prefabs/SimpleCitizens_Tourist_White.prefab",
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_StreetMan_White.prefab"
            };
            foreach (var p in prefabs)
            {
                GameObject pref = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (pref != null)
                {
                    barberNpc = Object.Instantiate(pref);
                    barberNpc.name = "SimplePeople_Prostitute_White";
                    barberNpc.transform.position = new Vector3(307.44f, 0.02f, 6.85f);
                    barberNpc.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    break;
                }
            }
#endif
        }

        if (barberNpc != null)
        {
            ConfigureBarberShopNPC(barberNpc, playerGo);
        }

        return barberNpc;
    }

    /// <summary>
    /// Mengonfigurasi NPC di depan Barber Shop agar berdiri di tempat dan menengok kiri-kanan tanpa berjalan.
    /// </summary>
    private static void ConfigureBarberShopNPC(GameObject npcGo, GameObject playerGo)
    {
        npcGo.tag = "Untagged";
        npcGo.SetActive(true);

        // 1. Bersihkan dan kembalikan skala tulang rig semua anak ke normal (1, 1, 1)
        Transform[] childTransforms = npcGo.GetComponentsInChildren<Transform>(true);
        foreach (var t in childTransforms)
        {
            if (t == npcGo.transform) continue;
            if (t.name.EndsWith("_jnt") || t.name.Contains("_jnt"))
            {
                t.localScale = Vector3.one;
                NPCWalker cw = t.GetComponent<NPCWalker>();
                if (cw != null) DestroyComponentSafe(cw);
                Animator ca = t.GetComponent<Animator>();
                if (ca != null) DestroyComponentSafe(ca);
                Rigidbody crb = t.GetComponent<Rigidbody>();
                if (crb != null) DestroyComponentSafe(crb);
                Collider ccol = t.GetComponent<Collider>();
                if (ccol != null) DestroyComponentSafe(ccol);
                PlayerController cpc = t.GetComponent<PlayerController>();
                if (cpc != null) DestroyComponentSafe(cpc);
            }
        }

        // 2. Hapus komponen pengendali keyboard jika ada
        PlayerController pc = npcGo.GetComponent<PlayerController>();
        if (pc != null) DestroyComponentSafe(pc);
        CharacterController cc = npcGo.GetComponent<CharacterController>();
        if (cc != null) DestroyComponentSafe(cc);

        // Skala proporsional 0.1 pada root GameObject
        npcGo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        // Menghadap ke arah jalan/selatan (180 derajat)
        npcGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        // Pastikan posisi Y pas di permukaan tanah/trotoar (Y = 0.02f)
        Vector3 curPos = npcGo.transform.position;
        curPos.y = 0.02f;
        npcGo.transform.position = curPos;

        // Sub-mesh yang benar (CH_Prostitute aktif, menampilkan kepala/rambut dan badan lengkap)
        EnsureCorrectCharacterMesh(npcGo);

        // Pasang Tekstur & Material URP asli
        AutoEnsureCharacterMaterial(npcGo);

        // Animator dengan SimpleCharacter_5.0
        Animator anim = npcGo.GetComponent<Animator>();
        if (anim == null) anim = npcGo.AddComponent<Animator>();
        anim.applyRootMotion = false;
#if UNITY_EDITOR
        if (anim.runtimeAnimatorController == null)
        {
            RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        }
#endif
        // Set idle parameter
        SafeSetAnimatorIdle(anim);
        anim.Update(0f);

        // Rigidbody Kinematic
        Rigidbody rb = npcGo.GetComponent<Rigidbody>();
        if (rb == null) rb = npcGo.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Capsule Collider
        CapsuleCollider col = npcGo.GetComponent<CapsuleCollider>();
        if (col == null) col = npcGo.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.center = new Vector3(0f, 0.9f, 0f);
        col.radius = 0.35f;

        // NPCWalker (Menengok Kiri-Kanan Tanpa Berjalan)
        NPCWalker walker = npcGo.GetComponent<NPCWalker>();
        if (walker == null) walker = npcGo.AddComponent<NPCWalker>();
        walker.isSittingPermanently = false;
        walker.isJumpingInPlace = false;
        walker.isLookingAroundInPlace = true;
        walker.lookAroundAngle = 40f;
        walker.lookAroundSpeed = 45f;
        walker.lookPauseDuration = new Vector2(1.5f, 3.0f);
        walker.navMode = NPCWalker.NavigationMode.Stationary;
        walker.walkSpeed = 0f;
        walker.runSpeed = 0f;
        walker.idleChance = 0f;
        walker.sitChance = 0f;
        walker.enableObstacleAvoidance = false;

        // Interaksi Dialog saat Player mendekat
        walker.enablePlayerInteraction = true;
        walker.interactionDistance = 1.6f;
        walker.characterDisplayName = "Pelanggan Barber Shop";
        walker.characterLine = "Halo! Saya sedang menunggu antrean potong rambut di Barber Shop ini.";
        if (playerGo != null) walker.playerTarget = playerGo.transform;

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(npcGo);
#endif
    }

    /// <summary>
    /// Mengonfigurasi NPC Wanita baju kuning agar berdiri dan melompat di tempat di trotoar timur.
    /// Sepenuhnya otonom tanpa kontrol user, posisi berdiri di trotoar menghadap jalan raya.
    /// </summary>
    private static void ConfigureFemaleJumpingNPC(GameObject npcGo, GameObject playerGo)
    {
        npcGo.tag = "Untagged";
        npcGo.SetActive(true);

        // Hapus komponen pengendali keyboard
        PlayerController pc = npcGo.GetComponent<PlayerController>();
        if (pc != null) DestroyComponentSafe(pc);
        CharacterController cc = npcGo.GetComponent<CharacterController>();
        if (cc != null) DestroyComponentSafe(cc);

        // Skala proporsional 0.1
        npcGo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        // Posisikan berdiri di trotoar timur dekat bangku taman & air mancur
        // Berdiri tegak di permukaan trotoar (Y = 0.02f)
        npcGo.transform.position = new Vector3(318.85f, 0.02f, -8.06f);
        // Menghadap ke barat ke arah jalan raya (-90 derajat)
        npcGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        // Pastikan hanya sub-mesh wanita (CH_Woman) yang aktif
        EnsureCorrectCharacterMesh(npcGo);

        // Tekstur & Material URP asli
        EnsureCharacterMaterial(npcGo, "SimplePeople_Waitress_White",
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_Waitress_White.mat");

        // Animator
        Animator anim = npcGo.GetComponent<Animator>();
        if (anim == null) anim = npcGo.AddComponent<Animator>();
        anim.applyRootMotion = false;
#if UNITY_EDITOR
        if (anim.runtimeAnimatorController == null)
        {
            RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        }
#endif
        // Reset parameter agar berdiri tegak & siap melompat
        SafeSetAnimatorIdle(anim);
        if (anim.layerCount > 3)
        {
            anim.Play("Standing_Jump", 3, 0f);
        }
        anim.Update(0f);

        // Rigidbody Kinematic
        Rigidbody rb = npcGo.GetComponent<Rigidbody>();
        if (rb == null) rb = npcGo.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Capsule Collider berdiri normal
        CapsuleCollider col = npcGo.GetComponent<CapsuleCollider>();
        if (col == null) col = npcGo.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.center = new Vector3(0f, 0.9f, 0f);
        col.radius = 0.35f;

        // NPCWalker (Melompat di Tempat & Dialog Menyapa)
        NPCWalker walker = npcGo.GetComponent<NPCWalker>();
        if (walker == null) walker = npcGo.AddComponent<NPCWalker>();
        walker.isSittingPermanently = false;
        walker.isJumpingInPlace = true;
        walker.jumpInterval = 1.4f;
        walker.jumpInPlaceHeight = 0.18f;
        walker.navMode = NPCWalker.NavigationMode.Stationary;
        walker.walkSpeed = 0f;
        walker.runSpeed = 0f;
        walker.idleChance = 0f;
        walker.sitChance = 0f;
        walker.enableObstacleAvoidance = false;

        // Interaksi Dialog saat Player mendekat
        walker.enablePlayerInteraction = true;
        walker.interactionDistance = 1.8f;
        walker.characterDisplayName = "Wanita Melompat";
        walker.characterLine = "Halo! Lihat, aku melompat di tempat!\nAsyik sekali olahraga di trotoar ini!";
        if (playerGo != null) walker.playerTarget = playerGo.transform;

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(npcGo);
#endif
    }

    /// <summary>
    /// Mengonfigurasi NPC Pria agar berjalan kaki mandiri di trotoar barat.
    /// Berjalan melawan arah pergerakan karakter utama (dari Utara Z=+6 ke Selatan Z=-22).
    /// </summary>
    private static void ConfigureMaleWalkingNPC(GameObject npcGo, GameObject playerGo)
    {
        npcGo.tag = "Untagged";
        npcGo.SetActive(true);

        // Hapus komponen pengendali keyboard
        PlayerController pc = npcGo.GetComponent<PlayerController>();
        if (pc != null) DestroyComponentSafe(pc);
        CharacterController cc = npcGo.GetComponent<CharacterController>();
        if (cc != null) DestroyComponentSafe(cc);

        // Skala proporsional 0.1
        npcGo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        // Buat atau dapatkan jalur trotoar barat melawan arah
        // Trotoar barat berada di X = 317.35f, membentang dari Utara (Z = +6.0f) ke Selatan (Z = -22.0f)
        NPCWaypointPath pathMelawanArah = EnsureSidewalkPath("Path_Trotoar_Melawan_Arah", 317.35f, 6.0f, -22.0f, Color.yellow);

        // Mulai posisi di sisi utara trotoar menghadap ke selatan (rotasi 180 derajat)
        // Karakter utama berjalan ke utara (+Z), sehingga NPC ini berjalan melawan arah (-Z) menuju player!
        npcGo.transform.position = new Vector3(317.35f, 0.02f, 4.0f);
        npcGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        // Pastikan hanya sub-mesh karakter yang sesuai yang aktif (CH_Pimp)
        EnsureCorrectCharacterMesh(npcGo);

        // Tekstur & Material URP asli
        EnsureCharacterMaterial(npcGo, "SimplePeople_Pimp_Brown",
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_Pimp_Brown.mat");

        // Animator
        Animator anim = npcGo.GetComponent<Animator>();
        if (anim == null) anim = npcGo.AddComponent<Animator>();
        anim.applyRootMotion = false;
#if UNITY_EDITOR
        if (anim.runtimeAnimatorController == null)
        {
            RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        }
#endif
        SafeSetAnimatorIdle(anim);

        // Rigidbody Kinematic
        Rigidbody rb = npcGo.GetComponent<Rigidbody>();
        if (rb == null) rb = npcGo.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Capsule Collider
        CapsuleCollider col = npcGo.GetComponent<CapsuleCollider>();
        if (col == null) col = npcGo.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.center = new Vector3(0f, 0.9f, 0f);
        col.radius = 0.35f;

        // NPCWalker (Berjalan Melawan Arah, Anti-Tabrakan & Dialog Menyapa)
        NPCWalker walker = npcGo.GetComponent<NPCWalker>();
        if (walker == null) walker = npcGo.AddComponent<NPCWalker>();
        walker.isSittingPermanently = false;
        walker.navMode = NPCWalker.NavigationMode.WaypointPath;
        walker.waypointPath = pathMelawanArah;
        walker.endBehavior = NPCWalker.EndBehavior.PingPong;
        walker.randomStartPoint = false;
        walker.maxStartDelay = 0.0f; // Langsung jalan maju tanpa delay
        walker.walkSpeed = 0.32f;
        walker.runSpeed = 0.55f;
        walker.rotationSpeed = 8f;
        walker.waypointReachDistance = 0.2f;
        walker.idleChance = 0f; // Terus berjalan maju tanpa jeda acak
        walker.sitChance = 0f;
        walker.runChance = 0f;
        walker.turnPauseTime = 0.5f;

        // Sensor anti-tabrakan
        walker.enableObstacleAvoidance = true;
        walker.obstacleDetectionDistance = 0.45f;
        walker.bodyRadius = 0.05f;

        // Interaksi Dialog saat Player mendekat (berhenti, melambai & menyapa)
        walker.enablePlayerInteraction = true;
        walker.interactionDistance = 1.8f;
        walker.characterDisplayName = "Pria Pejalan Kaki";
        walker.characterLine = "Halo bro! Selamat beraktivitas di kota. Tetap semangat!";
        if (playerGo != null) walker.playerTarget = playerGo.transform;

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(npcGo);
#endif
    }

    /// <summary>
    /// Mengonfigurasi komponen dan parameter NPC pejalan kaki.
    /// </summary>
    /// <summary>
    /// Pria berbaju merah (SimplePeople_StreetMan_Black) sebelumnya hanya berdiri diam.
    /// Dipasangi NPCWalker dengan jalur trotoar agar berjalan bolak-balik seperti warga lainnya.
    /// </summary>
    private static void EnsureWanderingStreetMan(GameObject playerGo)
    {
        GameObject street = GameObject.Find("SimplePeople_StreetMan_Black");
        if (street == null || street.GetComponent<DeliveryRecipient>() != null) return;

        NPCWaypointPath path = null;
        GameObject pathsRoot = GameObject.Find("NPC_Paths");
        if (pathsRoot != null)
        {
            Transform t = pathsRoot.transform.Find("SidewalkPath_03");
            if (t != null) path = t.GetComponent<NPCWaypointPath>();
        }
        if (path == null)
        {
            path = EnsureSidewalkPath("Path_StreetMan_Patrol", 301.7f, -38.7f, 21.3f, Color.red);
        }
        if (path == null) return;

        NPCWalker existing = street.GetComponent<NPCWalker>();
        if (existing != null && existing.enabled && existing.navMode != NPCWalker.NavigationMode.Stationary) return;

        ConfigureNPC(street, playerGo, path, 0.32f, "Halo Rian! Semangat mengantar paketnya ya!");
        NPCWalker walker = street.GetComponent<NPCWalker>();
        if (walker != null)
        {
            walker.enabled = true;
            walker.randomStartPoint = false;
        }
    }

    private static void ConfigureNPC(GameObject npcGo, GameObject playerGo, NPCWaypointPath path, float speed, string dialogText)
    {
        // Hapus PlayerController jika ada di NPC
        PlayerController pc = npcGo.GetComponent<PlayerController>();
        if (pc != null) DestroyComponentSafe(pc);

        CharacterController cc = npcGo.GetComponent<CharacterController>();
        if (cc != null) DestroyComponentSafe(cc);

        // Animator
        Animator anim = npcGo.GetComponent<Animator>();
        if (anim == null) anim = npcGo.AddComponent<Animator>();
        anim.applyRootMotion = false;

#if UNITY_EDITOR
        if (anim.runtimeAnimatorController == null)
        {
            RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        }
#endif

        // Rigidbody Kinematic
        Rigidbody rb = npcGo.GetComponent<Rigidbody>();
        if (rb == null) rb = npcGo.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Capsule Collider
        CapsuleCollider col = npcGo.GetComponent<CapsuleCollider>();
        if (col == null) col = npcGo.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.center = new Vector3(0f, 0.9f, 0f);
        col.radius = 0.35f;

        // NPCWalker (Sensor Anti-Tabrakan & Navigasi Trotoar)
        NPCWalker walker = npcGo.GetComponent<NPCWalker>();
        if (walker == null) walker = npcGo.AddComponent<NPCWalker>();

        walker.waypointPath = path;
        walker.navMode = NPCWalker.NavigationMode.WaypointPath;
        walker.endBehavior = NPCWalker.EndBehavior.PingPong;
        walker.randomStartPoint = false;
        walker.walkSpeed = speed;
        walker.runSpeed = speed * 1.8f;
        walker.rotationSpeed = 8f;
        walker.waypointReachDistance = 0.15f;
        walker.sitChance = 0f;
        walker.runChance = 0f;
        walker.idleChance = 0.1f;
        walker.turnPauseTime = 0.5f;

        // Sensor anti tabrakan
        walker.enableObstacleAvoidance = true;
        walker.obstacleDetectionDistance = 0.45f;
        walker.bodyRadius = 0.05f;

        // Interaksi dengan Player & Balon Dialog
        walker.enablePlayerInteraction = true;
        walker.interactionDistance = 1.2f;
        walker.characterLine = dialogText;
        if (playerGo != null) walker.playerTarget = playerGo.transform;

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(npcGo);
#endif
    }

    /// <summary>
    /// Membuat atau mendapatkan jalur waypoint trotoar lurus.
    /// </summary>
    public static NPCWaypointPath EnsureSidewalkPath(string pathName, float xPos, float zStart, float zEnd, Color pathColor)
    {
        GameObject pathsRoot = GameObject.Find("NPC_Paths");
        if (pathsRoot == null)
        {
            pathsRoot = new GameObject("NPC_Paths");
        }

        Transform existing = pathsRoot.transform.Find(pathName);
        GameObject pathObj = existing != null ? existing.gameObject : new GameObject(pathName);
        pathObj.transform.SetParent(pathsRoot.transform, false);

        // Bersihkan waypoints lama jika ada
        while (pathObj.transform.childCount > 0)
        {
            Transform child = pathObj.transform.GetChild(0);
            child.SetParent(null);
            DestroyGameObjectSafe(child.gameObject);
        }

        NPCWaypointPath wpPath = pathObj.GetComponent<NPCWaypointPath>();
        if (wpPath == null) wpPath = pathObj.AddComponent<NPCWaypointPath>();

        wpPath.pathColor = pathColor;
        wpPath.waypointSize = 0.25f;
        wpPath.isLoop = false;

        // Buat 2 Titik Lurus di Trotoar
        Vector3[] points = new Vector3[]
        {
            new Vector3(xPos, 0.02f, zStart),
            new Vector3(xPos, 0.02f, zEnd)
        };

        for (int i = 0; i < points.Length; i++)
        {
            GameObject wp = new GameObject($"WP_{i + 1:D2}");
            wp.transform.SetParent(pathObj.transform, false);
            wp.transform.position = points[i];
        }

        return wpPath;
    }

    /// <summary>
    /// Memastikan hanya sub-mesh karakter yang relevan yang aktif dan semua sub-mesh lain dinonaktifkan,
    /// agar tidak terjadi tumpang-tindih mesh ganda yang menyebabkan karakter berwarna magenta/pink.
    /// Mendukung seri SimpleCitizens (SC_) dan SimplePeople (CH_).
    /// </summary>
    public static void EnsureCorrectCharacterMesh(GameObject character)
    {
        if (character == null) return;
        string cName = character.name.Replace("(Clone)", "").Trim();

        Transform[] allTransforms = character.GetComponentsInChildren<Transform>(true);

        // 1. Cek seri SimpleCitizens (submesh berawalan "SC_")
        bool hasSCMeshes = false;
        foreach (var t in allTransforms)
        {
            if (t.name.StartsWith("SC_"))
            {
                hasSCMeshes = true;
                break;
            }
        }

        if (hasSCMeshes)
        {
            bool isTouristOrRian = cName.Contains("Tourist") || cName.Contains("Rian") || character.CompareTag("Player");
            foreach (var t in allTransforms)
            {
                if (t.name.StartsWith("SC_"))
                {
                    if (isTouristOrRian)
                    {
                        // Rian (Kurir): SC_Tourist (badan + ransel kurir kuning) dan SC_Cap (topi biru terbalik) aktif
                        t.gameObject.SetActive(t.name == "SC_Tourist" || t.name == "SC_Cap");
                    }
                    else
                    {
                        bool match = false;
                        if ((cName.Contains("ShopKeeper")) && t.name == "SC_ShopKeeper") match = true;
                        else if ((cName.Contains("Runner") || cName.Contains("Walker_07") || cName.Contains("Walker_7")) && t.name == "SC_Runner") match = true;
                        else if ((cName.Contains("Mountie")) && t.name == "SC_Mountie") match = true;
                        else if ((cName.Contains("Cheerleader")) && t.name == "SC_Cheerleader") match = true;
                        else if ((cName.Contains("Grandma")) && t.name == "SC_Grandma") match = true;
                        else if ((cName.Contains("Grandpa")) && t.name == "SC_Grandpa") match = true;
                        else if ((cName.Contains("Luchador")) && t.name == "SC_Luchador") match = true;
                        else if ((cName.Contains("Footballer")) && t.name == "SC_Footballer") match = true;
                        else if ((cName.Contains("Hippie")) && t.name == "SC_Hippie") match = true;
                        else if ((cName.Contains("Biker")) && t.name == "SC_Biker") match = true;
                        else if ((cName.Contains("Emo") || cName.Contains("Walker_04") || cName.Contains("Walker_4")) && t.name == "SC_Emo") match = true;
                        else if ((cName.Contains("Clown")) && t.name == "SC_Clown") match = true;
                        else if ((cName.Contains("Nerd")) && t.name == "SC_Nerd") match = true;
                        else if ((cName.Contains("Prisoner")) && t.name == "SC_Prisoner") match = true;
                        t.gameObject.SetActive(match);
                    }
                }
            }
            return;
        }

        // 2. Seri SimplePeople (submesh berawalan "CH_")
        string targetMesh = null;
        if (cName.Contains("Sheriff") || cName.Contains("PakBeni") || cName.Contains("Pak Beni")) targetMesh = "CH_Sheriff";
        else if (cName.Contains("Prostitute") || cName.Contains("PakJoko") || cName.Contains("Pak Joko") || cName == "Icha") targetMesh = "CH_Prostitute";
        else if (cName.Contains("BeachBabe") || cName.Contains("Naya")) targetMesh = "CH_Woman";
        else if (cName.Contains("Waitress") || cName.Contains("Clara") || cName.Contains("Sarah") || cName.Contains("Woman") || cName.Contains("HouseWife") || cName.Contains("Wanita")) targetMesh = "CH_Woman";
        else if (cName.Contains("StreetMan") || cName.Contains("Dimas")) targetMesh = "CH_StreetMan";
        else if (cName.Contains("Police") || cName.Contains("Cop")) targetMesh = "CH_Policeman";
        else if (cName.Contains("Pimp") || cName == "Derryl") targetMesh = "CH_Pimp";
        else if (cName.Contains("Trucker") || cName.Contains("Rian") || character.CompareTag("Player")) targetMesh = "CH_Trucker";
        else if (cName.Contains("FireFighter")) targetMesh = "CH_FireFighter";
        else if (cName.Contains("BusinessMan")) targetMesh = "CH_BusinessMan";
        else if (cName.Contains("Punk")) targetMesh = "CH_Punk";
        else if (cName.Contains("Robber")) targetMesh = "CH_Robber";
        else if (cName.Contains("Hobo")) targetMesh = "CH_Hobo";
        else if (cName.Contains("Roadworker") || cName.Contains("RoadWorker") || cName.Contains("PetugasParkir") || cName.Contains("Petugas Parkir")) targetMesh = "CH_Roadworker";
        else if (cName.Contains("RiotCop")) targetMesh = "CH_RiotCop";

        if (!string.IsNullOrEmpty(targetMesh))
        {
            foreach (var t in allTransforms)
            {
                if (t.name.StartsWith("CH_"))
                {
                    t.gameObject.SetActive(t.name == targetMesh);
                }
            }
        }
        else
        {
            // Jika nama prefab tidak terdeteksi, aktifkan hanya 1 mesh CH_ pertama dan nonaktifkan sisanya
            bool foundOne = false;
            foreach (var t in allTransforms)
            {
                if (t.name.StartsWith("CH_"))
                {
                    if (!foundOne && t.gameObject.activeSelf)
                    {
                        foundOne = true;
                    }
                    else if (foundOne)
                    {
                        t.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Memastikan material URP dan tekstur karakter terpasang dengan benar agar tidak muncul warna magenta (pink).
    /// </summary>
    public static void EnsureCharacterMaterial(GameObject character, string textureName, string materialAssetPath)
    {
        if (character == null) return;

#if UNITY_EDITOR
        // 1. Rapikan sub-mesh karakter agar tidak tumpang tindih
        EnsureCorrectCharacterMesh(character);

        // 2. Cari file tekstur asli
        Texture2D tex = null;
        if (!string.IsNullOrEmpty(textureName))
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                $"Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Textures/{textureName}.png"
            );
            if (tex == null)
            {
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    $"Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleCitizens/Textures/{textureName}.png"
                );
            }
        }

        string cName = character.name.Replace("(Clone)", "").Trim();

        if (tex == null && (cName == "Derryl" || cName.Contains("Pimp")))
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Textures/SimplePeople_Pimp_Brown.png"
            );
        }
        if (tex == null && (cName == "Icha"))
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Textures/SimplePeople_Prostitute_White.png"
            );
        }
        if (tex == null && (cName.Contains("Walker_04") || cName.Contains("Walker_4") || cName.Contains("Emo")))
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleCitizens/Textures/SimpleCitizens_Emo_White.png"
            );
        }
        if (tex == null && (cName.Contains("Walker_07") || cName.Contains("Walker_7") || cName.Contains("Runner")))
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleCitizens/Textures/SimpleCitizens_Runner_White.png"
            );
        }
        if (tex == null && (cName.Contains("Trucker") || cName.Contains("Rian") || character.CompareTag("Player")))
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Textures/SimplePeople_Redneck_Black.png"
            );
        }
        if (tex == null && (cName.Contains("Sheriff") || cName.Contains("PakBeni") || cName.Contains("Pak Beni")))
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Textures/SimplePeople_Sheriff_Black.png"
            );
        }
        if (tex == null && (cName.Contains("Prostitute") || cName.Contains("PakJoko") || cName.Contains("Pak Joko")))
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Textures/SimplePeople_Prostitute_Brown.png"
            );
        }
        if (tex == null && (cName.Contains("BeachBabe") || cName.Contains("Naya")))
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Textures/SimplePeople_BeachBabe_Black.png"
            );
        }
        if (tex == null && (cName.Contains("Waitress") || cName.Contains("Clara") || cName.Contains("Sarah")))
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Textures/SimplePeople_Waitress_White.png"
            );
        }
        if (tex == null && (cName.Contains("StreetMan") || cName.Contains("Dimas")))
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Textures/SimplePeople_StreetMan_Brown.png"
            );
        }
        if (tex == null && (cName.Contains("Roadworker") || cName.Contains("RoadWorker") || cName.Contains("PetugasParkir") || cName.Contains("Petugas Parkir")))
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Textures/SimplePeople_RoadWorker_White.png"
            );
        }
        if (tex == null && (cName.Contains("Police") || cName.Contains("Cop")))
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Textures/SimplePeople_Police_White.png"
            );
        }

        // Coba periksa apakah renderer saat ini sudah memiliki tekstur valid bawaan prefab
        if (tex == null)
        {
            Renderer[] curRends = character.GetComponentsInChildren<Renderer>(true);
            foreach (var r in curRends)
            {
                if (r.sharedMaterial != null && r.sharedMaterial.mainTexture != null)
                {
                    tex = r.sharedMaterial.mainTexture as Texture2D;
                    if (tex != null) break;
                }
            }
        }

        // Fallback tekstur agar karakter TIDAK PERNAH polos putih
        if (tex == null)
        {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Textures/SimplePeople_StreetMan_Brown.png"
            );
        }

        // 3. Muat Material URP yang valid
        Material targetMat = null;
        if (!string.IsNullOrEmpty(materialAssetPath))
        {
            targetMat = AssetDatabase.LoadAssetAtPath<Material>(materialAssetPath);
        }

        if (targetMat == null && (cName == "Derryl" || cName.Contains("Pimp")))
        {
            targetMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_Pimp_Brown.mat"
            );
        }
        if (targetMat == null && (cName == "Icha"))
        {
            targetMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_Prostitute_White.mat"
            );
        }
        if (targetMat == null && (cName.Contains("Walker_04") || cName.Contains("Walker_4") || cName.Contains("Emo")))
        {
            targetMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleCitizens/Materials/SimpleCitizens_Emo_White.mat"
            );
        }
        if (targetMat == null && (cName.Contains("Walker_07") || cName.Contains("Walker_7") || cName.Contains("Runner")))
        {
            targetMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleCitizens/Materials/SimpleCitizens_Runner_White.mat"
            );
        }
        if (targetMat == null && (cName.Contains("Trucker") || cName.Contains("Rian") || character.CompareTag("Player")))
        {
            targetMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_Trucker_Black.mat"
            );
        }
        if (targetMat == null && (cName.Contains("Sheriff") || cName.Contains("PakBeni") || cName.Contains("Pak Beni")))
        {
            targetMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_Sheriff_Black.mat"
            );
        }
        if (targetMat == null && (cName.Contains("Prostitute") || cName.Contains("PakJoko") || cName.Contains("Pak Joko")))
        {
            targetMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_Prostitute_Brown.mat"
            );
        }
        if (targetMat == null && (cName.Contains("BeachBabe") || cName.Contains("Naya")))
        {
            targetMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_BeachBabe_Black.mat"
            );
        }
        if (targetMat == null && (cName.Contains("Waitress") || cName.Contains("Clara") || cName.Contains("Sarah")))
        {
            targetMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_Waitress_White.mat"
            );
        }
        if (targetMat == null && (cName.Contains("StreetMan") || cName.Contains("Dimas")))
        {
            targetMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_StreetMan_Brown.mat"
            );
        }
        if (targetMat == null && (cName.Contains("Roadworker") || cName.Contains("RoadWorker") || cName.Contains("PetugasParkir") || cName.Contains("Petugas Parkir")))
        {
            targetMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_RoadWorker_White.mat"
            );
        }
        if (targetMat == null && (cName.Contains("Police") || cName.Contains("Cop")))
        {
            targetMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_Police_White.mat"
            );
        }

        // Fallback material jika masih null
        if (targetMat == null)
        {
            targetMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/SimplePeople_StreetMan_Brown.mat"
            );
        }

        Shader urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (urpShader == null) urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) urpShader = Shader.Find("Mobile/Diffuse");
        if (urpShader == null) urpShader = Shader.Find("Sprites/Default");

        if (targetMat != null)
        {
            if (targetMat.shader == null || !targetMat.shader.name.StartsWith("Universal Render Pipeline"))
            {
                if (urpShader != null) targetMat.shader = urpShader;
            }
            if (tex != null)
            {
                if (targetMat.HasProperty("_BaseMap")) targetMat.SetTexture("_BaseMap", tex);
                if (targetMat.HasProperty("_MainTex")) targetMat.SetTexture("_MainTex", tex);
                targetMat.mainTexture = tex;
            }
            if (targetMat.HasProperty("_BaseColor")) targetMat.SetColor("_BaseColor", Color.white);
            if (targetMat.HasProperty("_Color")) targetMat.SetColor("_Color", Color.white);
        }

        // 4. SELALU tetapkan targetMat ke seluruh Renderer anak karakter ini
        Renderer[] renderers = character.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r == null || (r is ParticleSystemRenderer)) continue;

            if (targetMat != null)
            {
                r.sharedMaterial = targetMat;
            }

            if (Application.isPlaying)
            {
                if (targetMat != null)
                {
                    r.material = targetMat;
                }
                if (tex != null && r.material != null)
                {
                    if (r.material.HasProperty("_BaseMap")) r.material.SetTexture("_BaseMap", tex);
                    if (r.material.HasProperty("_MainTex")) r.material.SetTexture("_MainTex", tex);
                    r.material.mainTexture = tex;
                    if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", Color.white);
                }
            }
        }
#endif
    }

    /// <summary>
    /// Memastikan seluruh bangunan (rumah, gedung, pos, toko), rintangan beton (planter, divider, barrier),
    /// kendaraan, pagar, tembok, pohon, dan benda-benda fisik memiliki collider solid agar tidak tembus.
    /// Permukaan tanah, aspal jalan, dan trotoar datar dibersihkan dari collider penghalang agar gerakan WASD mulus.
    /// </summary>
    public static int EnsureEnvironmentColliders()
    {
        int count = 0;
        Material townMat = null;
#if UNITY_EDITOR
        townMat = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Materials/SimpleTown.mat"
        );
#endif

        MeshRenderer[] renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
        foreach (var mr in renderers)
        {
            GameObject go = mr.gameObject;
            if (go.GetComponent<Animator>() != null || go.GetComponentInParent<Animator>() != null) continue;
            if (go.name.Contains("Camera") || go.name.Contains("Light")) continue;

            string lowerName = go.name.ToLower();
            string lowerParent = go.transform.parent != null ? go.transform.parent.name.ToLower() : "";

            // Perbaiki material ungu/pink pada grave_small_mesh atau prop dengan material hilang
            if (lowerName.Contains("grave") && townMat != null)
            {
                mr.sharedMaterial = townMat;
            }

            // Objek visual melayang di udara (billboard reklame melayang, antena, piringan satelit)
            bool isOverheadNonPhysical =
                lowerName.Contains("billboard") || lowerName.Contains("aerial") || lowerName.Contains("dish") ||
                lowerName.Contains("flag") || lowerName.Contains("cloud") || lowerName.Contains("skybox");

            // Permukaan jalan, aspal, trotoar, dan tanah datar
            bool isGroundOrStreet = 
                lowerName.Contains("road") || lowerName.Contains("jalan") || lowerName.Contains("runway") || 
                lowerName.Contains("plane") || lowerName.Contains("ground") || lowerName.Contains("floor") ||
                lowerName.Contains("sidewalk") || lowerName.Contains("trotoar") || lowerName.Contains("tile") ||
                lowerName.Contains("asphalt") || lowerName.Contains("aspal") || lowerName.Contains("dirt") ||
                lowerName.Contains("grass") || lowerName.Contains("rumput") || lowerName.Contains("paper") ||
                lowerName.Contains("rolledpaper") || lowerName.Contains("line") || lowerName.Contains("garis") ||
                lowerName.Contains("marking") || lowerName.Contains("crossing") || lowerName.Contains("carpet") ||
                lowerName.Contains("water") ||
                lowerParent.Contains("road") || lowerParent.Contains("jalan") || lowerParent.Contains("plane") ||
                lowerParent.Contains("ground") || lowerParent.Contains("floor") || lowerParent.Contains("sidewalk") ||
                lowerParent.Contains("trotoar") || lowerParent.Contains("runway");

            // Murni permukaan lantai datar (tebal < 3.5 cm)
            bool isActuallyFlatGround = isGroundOrStreet && mr.bounds.size.y <= 0.035f;

            if (isActuallyFlatGround || isOverheadNonPhysical)
            {
                // Bersihkan collider dari permukaan aspal datar agar tidak menimbulkan benturan mikro
                Collider existingCol = go.GetComponent<Collider>();
                if (existingCol != null && !(existingCol is MeshCollider))
                {
                    DestroyComponentSafe(existingCol);
                }
                continue;
            }

            // Seluruh rintangan fisik: Beton, Rumah, Gedung, Kendaraan, Tembok, Pagar, Pohon, Props
            bool isActualObstacle =
                lowerName.Contains("building") || lowerName.Contains("bangunan") || lowerName.Contains("house") || lowerName.Contains("rumah") ||
                lowerName.Contains("shop") || lowerName.Contains("toko") || lowerName.Contains("store") || lowerName.Contains("office") ||
                lowerName.Contains("cinema") || lowerName.Contains("gym") || lowerName.Contains("stripclub") || lowerName.Contains("drivethru") ||
                lowerName.Contains("repair") || lowerName.Contains("pawn") || lowerName.Contains("station") || lowerName.Contains("fire") ||
                lowerName.Contains("police") || lowerName.Contains("petrol") || lowerName.Contains("gas") || lowerName.Contains("hospital") ||
                lowerName.Contains("church") || lowerName.Contains("factory") || lowerName.Contains("town_hall") || lowerName.Contains("hall") ||
                lowerName.Contains("mall") || lowerName.Contains("garage") || lowerName.Contains("hotel") || lowerName.Contains("motel") ||
                lowerName.Contains("diner") || lowerName.Contains("warehouse") || lowerName.Contains("hangar") || lowerName.Contains("tower") ||
                lowerName.Contains("apartment") || lowerName.Contains("terminal") || lowerName.Contains("baber") || lowerName.Contains("barber") ||
                lowerName.Contains("cafe") || lowerName.Contains("donut") || lowerName.Contains("burger") || lowerName.Contains("icecream") ||
                lowerName.Contains("planter") || lowerName.Contains("divider") || lowerName.Contains("barrier") || lowerName.Contains("curb") ||
                lowerName.Contains("concrete") || lowerName.Contains("beton") || lowerName.Contains("seawall") ||
                lowerName.Contains("veh_") || lowerName.Contains("car") || lowerName.Contains("truck") || lowerName.Contains("bus") ||
                lowerName.Contains("van") || lowerName.Contains("cop") || lowerName.Contains("ambo") || lowerName.Contains("ute") ||
                lowerName.Contains("taxi") || lowerName.Contains("wagon") || lowerName.Contains("fire_truck") ||
                lowerName.Contains("wall") || lowerName.Contains("dinding") || lowerName.Contains("fence") || lowerName.Contains("pagar") ||
                lowerName.Contains("hedge") || lowerName.Contains("gate") || lowerName.Contains("spike") ||
                lowerName.Contains("tree") || lowerName.Contains("pohon") || lowerName.Contains("palm") || lowerName.Contains("rock") ||
                lowerName.Contains("batu") || lowerName.Contains("boulder") ||
                lowerName.Contains("dumpster") || lowerName.Contains("bin") || lowerName.Contains("trash") || lowerName.Contains("barrel") ||
                lowerName.Contains("bench") || lowerName.Contains("hydrant") || lowerName.Contains("post") || lowerName.Contains("pole") ||
                lowerName.Contains("memorial") || lowerName.Contains("table") || lowerName.Contains("cart") || lowerName.Contains("prop_") ||
                lowerParent.Contains("bangunan") || lowerParent.Contains("building") || lowerParent.Contains("cars") || 
                lowerParent.Contains("pohon") || lowerParent.Contains("wall") || lowerParent.Contains("dinding") || 
                lowerParent.Contains("pagar") || lowerParent.Contains("fence") || lowerParent.Contains("planter");

            // Pasang BoxCollider jika merupakan rintangan atau objek 3D fisik dengan tinggi >= 3.5 cm
            if (isActualObstacle || mr.bounds.size.y >= 0.035f)
            {
                Collider col = go.GetComponent<Collider>();
                if (col == null)
                {
                    col = go.AddComponent<BoxCollider>();
                    count++;
                }
                if (col != null)
                {
                    col.isTrigger = false;
                }
            }
        }
        return count;
    }

    /// <summary>
    /// Memastikan seluruh objek di Bandara Level 2 (kendaraan, truk bahan bakar, tangga pesawat, pesawat darat,
    /// gedung terminal, hanggar, pagar, dan props) memiliki BoxCollider padat (solid)
    /// sehingga karakter Rian tidak bisa menembus benda apapun di sekitarnya.
    /// </summary>
    public static int EnsureAirportEnvironmentColliders()
    {
        int count = 0;
        MeshRenderer[] renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
        foreach (var mr in renderers)
        {
            if (mr == null) continue;
            GameObject go = mr.gameObject;
            if (go == null) continue;

            // Abaikan Rian, karakter NPC, kamera, lampu, dan pesawat yang sedang terbang di langit
            if (go.CompareTag("Player") || go.CompareTag("NPC")) continue;
            if (go.GetComponent<AirportFlyingAirplane>() != null || go.GetComponentInParent<AirportFlyingAirplane>() != null || go.name.Contains("Plane01") || go.name.Contains("Flying")) continue;
            if (go.GetComponent<Animator>() != null || go.GetComponentInParent<Animator>() != null) continue;
            if (go.name.Contains("Camera") || go.name.Contains("Light")) continue;

            string n = go.name.ToLower();
            string p = go.transform.parent != null ? go.transform.parent.name.ToLower() : "";

            // Abaikan permukaan lantai aspal / runway / ground datar (tebal <= 8 cm)
            bool isGroundOrFloor = 
                n.Contains("road") || n.Contains("jalan") || n.Contains("runway") || 
                n.Contains("ground_concrete") || n.Contains("concrete_ground") ||
                n.Contains("asphalt") || n.Contains("grass") || n.Contains("floor") ||
                n.Contains("tile") || n.Contains("marking") || n.Contains("line") ||
                p.Contains("runway") || p.Contains("grass") || p.Contains("ground");

            if (isGroundOrFloor && mr.bounds.size.y <= 0.08f)
            {
                continue;
            }

            // Seluruh rintangan fisik bandara
            bool isAirportObstacle =
                p.Contains("vehicles") || p.Contains("buildings") || p.Contains("props") || p.Contains("fence") ||
                n.Contains("veh_") || n.Contains("plane") || n.Contains("truck") || n.Contains("car") ||
                n.Contains("bus") || n.Contains("van") || n.Contains("fuel") || n.Contains("stair") ||
                n.Contains("cart") || n.Contains("trailer") || n.Contains("tug") || n.Contains("tractor") ||
                n.Contains("building") || n.Contains("terminal") || n.Contains("hangar") || n.Contains("tower") ||
                n.Contains("fence") || n.Contains("gate") || n.Contains("prop_") || n.Contains("barrier") ||
                n.Contains("cone") || n.Contains("chock") || n.Contains("light") || n.Contains("pole") ||
                mr.bounds.size.y >= 0.08f;

            if (isAirportObstacle)
            {
                Collider col = go.GetComponent<Collider>();

                // Pesawat, helikopter, gangway, hanggar & gedung besar memakai MeshCollider sesuai bentuk aslinya,
                // sehingga Rian bisa lewat di bawah sayap / celah dan hanya "menabrak" saat benar-benar menyentuh badan benda.
                if (NeedsAccurateCollider(n))
                {
                    MeshFilter amf = go.GetComponent<MeshFilter>();
                    if (amf != null && amf.sharedMesh != null && !(col is MeshCollider))
                    {
                        if (col != null) DestroyComponentSafe(col);
                        MeshCollider mc = go.AddComponent<MeshCollider>();
                        mc.sharedMesh = amf.sharedMesh;
                        mc.convex = false;
                        mc.isTrigger = false;
                        col = mc;
                        count++;
                    }
                }

                if (col == null)
                {
                    MeshFilter mf = go.GetComponent<MeshFilter>();
                    BoxCollider box = go.AddComponent<BoxCollider>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        box.center = mf.sharedMesh.bounds.center;
                        box.size = mf.sharedMesh.bounds.size;
                    }
                    col = box;
                    count++;
                }
                if (col != null)
                {
                    col.isTrigger = false;
                }
            }
        }
        return count;
    }

    private static bool NeedsAccurateCollider(string lowerName)
    {
        if (lowerName.Contains("wheel") || lowerName.Contains("propell") || lowerName.Contains("rotor") || lowerName.Contains("missile")) return false;
        return (lowerName.Contains("plane") && !lowerName.Contains("cart")) ||
               lowerName.StartsWith("jet") || lowerName.Contains("fighter") ||
               (lowerName.Contains("heli") && !lowerName.Contains("pad")) ||
               lowerName.Contains("gangway") || lowerName.Contains("hanger") || lowerName.Contains("hangar") ||
               lowerName.Contains("terminal") || lowerName.StartsWith("airport_building") || lowerName.Contains("tower");
    }

    /// <summary>
    /// Memastikan 1 pesawat komersial (Plane01) yang ditempatkan di scene sedang terbang melintas
    /// di langit bandara Level 2 secara looping searah jarum jam (clockwise).
    /// </summary>
    public static GameObject EnsureAirportFlyingAirplane()
    {
        GameObject first = EnsureFirstFlyingAirplane();
        EnsureSecondFlyingAirplane();
        return first;
    }

    /// <summary>
    /// Mengatur lintasan terbang tiap pesawat langit. Pesawat pertama searah jarum jam,
    /// pesawat kedua berlawanan arah dengan lintasan, ketinggian, dan kecepatan berbeda agar tidak berimpit.
    /// </summary>
    private static void ConfigureFlyingAirplane(AirportFlyingAirplane comp, bool second)
    {
        if (comp == null) return;
        comp.gameObject.SetActive(true);
        comp.enabled = true;

        if (!second)
        {
            comp.flightCenter = new Vector3(35f, 13f, 130f);
            comp.radiusX = 95f;
            comp.radiusZ = 120f;
            comp.cruiseAltitude = 13f;
            comp.flightSpeed = 26f;
            comp.maxBankAngle = 18f;
            comp.altitudeWobble = 1.0f;
            comp.counterClockwise = false;
            comp.startAngleDegrees = 0f;
        }
        else
        {
            comp.flightCenter = new Vector3(35f, 24f, 130f);
            comp.radiusX = 150f;
            comp.radiusZ = 95f;
            comp.cruiseAltitude = 24f;
            comp.flightSpeed = 21f;
            comp.maxBankAngle = 14f;
            comp.altitudeWobble = 1.5f;
            comp.counterClockwise = true;
            comp.startAngleDegrees = 150f;
        }

        comp.retractLandingGear = false;
        comp.CleanFlightColliders();
        comp.ResetToStartAngle();
        comp.UpdateFlightTransform(0f);
    }

    private static GameObject EnsureSecondFlyingAirplane()
    {
        // Sudah punya komponen terbang? cukup konfigurasi ulang
        foreach (var c in Object.FindObjectsByType<AirportFlyingAirplane>(FindObjectsInactive.Include))
        {
            if (c != null && c.gameObject.name.Contains("Plane02"))
            {
                ConfigureFlyingAirplane(c, true);
                return c.gameObject;
            }
        }

        // Cari Plane02 yang ditempatkan di langit (ketinggian > 5 m)
        GameObject skyPlane = null;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
        {
            if (go != null && go.name == "Plane02" && go.transform.position.y > 5f && go.GetComponent<MeshRenderer>() != null)
            {
                skyPlane = go;
                break;
            }
        }
        if (skyPlane == null) return null;

        skyPlane.name = "Plane02_Flying";
        skyPlane.transform.SetParent(null, true);

        AirportFlyingAirplane comp = skyPlane.GetComponent<AirportFlyingAirplane>();
        if (comp == null) comp = skyPlane.AddComponent<AirportFlyingAirplane>();
        ConfigureFlyingAirplane(comp, true);
        return skyPlane;
    }

    private static GameObject EnsureFirstFlyingAirplane()
    {
        AirportFlyingAirplane existingComp = null;
        foreach (var c in Object.FindObjectsByType<AirportFlyingAirplane>(FindObjectsInactive.Include))
        {
            if (c != null && !c.gameObject.name.Contains("Plane02"))
            {
                existingComp = c;
                break;
            }
        }
        if (existingComp != null)
        {
            ConfigureFlyingAirplane(existingComp, false);
            return existingComp.gameObject;
        }

        // Cari Plane01 yang sudah ditempatkan di scene oleh user
        GameObject planeGo = null;
        GameObject[] allGos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allGos)
        {
            if (go == null) continue;
            if (go.name == "AirportFlyingAirplane" || 
                (go.name.StartsWith("Plane01") && (go.transform.position.y > 2f || go.transform.localPosition.y > 2f)))
            {
                planeGo = go;
                break;
            }
        }

        // Fallback jika belum ada di scene
        if (planeGo == null)
        {
            foreach (var go in allGos)
            {
                if (go != null && go.name.StartsWith("Plane01"))
                {
                    planeGo = go;
                    break;
                }
            }
        }

#if UNITY_EDITOR
        if (planeGo == null)
        {
            string prefabPath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleAirport/_Prefabs/Vehicles/Plane01.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                planeGo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }
        }
#endif
        if (planeGo == null)
        {
            planeGo = new GameObject("Plane01_Flying");
        }

        planeGo.name = "Plane01_Flying";
        planeGo.SetActive(true);
        // Lepas dari parent Vehicles agar independen di koordinat world
        planeGo.transform.SetParent(null, true);

        // Bersihkan BoxCollider yang mungkin terpasang oleh setup lingkungan
        Collider[] existingCols = planeGo.GetComponentsInChildren<Collider>(true);
        foreach (var col in existingCols)
        {
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        AirportFlyingAirplane flyComp = planeGo.GetComponent<AirportFlyingAirplane>();
        if (flyComp == null) flyComp = planeGo.AddComponent<AirportFlyingAirplane>();

        ConfigureFlyingAirplane(flyComp, false);

        return planeGo;
    }

    /// <summary>
    /// Memastikan langit di Level 2 Bandara berwarna biru cerah alami:
    /// - Memasang Default-Skybox (Skybox/Procedural) pada RenderSettings.
    /// - Mengatur warna latar kamera (Camera Clear Flags) ke Skybox dengan fallback biru langit cerah.
    /// - Menyelaraskan Ambient Mode ke Skybox agar pencahayaan lingkungan bernuansa siang cerah.
    /// </summary>
    public static void EnsureAirportSky()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = new Color(0.192f, 0.302f, 0.475f, 1f);
        }

#if UNITY_EDITOR
        if (RenderSettings.skybox == null)
        {
            Material defaultSkybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            if (defaultSkybox != null)
            {
                RenderSettings.skybox = defaultSkybox;
            }
        }
#endif

        if (RenderSettings.skybox == null)
        {
            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                Material skyMat = new Material(skyShader);
                skyMat.name = "AirportSkybox_Procedural";
                skyMat.SetColor("_SkyTint", new Color(0.5f, 0.75f, 1f, 1f));
                skyMat.SetColor("_GroundColor", new Color(0.37f, 0.35f, 0.35f, 1f));
                skyMat.SetFloat("_AtmosphereThickness", 1.0f);
                skyMat.SetFloat("_Exposure", 1.1f);
                RenderSettings.skybox = skyMat;
            }
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.0f;
    }

    /// <summary>
    /// Setup 3 NPC Pejalan Kaki Bandara Level 2 ("Level 2 saya"):
    /// 1. Penumpang Bandara (HouseWife_White) di area gate/tangga pesawat (boarding stairs).
    /// 2. Dokter Bandara (Doctor_White) di area taxiway/runway.
    /// 3. Pengunjung Hanggar (BeachBabe_White) di area hanggar jet tempur.
    /// Berjalan santai (FreeWander) dengan sensor anti-tabrakan, dan ketika bertemu Rian:
    /// NPC berhenti, menoleh menghadap Rian, melambaikan tangan, dan menampilkan balon dialog menyapa Rian.
    /// </summary>
    public static void EnsureAirportWanderingNPCs(GameObject playerGo)
    {
        // 1. Penumpang Bandara (Image 1: Dekat Tangga Pesawat / Luggage Ramp)
        GameObject npc1 = FindOrSpawnAirportWanderer(
            "NPC_Penumpang_Bandara",
            new string[] { "HouseWife", "Penumpang", "Turis", "Child" },
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_HouseWife_White.prefab",
            new Vector3(48f, 0.05f, 158f),
            Quaternion.Euler(0f, 180f, 0f)
        );
        ConfigureAirportWanderingNPC(
            npc1, playerGo,
            "NPC_Penumpang_Bandara",
            "Penumpang Bandara",
            "Halo Kak Rian! Wah, seru sekali melihat pesawat besar di bandara ini!",
            12f
        );

        // 2. Dokter / Tim Medis Bandara (Image 2: Di Taxiway / Runway dekat tanda 19 L)
        GameObject npc2 = FindOrSpawnAirportWanderer(
            "NPC_Dokter_Bandara",
            new string[] { "Doctor", "Medic", "Dokter", "Nurse" },
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Doctor_White.prefab",
            new Vector3(28f, 0.05f, 175f),
            Quaternion.Euler(0f, 90f, 0f)
        );
        ConfigureAirportWanderingNPC(
            npc2, playerGo,
            "NPC_Dokter_Bandara",
            "Dokter Bandara",
            "Halo Rian! Tetap jaga kesehatan dan stamina ya selama bertugas di bandara!",
            14f
        );

        // 3. Pengunjung Hanggar (Image 3: Dekat Dinding Hanggar dan Jet Tempur)
        GameObject npc3 = FindOrSpawnAirportWanderer(
            "NPC_Pengunjung_Hanggar",
            new string[] { "BeachBabe", "Runner", "Hanggar", "Girl", "Visitor" },
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_BeachBabe_White.prefab",
            new Vector3(12f, 0.05f, 148f),
            Quaternion.Euler(0f, 45f, 0f)
        );
        ConfigureAirportWanderingNPC(
            npc3, playerGo,
            "NPC_Pengunjung_Hanggar",
            "Pengunjung Hanggar",
            "Halo Kurir! Keren sekali ya deretan jet tempur dan pesawat di hanggar ini!",
            10f
        );

        // Scan tambahan: jika user menempatkan karakter baru di scene dengan nama lain,
        // pastikan mereka otomatis dilengkapi NPCWalker & tidak T-pose!
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
        foreach (var go in allObjects)
        {
            if (go == null || go.CompareTag("Player")) continue;
            string n = go.name;
            // Abaikan 6 penerima misi paket bandara
            if (n.Contains("Pak Beni") || n.Contains("Naya") || n.Contains("Dimas") ||
                n.Contains("Tukang Parkir") || n.Contains("Pak Joko") || n.Contains("Clara") ||
                go.GetComponent<DeliveryRecipient>() != null)
            {
                continue;
            }

            // Jika karakter SimplePeople / SimpleCitizens lain yang belum punya NPCWalker
            if (go.GetComponent<Animator>() != null || n.StartsWith("SimplePeople") || n.StartsWith("SimpleCitizens"))
            {
                NPCWalker w = go.GetComponent<NPCWalker>();
                if (w == null)
                {
                    ConfigureAirportWanderingNPC(
                        go, playerGo,
                        go.name,
                        "Warga Bandara",
                        "Halo Rian! Semangat bertugas di Bandara Internasional!",
                        10f
                    );
                }
            }
        }
    }

    private static GameObject FindOrSpawnAirportWanderer(
        string exactName,
        string[] fallbackKeywords,
        string prefabPath,
        Vector3 defaultPos,
        Quaternion defaultRot)
    {
        // 1. Cari berdasarkan nama exact
        GameObject go = GameObject.Find(exactName);
        if (go != null) return go;

        // 2. Cari karakter yang sudah ada di scene berdasarkan keyword
        GameObject[] allGos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var g in allGos)
        {
            if (g == null || g.CompareTag("Player") || g.GetComponent<DeliveryRecipient>() != null) continue;
            string gn = g.name.ToLower();
            foreach (var kw in fallbackKeywords)
            {
                if (gn.Contains(kw.ToLower()))
                {
                    return g;
                }
            }
        }

        // 3. Jika belum ada di scene, instansiasi dari prefab
#if UNITY_EDITOR
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = exactName;
            go.transform.position = defaultPos;
            go.transform.rotation = defaultRot;
            go.transform.localScale = Vector3.one;
            return go;
        }
#endif

        return null;
    }

    private static void ConfigureAirportWanderingNPC(
        GameObject npcGo,
        GameObject playerGo,
        string exactName,
        string displayName,
        string greetingLine,
        float wanderRadius = 12f)
    {
        if (npcGo == null) return;
        npcGo.name = exactName;
        npcGo.tag = "NPC";
        npcGo.SetActive(true);

        // Hapus script PlayerController jika ada
        PlayerController pc = npcGo.GetComponent<PlayerController>();
        if (pc != null) DestroyComponentSafe(pc);
        CharacterController cc = npcGo.GetComponent<CharacterController>();
        if (cc != null) DestroyComponentSafe(cc);

        // Pastikan tidak ada DeliveryRecipient agar tidak bentrok dengan misi pengantaran paket
        DeliveryRecipient dr = npcGo.GetComponent<DeliveryRecipient>();
        if (dr != null) DestroyComponentSafe(dr);

        // Pastikan Rigidbody Kinematic
        Rigidbody rb = npcGo.GetComponent<Rigidbody>();
        if (rb == null) rb = npcGo.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Pastikan CapsuleCollider padat (solid)
        CapsuleCollider col = npcGo.GetComponent<CapsuleCollider>();
        if (col == null) col = npcGo.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.radius = 0.38f;
        col.center = new Vector3(0f, 0.9f, 0f);
        col.isTrigger = false;

        // Animator (Menghilangkan T-Pose)
        Animator anim = npcGo.GetComponent<Animator>();
        if (anim == null) anim = npcGo.AddComponent<Animator>();
        anim.applyRootMotion = false;
        anim.enabled = true;

#if UNITY_EDITOR
        if (anim.runtimeAnimatorController == null)
        {
            RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleCitizens/Models/SimpleCitizens.controller");
            if (ctrl == null)
            {
                ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            }
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        }
#endif

        EnsureCorrectCharacterMesh(npcGo);
        AutoEnsureCharacterMaterial(npcGo);

        // Konfigurasi NPCWalker (FreeWander & Greeting)
        NPCWalker walker = npcGo.GetComponent<NPCWalker>();
        if (walker == null) walker = npcGo.AddComponent<NPCWalker>();

        walker.navMode = NPCWalker.NavigationMode.FreeWander;
        walker.wanderRadius = wanderRadius;
        walker.maxDistanceFromStart = wanderRadius * 1.8f;
        walker.walkSpeed = 2.6f;
        walker.runSpeed = 5.5f;
        walker.rotationSpeed = 8.0f;
        walker.idleChance = 0.25f;
        walker.sitChance = 0f;
        walker.runChance = 0.15f;
        walker.turnPauseTime = 0.8f;

        // Sensor Anti-Tabrakan
        walker.enableObstacleAvoidance = true;
        walker.obstacleDetectionDistance = 2.2f;
        walker.bodyRadius = 0.35f;

        // Interaksi Menyapa Pemain
        walker.enablePlayerInteraction = true;
        walker.interactionDistance = 4.5f;
        walker.speechBubbleHeight = 2.1f;
        walker.characterDisplayName = displayName;
        walker.characterLine = greetingLine;
        if (playerGo != null) walker.playerTarget = playerGo.transform;

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(npcGo);
#endif
    }

    /// <summary>
    /// Memastikan SELURUH orang / karakter / NPC di kota memiliki CapsuleCollider padat (solid)
    /// dan Rigidbody Kinematic sehingga Rian tidak bisa menembus orang yang sedang berdiri atau berjalan.
    /// </summary>
    public static int EnsureAllCharacterColliders()
    {
        int count = 0;
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
        foreach (var go in allObjects)
        {
            if (go == null || go.CompareTag("Player")) continue;

            string n = go.name;
            bool isChar = go.GetComponent<Animator>() != null ||
                          n.StartsWith("SimplePeople") ||
                          n.StartsWith("SimpleCitizens") ||
                          n.StartsWith("NPC_") ||
                          go.GetComponent<DeliveryRecipient>() != null ||
                          go.GetComponent<NPCWalker>() != null;

            if (!isChar || n.Contains("Camera") || n.Contains("Light")) continue;

            // Pastikan memiliki CapsuleCollider padat (solid, isTrigger = false)
            CapsuleCollider col = go.GetComponent<CapsuleCollider>();
            if (col == null) col = go.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.35f;
            col.center = new Vector3(0f, 0.9f, 0f);
            col.isTrigger = false;

            // Pastikan Rigidbody Kinematic agar posisinya stabil
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            count++;
        }
        return count;
    }

    /// <summary>
    /// Menghapus karakter berskala raksasa (skala > 0.2f) agar skala kota konsisten 0.1.
    /// </summary>
    /// <summary>
    /// Menyesuaikan skala karakter berskala raksasa (skala > 0.2f) menjadi proporsional 0.1.
    /// Tidak menghapus karakter baru yang baru saja di-drag user ke scene.
    /// </summary>
    public static int RemoveGiantNPCs()
    {
        CleanAllJointBones();

        int count = 0;
        GameObject[] allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
        foreach (var go in allGo)
        {
            if (go.name.Contains("_jnt") || go.name.StartsWith("CH_")) continue;
            if (go.GetComponent<RectTransform>() != null || 
                go.name.Contains("Fade") || 
                go.name.Contains("Portal") || 
                go.name.Contains("Canvas") || 
                go.name.Contains("UI"))
            {
                continue;
            }

            bool isCharacter = go.name.StartsWith("NPC_Walker") || 
                               go.name.StartsWith("SimplePeople") || 
                               go.name.StartsWith("SimpleCitizens") ||
                               go.name.Contains("ShopKeeper") ||
                               (go.GetComponent<Animator>() != null && !go.name.Contains("Camera") && !go.name.Contains("Building"));

            if (isCharacter)
            {
                if (go.transform.localScale.y > 0.2f || go.transform.lossyScale.y > 0.2f)
                {
                    go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    count++;
                }
            }
        }
        return count;
    }

    /// <summary>
    /// Secara otomatis mencarikan tekstur dan material URP asli untuk karakter berdasarkan namanya.
    /// Mencegah karakter menjadi putih atau magenta (pink).
    /// </summary>
    public static void AutoEnsureCharacterMaterial(GameObject character)
    {
        if (character == null) return;
#if UNITY_EDITOR
        string cName = character.name.Replace("(Clone)", "").Trim();

        // 1. Coba periksa apakah berasal dari Prefab Asset asli
        string prefabName = "";
        try
        {
            GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(character);
            if (root != null)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(root);
                if (source != null) prefabName = source.name;
            }
            if (string.IsNullOrEmpty(prefabName))
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(character);
                if (source != null) prefabName = source.name;
            }
        }
        catch {}

        string chosenMatName = null;
        string chosenTexName = null;

        if (!string.IsNullOrEmpty(prefabName))
        {
            if (prefabName.StartsWith("SimplePeople_") || prefabName.StartsWith("SimpleCitizens_"))
            {
                chosenMatName = prefabName;
                chosenTexName = prefabName;
                if (prefabName.Contains("Trucker"))
                {
                    chosenTexName = "SimplePeople_Redneck_Black";
                }
            }
        }

        // 2. Pemetaan eksplisit untuk nama scene kustom
        if (string.IsNullOrEmpty(chosenMatName))
        {
            if (cName == "Derryl" || cName.Contains("Pimp"))
            {
                chosenMatName = "SimplePeople_Pimp_Brown";
                chosenTexName = "SimplePeople_Pimp_Brown";
            }
            else if (cName == "Icha")
            {
                chosenMatName = "SimplePeople_Prostitute_White";
                chosenTexName = "SimplePeople_Prostitute_White";
            }
            else if (cName.Contains("PakJoko") || cName.Contains("Pak Joko") || cName.Contains("Prostitute"))
            {
                chosenMatName = "SimplePeople_Prostitute_Brown";
                chosenTexName = "SimplePeople_Prostitute_Brown";
            }
            else if (cName.Contains("Rian") || cName.Contains("Trucker") || character.CompareTag("Player"))
            {
                chosenMatName = "SimplePeople_Trucker_Black";
                chosenTexName = "SimplePeople_Redneck_Black";
            }
            else if (cName.Contains("PakBeni") || cName.Contains("Pak Beni") || cName.Contains("Sheriff"))
            {
                chosenMatName = "SimplePeople_Sheriff_Black";
                chosenTexName = "SimplePeople_Sheriff_Black";
            }
            else if (cName.Contains("Clara") || cName.Contains("Sarah") || cName.Contains("Waitress"))
            {
                chosenMatName = "SimplePeople_Waitress_White";
                chosenTexName = "SimplePeople_Waitress_White";
            }
            else if (cName.Contains("Dimas") || cName.Contains("StreetMan"))
            {
                chosenMatName = "SimplePeople_StreetMan_Brown";
                chosenTexName = "SimplePeople_StreetMan_Brown";
            }
            else if (cName.Contains("Naya") || cName.Contains("BeachBabe"))
            {
                chosenMatName = "SimplePeople_BeachBabe_Black";
                chosenTexName = "SimplePeople_BeachBabe_Black";
            }
            else if (cName.Contains("PetugasParkir") || cName.Contains("Petugas Parkir") || cName.Contains("Roadworker") || cName.Contains("RoadWorker"))
            {
                chosenMatName = "SimplePeople_RoadWorker_White";
                chosenTexName = "SimplePeople_RoadWorker_White";
            }
            else if (cName.Contains("Walker_04") || cName.Contains("Walker_4") || cName.Contains("Emo"))
            {
                chosenMatName = "SimpleCitizens_Emo_White";
                chosenTexName = "SimpleCitizens_Emo_White";
            }
            else if (cName.Contains("Walker_07") || cName.Contains("Walker_7") || cName.Contains("Runner"))
            {
                chosenMatName = "SimpleCitizens_Runner_White";
                chosenTexName = "SimpleCitizens_Runner_White";
            }
            else if (cName.Contains("Police") || cName.Contains("Cop"))
            {
                chosenMatName = "SimplePeople_Police_White";
                chosenTexName = "SimplePeople_Police_White";
            }
        }

        // 3. Deteksi sub-mesh anak aktif jika masih belum terpetakan
        if (string.IsNullOrEmpty(chosenMatName))
        {
            Transform[] children = character.GetComponentsInChildren<Transform>(true);
            foreach (var ch in children)
            {
                if (ch.name == "CH_Pimp") { chosenMatName = "SimplePeople_Pimp_Brown"; chosenTexName = "SimplePeople_Pimp_Brown"; break; }
                if (ch.name == "CH_Prostitute") { chosenMatName = "SimplePeople_Prostitute_White"; chosenTexName = "SimplePeople_Prostitute_White"; break; }
                if (ch.name == "CH_Sheriff") { chosenMatName = "SimplePeople_Sheriff_Black"; chosenTexName = "SimplePeople_Sheriff_Black"; break; }
                if (ch.name == "CH_StreetMan") { chosenMatName = "SimplePeople_StreetMan_Brown"; chosenTexName = "SimplePeople_StreetMan_Brown"; break; }
                if (ch.name == "CH_Roadworker") { chosenMatName = "SimplePeople_RoadWorker_White"; chosenTexName = "SimplePeople_RoadWorker_White"; break; }
                if (ch.name == "CH_Policeman") { chosenMatName = "SimplePeople_Police_White"; chosenTexName = "SimplePeople_Police_White"; break; }
                if (ch.name == "CH_FireFighter") { chosenMatName = "SimplePeople_FireFighter_White"; chosenTexName = "SimplePeople_FireFighter_White"; break; }
                if (ch.name == "CH_BusinessMan") { chosenMatName = "SimplePeople_BusinessMan_Black"; chosenTexName = "SimplePeople_BusinessMan_Black"; break; }
                if (ch.name == "CH_Hobo") { chosenMatName = "SimplePeople_Hobo_White"; chosenTexName = "SimplePeople_Hobo_White"; break; }
                if (ch.name == "CH_Punk") { chosenMatName = "SimplePeople_Punk_White"; chosenTexName = "SimplePeople_Punk_White"; break; }
                if (ch.name == "CH_Robber") { chosenMatName = "SimplePeople_Robber_White"; chosenTexName = "SimplePeople_Robber_White"; break; }
                if (ch.name == "CH_Waitress" || ch.name == "CH_Woman") { chosenMatName = "SimplePeople_Waitress_White"; chosenTexName = "SimplePeople_Waitress_White"; break; }
                if (ch.name == "CH_Trucker") { chosenMatName = "SimplePeople_Trucker_Black"; chosenTexName = "SimplePeople_Redneck_Black"; break; }
                if (ch.name == "SC_Runner") { chosenMatName = "SimpleCitizens_Runner_White"; chosenTexName = "SimpleCitizens_Runner_White"; break; }
                if (ch.name == "SC_Emo") { chosenMatName = "SimpleCitizens_Emo_White"; chosenTexName = "SimpleCitizens_Emo_White"; break; }
                if (ch.name == "SC_Cheerleader") { chosenMatName = "SimpleCitizens_Cheerleader_White"; chosenTexName = "SimpleCitizens_Cheerleader_White"; break; }
                if (ch.name == "SC_Biker") { chosenMatName = "SimpleCitizens_Biker_White"; chosenTexName = "SimpleCitizens_Biker_White"; break; }
                if (ch.name == "SC_Clown") { chosenMatName = "SimpleCitizens_Clown_White"; chosenTexName = "SimpleCitizens_Clown_White"; break; }
                if (ch.name == "SC_Footballer") { chosenMatName = "SimpleCitizens_Footballer_White"; chosenTexName = "SimpleCitizens_Footballer_White"; break; }
                if (ch.name == "SC_Grandma") { chosenMatName = "SimpleCitizens_Grandma_White"; chosenTexName = "SimpleCitizens_Grandma_White"; break; }
                if (ch.name == "SC_Grandpa") { chosenMatName = "SimpleCitizens_Grandpa_White"; chosenTexName = "SimpleCitizens_Grandpa_White"; break; }
                if (ch.name == "SC_Hippie") { chosenMatName = "SimpleCitizens_Hippie_White"; chosenTexName = "SimpleCitizens_Hippie_White"; break; }
                if (ch.name == "SC_Luchador") { chosenMatName = "SimpleCitizens_Luchador_White"; chosenTexName = "SimpleCitizens_Luchador_White"; break; }
                if (ch.name == "SC_Mountie") { chosenMatName = "SimpleCitizens_Mountie_White"; chosenTexName = "SimpleCitizens_Mountie_White"; break; }
                if (ch.name == "SC_Nerd") { chosenMatName = "SimpleCitizens_Nerd_White"; chosenTexName = "SimpleCitizens_Nerd_White"; break; }
                if (ch.name == "SC_Prisoner") { chosenMatName = "SimpleCitizens_Prisoner_White"; chosenTexName = "SimpleCitizens_Prisoner_White"; break; }
                if (ch.name == "SC_Tourist") { chosenMatName = "SimpleCitizens_Tourist_Brown"; chosenTexName = "SimpleCitizens_Tourist_Brown"; break; }
                if (ch.name == "SC_ShopKeeper") { chosenMatName = "SimpleCitizens_ShopKeeper_White"; chosenTexName = "SimpleCitizens_ShopKeeper_White"; break; }
            }
        }

        if (string.IsNullOrEmpty(chosenMatName))
        {
            chosenMatName = cName;
            chosenTexName = cName;
        }

        string matPath = $"Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Materials/{chosenMatName}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(matPath) == null)
        {
            matPath = $"Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleCitizens/Materials/{chosenMatName}.mat";
        }

        EnsureCharacterMaterial(character, chosenTexName, matPath);
#endif
    }

    /// <summary>
    /// Alias kompatibilitas untuk NPCWalkerEditor.
    /// </summary>
    public static void SetupPoliceAsVideoNPC(GameObject go)
    {
        if (go == null) return;
        NPCWaypointPath path = EnsureSidewalkPath("Path_Trotoar_Timur", 318.3f, -28.0f, 6.0f, Color.cyan);
        GameObject player = GameObject.FindWithTag("Player");
        ConfigureNPC(go, player, path, 0.30f, "Halo! Saya petugas patroli. Selamat menikmati kota!");
    }

    private static void DestroyComponentSafe(Component comp)
    {
        if (comp == null) return;
        if (!Application.isPlaying)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(comp);
#else
            Object.Destroy(comp);
#endif
        }
        else
        {
            Object.Destroy(comp);
        }
    }

    private static void DestroyGameObjectSafe(GameObject go)
    {
        if (go == null) return;
#if UNITY_EDITOR
        if (Selection.activeGameObject == go || (Selection.activeTransform != null && Selection.activeTransform.IsChildOf(go.transform)))
        {
            Selection.activeObject = null;
        }
#endif
        if (!Application.isPlaying)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(go);
#else
            Object.Destroy(go);
#endif
        }
        else
        {
            Object.Destroy(go);
        }
    }

    /// <summary>
    /// Menyiapkan Quizmaster Steve dan ExitPortal sesuai Modul 7:
    /// - Steve berdiri di trotoar kiri (Coffee Shop) dengan instruksi langsung.
    /// - Kanvas miring raksasa di atas Steve dinonaktifkan otomatis.
    /// - ExitPortal di trotoar kanan disiapkan dengan collider trigger dan PortalExit script.
    /// - ExitPortal disembunyikan di awal, lalu aktif saat pemain berbicara dengan Steve.
    /// - Canvas_FadeTransition dan Fade Panel dipastikan aktif.
    /// </summary>
    public static void EnsureSteveAndPortal()
    {
        // 1. Cari atau Siapkan Quizmaster Steve
        GameObject steveGo = GameObject.Find("Quizmaster Steve");
        if (steveGo == null)
        {
            GameObject[] allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allGo)
            {
                if (go.name.Contains("Quizmaster") || go.name.Contains("Steve"))
                {
                    steveGo = go;
                    break;
                }
            }
        }

        if (steveGo != null)
        {
            steveGo.name = "Quizmaster Steve";
            steveGo.tag = "NPC";
            steveGo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            Vector3 sPos = steveGo.transform.position;
            if (sPos.magnitude < 1f || sPos.y < 0f)
            {
                steveGo.transform.position = new Vector3(316.713f, 0.02f, 10.615f);
                steveGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }

            // Pastikan tidak ada NPCWalker agar Steve tidak berjalan sembarangan
            NPCWalker oldW = steveGo.GetComponent<NPCWalker>();
            if (oldW != null) DestroyComponentSafe(oldW);

            // Sembunyikan kanvas miring world space bawaan prefab
            Transform canvasChild = steveGo.transform.Find("Canvas");
            if (canvasChild != null)
            {
                canvasChild.gameObject.SetActive(false);
            }

            // Pastikan CapsuleCollider Trigger ada dan luasnya cukup untuk interaksi
            CapsuleCollider cc = steveGo.GetComponent<CapsuleCollider>();
            if (cc == null) cc = steveGo.AddComponent<CapsuleCollider>();
            cc.isTrigger = true;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.radius = 1.5f;
            cc.height = 1.8f;

            // Pastikan QuizMaster script terpasang
            QuizMaster qm = steveGo.GetComponent<QuizMaster>();
            if (qm == null) qm = steveGo.AddComponent<QuizMaster>();

            AutoEnsureCharacterMaterial(steveGo);
        }

        // 2. Cari atau Siapkan ExitPortal
        GameObject portalGo = null;
        PortalExit pe = Object.FindAnyObjectByType<PortalExit>(FindObjectsInactive.Include);
        if (pe != null) portalGo = pe.gameObject;
        if (portalGo == null) portalGo = GameObject.Find("ExitPortal");

        if (portalGo == null)
        {
            GameObject[] allGo = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allGo)
            {
                if (go.name.Contains("Portal") || go.name.Contains("ExitPortal"))
                {
                    portalGo = go;
                    break;
                }
            }
        }

#if UNITY_EDITOR
        if (portalGo == null)
        {
            GameObject portalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Develop 3D Mobile Games in Unity/Assets/QuizAdventure/Prefabs/ExitPortal.prefab");
            if (portalPrefab != null)
            {
                portalGo = PrefabUtility.InstantiatePrefab(portalPrefab) as GameObject;
                if (portalGo != null)
                {
                    portalGo.name = "ExitPortal";
                    portalGo.transform.position = new Vector3(318.844f, 0.216f, 9.84f);
                    portalGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    portalGo.transform.localScale = new Vector3(0.2f, 0.2f, 0.1f);
                }
            }
        }
#endif

        if (portalGo != null)
        {
            portalGo.name = "ExitPortal";
            portalGo.transform.localScale = new Vector3(0.2f, 0.2f, 0.1f);
            // Menghadap ke jalan (ke arah pemain yang datang) sesuai Gambar 2 (Rotation Y = 90)
            portalGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            if (portalGo.transform.position.y < 0.2f)
            {
                Vector3 curPos = portalGo.transform.position;
                portalGo.transform.position = new Vector3(curPos.x, 0.216f, curPos.z);
            }

            SphereCollider sc = portalGo.GetComponent<SphereCollider>();
            if (sc == null) sc = portalGo.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.center = Vector3.zero;
            sc.radius = 1.6f;

            PortalExit exitScript = portalGo.GetComponent<PortalExit>();
            if (exitScript == null) exitScript = portalGo.AddComponent<PortalExit>();
            exitScript.secondsOfFade = 1.0f; // Durasi transisi fade to black 1 detik sesuai standar

            // Pastikan material portal vortex warna ungu asli (ExitPortal_MAT.mat)
            MeshRenderer mr = portalGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
#if UNITY_EDITOR
                Material pMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Develop 3D Mobile Games in Unity/Assets/QuizAdventure/Materials/ExitPortal_MAT.mat");
                if (pMat != null)
                {
                    mr.sharedMaterial = pMat;
                }
#endif
            }
        }

        // 3. Pastikan Canvas_FadeTransition dan Fade Panel ada di scene
        EnsureFadePanel();
    }

    private static void EnsureFadePanel()
    {
        // 1. Pastikan Canvas_FadeTransition ada dan dikonfigurasi fullscreen
        GameObject canvasFade = GameObject.Find("Canvas_FadeTransition");
        if (canvasFade == null)
        {
            canvasFade = new GameObject("Canvas_FadeTransition");
        }

        Canvas c = canvasFade.GetComponent<Canvas>();
        if (c == null) c = canvasFade.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 999; // Urutan rendering paling depan

        UnityEngine.UI.CanvasScaler cs = canvasFade.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (cs == null) cs = canvasFade.AddComponent<UnityEngine.UI.CanvasScaler>();
        cs.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1280, 720);
        cs.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight = 0.5f;

        if (canvasFade.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
        {
            canvasFade.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // 2. Cari atau Instantiate Fade Panel
        GameObject fadePanelGo = null;
        Transform existingChild = canvasFade.transform.Find("Fade Panel");
        if (existingChild != null) fadePanelGo = existingChild.gameObject;

        if (fadePanelGo == null)
        {
            Animator[] anims = Object.FindObjectsByType<Animator>(FindObjectsInactive.Include);
            foreach (var anim in anims)
            {
                if (anim.gameObject.name.Contains("Fade"))
                {
                    fadePanelGo = anim.gameObject;
                    break;
                }
            }
        }

#if UNITY_EDITOR
        if (fadePanelGo == null)
        {
            GameObject fadePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Develop 3D Mobile Games in Unity/Assets/QuizAdventure/Prefabs/Fade Panel.prefab");
            if (fadePrefab != null)
            {
                fadePanelGo = PrefabUtility.InstantiatePrefab(fadePrefab, canvasFade.transform) as GameObject;
            }
        }
#endif

        if (fadePanelGo != null)
        {
            fadePanelGo.name = "Fade Panel";
            fadePanelGo.transform.SetParent(canvasFade.transform, false);

            // Skala WAJIB Vector3.one (1, 1, 1) agar tidak menciut menjadi kotak kecil (0.1x)
            fadePanelGo.transform.localScale = Vector3.one;

            RectTransform rt = fadePanelGo.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
            }

            // Pastikan Image menggunakan warna hitam flat solid tanpa terdistorsi/terpotong sprite tombol
            UnityEngine.UI.Image img = fadePanelGo.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.sprite = null; // Hapus referensi button_square agar solid flat fullscreen
                img.type = UnityEngine.UI.Image.Type.Simple;
                img.preserveAspect = false;
                img.raycastTarget = false;
            }

            // Nonaktifkan tombol menu anak (Quit, Return, Home) agar layar transisi bersih
            for (int i = 0; i < fadePanelGo.transform.childCount; i++)
            {
                fadePanelGo.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Menyiapkan SubSceneManager dengan komponen SubSceneLoader untuk modul 6.
    /// </summary>
    public static GameObject EnsureSubSceneManager(GameObject playerGo = null)
    {
        GameObject managerGo = GameObject.Find("SubSceneManager");
        if (managerGo == null)
        {
            managerGo = new GameObject("SubSceneManager");
        }

        SubSceneLoader loader = managerGo.GetComponent<SubSceneLoader>();
        if (loader == null) loader = managerGo.AddComponent<SubSceneLoader>();

        if (playerGo != null) loader.player = playerGo.transform;
        loader.numberOfColumns = 3;
        loader.numberOfRows = 3;
        loader.loadRadius = 1;
        loader.tickTime = 0.1f;
        loader.baseSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        loader.EnsureConfigured();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SerializedObject so = new SerializedObject(loader);
            SerializedProperty playerProp = so.FindProperty("player");
            if (playerGo != null && playerProp != null)
            {
                playerProp.objectReferenceValue = playerGo.transform;
            }

            SerializedProperty subScenesProp = so.FindProperty("subScenes");
            string[] guids = AssetDatabase.FindAssets("t:SubSceneData", new[] { "Assets/ScriptableObjects/SubScenes", "Assets/Resources/SubScenes" });
            if (guids != null && guids.Length > 0 && subScenesProp != null)
            {
                var list = new System.Collections.Generic.List<SubSceneData>();
                foreach (var g in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(g);
                    SubSceneData data = AssetDatabase.LoadAssetAtPath<SubSceneData>(path);
                    if (data != null && !list.Exists(x => x.sceneName == data.sceneName)) list.Add(data);
                }
                list.Sort((a, b) => string.Compare(a.sceneName, b.sceneName, System.StringComparison.Ordinal));
                subScenesProp.arraySize = list.Count;
                for (int i = 0; i < list.Count; i++)
                {
                    subScenesProp.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
                }
            }

            SerializedProperty baseSceneProp = so.FindProperty("baseSceneName");
            if (baseSceneProp != null)
            {
                baseSceneProp.stringValue = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }

            SerializedProperty colsProp = so.FindProperty("numberOfColumns");
            if (colsProp != null) colsProp.intValue = 3;
            SerializedProperty rowsProp = so.FindProperty("numberOfRows");
            if (rowsProp != null) rowsProp.intValue = 3;
            SerializedProperty radiusProp = so.FindProperty("loadRadius");
            if (radiusProp != null) radiusProp.intValue = 1;
            SerializedProperty tickProp = so.FindProperty("tickTime");
            if (tickProp != null) tickProp.floatValue = 0.1f;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(managerGo);
        }
#endif
        return managerGo;
    }

    /// <summary>
    /// Menyiapkan sistem Sinematografi & Cutscene (Modul 8) di area Donut Shop:
    /// - CinemachineBrain pada Main Camera.
    /// - Karakter NPC Sarah (Waitress) di konter Donut Shop.
    /// - Virtual Cameras (Wide, Dolly Pan, Waitress Medium Shot).
    /// - UI Cutscene Banner.
    /// - PlayableDirector Cutscene_Manager dengan City_CutsceneTimeline.playable & AudioSource Jingle.
    /// - CutsceneTriggerZone di trotoar depan Donut Shop.
    /// </summary>
    public static GameObject EnsureCutsceneSystem(GameObject playerGo = null)
    {
        // 1. Setup CinemachineBrain pada Main Camera
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = Object.FindAnyObjectByType<Camera>();
        CinemachineBrain brain = null;
        if (mainCam != null)
        {
            brain = mainCam.GetComponent<CinemachineBrain>();
            if (brain == null) brain = mainCam.gameObject.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.8f);
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.enabled = false;
        }

        // 2. Tentukan posisi Donut Shop
        Vector3 donutShopPos = new Vector3(316.0f, 0.02f, 15.0f);
        GameObject donutPoi = GameObject.Find("POI_DonutShop");
        if (donutPoi != null)
        {
            donutShopPos = new Vector3(donutPoi.transform.position.x - 0.2f, 0.02f, donutPoi.transform.position.z + 1.5f);
        }

        // 3. Setup Sarah (Waitress) di depan konter Donut Shop
        GameObject sarahGo = GameObject.Find("NPC_Sarah_Waitress");
        if (sarahGo == null)
        {
#if UNITY_EDITOR
            string waitressPrefabPath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimplePeople/Prefabs/SimplePeople_Waitress_White.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(waitressPrefabPath);
            if (prefab != null)
            {
                sarahGo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }
#endif
            if (sarahGo == null)
            {
                sarahGo = new GameObject("NPC_Sarah_Waitress");
            }
        }

        sarahGo.name = "NPC_Sarah_Waitress";
        sarahGo.tag = "NPC";
        sarahGo.transform.position = donutShopPos;
        sarahGo.transform.rotation = Quaternion.Euler(0f, 135f, 0f);
        sarahGo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        sarahGo.SetActive(true);

        AutoEnsureCharacterMaterial(sarahGo);

        // Animator Sarah
        Animator anim = sarahGo.GetComponent<Animator>();
        if (anim == null) anim = sarahGo.AddComponent<Animator>();
#if UNITY_EDITOR
        RuntimeAnimatorController animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
        if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;
#endif
        anim.applyRootMotion = false;
        anim.SetInteger("Animation_type", 1);

        // Interaksi dialog
        NPCWalker walker = sarahGo.GetComponent<NPCWalker>();
        if (walker == null) walker = sarahGo.AddComponent<NPCWalker>();
        walker.characterDisplayName = "Sarah (Waitress)";
        walker.characterLine = "Selamat datang di Donut Shop! Cobalah menu donat lezat andalan kami hari ini!";
        walker.navMode = NPCWalker.NavigationMode.Stationary;
        walker.isLookingAroundInPlace = true;
        walker.lookAroundAngle = 30f;
        walker.enablePlayerInteraction = true;
        walker.interactionDistance = 1.8f;
        if (playerGo != null) walker.playerTarget = playerGo.transform;

        // 4. Root Container untuk Cutscene
        GameObject rootGo = GameObject.Find("[MODULE8_CUTSCENE]");
        if (rootGo == null)
        {
            rootGo = new GameObject("[MODULE8_CUTSCENE]");
        }

        // Target fokus di titik tengah gedung Toko Donut (bukan di kaki Sarah)
        GameObject shopFocusGo = GameObject.Find("Target_DonutShopCenter");
        if (shopFocusGo == null)
        {
            shopFocusGo = new GameObject("Target_DonutShopCenter");
            shopFocusGo.transform.SetParent(rootGo.transform);
        }
        GameObject donutShopGo = GameObject.Find("donut_shop");
        Vector3 shopCenter = (donutShopGo != null)
            ? new Vector3(donutShopGo.transform.position.x, donutShopGo.transform.position.y + 0.95f, donutShopGo.transform.position.z)
            : new Vector3(315.62f, 0.95f, 16.29f);
        shopFocusGo.transform.position = shopCenter;

        // 5. CM_vcam_Wide
        GameObject wideCamGo = GameObject.Find("CM_vcam_Wide");
        if (wideCamGo == null)
        {
            wideCamGo = new GameObject("CM_vcam_Wide");
            wideCamGo.transform.SetParent(rootGo.transform);
        }
        wideCamGo.transform.position = new Vector3(320.0f, 2.6f, 10.2f);
        CinemachineVirtualCamera vcamWide = wideCamGo.GetComponent<CinemachineVirtualCamera>();
        if (vcamWide == null) vcamWide = wideCamGo.AddComponent<CinemachineVirtualCamera>();
        vcamWide.Priority = 0;
        vcamWide.m_Lens.FieldOfView = 52f; // Sudut lebar arsitektural menyorot seluruh gedung toko donat
        vcamWide.m_LookAt = shopFocusGo.transform; // Arahkan fokus ke titik tengah toko donat
        var wideComposer = vcamWide.GetCinemachineComponent<CinemachineComposer>();
        if (wideComposer == null) wideComposer = vcamWide.AddCinemachineComponent<CinemachineComposer>();
        wideComposer.m_TrackedObjectOffset = Vector3.zero;
        wideComposer.m_ScreenX = 0.5f;
        wideComposer.m_ScreenY = 0.5f;
        wideComposer.m_DeadZoneWidth = 0f;
        wideComposer.m_DeadZoneHeight = 0f;
        wideComposer.m_SoftZoneWidth = 0.8f;
        wideComposer.m_SoftZoneHeight = 0.8f;

        // 6. Dolly Track & CM_vcam_Dolly (Mengitari / memutari Toko Donut 360 derajat)
        GameObject dollyTrackGo = GameObject.Find("DollyTrack_DonutShop");
        if (dollyTrackGo == null)
        {
            dollyTrackGo = new GameObject("DollyTrack_DonutShop");
            dollyTrackGo.transform.SetParent(rootGo.transform);
        }
        dollyTrackGo.transform.position = Vector3.zero;
        dollyTrackGo.transform.rotation = Quaternion.identity;
        CinemachineSmoothPath smoothPath = dollyTrackGo.GetComponent<CinemachineSmoothPath>();
        if (smoothPath == null) smoothPath = dollyTrackGo.AddComponent<CinemachineSmoothPath>();
        smoothPath.m_Looped = false;
        smoothPath.m_Waypoints = new CinemachineSmoothPath.Waypoint[]
        {
            // Waypoint 0: Depan-Timur di jalan/trotoar depan toko donat
            new CinemachineSmoothPath.Waypoint { position = new Vector3(319.4f, 2.1f, 13.0f), roll = 0f },
            // Waypoint 1: Sudut Utara-Timur di tikungan jalan samping toko donat
            new CinemachineSmoothPath.Waypoint { position = new Vector3(318.6f, 2.5f, 19.8f), roll = 0f },
            // Waypoint 2: Belakang Barat-Utara elevated tinggi menyorot donat raksasa di atas atap
            new CinemachineSmoothPath.Waypoint { position = new Vector3(312.2f, 3.2f, 17.5f), roll = 0f },
            // Waypoint 3: Kembali memutari ke arah depan toko donat
            new CinemachineSmoothPath.Waypoint { position = new Vector3(318.2f, 1.8f, 12.2f), roll = 0f }
        };

        GameObject dollyCamGo = GameObject.Find("CM_vcam_Dolly");
        if (dollyCamGo == null)
        {
            dollyCamGo = new GameObject("CM_vcam_Dolly");
            dollyCamGo.transform.SetParent(rootGo.transform);
        }
        dollyCamGo.transform.position = new Vector3(319.4f, 2.1f, 13.0f);
        CinemachineVirtualCamera vcamDolly = dollyCamGo.GetComponent<CinemachineVirtualCamera>();
        if (vcamDolly == null) vcamDolly = dollyCamGo.AddComponent<CinemachineVirtualCamera>();
        vcamDolly.Priority = 0;
        vcamDolly.m_Lens.FieldOfView = 50f; // Sudut pandang luas untuk sapuan sinematik mengitari toko donat
        vcamDolly.m_LookAt = shopFocusGo.transform; // Fokus memusat ke titik tengah toko donat saat kamera berputar
        var dollyComponent = vcamDolly.GetCinemachineComponent<CinemachineTrackedDolly>();
        if (dollyComponent == null) dollyComponent = vcamDolly.AddCinemachineComponent<CinemachineTrackedDolly>();
        dollyComponent.m_Path = smoothPath;
        dollyComponent.m_CameraUp = CinemachineTrackedDolly.CameraUpMode.Default;

        // Pasang CinemachineComposer agar kamera aktif berputar mengunci pandangan ke gedung toko donat
        var dollyComposer = vcamDolly.GetCinemachineComponent<CinemachineComposer>();
        if (dollyComposer == null) dollyComposer = vcamDolly.AddCinemachineComponent<CinemachineComposer>();
        dollyComposer.m_TrackedObjectOffset = Vector3.zero;
        dollyComposer.m_ScreenX = 0.5f;
        dollyComposer.m_ScreenY = 0.5f;
        dollyComposer.m_DeadZoneWidth = 0f;
        dollyComposer.m_DeadZoneHeight = 0f;
        dollyComposer.m_SoftZoneWidth = 0.8f;
        dollyComposer.m_SoftZoneHeight = 0.8f;
        dollyComposer.m_HorizontalDamping = 0.1f;
        dollyComposer.m_VerticalDamping = 0.1f;

        Animator dollyAnim = dollyCamGo.GetComponent<Animator>();
        if (dollyAnim == null) dollyAnim = dollyCamGo.AddComponent<Animator>();

        // 7. CM_vcam_Waitress (Medium Portrait Shot)
        GameObject waitressFocusGo = GameObject.Find("Target_WaitressFocus");
        if (waitressFocusGo == null)
        {
            waitressFocusGo = new GameObject("Target_WaitressFocus");
            waitressFocusGo.transform.SetParent(rootGo.transform);
        }
        waitressFocusGo.transform.position = sarahGo.transform.position + new Vector3(0f, 0.14f, 0f);

        GameObject waitressCamGo = GameObject.Find("CM_vcam_Waitress");
        if (waitressCamGo == null)
        {
            waitressCamGo = new GameObject("CM_vcam_Waitress");
            waitressCamGo.transform.SetParent(rootGo.transform);
        }
        waitressCamGo.transform.position = new Vector3(317.8f, 0.28f, 13.8f);
        CinemachineVirtualCamera vcamWaitress = waitressCamGo.GetComponent<CinemachineVirtualCamera>();
        if (vcamWaitress == null) vcamWaitress = waitressCamGo.AddComponent<CinemachineVirtualCamera>();
        vcamWaitress.Priority = 0;
        vcamWaitress.m_Lens.FieldOfView = 40f; // Medium shot terbuka memperlihatkan etalase & Sarah
        vcamWaitress.m_LookAt = waitressFocusGo.transform; // Fokus terarah tepat ke tubuh atas Sarah & konter
        var waitressComposer = vcamWaitress.GetCinemachineComponent<CinemachineComposer>();
        if (waitressComposer == null) waitressComposer = vcamWaitress.AddCinemachineComponent<CinemachineComposer>();
        waitressComposer.m_TrackedObjectOffset = Vector3.zero;
        waitressComposer.m_ScreenX = 0.5f;
        waitressComposer.m_ScreenY = 0.5f;
        waitressComposer.m_DeadZoneWidth = 0f;
        waitressComposer.m_DeadZoneHeight = 0f;
        waitressComposer.m_SoftZoneWidth = 0.8f;
        waitressComposer.m_SoftZoneHeight = 0.8f;

        // 8. UI Cutscene Banner
        GameObject bannerGo = EnsureCutsceneBanner(rootGo);

        // 9. Cutscene_Manager (PlayableDirector & AudioSource)
        GameObject managerGo = GameObject.Find("Cutscene_Manager");
        if (managerGo == null)
        {
            managerGo = new GameObject("Cutscene_Manager");
            managerGo.transform.SetParent(rootGo.transform);
        }
        AudioSource audioSource = managerGo.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = managerGo.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
#if UNITY_EDITOR
        AudioClip jingleClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/CutsceneJingle.wav");
        if (jingleClip != null) audioSource.clip = jingleClip;
#endif

        PlayableDirector director = managerGo.GetComponent<PlayableDirector>();
        if (director == null) director = managerGo.AddComponent<PlayableDirector>();
        director.playOnAwake = false;
#if UNITY_EDITOR
        TimelineAsset timelineAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>("Assets/Timelines/City_CutsceneTimeline.playable");
        if (timelineAsset == null)
        {
            timelineAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>("Assets/Timelines/Module8_CutsceneTimeline.playable");
        }
        if (timelineAsset != null)
        {
            director.playableAsset = timelineAsset;

            // Hubungkan scene bindings
            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track is CinemachineTrack)
                {
                    if (brain != null) director.SetGenericBinding(track, brain);
                }
                else if (track is AnimationTrack)
                {
                    director.SetGenericBinding(track, dollyAnim);
                }
                else if (track is ActivationTrack)
                {
                    director.SetGenericBinding(track, bannerGo);
                }
                else if (track is AudioTrack)
                {
                    director.SetGenericBinding(track, audioSource);
                }
            }

            // Exposed reference binding untuk ketiga virtual camera
            director.SetReferenceValue((PropertyName)"c1000000000000000000000000000001", vcamWide);
            director.SetReferenceValue((PropertyName)"c1000000000000000000000000000002", vcamDolly);
            director.SetReferenceValue((PropertyName)"c1000000000000000000000000000003", vcamWaitress);
            // Fallback bindings jika memakai Module8_CutsceneTimeline lama
            director.SetReferenceValue((PropertyName)"f51104e43b177d8489c79590ea7e0102", vcamDolly);
            director.SetReferenceValue((PropertyName)"f51104e43b177d8489c79590ea7e0101", vcamWaitress);
        }
#endif

        // 10. CutsceneTriggerZone di trotoar depan Donut Shop
        GameObject triggerGo = GameObject.Find("CutsceneTriggerZone_DonutShop");
        if (triggerGo == null)
        {
            triggerGo = new GameObject("CutsceneTriggerZone_DonutShop");
            triggerGo.transform.SetParent(rootGo.transform);
        }
        triggerGo.transform.position = new Vector3(316.2f, 0.5f, 15.2f);
        BoxCollider boxCol = triggerGo.GetComponent<BoxCollider>();
        if (boxCol == null) boxCol = triggerGo.AddComponent<BoxCollider>();
        boxCol.isTrigger = true;
        boxCol.size = new Vector3(2.4f, 2.2f, 2.6f);

        CutsceneTrigger trigger = triggerGo.GetComponent<CutsceneTrigger>();
        if (trigger == null) trigger = triggerGo.AddComponent<CutsceneTrigger>();
        trigger.cutsceneDirector = director;
        trigger.playOnStart = false;
        trigger.playOnce = true;
        trigger.playerTag = "Player";
        trigger.introBanner = bannerGo;

        // Pasang CutsceneTriggerProxy pada gedung donut_shop fisik sehingga sentuhan langsung ke bangunan toko memicu cutscene
        GameObject donutShopBuilding = GameObject.Find("donut_shop");
        if (donutShopBuilding != null)
        {
            CutsceneTriggerProxy proxy = donutShopBuilding.GetComponent<CutsceneTriggerProxy>();
            if (proxy == null) proxy = donutShopBuilding.AddComponent<CutsceneTriggerProxy>();
            proxy.targetTrigger = trigger;
        }

        return rootGo;
    }

    private static GameObject EnsureCutsceneBanner(GameObject parentRoot)
    {
        GameObject bannerGo = GameObject.Find("UI_CutsceneBanner");
        if (bannerGo != null)
        {
            bannerGo.SetActive(false);
            return bannerGo;
        }

        GameObject canvasGo = GameObject.Find("Canvas_CutsceneIntro");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("Canvas_CutsceneIntro");
            if (parentRoot != null) canvasGo.transform.SetParent(parentRoot.transform);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
        }

        bannerGo = new GameObject("UI_CutsceneBanner");
        bannerGo.transform.SetParent(canvasGo.transform, false);

        RectTransform bannerRect = bannerGo.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 0.85f);
        bannerRect.anchorMax = new Vector2(0.5f, 0.85f);
        bannerRect.pivot = new Vector2(0.5f, 0.5f);
        bannerRect.sizeDelta = new Vector2(500f, 90f);
        bannerRect.anchoredPosition = Vector2.zero;

        Image bgImage = bannerGo.AddComponent<Image>();
        bgImage.color = new Color(0.08f, 0.08f, 0.12f, 0.88f);

        // Header Title
        GameObject titleGo = new GameObject("TitleText");
        titleGo.transform.SetParent(bannerGo.transform, false);
        RectTransform titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.offsetMin = new Vector2(15f, 0f);
        titleRect.offsetMax = new Vector2(-15f, -8f);

        Text titleText = titleGo.AddComponent<Text>();
        titleText.text = "🍩 DONUT SHOP - SARAH";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.97f, 0.82f, 0.33f, 1f);

        // Subtitle
        GameObject subGo = new GameObject("SubtitleText");
        subGo.transform.SetParent(bannerGo.transform, false);
        RectTransform subRect = subGo.AddComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0f);
        subRect.anchorMax = new Vector2(1f, 0.5f);
        subRect.pivot = new Vector2(0.5f, 0.5f);
        subRect.offsetMin = new Vector2(15f, 8f);
        subRect.offsetMax = new Vector2(-15f, 0f);

        Text subText = subGo.AddComponent<Text>();
        subText.text = "Temui Sarah di konter Donut Shop untuk mencicipi donat kota!";
        subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subText.fontSize = 15;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        bannerGo.SetActive(false);
        return bannerGo;
    }

    /// <summary>
    /// Menyiapkan sistem Cinemachine & Timeline Cutscene Rian Mengantar Paket (10 Detik) di scene Module 2:
    /// - Mengonfigurasi CinemachineBrain pada Main Camera.
    /// - Menyiapkan 3 Virtual Cameras (Pak Beni di depan Mall, Clara di Coffee Shop, Pemandangan Jalan Kota).
    /// - Memuat Timeline Asset 'Assets/Timelines/RianDeliveryCutsceneTimeline.playable' & PlayableDirector.
    /// - Mengikat trek Cinemachine & Audio ke scene objects.
    /// - Memasang dan menginisialisasi controller runtime RianDeliveryCutscene (auto-play, subtitle, instant skip).
    /// </summary>
    public static GameObject EnsureRianDeliveryCutsceneSystem(GameObject playerGo = null)
    {
        // 1. CinemachineBrain pada Main Camera
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = Object.FindAnyObjectByType<Camera>();
        CinemachineBrain brain = null;
        ThirdPersonCamera tpc = null;
        if (mainCam != null)
        {
            brain = mainCam.GetComponent<CinemachineBrain>();
            if (brain == null) brain = mainCam.gameObject.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.6f);
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.enabled = false;

            tpc = mainCam.GetComponent<ThirdPersonCamera>();
        }

        PlayerController pc = playerGo != null ? playerGo.GetComponent<PlayerController>() : null;
        if (pc == null) pc = Object.FindAnyObjectByType<PlayerController>();

        // 2. Root Container untuk Cutscene
        GameObject rootGo = GameObject.Find("RianDeliveryCutscene_System");
        if (rootGo == null)
        {
            rootGo = new GameObject("RianDeliveryCutscene_System");
        }
        rootGo.transform.position = Vector3.zero;
        rootGo.transform.rotation = Quaternion.identity;

        // 3. AudioSource untuk Jingle
        AudioSource audioSrc = rootGo.GetComponent<AudioSource>();
        if (audioSrc == null) audioSrc = rootGo.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;

        // 4. PlayableDirector
        PlayableDirector director = rootGo.GetComponent<PlayableDirector>();
        if (director == null) director = rootGo.AddComponent<PlayableDirector>();
        director.playOnAwake = false;
        director.timeUpdateMode = DirectorUpdateMode.GameTime;

        // 5. Shot 1: Pak Beni & Rian di depan Mall
        GameObject cam1Go = GameObject.Find("CM_Delivery_Shot1_PakBeni");
        if (cam1Go == null)
        {
            cam1Go = new GameObject("CM_Delivery_Shot1_PakBeni");
            cam1Go.transform.SetParent(rootGo.transform);
        }
        cam1Go.transform.position = new Vector3(311.6f, 0.45f, 4.8f);
        cam1Go.transform.rotation = Quaternion.Euler(5f, -145f, 0f);

        CinemachineVirtualCamera vcam1 = cam1Go.GetComponent<CinemachineVirtualCamera>();
        if (vcam1 == null) vcam1 = cam1Go.AddComponent<CinemachineVirtualCamera>();
        vcam1.Priority = 0;
        vcam1.m_Lens.FieldOfView = 45f;

        GameObject focus1 = GameObject.Find("Target_DeliveryShot1_Focus");
        if (focus1 == null)
        {
            focus1 = new GameObject("Target_DeliveryShot1_Focus");
            focus1.transform.SetParent(rootGo.transform);
        }
        focus1.transform.position = new Vector3(310.2f, 0.25f, 6.0f);
        vcam1.m_LookAt = focus1.transform;
        var comp1 = vcam1.GetCinemachineComponent<CinemachineComposer>();
        if (comp1 == null) comp1 = vcam1.AddCinemachineComponent<CinemachineComposer>();
        comp1.m_TrackedObjectOffset = Vector3.zero;

        // Shot 2: Clara & Rian di Coffee Shop
        GameObject cam2Go = GameObject.Find("CM_Delivery_Shot2_ClaraCoffee");
        if (cam2Go == null)
        {
            cam2Go = new GameObject("CM_Delivery_Shot2_ClaraCoffee");
            cam2Go.transform.SetParent(rootGo.transform);
        }
        cam2Go.transform.position = new Vector3(314.0f, 0.42f, 10.6f);
        cam2Go.transform.rotation = Quaternion.Euler(6f, -140f, 0f);

        CinemachineVirtualCamera vcam2 = cam2Go.GetComponent<CinemachineVirtualCamera>();
        if (vcam2 == null) vcam2 = cam2Go.AddComponent<CinemachineVirtualCamera>();
        vcam2.Priority = 0;
        vcam2.m_Lens.FieldOfView = 45f;

        GameObject focus2 = GameObject.Find("Target_DeliveryShot2_Focus");
        if (focus2 == null)
        {
            focus2 = new GameObject("Target_DeliveryShot2_Focus");
            focus2.transform.SetParent(rootGo.transform);
        }
        focus2.transform.position = new Vector3(315.2f, 0.25f, 9.1f);
        vcam2.m_LookAt = focus2.transform;
        var comp2 = vcam2.GetCinemachineComponent<CinemachineComposer>();
        if (comp2 == null) comp2 = vcam2.AddCinemachineComponent<CinemachineComposer>();
        comp2.m_TrackedObjectOffset = Vector3.zero;

        // Shot 3: Pemandangan koridor jalan kota luas
        GameObject cam3Go = GameObject.Find("CM_Delivery_Shot3_CityStreet");
        if (cam3Go == null)
        {
            cam3Go = new GameObject("CM_Delivery_Shot3_CityStreet");
            cam3Go.transform.SetParent(rootGo.transform);
        }
        cam3Go.transform.position = new Vector3(322.0f, 1.8f, -2.5f);
        cam3Go.transform.rotation = Quaternion.Euler(14f, -40f, 0f);

        CinemachineVirtualCamera vcam3 = cam3Go.GetComponent<CinemachineVirtualCamera>();
        if (vcam3 == null) vcam3 = cam3Go.AddComponent<CinemachineVirtualCamera>();
        vcam3.Priority = 0;
        vcam3.m_Lens.FieldOfView = 55f;

        GameObject focus3 = GameObject.Find("Target_DeliveryShot3_Focus");
        if (focus3 == null)
        {
            focus3 = new GameObject("Target_DeliveryShot3_Focus");
            focus3.transform.SetParent(rootGo.transform);
        }
        focus3.transform.position = new Vector3(312.0f, 0.4f, 12.0f);
        vcam3.m_LookAt = focus3.transform;
        var comp3 = vcam3.GetCinemachineComponent<CinemachineComposer>();
        if (comp3 == null) comp3 = vcam3.AddCinemachineComponent<CinemachineComposer>();
        comp3.m_TrackedObjectOffset = Vector3.zero;

        // 6. Muat Timeline Asset
#if UNITY_EDITOR
        string timelinePath = "Assets/Timelines/RianDeliveryCutsceneTimeline.playable";
        TimelineAsset timelineAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
        if (timelineAsset != null)
        {
            director.playableAsset = timelineAsset;

            // Bind clips
            director.SetReferenceValue("rian_shot1_pakbeni", vcam1);
            director.SetReferenceValue("rian_shot2_claracoffee", vcam2);
            director.SetReferenceValue("rian_shot3_citystreet", vcam3);

            // Bind tracks
            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track is CinemachineTrack && brain != null)
                {
                    director.SetGenericBinding(track, brain);
                }
                else if (track is AudioTrack)
                {
                    director.SetGenericBinding(track, audioSrc);
                }
            }
        }
#endif

        // 7. Pasang dan konfigurasi RianDeliveryCutscene
        RianDeliveryCutscene cutsceneCtrl = rootGo.GetComponent<RianDeliveryCutscene>();
        if (cutsceneCtrl == null) cutsceneCtrl = rootGo.AddComponent<RianDeliveryCutscene>();
        cutsceneCtrl.cutsceneDirector = director;
        cutsceneCtrl.cinemachineBrain = brain;
        cutsceneCtrl.thirdPersonCamera = tpc;
        cutsceneCtrl.playerController = pc;
        cutsceneCtrl.playOnStart = false;
        cutsceneCtrl.playOnce = true;
        cutsceneCtrl.allowSkip = true;
        cutsceneCtrl.showSubtitles = true;

        Debug.Log("[PlayerBootstrap] 🎬 Sistem Cutscene Rian Mengantar Paket (10 Detik) Siap Dijalankan!");
        return rootGo;
    }
}
