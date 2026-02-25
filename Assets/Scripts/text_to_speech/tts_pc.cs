using UnityEngine;

public class native_tts_manager : MonoBehaviour
{
    private static native_tts_manager instance;
    
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject ttsEngine;
#endif

    public static native_tts_manager Manager
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<native_tts_manager>();
                if (instance == null)
                {
                    GameObject managerObj = new GameObject("native_tts_manager");
                    instance = managerObj.AddComponent<native_tts_manager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        ttsEngine = new AndroidJavaObject("android.speech.tts.TextToSpeech", currentActivity, null);
#endif
    }

    private void Start() 
    {
        Speak("Hello, welcome. This is Lumiere!");
    }

    public void Speak(string text)
    {
    #if UNITY_ANDROID && !UNITY_EDITOR
        if (ttsEngine != null)
        {
            ttsEngine.Call<int>("speak", text, 0, null, null);
        }
    #elif UNITY_STANDALONE_WIN
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-Command \"Add-Type -AssemblyName System.Speech; $speak = New-Object System.Speech.Synthesis.SpeechSynthesizer; $speak.Speak('{text}')\"",
            CreateNoWindow = true,
            UseShellExecute = false
        };
        System.Diagnostics.Process.Start(startInfo);
    #else
        Debug.Log($"TTS: {text}");
    #endif
    }


    void OnDestroy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (ttsEngine != null)
        {
            ttsEngine.Call("shutdown");
        }
#endif
    }
}
