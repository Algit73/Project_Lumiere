// TTSManager.cs

using UnityEngine;
using LMNT;

public class TTSManager : MonoBehaviour
{
    private static TTSManager instance;

    // Reference to the LMNTSpeech component
    private LMNT_Rev speech;
    

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

        // Initialize the LMNTSpeech component
        
    }

    private void Start() 
    {
        speech = GetComponent<LMNT_Rev>();
        
    }

    // Public method to trigger speech
    public void Speak(string dialogue)
    {
        speech = GetComponent<LMNT_Rev>();
        speech.dialogue = dialogue;
        StartCoroutine(speech.Talk());
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

