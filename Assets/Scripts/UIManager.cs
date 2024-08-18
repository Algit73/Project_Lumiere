using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Manager;
    public UIManager() => Manager = this;

    [SerializeField] private Transform env;
    [SerializeField] private GameObject iconPrefab;

    private bool _envIsRotating;

    public GameObject IconPrefab => iconPrefab;

    // these two methods are assigned to buttons in UI
    public void BackToMenu() => SceneManager.LoadScene("Main Menu");
    public void RotateEnv(float angle = 45f)
    {
        if (_envIsRotating) return;

        Vector3 targetRot = env.eulerAngles;
        targetRot.y += angle;

        StartCoroutine(RotatingEnv(targetRot));
    }

    private IEnumerator RotatingEnv(Vector3 targetRot)
    {
        _envIsRotating = true;

        Vector3 startRot = env.eulerAngles;
        float timer = 0f;

        while (timer < 1f)
        {
            timer += Time.deltaTime;
            env.eulerAngles = Vector3.Lerp(startRot, targetRot, timer);

            yield return null;
        }

        _envIsRotating = false;
    }
}