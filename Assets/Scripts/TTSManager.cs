// TTSManager.cs

using UnityEngine;

public class TTSManager : MonoBehaviour
{
    private static TTSManager instance;
    private OpenAITTSManager openAiTts;

    // Reference to the LMNTSpeech component
    // private LMNT_Rev speech;
    

    // Singleton property to access the instance
    public static TTSManager Manager
    {
        get
        {
            if (instance == null)
            {
                // Create the instance if it doesn't exist
                instance = FindObjectOfType<TTSManager>();
                if (instance == null)
                {
                    // If no TTSManager exists in the scene, create an empty GameObject
                    GameObject managerObj = new GameObject("TTSManager");
                    instance = managerObj.AddComponent<TTSManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        // Ensure there's only one instance
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        CacheProviders();
    }

    // Public method to trigger speech
    public void Speak(string dialogue)
    {
        if (string.IsNullOrWhiteSpace(dialogue))
            return;

        if (openAiTts == null)
            CacheProviders();

        var openAi = GetComponent<OpenAITTSManager>();
        if (openAi != null)
        {
            openAi.Speak(dialogue);
            return;
        }

        if (openAiTts != null)
        {
            openAiTts.Speak(dialogue);
            return;
        }

        Debug.LogWarning("TTSManager: OpenAITTSManager not found. Add it to an active GameObject in scene.");
    }

    private void CacheProviders()
    {
        openAiTts = GetComponent<OpenAITTSManager>();
        if (openAiTts == null)
            openAiTts = FindObjectOfType<OpenAITTSManager>();
    }
}



// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using LMNT;

// public class TTS : MonoBehaviour
// {
//     private LMNTSpeech speech;
//     bool once = true;

//   void Start() 
//   {
//     // ... your code here ...
//     speech = GetComponent<LMNTSpeech>();
//   }

//   void Update() 
//   {
//     // ... your code here ...
//     if (once) 
//     {
//         speech.dialogue = "Helloooo, welcome. This is Lumiere!";
//         StartCoroutine(speech.Talk());
//         once = false;
//     }
//   }
// }

