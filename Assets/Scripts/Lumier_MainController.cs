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
    [SerializeField] private Button lumiere_menu_close;

    [SerializeField] private GameObject lumiere_story;
    [SerializeField] private GameObject lumiere_word;
    // Start is called before the first frame update
    void Start()
    {
        hide_lumiere_menu();
        lumiere_button.onClick.AddListener(() => { lumiere_menu.gameObject.SetActive(true); });
        lumiere_menu_close.onClick.AddListener(() => { lumiere_menu.gameObject.SetActive(false); });

        Story_Telling story_telling = lumiere_story.GetComponent<Story_Telling>();
        
        Word_Exercise word_exercise = lumiere_word.GetComponent<Word_Exercise>();

        lumiere_story_button.onClick.AddListener(() => 
        { 
            hide_lumiere_menu();
            story_telling.on_lumiere_story_clicked();
        } );
        
        lumiere_word_button.onClick.AddListener(() => 
        { 
            hide_lumiere_menu();
            word_exercise.on_lumiere_word_clicked();
        } );


        
    }

    private void hide_lumiere_menu()
    {lumiere_menu.gameObject.SetActive(false);}

    // Update is called once per frame
    void Update()
    {
        
    }
}
