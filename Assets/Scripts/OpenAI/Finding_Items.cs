using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using UnityEngine.EventSystems;
using Newtonsoft.Json;


using cmd = CommandsCenter;

using Find_Instructs = Lumiere_Finding_Instructions;
using System;
// using UnityEngine.UIElements;

public class Finding_Items : MonoBehaviour
{
    private OpenAIBasics openAI;
    private Transform[] available_card_objs;
    private int story_step = 0;
    private StoryObject story;
    
    private string scence_name;

    private bool question_asked = false;

    private bool is_game_started = false;

    enum Method_Num {M1, M2, M3, M4, M5, M6}
    // struct Method_Index {public int Mi1, Mi2, Mi3, Mi4, Mi5, Mi6;}
    private const int METHODS_LEN = 6;
    // struct MethodX_Prompts {public string Mp1, Mp2, Mp3, Mp4, Mp5, Mp6;}

    private Method_Num m_method_num;
    private int current_method_index;
    private List<string> method_prompts;

    private Finding_Items_Obj QAObjects;

    [SerializeField] private T2S t2s;



    [SerializeField] private UnityEngine.UI.Button confirm_button;
    // [SerializeField] private UnityEngine.UI.Button Ammat;

    private bool is_story_finished = false;

    // Start is called before the first frame update
    void Start()
    {
        // string filePath = Application.dataPath + "/Scripts/OpenAI/json.txt";
        // string fileContent = File.ReadAllText(filePath);
        // Finding_Items_Obj methodsContainer = Finding_Items_Obj.FromJson(fileContent);
        // string ammam = "{\"feedback\":\"Good job finding something in the freezer! The item you\'re looking for is a whole chicken. Do you see another item that might be a whole chicken?\", \"A\":\"false\"}";
        // var methodsDict = JsonConvert.DeserializeObject<Method_1_Obj>(ammam);
        // var ammaam = JsonUtility.FromJson<Method_1_Obj>(ammam);

        /// to clean the scene and be prepared for the next story
        reset_story_scene();

        confirm_button.onClick.AddListener(() => { on_confirm_clicked(); });

        openAI = new OpenAIBasics();

        scence_name = Instructions.scenes[(int)Instructions.scene_index.HOUSE];

        extract_all_objects_descriptions();

        method_prompts = new List<string>();
        for(int i = 0; i < METHODS_LEN; i++) method_prompts.Add("");
        
        
    }


    private async void preparing_game()
    {
        card_visible(true);
        cmd.Manager.single_object_card_mode = true;
        /// Being alert to the object clicking
        is_game_started = true;

        // zeroing all items in method_index
        // current_method_index = new Method_Index();

        /// Removing previous texts, preparing new game
        reset_card_texts();

        /// check if there is any item inside the card
        cmd.Manager.Card.Chat_Context = Find_Instructs.find_waiting_message;
        // TTSManager.Manager.Speak(Find_Instructs.find_waiting_message);
        t2s.Speak(Find_Instructs.find_waiting_message);

        /// Creating a list of random objects to find
        Dictionary<string, string> random_items_dic = random_obj_extracor();

        /// Request modified prompt
        var game_plot_prompt = main_prompt_modifying(random_items_dic);

        // string filePath = Application.dataPath + "/Scripts/OpenAI/json.txt";
        // string fileContent = File.ReadAllText(filePath);
        // Finding_Items_Obj methodsContainer = Finding_Items_Obj.FromJson(fileContent);

        /// Questions/Answers
        QAObjects = await GetQAObjects(game_plot_prompt);

        ///Initiating games based on the methods
        m_method_num = Method_Num.M1;
        initiating_method((int)m_method_num);
        // playing_method((int)method_num);


        /// Enabling the confirm_button
        confirm_button.gameObject.SetActive(true);

    }

    private async Task<Finding_Items_Obj> GetQAObjects(string game_plot_prompt)
    {
        var llm_return = await openAI.llm_do_task(game_plot_prompt);
        
        return Finding_Items_Obj.FromJson(string_trim(llm_return));
    }

    private string string_trim(string input)
    {
        input = input.Trim();
        input = input.Replace("\n", "");
        input = input.Replace("\r", "");
        input = input.Replace("\t", "");
        input = input.Replace("  ", "");
        return input;
    }

