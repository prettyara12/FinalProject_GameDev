using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Generator Resmi Kota Baru Level 2 (Module 2_Stage2):
/// - Menghadirkan tata letak kota 100% baru dengan aset SimpleTown & SimpleBuildings yang ada di proyek.
/// - Desain Koridor Ganda (North Avenue Z=+20 & South Avenue Z=-20), Central Boulevard (X=0),
///   dan Gang Liku Pejalan Kaki (X=-25, X=+10, X=+25).
/// - Suasana Senja / Golden Hour (cahaya amber hangat sudut rendah & lampu jalan temaram).
/// - Sirkulasi lalu lintas mobil di koridor jalan raya sebagai rintangan aktif.
/// - Menjamin integritas Module 2.unity (scene awal) 100% utuh tanpa modifikasi.
/// </summary>
public static class Level2CityGenerator
{
    public const string Stage2ScenePath = "Assets/Module 2_Stage2.unity";
    public const string RootCityName = "Kota Baru Level 2";

#if UNITY_EDITOR
    [MenuItem("Kurir Terakhir/🏆 Bangun Kota Baru Level 2", priority = 0)]
    public static void MenuBuildCityLevel2()
    {
        BuildLevel2City(false);
    }
#endif

    public static GameObject BuildLevel2City(bool isRuntime)
    {
        Scene activeScene = SceneManager.GetActiveScene();

#if UNITY_EDITOR
        if (!isRuntime && !Application.isPlaying)
        {
            if (activeScene.path != Stage2ScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(Stage2ScenePath);
            }
        }
#endif

        // Keamanan ketat: Jangan pernah menyentuh Module 2.unity
        if (activeScene.path == "Assets/Module 2.unity")
        {
            Debug.LogError("[Level2CityGenerator] CRITICAL: Module 2.unity tidak boleh dimodifikasi!");
            return null;
        }

        Debug.Log("[Level2CityGenerator] Memulai pembangunan Kota Baru Level 2...");

        // 1. Bersihkan elemen kota lama di Module 2_Stage2
        CleanupLegacyCityObjects();

        // 2. Buat Root Container Baru
        GameObject cityRoot = GameObject.Find(RootCityName);
        if (cityRoot != null)
        {
            SafeDestroy(cityRoot);
        }
        cityRoot = new GameObject(RootCityName);
        cityRoot.transform.position = Vector3.zero;

        // 3. Bangun Pondasi Aspal Dasar
        BuildAsphaltFoundation(cityRoot.transform);

        // 4. Bangun Jaringan Jalan Raya & Trotoar
        BuildRoadAndSidewalkNetwork(cityRoot.transform);

        // 5. Bangun Bangunan Inti Misi & Bangunan Kota
        BuildAllCityBuildings(cityRoot.transform);

        // 6. Pasang Properti Jalan, Lampu, Pohon & Barikade
        BuildStreetPropsAndLamps(cityRoot.transform);

        // 7. Pasang Kendaraan Lalu Lintas Bergerak
        BuildTrafficCars(cityRoot.transform);

        // 8. Terapkan Pencahayaan Senja (Golden Hour)
        ApplyGoldenHourLighting();

        // 9. Pastikan Seluruh Bangunan Memiliki Collider
        EnsureBuildingColliders(cityRoot);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[Level2CityGenerator] ⭐ Kota Baru Level 2 Berhasil Disimpan ke " + Stage2ScenePath);
        }
#endif

