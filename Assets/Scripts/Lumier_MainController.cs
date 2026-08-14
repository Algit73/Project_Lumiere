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

    [Header("Task Menu")]
    [Tooltip("Assign the LumiereMenuOverlay GameObject (created by Tools > Build Lumiere Task Menu).")]
    [SerializeField] private TaskMenuController taskMenu;

    [Header("Find Object Task")]
    [Tooltip("Handles the Find Object dialogue sequence. Auto-adds if missing.")]
    [SerializeField] private FindObjectTask findObjectTask;

    [Tooltip("Character registry asset (Assets › Create › Lumiere › Character Registry). "
           + "Used to pre-warm the Find-Object session at startup.")]
    [SerializeField] private CharacterProfileSO findGameRegistry;

    [Tooltip("3D scene to load when a task begins. Must be added to Build Settings.")]
    [SerializeField] private string dialogueSceneName = "Farm";

    [Header("HUD")]
    [SerializeField] public Image hud_selected_objects;

    [Header("Legacy Scene Controllers (keep assigned)")]
    [SerializeField] private GameObject lumiere_story;
    [SerializeField] private GameObject lumiere_word;
    [SerializeField] private GameObject lumiere_find;

    private SinusoidalGlowEffect sgEffect;


    void Awake()
    {
        // Auto-create FindObjectTask on this GameObject if not assigned
        if (findObjectTask == null)
            findObjectTask = GetComponent<FindObjectTask>() ?? gameObject.AddComponent<FindObjectTask>();

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

    // Start is called before the first frame update
    void Start()
    {
        if (lumiere_button == null)
        {
            Debug.LogError("Lumier_MainController: Assign lumiere_button in the Inspector.");
            return;
        }

        hide_hud();

        // Pre-warm the Find-Object session so item + API clue are ready before the user taps.
        FindObjectPrewarm.Execute(findGameRegistry);

        // Glow effect on the main button
        lumiere_glow_effect_init(lumiere_button);

        // Open the task menu when the main button is pressed
        lumiere_button.onClick.AddListener(() =>
        {
            if (taskMenu != null)
                taskMenu.Open();
            else
                Debug.LogWarning("Lumier_MainController: taskMenu is not assigned. " +
                                 "Run Tools > Build Lumiere Task Menu and assign the result.");
        });

        // Subscribe to task-type selection from the new menu
        if (taskMenu != null)
        {
            taskMenu.OnTaskSelected += HandleTaskSelected;
            taskMenu.OnMenuClosed   += () => { if (sgEffect != null) sgEffect.StartGlow(); };
        }
    }

    private void HandleTaskSelected(TaskType type)
    {
        show_hud();
        if (sgEffect != null) sgEffect.StopGlow();

        switch (type)
        {
            case TaskType.Story:
                if (lumiere_story != null)
                    lumiere_story.GetComponent<Story_Telling>()?.on_lumiere_story_clicked();
                break;

            case TaskType.Name:
                if (lumiere_word != null)
                    lumiere_word.GetComponent<Word_Exercise>()?.on_lumiere_word_clicked();
                break;

            case TaskType.Find:
                // Save the AR anchor pose (if an anchor controller exists) and switch to the 3D scene.
                // FindObjectTask.Start() in the 3D scene will auto-call Begin().
                Pose anchorPose = default;
                bool hasAnchor  = false;
                var anchor = FindFirstObjectByType<ARSceneAnchorController>();
                if (anchor != null)
                {
                    anchorPose = new Pose(anchor.transform.position, anchor.transform.rotation);
                    hasAnchor  = true;
                }

                if (SceneTransitionManager.Instance != null)
                    SceneTransitionManager.Instance.GoTo(
                        dialogueSceneName, TaskType.Find,
                        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                        anchorPose, hasAnchor);
                else
                    findObjectTask?.Begin(); // fallback: no transition manager (editor testing)
                break;

            case TaskType.Manipulate:
            case TaskType.Sort:
            case TaskType.Match:
                Debug.Log($"[Lumier_MainController] Task type '{type}' selected — handler not yet implemented.");
                break;
        }
    }

    private void hide_lumiere_menu()
    {
        if (taskMenu != null && taskMenu.IsOpen) taskMenu.Close();
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
