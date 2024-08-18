using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void Exit() => Application.Quit();
    public void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);
}