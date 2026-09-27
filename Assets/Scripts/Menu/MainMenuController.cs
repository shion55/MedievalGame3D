using DG.Tweening.Core.Easing;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField]
    private string gameSceneName = "Medievalscene1";


    public void NewGame()
    {
        SaveManager.LoadRequested = false;
        SceneManager.LoadScene(
            gameSceneName
        );
    }


    public void LoadGame()
    {
        SaveManager.LoadRequested = true;//load‚µ‚Ä‚Ë

        SceneManager.LoadScene(
            gameSceneName
        );
    }


   
}
