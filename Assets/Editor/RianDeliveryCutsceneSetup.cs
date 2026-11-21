using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Unity.Cinemachine;

namespace DeliveryGame.Editor
{
    /// <summary>
    /// Editor Tool untuk menyusun Cinemachine & Timeline Cutscene Rian Mengantar Paket (10 Detik)
    /// pada scene Module 2 secara otomatis dan sekali klik.
    /// </summary>
    public static class RianDeliveryCutsceneSetup
    {
        public const string ScenePath = "Assets/Module 2.unity";
        public const string TimelinePath = "Assets/Timelines/RianDeliveryCutsceneTimeline.playable";

        [MenuItem("Tools/🎬 Setup Rian Delivery Cutscene (10 Detik)", priority = 1)]
        public static void MenuSetupCutscene()
        {
            SetupCutsceneInModule2();
        }

        // Catatan: auto-run saat editor memuat ulang script DINONAKTIFKAN.
        // Sebelumnya fungsi ini membuka "Module 2" (OpenSceneMode.Single) di setiap compile,
        // sehingga scene lain yang sedang terbuka (mis. Level 2) bisa tertutup tanpa disimpan,
        // dan menimpa konfigurasi cutscene. Jalankan manual lewat menu Tools jika diperlukan.

        public static void SetupCutsceneInModule2()
        {
            // 1. Buka scene Module 2
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            if (!activeScene.IsValid())
            {
                Debug.LogError($"[RianDeliveryCutsceneSetup] Gagal membuka scene di {ScenePath}");
                return;
            }

            Debug.Log("[RianDeliveryCutsceneSetup] Memulai konfigurasi Cutscene Cinemachine & Timeline 10 Detik...");

            // 2. Pastikan Main Camera memiliki CinemachineBrain
            Camera mainCam = Camera.main;
            if (mainCam == null) mainCam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (mainCam == null)
            {
                Debug.LogError("[RianDeliveryCutsceneSetup] Main Camera tidak ditemukan di scene!");
                return;
            }

            CinemachineBrain brain = mainCam.GetComponent<CinemachineBrain>();
            if (brain == null) brain = mainCam.gameObject.AddComponent<CinemachineBrain>();
            brain.enabled = false; // default off sampai cutscene mulai
            EditorUtility.SetDirty(mainCam.gameObject);

            ThirdPersonCamera tpc = mainCam.GetComponent<ThirdPersonCamera>();

            // 3. Cari Player
            GameObject playerGo = GameObject.FindWithTag("Player");
            PlayerController pc = playerGo != null ? playerGo.GetComponent<PlayerController>() : null;

            // 4. Muat Timeline Asset
            TimelineAsset timelineAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelinePath);
            if (timelineAsset == null)
            {
                Debug.LogError($"[RianDeliveryCutsceneSetup] Timeline Asset tidak ditemukan di {TimelinePath}!");
                return;
            }

            // 5. Buat atau perbarui Objek Root Cutscene
            GameObject rootGo = GameObject.Find("RianDeliveryCutscene_System");
            if (rootGo == null)
            {
                rootGo = new GameObject("RianDeliveryCutscene_System");
            }
            rootGo.transform.position = Vector3.zero;
            rootGo.transform.rotation = Quaternion.identity;

            // Pasang AudioSource untuk jingle
            AudioSource audioSrc = rootGo.GetComponent<AudioSource>();
            if (audioSrc == null) audioSrc = rootGo.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false;
            audioSrc.spatialBlend = 0f; // 2D Stereo sound

            // Pasang PlayableDirector
            PlayableDirector director = rootGo.GetComponent<PlayableDirector>();
            if (director == null) director = rootGo.AddComponent<PlayableDirector>();
            director.playableAsset = timelineAsset;
            director.playOnAwake = false;
            director.timeUpdateMode = DirectorUpdateMode.GameTime;

            // 6. Buat 3 Virtual Cameras Cinemachine
            // Shot 1: Pak Beni & Rian di depan Mall
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

            // Shot 3: Pemandangan jalan raya kota saat Rian bersiap melanjutkan misi
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

            // 7. Bind Virtual Camera ke Timeline CinemachineTrack Clips
            director.SetReferenceValue("rian_shot1_pakbeni", vcam1);
            director.SetReferenceValue("rian_shot2_claracoffee", vcam2);
            director.SetReferenceValue("rian_shot3_citystreet", vcam3);

            // Bind Tracks ke Scene Objects
            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track is CinemachineTrack)
                {
                    director.SetGenericBinding(track, brain);
                }
                else if (track is AudioTrack)
                {
                    director.SetGenericBinding(track, audioSrc);
                }
            }

            // 8. Pasang komponen kontrol runtime RianDeliveryCutscene
            RianDeliveryCutscene cutsceneCtrl = rootGo.GetComponent<RianDeliveryCutscene>();
            if (cutsceneCtrl == null) cutsceneCtrl = rootGo.AddComponent<RianDeliveryCutscene>();
            cutsceneCtrl.cutsceneDirector = director;
            cutsceneCtrl.cinemachineBrain = brain;
            cutsceneCtrl.thirdPersonCamera = tpc;
            cutsceneCtrl.playerController = pc;
            cutsceneCtrl.playOnStart = false;
            cutsceneCtrl.allowSkip = true;
            cutsceneCtrl.showSubtitles = true;

            // 9. Simpan scene
            EditorUtility.SetDirty(rootGo);
            EditorUtility.SetDirty(director);
            EditorUtility.SetDirty(cutsceneCtrl);
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=green>[RianDeliveryCutsceneSetup] SUKSES:</color> Cinemachine & Timeline Cutscene Rian (10 Detik) berhasil dipasang lengkap di Module 2!");
        }
    }
}
