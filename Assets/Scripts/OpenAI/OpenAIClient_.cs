// using System;
// using System.Collections.Generic;
// using System.Text;
// using System.Threading.Tasks;
// using UnityEngine;
// using UnityEngine.Networking; // Use UnityWebRequest for cross-platform compatibility
// using Newtonsoft.Json; // Keep Newtonsoft.Json for JSON serialization

// public class OpenAIClient
// {
//     private readonly string apiKey;

//     public OpenAIClient(string openAiApiKey)
//     {
//         apiKey = openAiApiKey;
//     }

//     public async Task<string> SendChatRequestAsync(List<ChatMessage> messages, string model = "gpt-4", float temperature = 0.5f)
//     {
//         var chatRequest = new ChatRequest
//         {
//             model = model,
//             messages = messages,
//             temperature = temperature
//         };

//         var jsonRequest = JsonConvert.SerializeObject(chatRequest); // Use Newtonsoft.Json to serialize the request
//         var content = new System.Text.UTF8Encoding().GetBytes(jsonRequest);

//         using (UnityWebRequest webRequest = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST"))
//         {
//             // Set the request headers and body
//             webRequest.uploadHandler = new UploadHandlerRaw(content);
//             webRequest.downloadHandler = new DownloadHandlerBuffer();
//             webRequest.SetRequestHeader("Content-Type", "application/json");
//             webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");

//             // Send the request and await the response
//             var operation = webRequest.SendWebRequest();

//             while (!operation.isDone)
//             {
//                 await Task.Yield(); // Wait until the request is done
//             }

//             if (webRequest.result == UnityWebRequest.Result.Success)
//             {
//                 var jsonResponse = webRequest.downloadHandler.text;
//                 var completionResponse = JsonConvert.DeserializeObject<ChatResponse>(jsonResponse); // Deserialize the response
//                 return completionResponse.Choices[0].Message.Content;
//             }
//             else
//             {
//                 Debug.LogError($"Error: {webRequest.responseCode}. Response: {webRequest.downloadHandler.text}");
//                 return null;
//             }
//         }
//     }
// }

// public class ChatRequest
// {
//     [JsonProperty("model")]
//     public string model;

//     [JsonProperty("messages")]
//     public List<ChatMessage> messages;

//     [JsonProperty("temperature")]
//     public float temperature;
// }

// public class ChatMessage
// {
//     [JsonProperty("role")]
//     public string Role;

//     [JsonProperty("content")]
//     public string Content;
// }

// public class ChatResponse
// {
//     [JsonProperty("choices")]
//     public List<Choice> Choices;

//     public class Choice
//     {
//         [JsonProperty("message")]
//         public MessageData Message;

//         public class MessageData
//         {
//             [JsonProperty("role")]
//             public string Role;

//             [JsonProperty("content")]
//             public string Content;
//         }
//     }
// }

// // Usage example:
// // var api = new OpenAIClient("YOUR_OPENAI_API_KEY");
// // var messages = new List<ChatMessage> 
// // { 
// //     new ChatMessage { Role = "user", Content = "Hello, GPT!" } 
// // };
// // string response = await api.SendChatRequestAsync(messages, "gpt-4", 0.5f);
// // Debug.Log(response);
