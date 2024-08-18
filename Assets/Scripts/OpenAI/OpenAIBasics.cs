using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using UnityEngine;
using OpenAI;
using OpenAI.Assistants;

using NUnit.Framework;
using OpenAI.Models;
using OpenAI.Chat;
using Mono.Cecil.Cil;
using System.Threading.Tasks;
using System.IO;
// using UnityEngine;
// using UnityEngine.Assertions;



public class OpenAIBasics //: MonoBehaviour
{
    // Start is called before the first frame update
    string GPT4_TURBO_MODEL = "gpt-4-turbo";
    string GPT4_O_MODEL = "gpt-4o";
    string GPT35_TURBO_MODEL = "gpt-3.5-turbo";
    string GPT35_TURBO_MODEL_125 = "gpt-3.5-turbo-0125";
    string GPT4_VISION_MODEL_1106 = "gpt-4-1106-vision-preview";
    string GPT4_VISION_MODEL= "gpt-4-vision-preview";

    // Instructions instruction;

    OpenAIClient api;

    public OpenAIBasics()
    {
    // async void Start()
    // {
        Debug.Log("Start");

        string path_openai_key = "Assets/Scripts/OpenAI/OPENAI_KEY.txt";
        string OPENAI_KEY_TXT = File.ReadAllText(path_openai_key);

        api = new OpenAIClient(OPENAI_KEY_TXT);
        // instruction = new Instructions();
        // TextMesh textMesh = FindAnyObjectByType(typeof(TextMesh)) as TextMesh;
        // open_ai_get_models();
        // retrieve_model();
        // list_assistants();
        // chat_classic();
        // chat_stream();
        /*
        Models:
        gpt-4
        gpt-4-0613
        gpt-4-1106-preview
        gpt-4-vision-preview
        gpt-3.5-turbo-1106
        gpt-3.5-turbo-instruct
        */
        // var story_obj = await create_story("farm","shovel, male farmer, sheep");
        // Debug.Log($"story preface: {story_obj.preface}, story para_0: {story_obj.paragraphs[0].text}");

    }

    async void open_ai_get_models()
    {
        var api = new OpenAIClient(OPENAI_KEY);
        var models = await api.ModelsEndpoint.GetModelsAsync();

        foreach (var model in models)
        {
            Debug.Log("Inside For");
            Debug.Log(model.ToString());
        }

    }
    
    async void retrieve_model()
    {
        var model = await api.ModelsEndpoint.GetModelDetailsAsync("gpt-4-turbo");
        Debug.Log(model.ToString());
    }

    async void list_assistants()
    {
        var assistantsList = await api.AssistantsEndpoint.ListAssistantsAsync();
        // Debug.Log($"length: {assistantsList.Count()}");
        foreach (var assistant in assistantsList.Items)
        {Debug.Log($"{assistant} -> {assistant.CreatedAt}");}
    }

    async void creat_assist()
    {
        var api = new OpenAIClient();
        var request = new CreateAssistantRequest("gpt-3.5-turbo-1106");
        var assistant = await api.AssistantsEndpoint.CreateAssistantAsync(request);
    }

    

    public async Task<StoryObject> create_story(string scene, string items)
    {
        var system_prompt = Instructions.teacher_story_role_v01;
        system_prompt = system_prompt.Replace("scene", scene);
        system_prompt = system_prompt.Replace("items", items);


        var llm_return = await llm_do_task(system_prompt);
        var story_object = JsonUtility.FromJson<StoryObject>(llm_return);
        return story_object;
    }

    public async Task<string> hint_maker(StoryParagraph story_paragraph, string user_answer)
    {
        var system_prompt = Instructions.examiner_hint_role;
        system_prompt = system_prompt.Replace("{paragraph}", story_paragraph.text);
        system_prompt = system_prompt.Replace("{question}", story_paragraph.question);
        system_prompt = system_prompt.Replace("{right_answer}", story_paragraph.answer);
        system_prompt = system_prompt.Replace("{wrong_answer}", user_answer);

        var llm_return = await llm_do_task(system_prompt);
        return llm_return;
    }

    public async Task<string> llm_do_task(string system_prompt)
    {
        Assert.IsNotNull(api.ChatEndpoint);
        var messages = new List<Message>
        {new Message(Role.System, system_prompt)};

        var chatRequest = new ChatRequest(messages, GPT4_O_MODEL, temperature:1);
        // var chatRequest = new ChatRequest(messages, GPT4_TURBO_MODEL, temperature:1);
        var response = await api.ChatEndpoint.GetCompletionAsync(chatRequest);
        
        Assert.IsNotNull(response);
        Assert.IsNotNull(response.Choices);
        Assert.IsNotEmpty(response.Choices);

        var choice = response.Choices[0];    
        Debug.Log($"[{choice.Index}] {choice.Message.Role}: {choice} | Finish Reason: {choice.FinishReason}");

        var message_content = choice.Message.Content;
        response.GetUsage();
        
        return message_content.ToString();
    }

    async void chat_classic()
    {
        Assert.IsNotNull(api.ChatEndpoint);
        var messages = new List<Message>
            {
                new Message(Role.System, "You are a helpful assistant."),
                new Message(Role.User, "Who won the world series in 2020?"),
                new Message(Role.Assistant, "The Los Angeles Dodgers won the World Series in 2020."),
                new Message(Role.User, "Where was it played?"),
            };
            var chatRequest = new ChatRequest(messages, "gpt-4-1106-preview");
            var response = await api.ChatEndpoint.GetCompletionAsync(chatRequest);
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Choices);
            Assert.IsNotEmpty(response.Choices);

            foreach (var choice in response.Choices)
            {
                Debug.Log($"[{choice.Index}] {choice.Message.Role}: {choice} | Finish Reason: {choice.FinishReason}");
            }

            response.GetUsage();
    }

    async void chat_stream()
    {
        var messages = new List<Message>
        {
            new Message(Role.System, "You are a helpful assistant."),
            new Message(Role.User, "Who won the world series in 2020?"),
            new Message(Role.Assistant, "The Los Angeles Dodgers won the World Series in 2020."),
            new Message(Role.User, "Where was it played?"),
        };
        var chatRequest = new ChatRequest(messages,temperature:0);
        var response = await api.ChatEndpoint.StreamCompletionAsync(chatRequest, partialResponse =>
        {
            // Console.Write(partialResponse.FirstChoice.Delta.ToString());
            Debug.Log(partialResponse.FirstChoice.Delta.ToString());
        });
        var choice = response.FirstChoice;
        Debug.Log($"[{choice.Index}] {choice.Message.Role}: {choice.Message} | Finish Reason: {choice.FinishReason}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