    private Dictionary<string, string> random_obj_extracor()
    {
        Dictionary<string, string> random_kitchen_items = KitchenItems.GetRandomItems(10);

        // foreach (var item in random_kitchen_items.Keys.ToList())
        // {
        //     // Find the GameObject in the scene by the item name
        //     GameObject foundObject = GameObject.Find(item);

        //     if (foundObject != null)
        //     {
        //         // Get the Interactive component from the found GameObject
        //         Interactive interactiveComponent = foundObject.GetComponent<Interactive>();

        //         // If the object has an Interactive component, get the description
        //         if (interactiveComponent != null && interactiveComponent.Meta.ContainsKey("description"))
        //         {
        //             // Assign the description to the randomItems dictionary
        //             random_kitchen_items[item] = interactiveComponent.Meta["description"];
        //         }
        //         else{Debug.LogWarning($"No description found for item: {item}");
        //         }
        //     }
        //     else{Debug.LogWarning($"GameObject {item} not found in the scene.");}
        // }

        return random_kitchen_items;
    }

    void extract_all_objects_descriptions()
    {

        // Iterate through each GameObject
        foreach (var item in KitchenItems.Items.Keys.ToList())
        {
            GameObject obj = GameObject.Find(item);

            if (obj != null)
            {
            // Check if the GameObject has an Interactive component
                Interactive interactiveComponent = obj.GetComponent<Interactive>();

                // If the object has an Interactive component, get the description
                if (interactiveComponent != null && interactiveComponent.Meta.ContainsKey("description"))
                {
                    // Assign the description to the randomItems dictionary
                    KitchenItems.Items[item] = interactiveComponent.Meta["description"];
                }
            }
        }
    }



    private String main_prompt_modifying(Dictionary<string, string> random_items_dic)
    {
        var modified = Find_Instructs.teacher_findng_role_v01;
        for (int i = 0; i < random_items_dic.Count; i++)
        {
            modified = modified.Replace($"{{items_{i}}}", random_items_dic.ElementAt(i).Key);
            modified = modified.Replace($"{{descrition_{i}}}", random_items_dic.ElementAt(i).Value);
        }

        return modified;
    }

    private void initiating_method( int method_num)
    {
        /// Being assured there is no element in the card
        cmd.Manager.Card.reset_all_cards();
        var QAObject = QAObjects[(int)method_num].QAndAs[0];

        ///Setting the chat context equal to the question
        cmd.Manager.Card.Chat_Context = QAObject.Q;
        // TTSManager.Manager.Speak(QAObject.Q);

        /// initiating the prompt
        method_prompts[method_num] = Find_Instructs.teacher_finding_feedback.Replace("{METHOD}",Find_Instructs.Methods[method_num]);
        method_prompts[method_num] += "\nQ: " + QAObject.Q + " - " + QAObject.A + " - " + KitchenItems.Items[QAObject.A];
        // TTSManager.Manager.Speak(QAObject.Q);
        t2s.Speak(QAObject.Q);
        
        /// wating until the card being filled
        // while (cmd.Manager.Card.is_card_empty());

        /// receving triggers from the scene objects
        is_game_started = true;
    }

    private async void playing_method(int method_num)
    {
        while (cmd.Manager.Card.is_card_empty()) await Task.Delay(500);
        var selected_obj = cmd.Manager.Card.ObjsInCardNames[0];
        method_prompts[method_num] += "\nA: " + selected_obj + " - " + KitchenItems.Items[selected_obj];

        var llm_return = await openAI.llm_do_task(method_prompts[method_num]);
        var method_1_obj = JsonConvert.DeserializeObject<Method_1_Obj>(llm_return);

        /// Showing the feedback
        // var user_answer = bool.Parse(method_1_obj.A);
        var user_answer = method_1_obj.A;
        cmd.Manager.Card.FeedbackText = method_1_obj.feedback;
        

        if (user_answer)
        {
            /// Preparing the next question
            cmd.Manager.Card.FeedbackText += "\n" + Find_Instructs.finding_finished;
            await Task.Delay(5000);
            cmd.Manager.Card.reset_all_cards();

            current_method_index++;
            if (current_method_index > 2)
            {
                m_method_num = (Method_Num)((int)m_method_num + 1);
                current_method_index = 0;
                initiating_method((int)m_method_num);
                return;
            }
            var QAObject = QAObjects[(int)method_num].QAndAs[current_method_index];

            ///Setting the chat context equal to the question
            cmd.Manager.Card.Chat_Context = QAObject.Q;
            /// initiating the prompt
            method_prompts[method_num] = Find_Instructs.teacher_finding_feedback;
            method_prompts[method_num] += "\nQ: " + QAObject.Q + " - " + QAObject.A + " - " + KitchenItems.Items[QAObject.A];
            // TTSManager.Manager.Speak(cmd.Manager.Card.FeedbackText+QAObject.Q);
            t2s.Speak(cmd.Manager.Card.FeedbackText+QAObject.Q);
        }
        else
        {
            /// resetting the card and do it again
            // var txt = "\n" + Find_Instructs.try_finding_again;
            cmd.Manager.Card.FeedbackText += "\n" + Find_Instructs.try_finding_again;
            // TTSManager.Manager.Speak(cmd.Manager.Card.FeedbackText);
            t2s.Speak(cmd.Manager.Card.FeedbackText);
            await Task.Delay(5000);
            cmd.Manager.Card.reset_all_cards();

            method_prompts[method_num] += "\n" + llm_return;
        }
    }

