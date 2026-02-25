using UnityEngine;
using UnityEditor;

public class AndroidDebugSetup : MonoBehaviour
{
    [MenuItem("Tools/Android Debug Setup")]
    public static void SetupAndroidDebugging()
    {
        // Enable development build
        EditorUserBuildSettings.development = true;
        
        // Enable script debugging
        EditorUserBuildSettings.allowDebugging = true;
        
        // Enable deep profiling for better debugging
        EditorUserBuildSettings.connectProfiler = true;
        
        // Set Android as target platform
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        
        Debug.Log("Android debugging setup completed!");
        Debug.Log("Make sure to:");
        Debug.Log("1. Build with Development Build enabled");
        Debug.Log("2. Enable Script Debugging in Build Settings");
        Debug.Log("3. Connect your Android 16 device via WiFi debugging");
        Debug.Log("4. Use 'adb connect <device_ip>:5555' if using wireless debugging");
    }
    
    [MenuItem("Tools/Check Android Debug Status")]
    public static void CheckDebugStatus()
    {
        Debug.Log($"Development Build: {EditorUserBuildSettings.development}");
        Debug.Log($"Script Debugging: {EditorUserBuildSettings.allowDebugging}");
        Debug.Log($"Connect Profiler: {EditorUserBuildSettings.connectProfiler}");
        Debug.Log($"Active Build Target: {EditorUserBuildSettings.activeBuildTarget}");
    }
}