        return cityRoot;
    }

    private static void CleanupLegacyCityObjects()
    {
        string[] legacyNames = new string[]
        {
            "Jalan Raya (2)",
            "Jalan Raya (3)",
            "Jalan Raya (5)",
            "Path_Trotoar_Melawan_Arah",
            "Dinding Tanaman",
            "Plane",
            "SidewalkPath_02",
            "SidewalkPath_03",
            "SidewalkPath_04",
            "Cutscene_Manager",
            "Canvas_CutsceneIntro",
            "CutsceneTriggerZone_DonutShop",
            "SubSceneManager",
            "Point Light"
        };

        List<GameObject> toRemove = new List<GameObject>();
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var go in roots)
        {
            if (go == null) continue;
            if (go.name == "Main Camera" || go.name == "DeliveryGameManager" || go.name == "Canvas_DeliveryHUD")
            {
                continue;
            }

            Light l = go.GetComponent<Light>();
            if (l != null && l.type == LightType.Directional)
            {
                continue;
            }

            foreach (string leg in legacyNames)
            {
                if (go.name == leg || go.name.StartsWith(leg))
                {
                    toRemove.Add(go);
                    break;
                }
            }
        }

        foreach (var go in toRemove)
        {
            if (go != null)
            {
                SafeDestroy(go);
            }
        }
    }

    private static void BuildAsphaltFoundation(Transform parent)
    {
        GameObject baseAsphalt = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseAsphalt.name = "Fondasi_Aspal_Kota";
        baseAsphalt.transform.SetParent(parent, false);
        baseAsphalt.transform.position = new Vector3(0f, -0.05f, 0f);
        baseAsphalt.transform.localScale = new Vector3(120f, 0.1f, 90f);

#if UNITY_EDITOR
        Material roadMat = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Materials/Road.mat");
        if (roadMat != null)
        {
            baseAsphalt.GetComponent<MeshRenderer>().sharedMaterial = roadMat;
        }
#endif

        BoxCollider col = baseAsphalt.GetComponent<BoxCollider>();
        if (col != null)
        {
            col.isTrigger = false;
        }
    }

    private static void BuildRoadAndSidewalkNetwork(Transform parent)
    {
        GameObject roadContainer = new GameObject("Jalan_Raya_Dan_Trotoar");
        roadContainer.transform.SetParent(parent, false);

        string roadStraightPath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Environemnt/road_straight_mesh.prefab";
        string roadCrossingPath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Environemnt/road_crossing_mesh.prefab";
        string pathStraightPath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Props/path_straight_mesh.prefab";

        // 1. KORIDOR UTARA (North Avenue, Z = +20, X dari -40 s/d +40)
        for (float x = -40f; x <= 40f; x += 10f)
        {
            if (Mathf.Abs(x) < 2f)
            {
                LoadAndInstantiate(roadCrossingPath, new Vector3(0f, 0.005f, 20f), Quaternion.Euler(0f, 0f, 0f), roadContainer.transform, "Persimpangan_Utara");
            }
            else
            {
                LoadAndInstantiate(roadStraightPath, new Vector3(x, 0.005f, 20f), Quaternion.Euler(0f, 90f, 0f), roadContainer.transform, $"NorthAvenue_{x}");
            }
        }

        // 2. KORIDOR SELATAN (South Avenue, Z = -20, X dari -40 s/d +40)
        for (float x = -40f; x <= 40f; x += 10f)
        {
            if (Mathf.Abs(x) < 2f)
            {
                LoadAndInstantiate(roadCrossingPath, new Vector3(0f, 0.005f, -20f), Quaternion.Euler(0f, 0f, 0f), roadContainer.transform, "Persimpangan_Selatan");
            }
            else
            {
                LoadAndInstantiate(roadStraightPath, new Vector3(x, 0.005f, -20f), Quaternion.Euler(0f, 90f, 0f), roadContainer.transform, $"SouthAvenue_{x}");
            }
        }

        // 3. CENTRAL BOULEVARD (Penghubung Utara-Selatan, X = 0, Z dari -10 s/d +10)
        for (float z = -10f; z <= 10f; z += 10f)
        {
            LoadAndInstantiate(roadStraightPath, new Vector3(0f, 0.005f, z), Quaternion.Euler(0f, 0f, 0f), roadContainer.transform, $"CentralBoulevard_{z}");
        }

        // 4. TROTOAR PEJALAN KAKI & JALUR GANG LIKU
        // Trotoar sepanjang North Avenue (Sisi Utara & Selatan)
        for (float x = -40f; x <= 40f; x += 5f)
        {
            if (Mathf.Abs(x) < 3f) continue;
            LoadAndInstantiate(pathStraightPath, new Vector3(x, 0.01f, 23.5f), Quaternion.Euler(0f, 90f, 0f), roadContainer.transform, $"Sidewalk_North_Top_{x}");
            LoadAndInstantiate(pathStraightPath, new Vector3(x, 0.01f, 16.5f), Quaternion.Euler(0f, 90f, 0f), roadContainer.transform, $"Sidewalk_North_Btm_{x}");
        }

        // Trotoar sepanjang South Avenue (Sisi Utara & Selatan)
        for (float x = -40f; x <= 40f; x += 5f)
        {
            if (Mathf.Abs(x) < 3f) continue;
            LoadAndInstantiate(pathStraightPath, new Vector3(x, 0.01f, -16.5f), Quaternion.Euler(0f, 90f, 0f), roadContainer.transform, $"Sidewalk_South_Top_{x}");
            LoadAndInstantiate(pathStraightPath, new Vector3(x, 0.01f, -23.5f), Quaternion.Euler(0f, 90f, 0f), roadContainer.transform, $"Sidewalk_South_Btm_{x}");
        }

        // Gang Liku Pejalan Kaki (Safe Zones)
        // Gang Barat (X = -25, Z dari -15 s/d +15)
        for (float z = -15f; z <= 15f; z += 5f)
        {
            LoadAndInstantiate(pathStraightPath, new Vector3(-25f, 0.01f, z), Quaternion.Euler(0f, 0f, 0f), roadContainer.transform, $"Alley_West_{z}");
        }

        // Gang Tengah (X = +10, Z dari -15 s/d +15 - dekat Toko Donat)
        for (float z = -15f; z <= 15f; z += 5f)
        {
            LoadAndInstantiate(pathStraightPath, new Vector3(10f, 0.01f, z), Quaternion.Euler(0f, 0f, 0f), roadContainer.transform, $"Alley_Central_{z}");
        }

        // Gang Timur (X = +25, Z dari -15 s/d +15)
        for (float z = -15f; z <= 15f; z += 5f)
        {
            LoadAndInstantiate(pathStraightPath, new Vector3(25f, 0.01f, z), Quaternion.Euler(0f, 0f, 0f), roadContainer.transform, $"Alley_East_{z}");
        }
    }

    private static void BuildAllCityBuildings(Transform parent)
    {
        GameObject bldContainer = new GameObject("Bangunan_Kota");
        bldContainer.transform.SetParent(parent, false);

        // A. TARGET MISI LEVEL 2
        // 1. MEGA MALL (Pusat Pengiriman - Pak Beni & Titik Awal Rian)
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Buildings/Building_Mall.prefab",
            new Vector3(0f, 0f, -5f), Quaternion.Euler(0f, 0f, 0f), bldContainer.transform, "Building_MegaMall");

        // 2. COFFEE SHOP (Target Clara - Paket 1)
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Buildings/Building_CoffeeShop.prefab",
            new Vector3(-25f, 0f, 24f), Quaternion.Euler(0f, 180f, 0f), bldContainer.transform, "Building_CoffeeShop_Clara");

        // 3. DRUG STORE (Target Pak Joko - Paket 2)
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Buildings/Building_StoreCorner_Drug.prefab",
            new Vector3(-25f, 0f, -24f), Quaternion.Euler(0f, 0f, 0f), bldContainer.transform, "Building_DrugStore_PakJoko");

        // 4. FIRE STATION (Target Dimas - Paket 3)
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleBuildings/Prefabs/fire_station.prefab",
            new Vector3(25f, 0f, 24f), Quaternion.Euler(0f, 180f, 0f), bldContainer.transform, "Building_FireStation_Dimas");

        // 5. TOKO DONAT (Lokasi Paket Terjatuh di Gang Tengah - Misi 4)
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleBuildings/Prefabs/donut_shop.prefab",
            new Vector3(10f, 0f, 6f), Quaternion.Euler(0f, -90f, 0f), bldContainer.transform, "Building_DonutShop_Alley");

        // 6. GEDUNG APARTEMEN BESAR MERAH (Target Naya - Paket 5)
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Buildings/Building_ApartmentLarge_Red.prefab",
            new Vector3(25f, 0f, -24f), Quaternion.Euler(0f, 0f, 0f), bldContainer.transform, "Building_Apartment_Naya");

        // B. BANGUNAN KOTA PENUNJANG
        // Distrik Barat
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Buildings/Building_Cinema.prefab",
            new Vector3(-36f, 0f, 5f), Quaternion.Euler(0f, 90f, 0f), bldContainer.transform, "Building_Cinema");
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Buildings/Building_Gym.prefab",
            new Vector3(-36f, 0f, -5f), Quaternion.Euler(0f, 90f, 0f), bldContainer.transform, "Building_Gym");
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Buildings/Building_BaberShop.prefab",
            new Vector3(-14f, 0f, 5f), Quaternion.Euler(0f, -90f, 0f), bldContainer.transform, "Building_BarberShop");
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Buildings/Building_AutoRepair.prefab",
            new Vector3(-14f, 0f, -5f), Quaternion.Euler(0f, -90f, 0f), bldContainer.transform, "Building_AutoRepair");

        // Distrik Timur
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleBuildings/Prefabs/burger_shop_base.prefab",
            new Vector3(10f, 0f, -6f), Quaternion.Euler(0f, -90f, 0f), bldContainer.transform, "Building_BurgerShop");
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleBuildings/Prefabs/town_hall_red.prefab",
            new Vector3(36f, 0f, 5f), Quaternion.Euler(0f, -90f, 0f), bldContainer.transform, "Building_TownHall");
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleBuildings/Prefabs/police_station.prefab",
            new Vector3(36f, 0f, -5f), Quaternion.Euler(0f, -90f, 0f), bldContainer.transform, "Building_PoliceStation");
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleBuildings/Prefabs/church.prefab",
            new Vector3(15f, 0f, 24f), Quaternion.Euler(0f, 180f, 0f), bldContainer.transform, "Building_Church");

        // Hunian Pinggiran
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Buildings/Building_House_01.prefab",
            new Vector3(-48f, 0f, 20f), Quaternion.Euler(0f, 90f, 0f), bldContainer.transform, "House_West_1");
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Buildings/Building_House_02.prefab",
            new Vector3(-48f, 0f, 5f), Quaternion.Euler(0f, 90f, 0f), bldContainer.transform, "House_West_2");
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Buildings/Building_House_Green.prefab",
            new Vector3(-48f, 0f, -5f), Quaternion.Euler(0f, 90f, 0f), bldContainer.transform, "House_West_3");
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Buildings/Building_House_Red.prefab",
            new Vector3(-48f, 0f, -20f), Quaternion.Euler(0f, 90f, 0f), bldContainer.transform, "House_West_4");

        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Buildings/Building_House_Orange.prefab",
            new Vector3(48f, 0f, 20f), Quaternion.Euler(0f, -90f, 0f), bldContainer.transform, "House_East_1");
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Buildings/Building_House_01.prefab",
            new Vector3(48f, 0f, 5f), Quaternion.Euler(0f, -90f, 0f), bldContainer.transform, "House_East_2");
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Buildings/Building_House_02.prefab",
            new Vector3(48f, 0f, -5f), Quaternion.Euler(0f, -90f, 0f), bldContainer.transform, "House_East_3");
        LoadAndInstantiate("Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Buildings/Building_House_Red.prefab",
            new Vector3(48f, 0f, -20f), Quaternion.Euler(0f, -90f, 0f), bldContainer.transform, "House_East_4");
    }

    private static void BuildStreetPropsAndLamps(Transform parent)
    {
        GameObject propsContainer = new GameObject("Properti_Jalan_Dan_Lampu");
        propsContainer.transform.SetParent(parent, false);

        string lampPath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Props/lamp_mesh.prefab";
        string treeLargePath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Props/tree_large_mesh.prefab";
        string treeSmallPath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Props/tree_small_mesh.prefab";
        string dumpsterPath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Props/dumpster_mesh.prefab";
        string hydrantPath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Props/hydrant_mesh.prefab";
        string benchPath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleProps/_Prefabs/bench_Small.prefab";
        string signBarricadePath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleProps/_Prefabs/SignFolding_01.prefab";

        Color warmLampColor = new Color(1.0f, 0.88f, 0.62f, 1.0f);

        // 1. LAMPU JALAN
        float[] lampXCoords = new float[] { -35f, -20f, -5f, 5f, 20f, 35f };
        foreach (float x in lampXCoords)
        {
            CreateStreetLampWithLight(lampPath, new Vector3(x, 0f, 16.5f), Quaternion.Euler(0f, 0f, 0f), propsContainer.transform, warmLampColor);
            CreateStreetLampWithLight(lampPath, new Vector3(x, 0f, 23.5f), Quaternion.Euler(0f, 180f, 0f), propsContainer.transform, warmLampColor);
            CreateStreetLampWithLight(lampPath, new Vector3(x, 0f, -16.5f), Quaternion.Euler(0f, 0f, 0f), propsContainer.transform, warmLampColor);
            CreateStreetLampWithLight(lampPath, new Vector3(x, 0f, -23.5f), Quaternion.Euler(0f, 180f, 0f), propsContainer.transform, warmLampColor);
        }

        // Lampu Plaza Mall
        CreateStreetLampWithLight(lampPath, new Vector3(-4.5f, 0f, -2f), Quaternion.Euler(0f, 90f, 0f), propsContainer.transform, warmLampColor);
        CreateStreetLampWithLight(lampPath, new Vector3(4.5f, 0f, -2f), Quaternion.Euler(0f, -90f, 0f), propsContainer.transform, warmLampColor);
        CreateStreetLampWithLight(lampPath, new Vector3(-4.5f, 0f, 10f), Quaternion.Euler(0f, 90f, 0f), propsContainer.transform, warmLampColor);
        CreateStreetLampWithLight(lampPath, new Vector3(4.5f, 0f, 10f), Quaternion.Euler(0f, -90f, 0f), propsContainer.transform, warmLampColor);

        // 2. BANGKU & POHON DI PLAZA MALL
        LoadAndInstantiate(benchPath, new Vector3(-2.5f, 0f, -2f), Quaternion.Euler(0f, 90f, 0f), propsContainer.transform, "Bench_Plaza_Left");
        LoadAndInstantiate(benchPath, new Vector3(2.5f, 0f, -2f), Quaternion.Euler(0f, -90f, 0f), propsContainer.transform, "Bench_Plaza_Right");

        LoadAndInstantiate(treeLargePath, new Vector3(-6f, 0f, -4f), Quaternion.identity, propsContainer.transform, "Tree_Plaza_1");
        LoadAndInstantiate(treeLargePath, new Vector3(6f, 0f, -4f), Quaternion.identity, propsContainer.transform, "Tree_Plaza_2");
        LoadAndInstantiate(treeLargePath, new Vector3(-15f, 0f, 15f), Quaternion.identity, propsContainer.transform, "Tree_North_1");
        LoadAndInstantiate(treeLargePath, new Vector3(15f, 0f, -15f), Quaternion.identity, propsContainer.transform, "Tree_South_1");

        LoadAndInstantiate(treeSmallPath, new Vector3(-20f, 0f, 23.5f), Quaternion.identity, propsContainer.transform, "TreeSmall_1");
        LoadAndInstantiate(treeSmallPath, new Vector3(20f, 0f, -23.5f), Quaternion.identity, propsContainer.transform, "TreeSmall_2");

        // 3. TONG SAMPAH DI GANG LIKU
        LoadAndInstantiate(dumpsterPath, new Vector3(-24f, 0f, 3f), Quaternion.Euler(0f, 90f, 0f), propsContainer.transform, "Dumpster_WestAlley");
        LoadAndInstantiate(dumpsterPath, new Vector3(12f, 0f, 2f), Quaternion.Euler(0f, -90f, 0f), propsContainer.transform, "Dumpster_CentralAlley");
        LoadAndInstantiate(dumpsterPath, new Vector3(26f, 0f, 3f), Quaternion.Euler(0f, 90f, 0f), propsContainer.transform, "Dumpster_EastAlley");

        // 4. HYDRANT
        LoadAndInstantiate(hydrantPath, new Vector3(-4.5f, 0f, 17f), Quaternion.identity, propsContainer.transform, "Hydrant_NorthCross");
        LoadAndInstantiate(hydrantPath, new Vector3(4.5f, 0f, -17f), Quaternion.identity, propsContainer.transform, "Hydrant_SouthCross");

        // 5. BARIKADE & RAMBU PERBAIKAN JALAN
        LoadAndInstantiate(signBarricadePath, new Vector3(-8f, 0f, 20f), Quaternion.Euler(0f, 0f, 0f), propsContainer.transform, "Barricade_North_1");
        LoadAndInstantiate(signBarricadePath, new Vector3(8f, 0f, -20f), Quaternion.Euler(0f, 0f, 0f), propsContainer.transform, "Barricade_South_1");
    }

    private static void CreateStreetLampWithLight(string lampPrefabPath, Vector3 pos, Quaternion rot, Transform parent, Color lightColor)
    {
        GameObject lamp = LoadAndInstantiate(lampPrefabPath, pos, rot, parent, "StreetLamp");
        if (lamp == null) return;

        GameObject lightChild = new GameObject("StreetPointLight");
        lightChild.transform.SetParent(lamp.transform, false);
        lightChild.transform.localPosition = new Vector3(0f, 4.2f, 0.5f);

        Light pLight = lightChild.AddComponent<Light>();
        pLight.type = LightType.Point;
        pLight.color = lightColor;
        pLight.range = 10.0f;
        pLight.intensity = 1.8f;
        pLight.shadows = LightShadows.None;
    }

    private static void BuildTrafficCars(Transform parent)
    {
        GameObject trafficContainer = new GameObject("Lalu_Lintas_Mobil");
        trafficContainer.transform.SetParent(parent, false);

        string carBluePath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Vehicles/car_blue.prefab";
        string carRedPath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Vehicles/car_red.prefab";
        string taxiPath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Vehicles/taxi_mesh.prefab";
        string busPath = "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleTown/Prefabs/Vehicles/bus_blue.prefab";

        SetupPatrolCar(carBluePath, new Vector3(-30f, 0f, 20.8f), Quaternion.Euler(0f, 90f, 0f), trafficContainer.transform,
            new Vector3[] { new Vector3(-35f, 0f, 20.8f), new Vector3(35f, 0f, 20.8f) }, "Mobil_North_Eastbound");

        SetupPatrolCar(taxiPath, new Vector3(30f, 0f, 19.2f), Quaternion.Euler(0f, -90f, 0f), trafficContainer.transform,
            new Vector3[] { new Vector3(35f, 0f, 19.2f), new Vector3(-35f, 0f, 19.2f) }, "Mobil_North_Westbound");

        SetupPatrolCar(busPath, new Vector3(-25f, 0f, -19.2f), Quaternion.Euler(0f, 90f, 0f), trafficContainer.transform,
            new Vector3[] { new Vector3(-35f, 0f, -19.2f), new Vector3(35f, 0f, -19.2f) }, "Mobil_South_Eastbound");

        SetupPatrolCar(carRedPath, new Vector3(25f, 0f, -20.8f), Quaternion.Euler(0f, -90f, 0f), trafficContainer.transform,
            new Vector3[] { new Vector3(35f, 0f, -20.8f), new Vector3(-35f, 0f, -20.8f) }, "Mobil_South_Westbound");
    }

    private static void SetupPatrolCar(string prefabPath, Vector3 pos, Quaternion rot, Transform parent, Vector3[] waypoints, string name)
    {
        GameObject car = LoadAndInstantiate(prefabPath, pos, rot, parent, name);
        if (car == null) return;

        BoxCollider col = car.GetComponent<BoxCollider>();
        if (col == null)
        {
            col = car.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.7f, 0f);
            col.size = new Vector3(1.6f, 1.4f, 3.2f);
        }

        CarHazardTrigger hazard = car.GetComponent<CarHazardTrigger>();
        if (hazard == null)
        {
            hazard = car.AddComponent<CarHazardTrigger>();
            hazard.knockbackStrength = 3.5f;
            hazard.hitCooldown = 2.0f;
        }

        CityCarMover mover = car.GetComponent<CityCarMover>();
        if (mover == null)
        {
            mover = car.AddComponent<CityCarMover>();
        }
        mover.speed = 2.5f;
        mover.turnSpeed = 180f;
        mover.waypoints = waypoints;
    }

    private static void ApplyGoldenHourLighting()
    {
        Light dirLight = null;
        Light[] allLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in allLights)
        {
            if (l != null && l.type == LightType.Directional)
            {
                dirLight = l;
                break;
            }
        }

        if (dirLight == null)
        {
            GameObject lightGo = new GameObject("Directional Light");
            dirLight = lightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
        }

        dirLight.transform.rotation = Quaternion.Euler(18f, -45f, 0f);
        dirLight.color = new Color(1.0f, 0.70f, 0.38f, 1.0f);
        dirLight.intensity = 1.25f;
        dirLight.shadows = LightShadows.Soft;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.95f, 0.72f, 0.50f, 1.0f);
        RenderSettings.ambientEquatorColor = new Color(0.65f, 0.45f, 0.35f, 1.0f);
        RenderSettings.ambientGroundColor = new Color(0.25f, 0.20f, 0.18f, 1.0f);
    }

    private static void EnsureBuildingColliders(GameObject root)
    {
        Transform bldContainer = root.transform.Find("Bangunan_Kota");
        if (bldContainer == null) return;

        foreach (Transform child in bldContainer)
        {
            if (child == null) continue;
            Collider existingCol = child.GetComponentInChildren<Collider>();
            if (existingCol == null)
            {
                Renderer[] renderers = child.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds b = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        b.Encapsulate(renderers[i].bounds);
                    }

                    BoxCollider box = child.gameObject.AddComponent<BoxCollider>();
                    box.center = child.InverseTransformPoint(b.center);
                    box.size = b.size;
                }
            }
        }
    }

    private static GameObject LoadAndInstantiate(string prefabPath, Vector3 pos, Quaternion rot, Transform parent, string name)
    {
#if UNITY_EDITOR
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[Level2CityGenerator] Prefab tidak ditemukan: {prefabPath}");
            return null;
        }

        GameObject instance = null;
        if (!Application.isPlaying)
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }
        else
        {
            instance = UnityEngine.Object.Instantiate(prefab);
        }

        if (instance != null)
        {
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.position = pos;
            instance.transform.rotation = rot;
        }
        return instance;
#else
        return null;
#endif
    }

    private static void SafeDestroy(GameObject go)
    {
        if (go == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEngine.Object.DestroyImmediate(go);
            return;
        }
#endif
        UnityEngine.Object.Destroy(go);
    }
}

