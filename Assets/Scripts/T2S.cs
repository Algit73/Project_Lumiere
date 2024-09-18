using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LMNT;

public class T2S : MonoBehaviour
{
    // private LMNT_Rev speech;
    // bool once = true;

    void Start() 
    {
    // ... your code here ...
    // speech = GetComponent<LMNT_Rev>();
    // speech = GetComponent<LMNTSpeech>();
    }

    public void Speak(string dialogue)
    {
    LMNT_Rev speech;
    speech = GetComponent<LMNT_Rev>();
    speech.dialogue = dialogue;
    // StartCoroutine(speech.Prefetch());
    // StartCoroutine(New_Dialogue(dialogue));
    StartCoroutine(speech.Talk());
    }


}