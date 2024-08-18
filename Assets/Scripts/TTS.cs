using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LMNT;

public class TTS : MonoBehaviour
{
    private LMNTSpeech speech;
    bool once = true;

  void Start() 
  {
    // ... your code here ...
    speech = GetComponent<LMNTSpeech>();
  }

  void Update() 
  {
    // ... your code here ...
    if (once) 
    {
        speech.dialogue = "Helloooo, welcome. This is Lumiere!";
        StartCoroutine(speech.Talk());
        once = false;
    }
  }
}
