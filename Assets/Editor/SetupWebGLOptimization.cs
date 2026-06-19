using UnityEngine;
using UnityEditor;

public class SetupWebGLOptimization
{
    [MenuItem("Optimization/Apply WebGL Settings")]
    public static void Apply()
    {
        // 1. Switch Active Build Target to WebGL if not already
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
        {
            Debug.Log("[WebGL Setup] Switching build target to WebGL...");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        }

        // 2. Configure Publish & Loading Settings
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true; // Essential for hosting environments without custom Brotli headers
        PlayerSettings.WebGL.dataCaching = true; // Cache build files on user browser using IndexedDB
        PlayerSettings.WebGL.nameFilesAsHashes = true; // Cache busting

        // 3. Configure WASM & Browser Compatibility
        PlayerSettings.WebGL.webAssemblyBigInt = true; // Use native 64-bit int ABI, improves speed and size
        PlayerSettings.WebGL.webAssemblyTable = true;  // Speeds up indirect function calls
        PlayerSettings.WebGL.threadsSupport = false;  // Disabled by default to avoid COOP/COEP header requirements on servers

        // 4. Memory Management (Safe growth & budget)
        PlayerSettings.WebGL.initialMemorySize = 64; // Low initial footprint to avoid startup OOMs
        PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric; // Grow as needed
        PlayerSettings.WebGL.maximumMemorySize = 1024; // 1GB max limit to prevent mobile browser crashes

        // 5. Exception Handling & Performance
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly; // Best balance of debuggability and performance

        // 6. Global Build optimizations for size
        PlayerSettings.stripEngineCode = true; // Strip unused engine modules
        PlayerSettings.SetIl2CppCodeGeneration(UnityEditor.Build.NamedBuildTarget.WebGL, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);

        Debug.Log("[WebGL Setup] High-Performance WebGL Optimization Settings applied successfully!");
    }
}
