using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Lumier_MainController : MonoBehaviour
{
    public Image buttonImage;
    public Color startColor = Color.white;
    public Color glowColor = Color.yellow;
    public float glowDuration = 0.5f;
    [SerializeField] private Button lumiere_button;
    [SerializeField] private Button lumiere_story_button;
    [SerializeField] private Button lumiere_chat_button;
    [SerializeField] private Button lumiere_word_button;
    [SerializeField] private Button lumiere_find_button;

    [SerializeField] private Image lumiere_menu;
    [SerializeField] public  Image hud_selected_objects;
    [SerializeField] private Button lumiere_menu_close;

    [SerializeField] private GameObject lumiere_story;
    [SerializeField] private GameObject lumiere_word;
    [SerializeField] private GameObject lumiere_find;
    private SinusoidalGlowEffect sgEffect;


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

        /// initializing lumiere glow effect
        lumiere_glow_effect_init(lumiere_button);

        /// adding listeners to buttons
        lumiere_button.onClick.AddListener(() => { lumiere_menu.gameObject.SetActive(true); sgEffect.StopGlow();});
        lumiere_menu_close.onClick.AddListener(() => { lumiere_menu.gameObject.SetActive(false); sgEffect.StartGlow();});

        Story_Telling story_telling = lumiere_story.GetComponent<Story_Telling>();
        
        Word_Exercise word_exercise = lumiere_word.GetComponent<Word_Exercise>();

        Finding_Items finding_items = lumiere_find.GetComponent<Finding_Items>();

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

        lumiere_find_button.onClick.AddListener(() => 
        { 
            hide_lumiere_menu();
            show_hud();
            finding_items.on_lumiere_find_clicked();
        } );


        
    }

    private void hide_lumiere_menu()
    {lumiere_menu.gameObject.SetActive(false);}

    public void hide_hud()
    {if (hud_selected_objects !=null) hud_selected_objects.gameObject.SetActive(false);}

    public void show_hud()
    {if (hud_selected_objects !=null) hud_selected_objects.gameObject.SetActive(true);}

    private void lumiere_glow_effect_init(Button button)
    {
        sgEffect = gameObject.AddComponent<SinusoidalGlowEffect>();
        // Initialize the glow effect with the button
        sgEffect.Initialize(lumiere_button);
        // Start the glow effect
        sgEffect.StartGlow();
    }

    IEnumerator GlowEffect()
    {
        float elapsedTime = 0f;
        while (elapsedTime < glowDuration)
        {
            buttonImage.color = Color.Lerp(startColor, glowColor, (elapsedTime / glowDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        buttonImage.color = glowColor;
    }

    // Update is called once per frame
    void Update() {}
}
