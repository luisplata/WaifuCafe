using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using GameManager;
using Unity.Cinemachine;
using UnityEditor.SceneManagement;

namespace EditorTools
{
    /// <summary>
    /// Editor tool to configure Version_1 scene with the proper hierarchy, components, and references.
    /// Run via Tools > Setup Version_1 Scene after opening the project.
    /// </summary>
    public static class SetupVersion1Scene
    {
        private const string ScenePath = "Assets/Scenes/Version_1.unity";

        [MenuItem("Tools/Setup Version_1 Scene")]
        public static void Execute()
        {
            // Open the scene
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            // Clear existing (keep only scene settings, remove all GameObjects)
            var allGOs = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            var rootGOs = new System.Collections.Generic.List<GameObject>();
            foreach (var go in allGOs)
            {
                if (go.transform.parent == null && go.name != "Main Camera")
                    rootGOs.Add(go);
            }
            foreach (var go in rootGOs)
            {
                Object.DestroyImmediate(go);
            }

            // Get or create Main Camera
            GameObject mainCamGO = GameObject.Find("Main Camera");
            if (mainCamGO == null)
            {
                mainCamGO = new GameObject("Main Camera");
                mainCamGO.tag = "MainCamera";
                mainCamGO.AddComponent<Camera>();
                mainCamGO.AddComponent<AudioListener>();
            }

            // Configure Main Camera
            Camera cam = mainCamGO.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.19215687f, 0.3019608f, 0.4745098f, 0f);
            cam.depth = -1;
            cam.farClipPlane = 1000;
            cam.nearClipPlane = 0.3f;
            cam.transform.localPosition = new Vector3(0, 0, -1);
            cam.transform.localRotation = Quaternion.identity;
            cam.transform.localScale = Vector3.one;

            // PixelPerfectCamera
            if (mainCamGO.GetComponent<PixelPerfectCamera>() == null)
            {
                var ppc = mainCamGO.AddComponent<PixelPerfectCamera>();
                ppc.assetsPPU = 100;
                ppc.refResolutionX = 320;
                ppc.refResolutionY = 180;
            }

            // CinemachineBrain (CM3 API — no m_ prefix)
            if (mainCamGO.GetComponent<CinemachineBrain>() == null)
            {
                var cb = mainCamGO.AddComponent<CinemachineBrain>();
                cb.ShowDebugText = false;
                cb.ShowCameraFrustum = true;
                cb.IgnoreTimeScale = false;
                cb.UpdateMethod = CinemachineBrain.UpdateMethods.SmartUpdate;
                cb.DefaultBlend = new CinemachineBlendDefinition(
                    CinemachineBlendDefinition.Styles.EaseInOut, 2f);
            }

            // Physics2DRaycaster
            if (mainCamGO.GetComponent<Physics2DRaycaster>() == null)
            {
                mainCamGO.AddComponent<Physics2DRaycaster>();
            }

            // UniversalAdditionalCameraData
            if (mainCamGO.GetComponent<UniversalAdditionalCameraData>() == null)
            {
                var uacd = mainCamGO.AddComponent<UniversalAdditionalCameraData>();
                uacd.renderShadows = true;
                uacd.requiresDepthTexture = false;
                uacd.requiresColorTexture = false;
            }

            // ============================================================
            // _Environment
            // ============================================================
            GameObject envGO = CreateFolder("_Environment");

            // Reparent Main Camera into _Environment
            mainCamGO.transform.SetParent(envGO.transform);
            mainCamGO.transform.localPosition = new Vector3(0, 0, -1);

            // CinemachineCamera (CM3 virtual camera — required for CinemachineBrain)
            GameObject cmCamGO = new GameObject("CinemachineCamera");
            cmCamGO.transform.SetParent(envGO.transform);
            cmCamGO.transform.localPosition = Vector3.zero;
            cmCamGO.transform.localRotation = Quaternion.identity;
            cmCamGO.transform.localScale = Vector3.one;
            cmCamGO.AddComponent<CinemachineCamera>();
            cmCamGO.AddComponent<CinemachinePixelPerfect>();

