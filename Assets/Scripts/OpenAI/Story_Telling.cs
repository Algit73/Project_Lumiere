using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using cmd = CommandsCenter;

public class Story_Telling : MonoBehaviour
{
    private OpenAIBasics openAI;
    private Transform[] available_card_objs;
    private int story_step = 0;
    private StoryObject story;
    
    private string scence_name;

    private bool question_asked = false;

    [SerializeField] private TMP_InputField user_prompt;

    [SerializeField] private bool command;

    [SerializeField] private Button confirm_button;

    private bool is_story_finished = false;

    // Start is called before the first frame update
    void Start()
    {
        /// to clean the scene and be prepared for the next story
        reset_story_scene();

        confirm_button.onClick.AddListener(() => { on_confirm_clicked(); });

        openAI = new OpenAIBasics();

        scence_name = Instructions.scenes[(int)Instructions.scene_index.HOUSE];
        
        
    }

    private async void OnValidate()
    {
        if (command)
        {
            command = false;
            story_telling();

        }
    }

    private async void story_telling()
    {

        card_visible(true);
        /// check if there is any item inside the card
        if (!is_card_empty())
            available_card_objs = CardHandler.ObjectsInCard;
        else 
        {
            cmd.Manager.Card.Chat_Context = Instructions.card_is_empty_alert;
            return;
        }

        /// reset all the texts inside the card
        reset_card_texts();

        /// create the story
        await create_story();

        /// Enabling the confirm_button
        confirm_button.gameObject.SetActive(true);

        /// starting QandA procedure
        // story_QandA(story);
    }

    private async Task create_story()
    {
        string names = extract_card_items(available_card_objs);

        /// Give the waiting message to the user unitl the story become available
        cmd.Manager.Card.Chat_Context = Instructions.story_waiting_message;

        story = await story_preparation(scence_name , names);
        /// Showing the preface of the story
        cmd.Manager.Card.Chat_Context = story.preface;
        Debug.Log($"story preface: {story.preface}, story para_0: {story.paragraphs[0].text}");
    }

    private void reset_card_texts()
    {
        // check if cmd.Manager.Card is not null
        if (cmd.Manager.Card != null)
            cmd.Manager.Card.reset_card_texts();
    }

    // private bool is_card_empty(Transform[] items

    private bool is_card_empty()
    {
        bool allNull = true;

        foreach (Transform t in CardHandler.ObjectsInCard)
        {
            if (t != null)
            {
                allNull = false;
                break; // Exit the loop early if a non-null element is found
            }
        }
        return allNull;
    }
    // {return CardHandler.ObjectsInCard == null;}

    private string extract_card_items(Transform[] items)
    {
        string names = "";
        foreach (var item in items)
        {
            var interactive = item.GetComponents<Interactive>();
            var data = interactive[0].data;
            /// TODO: this part should be reconsidered
            var meta = new Dictionary<string, string>();
            foreach (Interactive.KeyAndValue d in data)
                meta.Add(d.Key, d.Value);
            // string[] parts = item.ToString().Split('_');
            // string[] parts = item.ToString().Split('_');
            names += meta["description"] + ", ";
            // names += parts[0] + ", ";
        }
        return names;
    }

    private async void story_QandA(StoryObject story)
    {
        user_prompt.gameObject.SetActive(true);
        // if (story_step == 0)
        //     return;
        var question_items = "1. " + story.paragraphs[story_step].answer +
                             " 2. " + story.paragraphs[story_step].choice_0 + "" +
                             " 3. " + story.paragraphs[story_step].choice_1 + "" +
                             " 4. " + story.paragraphs[story_step].choice_2;

        cmd.Manager.Card.Chat_Context = story.paragraphs[story_step].text;
        cmd.Manager.Card.QuestionText = story.paragraphs[story_step].question;
        cmd.Manager.Card.Question_Items = question_items;
        cmd.Manager.Card.ResultText = "";
        Debug.Log($"story right answer: {story.paragraphs[story_step].answer}");
        // CommandsCenter.Manager.Card.ResultText = story.paragraphs[story_step].answer;
        if (!question_asked)
        {
            question_asked = true;
            return;
        }

        /// check if the answer is right
        if (user_prompt.text == "1")
        {
            story_step++;
            cmd.Manager.Card.ResultText = Instructions.greeting_message;
            question_asked = false;
            // story_QandA(story);
        }

        /// Try to help the user to find the right answer
        else
        {
            var ai_hint = await hint_maker(story.paragraphs[story_step],user_prompt.text);
            cmd.Manager.Card.ResultText = ai_hint;
        }

        if ((story_step+1)>story.paragraphs.Count)

        {
            // reset_story_scene();
            is_story_finished = true;
            story_step = 0; /// To start the story finishing process
        }
        
    }

    private void reset_story_scene()
    {
        reset_card_texts();
        story_step = 0;
        user_prompt.gameObject.SetActive(false);
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
            story_QandA(story);
        }
        // Debug.Log($"Next Button has been clicked!");
    }

    public void on_lumiere_story_clicked()
    {
        card_visible(true);
        story_telling();
    }

    private void next_story()
    {
        /// Ask if the user wants to try the story again
        if (story_step == 0)
        {
            cmd.Manager.Card.Chat_Context = Instructions.story_finished;
            cmd.Manager.Card.QuestionText = Instructions.try_story_again;
            cmd.Manager.Card.Question_Items = "1. Yes   2. No";
            story_step++; /// Redundant line to make a delay that user can make a decision
        }
        else
        {
            is_story_finished = false;
            story_step = 0;
            
            if (user_prompt.text == "1")  story_telling(); else {reset_story_scene();card_visible(false);}
        }
    }

    // private void tell_story(StoryObject story)

    // Update is called once per frame
    void Update()
    {
        
    }
}
