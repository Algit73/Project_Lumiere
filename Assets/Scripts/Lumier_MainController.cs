using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Lumier_MainController : MonoBehaviour
{
    [SerializeField] private Button lumiere_button;
    [SerializeField] private Button lumiere_story_button;
    [SerializeField] private Button lumiere_chat_button;
    [SerializeField] private Button lumiere_word_button;

    [SerializeField] private Image lumiere_menu;
    [SerializeField] public  Image hud_selected_objects;
    [SerializeField] private Button lumiere_menu_close;

    [SerializeField] private GameObject lumiere_story;
    [SerializeField] private GameObject lumiere_word;

    void Awake()
    {
        hud_selected_objects = GameObject.Find("HUD_Selected_Objects").GetComponent<Image>();
        if (hud_selected_objects == null)
            Debug.LogError("Failed to find HUD_Selected_Objects or it doesn't have an Image component.");
    }

    // Start is called before the first frame update}
    // Start is called before the first frame update
    void Start()
    {
        /// hiding on display menues
        hide_lumiere_menu();
        hide_hud();

        /// adding listeners to buttons
        lumiere_button.onClick.AddListener(() => { lumiere_menu.gameObject.SetActive(true); });
        lumiere_menu_close.onClick.AddListener(() => { lumiere_menu.gameObject.SetActive(false); });

        Story_Telling story_telling = lumiere_story.GetComponent<Story_Telling>();
        
        Word_Exercise word_exercise = lumiere_word.GetComponent<Word_Exercise>();

        lumiere_story_button.onClick.AddListener(() => 
        { 
            hide_lumiere_menu();
            show_hud();
            story_telling.on_lumiere_story_clicked();
        } );
        
        lumiere_word_button.onClick.AddListener(() => 
        { 
            hide_lumiere_menu();
            show_hud();
            word_exercise.on_lumiere_word_clicked();
        } );


        
    }

    private void hide_lumiere_menu()
    {lumiere_menu.gameObject.SetActive(false);}

    public void hide_hud()
    {if (hud_selected_objects !=null) hud_selected_objects.gameObject.SetActive(false);}

    public void show_hud()
    {if (hud_selected_objects !=null) hud_selected_objects.gameObject.SetActive(true);}

    // Update is called once per frame
    void Update() {}
}
