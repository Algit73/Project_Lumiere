using System;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Models;
using UnityEngine;

public class OpenAITTSManager : MonoBehaviour
{
    [Header("OpenAI Config (optional)")]
    [SerializeField] private OpenAIConfiguration configuration;

    [Header("Voice Settings")]
    [SerializeField] private SpeechVoice voice = SpeechVoice.Nova;
    [SerializeField] private bool useHighDefinitionModel = true;
    [SerializeField] private SpeechResponseFormat responseFormat = SpeechResponseFormat.MP3;
    [SerializeField, Range(0.25f, 4f)] private float speed = 1.0f;

    [Header("Startup")]
    [SerializeField] private bool playOnStart;
    [SerializeField, TextArea(2, 5)] private string startupText = "Hello, welcome. This is Lumiere!";

    private OpenAIClient client;
    private AudioSource audioSource;
    private CancellationTokenSource ttsCts;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        client = configuration != null ? new OpenAIClient(configuration) : new OpenAIClient();
    }

    private void Start()
    {
        if (playOnStart && !string.IsNullOrWhiteSpace(startupText))
        {
            Speak(startupText);
        }
    }

    private void OnDestroy()
    {
        ttsCts?.Cancel();
        ttsCts?.Dispose();
        ttsCts = null;
    }

    public void Speak(string text)
    {
        _ = SpeakAsync(text);
    }

    public async Task SpeakAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogWarning("OpenAI TTS: text is empty.");
            return;
        }

        try
        {
            ttsCts?.Cancel();
            ttsCts?.Dispose();
            ttsCts = new CancellationTokenSource();

            var model = useHighDefinitionModel ? Model.TTS_1HD : Model.TTS_1;
            var request = new SpeechRequest(text, model, voice, responseFormat, speed);

            var result = await client.AudioEndpoint.CreateSpeechAsync(request, ttsCts.Token);
            var clip = result?.Item2;

            if (clip == null)
            {
                Debug.LogError("OpenAI TTS: received empty AudioClip.");
                return;
            }

            audioSource.clip = clip;
            audioSource.Play();
        }
        catch (Exception ex)
        {
            Debug.LogError($"OpenAI TTS error: {ex.Message}");
        }
    }
}
