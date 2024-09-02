using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandsCenter : MonoBehaviour
{
    public static CommandsCenter Manager;

    [SerializeField] private Transform env;
    [SerializeField] private Transform cam;
    [SerializeField] private string commandData;
    [SerializeField] private bool command;
    [SerializeField] private string[] test;

    private Dictionary<string, Interactive> _interactivesDic;
    private Vector3 _cardFixDis;
    private Vector3 _camFixDis;

    public CardHandler Card { set; get; }
    public bool CommandsAreDone { private set; get; }

    private void Awake()
    {
        _interactivesDic = new Dictionary<string, Interactive>();

        if (Manager != null) Destroy(Manager);
        Manager = this;
        CommandsAreDone = true;
    }
    private void Start()
    {
        // calculate the distance between card and cam from environment
        // _cardFixDis = Card.transform.position - env.position;
        // _camFixDis = cam.position - env.position;
    }

    // this method will be called each time that any properties in inspector changed
    private void OnValidate()
    {
        if (command && commandData != "" && Application.isPlaying)
        {
            command = false;
           Set_instruction(commandData);
        }
    }

    // AddInteractives changed to involve cases that are multi part
    public void AddInteractives(Interactive interactive) => _interactivesDic.TryAdd(interactive.name, interactive);//_interactivesDic.Add(interactive.name, interactive);
    public void RemoveInteractives(string name) => _interactivesDic.Remove(name);
    public void Set_instruction(string data)
    {
        if (CommandsAreDone) StopAllCoroutines();
        StartCoroutine(SetCommands(data));
    }
    public void AddObjectToCard(InteractionData commandData)
    {
        int emptyCardNum = Card.GetEmptyCardNumber();

        if (emptyCardNum == -1) return;

        CommandsAreDone = false;

        commandData.ActionName = "SetImage";
        commandData.DataFloatValues = new float[] { emptyCardNum };

        StartCoroutine(CommandCard(commandData));
    }

    private IEnumerator SetCommands(string data)
    {
        // I'm using IEnumerators and 'yield return' to wait for the actions of objects to be done

        CommandsAreDone = false;

        SplitData(data, out InteractionData[] interactionData);

        foreach (InteractionData commandData in interactionData)
        {
            switch (commandData.ObjectName)
            {
                case "":
                    continue;
                case "CardUI":
                    yield return CommandCard(commandData);
                    break;
                case "Env":
                    yield return CommandEnv(commandData);
                    break;
                default:
                    yield return _interactivesDic[commandData.ObjectName].Interact(commandData);
                    break;
            }
        }

        CommandsAreDone = true;
    }
    private void SplitData(string data, out InteractionData[] interactionData)
    {
        string[] commands = data.Split('-');

        interactionData = new InteractionData[commands.Length];

        for (int i = 0; i < commands.Length; i++)
        {
            string[] splitValues = commands[i].Split('/');

            if (splitValues.Length < 2 || splitValues.Length > 4)
            {
                Debug.Log("Invalid data");
                continue;
            }

            interactionData[i].ObjectName = splitValues[0];
            interactionData[i].ActionName = splitValues[1];
            interactionData[i].DataFloatValues = new float[3];

            if (splitValues.Length == 3)
            {
                string[] dataValues = splitValues[2].Split(",");

                if (dataValues.Length == 1) interactionData[i].DataStringValue = splitValues[2];
                else
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (dataValues.Length > j)
                        {
                            if (int.TryParse(dataValues[j], out int x)) interactionData[i].DataFloatValues[j] = x;
                            else interactionData[i].DataFloatValues[j] = 0f;
                        }
                        else interactionData[i].DataFloatValues[j] = 0f;
                    }
                }
            }
            else if (splitValues.Length == 4)
            {
                interactionData[i].DataStringValue = splitValues[2];
                if (int.TryParse(splitValues[3], out int x)) interactionData[i].DataFloatValues[0] = x;
            }
        }
    }
    private IEnumerator CommandCard(InteractionData data)
    {
        // I'm using IEnumerators and 'yield return' to wait for the actions of objects to be done

        switch (data.ActionName)
        {
            case "SetImage":
                Card.SetImage(_interactivesDic[data.DataStringValue], (int)data.DataFloatValues[0]);
                break;
            case "ResetImage":
                yield return Card.ResetImage(int.Parse(data.DataStringValue));
                break;
            case "SetQuestion":
                Card.QuestionText = data.DataStringValue;
                break;
            case "SetResult":
                Card.ResultText = data.DataStringValue;
                break;
            case "ShowCard":
                yield return Card.ShowCard();
                break;
            case "HideCard":
                yield return Card.HideCard();
                break;
            default:
                Debug.Log("Invalid card command");
                break;
        }
    }
    private IEnumerator CommandEnv(InteractionData data)
    {
        // I'm using IEnumerators and 'yield return' to wait for the actions of objects to be done

        switch (data.ActionName)
        {
            case "SetEnvPos":
                SetEnvPos(new Vector3(data.DataFloatValues[0], data.DataFloatValues[1], data.DataFloatValues[2]));
                break;
            default:
                Debug.Log("Invalid env command");
                break;
        }

        yield return null;
    }

    private void SetCamPos(Vector3 pos) => cam.position = pos + _camFixDis;
    private void SetCardPos(Vector3 pos) => Card.transform.position = pos + _cardFixDis;
    private void SetEnvPos(Vector3 pos)
    {
        env.position = pos;
        SetCamPos(pos);
        SetCardPos(pos);
    }
}