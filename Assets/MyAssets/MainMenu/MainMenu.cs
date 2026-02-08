using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string Scene_ShootingRange_name;


    // ----- BUTTONS -----
    public void StartGame()
    {
        StartCoroutine(LoadSceneAsync(Scene_ShootingRange_name));
    }
    public void ExitGame()
    {
        Application.Quit();
    }


    // ----- SCENE MANAGEMENT -----
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        while(operation.progress < 1)
        {
            yield return null;
        }
    }
}