            // Global Light 2D
            GameObject lightGO = new GameObject("Global Light 2D");
            lightGO.transform.SetParent(envGO.transform);
            lightGO.transform.localPosition = Vector3.zero;
            var light2D = lightGO.AddComponent<Light2D>();
            // Use reflection to set LightType to Global since the property may not be exposed directly
            var lightTypeField = typeof(Light2D).GetField("m_LightType",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (lightTypeField != null)
                lightTypeField.SetValue(light2D, Light2D.LightType.Global);
            light2D.intensity = 1f;
            light2D.color = Color.white;
            // alphaBlendOnOverlap is read-only in this URP version — use overlapOperation instead
            light2D.overlapOperation = Light2D.OverlapOperation.Additive;

            // Background sprite
            GameObject bgGO = new GameObject("bg_maid_cafe_0");
            bgGO.transform.SetParent(envGO.transform);
            bgGO.transform.localPosition = Vector3.zero;
            bgGO.transform.localScale = new Vector3(2f, 2f, 2f);
            var sr = bgGO.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/bg_maid_cafe_0.png");
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 0;

            // ============================================================
            // EventSystem + Input
            // ============================================================
            GameObject esGO = new GameObject("EventSystem");
            var es = esGO.AddComponent<EventSystem>();
            es.sendNavigationEvents = true;
            es.pixelDragThreshold = 10;

            var inputModule = esGO.AddComponent<InputSystemUIInputModule>();
            var actionsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                "Assets/Settings/InputSystem_Actions.inputactions");
            if (actionsAsset != null)
            {
                // Assign via serializedObject to properly set m_ActionsAsset
                var so = new SerializedObject(inputModule);
                so.FindProperty("m_ActionsAsset").objectReferenceValue = actionsAsset;
                so.ApplyModifiedProperties();
            }

            // ============================================================
            // GameManager+
            // ============================================================
            GameObject gmGO = new GameObject("GameManager+");
            gmGO.transform.localPosition = new Vector3(344.9993f, 1505.5121f, 0f);

            var gm = gmGO.AddComponent<GameManager.GameManager>();
            var gmSO = new SerializedObject(gm);
            gmSO.FindProperty("preparationDuration").floatValue = 2f;
            gmSO.FindProperty("transitionDuration").floatValue = 2f;
            gmSO.FindProperty("gameOverAfterSeconds").floatValue = 60f;
            gmSO.FindProperty("autoStart").boolValue = true;
            // References start null — connected in later phases
            gmSO.FindProperty("staffManager").objectReferenceValue = null;
            gmSO.FindProperty("customerQueue").objectReferenceValue = null;
            gmSO.ApplyModifiedProperties();

            var regards = gmGO.AddComponent<RegardsManager>();
            var rmSO = new SerializedObject(regards);
            rmSO.FindProperty("gold").floatValue = 0f;
            rmSO.ApplyModifiedProperties();

            // Wire regardsManager reference in GameManager
            gmSO = new SerializedObject(gm);
            gmSO.FindProperty("regardsManager").objectReferenceValue = regards;
            gmSO.ApplyModifiedProperties();

            // ============================================================
            // Empty folders for later phases
            // ============================================================
            CreateFolder("_Queue");
            CreateFolder("_Staff");
            CreateFolder("ServiceCoordinator");
            CreateFolder("_UI");
            CreateFolder("DragManager");

            // Save
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("[Setup] Version_1 scene configured successfully!");
            Debug.Log("[Setup] Root GOs: _Environment (MainCamera, CinemachineCamera, GlobalLight2D, bg), EventSystem, GameManager+, _Queue, _Staff, ServiceCoordinator, _UI, DragManager");
            Debug.Log("[Setup] ⚠️ Connect StaffManager & CustomerQueue refs in GameManager+ during Phase 3-4");
        }

        private static GameObject CreateFolder(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }
    }
}
