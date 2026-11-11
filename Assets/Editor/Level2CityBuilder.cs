#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameDev.CityBuilder
{
    [InitializeOnLoad]
    public static class Level2CityBuilder
    {
        static Level2CityBuilder()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (activeScene.path == Level2CityGenerator.Stage2ScenePath)
            {
                if (GameObject.Find(Level2CityGenerator.RootCityName) == null)
                {
                    Debug.Log("[Level2CityBuilder] Module 2_Stage2 terdeteksi tanpa Kota Baru, membangun...");
                    Level2CityGenerator.BuildLevel2City(false);
                }
            }
        }

        [MenuItem("Kurir Terakhir/🏆 Bangun Kota Baru Level 2 (Bake ke Scene)", priority = 10)]
        public static void MenuBakeLevel2City()
        {
            Level2CityGenerator.BuildLevel2City(false);
        }
    }
}
#endif
