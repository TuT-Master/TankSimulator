using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ESCScreen : MonoBehaviour
{
    public bool IsOpen = false;
    [SerializeField] private string Scene_MainMenu_name;


    // ----- START -----
    private void Start()
    {
        Continue();
    }


    // ----- BUTTONS -----
    public void BackToMainMenu()
    {
        StartCoroutine(LoadSceneAsync(Scene_MainMenu_name));
    }
    public void Continue()
    {
        IsOpen = false;
        gameObject.SetActive(false);
    }


    // ----- SCENE MANAGEMENT -----
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        while (operation.progress < 1)
        {
            yield return null;
        }

        Continue();
    }
}
