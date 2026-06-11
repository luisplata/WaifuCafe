using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class SetupMobileOptimization
{
    [MenuItem("Optimization/Apply Mobile Graphics Settings")]
    public static void Apply()
    {
        // 1. Lock Orientation to Landscape
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        Debug.Log("Orientation locked to Landscape.");

        string mobilePath = "Assets/Settings/MobileURP.asset";
        UniversalRenderPipelineAsset mobileURP = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(mobilePath);
        
        if (mobileURP == null)
        {
            mobileURP = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            AssetDatabase.CreateAsset(mobileURP, mobilePath);
        }

        // Optimization tweaks via SerializedObject
        SerializedObject so = new SerializedObject(mobileURP);
        
        SetProperty(so, "m_MSAA", 1); 
        SetProperty(so, "m_MainLightShadowsSupported", true);
        SetProperty(so, "m_MainLightShadowmapResolution", 1024);
        SetProperty(so, "m_SupportsHDR", false);

        // Assign Renderer Data (using existing 2D renderer if found)
        ScriptableRendererData rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/Renderer2D.asset");
        if (rendererData != null)
        {
            SerializedProperty rendererList = so.FindProperty("m_RendererDataList");
            rendererList.arraySize = 1;
            rendererList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            so.FindProperty("m_DefaultRendererIndex").intValue = 0;
        }
        
        so.ApplyModifiedProperties();
        
        EditorUtility.SetDirty(mobileURP);
        AssetDatabase.SaveAssets();

        // 2. Assign to ALL Quality Levels
        string[] names = QualitySettings.names;
        int current = QualitySettings.GetQualityLevel();
        for (int i = 0; i < names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = mobileURP;
        }
        QualitySettings.SetQualityLevel(current, false);

        // 3. Set Active Render Pipeline in Graphics Settings as fallback
        GraphicsSettings.defaultRenderPipeline = mobileURP;

        Debug.Log("Mobile Optimization Applied: URP created and assigned to all quality levels.");
    }

    private static void SetProperty(SerializedObject so, string name, object value)
    {
        SerializedProperty prop = so.FindProperty(name);
        if (prop != null)
        {
            if (value is int i) prop.intValue = i;
            else if (value is bool b) prop.boolValue = b;
            else if (value is float f) prop.floatValue = f;
            else if (value is string s) prop.stringValue = s;
        }
        else
        {
            Debug.LogWarning("Property not found: " + name);
        }
    }
}
