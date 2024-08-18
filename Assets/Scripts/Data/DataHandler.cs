using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// this class written to handle transferring data through api and currently have no use

public class DataHandler
{
    private string _apiUrlForGet = "http://...";
    public string _apiUrlForSend = "http://...";

    public string Data { private set; get; }
    public bool IsWaitingForResponse { private set; get; }

    public void GetData(MonoBehaviour monoBehaviour) => monoBehaviour.StartCoroutine(GetDataFromAPI());
    public void SendData(MonoBehaviour monoBehaviour, string data) => monoBehaviour.StartCoroutine(SendDataToAPI(data));

    private IEnumerator GetDataFromAPI()
    {
        using UnityWebRequest webRequest = UnityWebRequest.Get(_apiUrlForGet);

        IsWaitingForResponse = false;

        yield return webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(webRequest.error);
        }
        else
        {
            Data = webRequest.downloadHandler.text;

            Debug.Log(Data);
        }

        IsWaitingForResponse = true;
    }
    private IEnumerator SendDataToAPI(string data)
    {
        using UnityWebRequest webRequest = new UnityWebRequest(_apiUrlForSend, "POST");

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(data);
        webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");

        yield return webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(webRequest.error);
        }
        else
        {
            Debug.Log("Data sent successfully");
        }
    }
}