    private void AddToMethodNum(int value)
    {
        // Add the integer value to the enum and cast it back to the enum type
        m_method_num = (Method_Num)(((int)m_method_num + value) % Enum.GetValues(typeof(Method_Num)).Length);

        // This prevents the value from going out of the enum range
        if (m_method_num < 0)
        {
            m_method_num = (Method_Num)(Enum.GetValues(typeof(Method_Num)).Length + (int)m_method_num);
        }
    }


    IEnumerator DelayedAction()
    {
        // Do some initial work
        Debug.Log("Starting work...");

        // Wait for 2 seconds
        yield return new WaitForSeconds(2);

        // Do some work after the delay
        Debug.Log("Work completed after 2 seconds.");
    }



    private void reset_card_texts()
    {
        // check if cmd.Manager.Card is not null
        if (cmd.Manager.Card != null)
            cmd.Manager.Card.reset_card_texts();
    }


    private void reset_story_scene()
    {
        reset_card_texts();
        story_step = 0;
        confirm_button.gameObject.SetActive(false);
    }

    private void card_visible(bool visible)
    {StartCoroutine(visible ? cmd.Manager.Card.ShowCard() : cmd.Manager.Card.HideCard());}

    

    public async Task<StoryObject> story_preparation(string scene, string items)
    {
        var system_prompt = Instructions.teacher_story_role_v01;
        system_prompt = system_prompt.Replace("scene", scene);
        system_prompt = system_prompt.Replace("items", items);


        var llm_return = await openAI.llm_do_task(system_prompt);
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

        var llm_return = await openAI.llm_do_task(system_prompt);
        return llm_return;
    }

    public void on_confirm_clicked()
    {
        
        if (is_story_finished)
            next_story();
        else
        {
            cmd.Manager.Card.ShowCard();

        }
        // Debug.Log($"Next Button has been clicked!");
    }

    public void on_lumiere_find_clicked()
    {
        // cmd.Manager.single_object_card_mode = true;
        // card_visible(true);
        preparing_game();
    }

    private void next_story()
    {
        /// Ask if the user wants to try the story again
        if (story_step == 0)
        {
            cmd.Manager.Card.Chat_Context = Instructions.story_finished;
            cmd.Manager.Card.FeedbackText = Instructions.try_story_again;
            cmd.Manager.Card.Question_Items = "1. Yes   2. No";
            story_step++; /// Redundant line to make a delay that user can make a decision
        }
        else
        {
            is_story_finished = false;
            story_step = 0;
            
        }
    }

    public void OnObjectClick()
    {
        ///check if the mode is in finding game
        if (!is_game_started) return;

        playing_method((int)m_method_num);

        // swtich case for every items in Merhod_Num
        // switch (method_num)
        // {
        //     case Method_Num.M1:
        //         playing_method((int)Method_Num.M1);
        //         break;
        //     case Method_Num.M2:
        //         playing_method((int)Method_Num.M2);
        //         break;
        //     case Method_Num.M3:
        //         playing_method((int)Method_Num.M3);
        //         break;
        //     case Method_Num.M4:
        //         playing_method((int)Method_Num.M4);
        //         break;
        //     case Method_Num.M5:
        //         playing_method((int)Method_Num.M5);
        //         break;
        //     case Method_Num.M6:
        //         playing_method((int)Method_Num.M6);
        //         break;
        // }
    }
    // private void tell_story(StoryObject story)

    // Update is called once per frame
    void Update()
    {
    
        
    }
}
