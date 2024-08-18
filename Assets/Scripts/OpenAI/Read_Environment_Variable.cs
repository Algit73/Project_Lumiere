using UnityEngine;
using System;

public class Read_Environment_Variable : MonoBehaviour
{
    void Start()
    {
        // Replace "MY_VARIABLE_NAME" with the name of your environment variable
        string myVariable = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (!string.IsNullOrEmpty(myVariable))
        {
            Debug.Log("Environment Variable Value: " + myVariable);
        }
        else
        {
            Debug.Log("Environment Variable not found or is empty.");
        }
    }
}
