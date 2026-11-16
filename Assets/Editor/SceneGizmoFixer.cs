#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    [InitializeOnLoad]
    public static class SceneGizmoFixer
    {
        static SceneGizmoFixer()
        {
            EditorApplication.delayCall += ApplyFix;
        }

        [MenuItem("Tools/🛠️ Perbaiki Tool & Gizmo Geser Objek", priority = 0)]
        public static void ApplyFix()
        {
            // 1. Kembalikan tool ke Move Tool (W)
            Tools.current = Tool.Move;
            Tools.hidden = false;
            Tools.lockedLayers = 0;

            // 2. Aktifkan kembali picking dan visibility untuk semua objek & layer di scene
            if (SceneVisibilityManager.instance != null)
            {
                SceneVisibilityManager.instance.EnableAllPicking();
                SceneVisibilityManager.instance.ShowAll();
            }

            // 3. Matikan preview animasi jika sedang aktif mengunci Transform
            if (AnimationMode.InAnimationMode())
            {
                AnimationMode.StopAnimationMode();
            }

            // 4. Buka kunci inspector jika terkunci
            if (ActiveEditorTracker.sharedTracker != null)
            {
                ActiveEditorTracker.sharedTracker.isLocked = false;
            }

            // 5. Normalisasi setiap SceneView yang terbuka
            if (SceneView.sceneViews != null)
            {
                foreach (SceneView sv in SceneView.sceneViews)
                {
                    if (sv == null) continue;

                    sv.drawGizmos = true;
                    sv.in2DMode = false;
                    sv.isRotationLocked = false;
                    sv.cameraMode = SceneView.GetBuiltinCameraMode(DrawCameraMode.Textured);
                    sv.Repaint();
                }
            }

            SceneView.RepaintAll();
            Debug.Log("<color=green>[SceneGizmoFixer] BERHASIL:</color> Move Tool (W), Gizmo 3D, Picking Objek, dan Tampilan Scene telah di-reset dan diaktifkan kembali!");
        }
    }
}
#endif
