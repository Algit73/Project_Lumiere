using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;


namespace LMNT {
public class LMNT_Rev : MonoBehaviour 
{
  private AudioSource _audioSource;
  private string _apiKey;
  private List<Voice> _voiceList;
  private DownloadHandlerAudioClip _handler;

  private string _previousDialogue;
  private string _previousVoice;

  public string voice;
  public string dialogue;

  void Awake() 
  {
    _audioSource = gameObject.GetComponent<AudioSource>();
    if (_audioSource == null) {
      _audioSource = gameObject.AddComponent<AudioSource>();
    }
    _apiKey = LMNTLoader.LoadApiKey();
    _voiceList = LMNTLoader.LoadVoices();
  }

  public IEnumerator Prefetch() 
  {
    // Check if the voice or dialogue has changed
    if (_handler != null && _previousDialogue == dialogue && _previousVoice == voice) {
      yield break; // No need to refetch if nothing has changed
    }

    // Store the current dialogue and voice
    _previousDialogue = dialogue;
    _previousVoice = voice;

    WWWForm form = new WWWForm();
    form.AddField("voice", LookupByName(voice));
    form.AddField("text", dialogue);
    using (UnityWebRequest request = UnityWebRequest.Post(Constants.LMNT_SYNTHESIZE_URL, form)) {
      _handler = new DownloadHandlerAudioClip(Constants.LMNT_SYNTHESIZE_URL, AudioType.WAV);
      request.SetRequestHeader("X-API-Key", _apiKey);
      request.SetRequestHeader("X-Client", "unity/0.1.0"); // Example hard-coded version
      request.downloadHandler = _handler;
      yield return request.SendWebRequest();

      if (request.result == UnityWebRequest.Result.Success) {
        _audioSource.clip = _handler.audioClip;
      } else {
        Debug.LogError("Failed to fetch audio clip: " + request.error);
      }
    }
  }

  public IEnumerator Talk() 
  {
    // Always refetch if necessary
    yield return StartCoroutine(Prefetch());

    if (_audioSource.clip == null) 
    {
      yield return new WaitUntil(() => _audioSource.clip != null);
    }

    _audioSource.Play();
  }

  private string LookupByName(string name) {
    return _voiceList.Find(v => v.name == name).id;
  }
}
}


