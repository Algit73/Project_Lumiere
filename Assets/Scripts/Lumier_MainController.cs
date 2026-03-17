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

    [SerializeField] private GameObject lumiere_menu;
    [SerializeField] public  Image hud_selected_objects;
    [SerializeField] private Button lumiere_menu_close;

    [SerializeField] private GameObject lumiere_story;
    [SerializeField] private GameObject lumiere_word;
    [SerializeField] private GameObject lumiere_find;
    private SinusoidalGlowEffect sgEffect;


    void Awake()
    {
        if (hud_selected_objects == null)
        {
            var hud = GameObject.Find("HUD_Selected_Objects");
            if (hud != null)
            {
                hud_selected_objects = hud.GetComponent<Image>();
            }
        }

        if (hud_selected_objects == null)
            Debug.LogWarning("HUD_Selected_Objects Image is not assigned/found. HUD show/hide will be skipped.");
    }

    // Start is called before the first frame update}
    // Start is called before the first frame update
    void Start()
    {
        if (lumiere_button == null || lumiere_menu_close == null || lumiere_menu == null ||
            lumiere_story_button == null || lumiere_word_button == null || lumiere_find_button == null)
        {
            Debug.LogError("Lumier_MainController: Assign all Lumiere buttons and menu references in Inspector.");
            return;
        }

        /// hiding on display menues
        hide_lumiere_menu();
        hide_hud();

        /// initializing lumiere glow effect
        lumiere_glow_effect_init(lumiere_button);

        /// adding listeners to buttons
        lumiere_button.onClick.AddListener(OpenLumiereMenu);
        lumiere_menu_close.onClick.AddListener(CloseLumiereMenu);

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
    {
        CloseLumiereMenu();
    }

    private void OpenLumiereMenu()
    {
        if (lumiere_menu != null) lumiere_menu.SetActive(true);
        if (lumiere_story_button != null) lumiere_story_button.gameObject.SetActive(true);
        if (lumiere_word_button != null) lumiere_word_button.gameObject.SetActive(true);
        if (lumiere_find_button != null) lumiere_find_button.gameObject.SetActive(true);

        if (sgEffect != null) sgEffect.StopGlow();
    }

    private void CloseLumiereMenu()
    {
        if (lumiere_menu != null) lumiere_menu.SetActive(false);
        if (sgEffect != null) sgEffect.StartGlow();
    }

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
