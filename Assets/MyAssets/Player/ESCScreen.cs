using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ESCScreen : MonoBehaviour
{
    public bool IsOpen = false;
    [SerializeField] private SceneAsset Scene_MainMenu;


    // ----- START -----
    private void Start()
    {
        Continue();
    }


    // ----- BUTTONS -----
    public void BackToMainMenu()
    {
        StartCoroutine(LoadSceneAsync(Scene_MainMenu));
    }
    public void Continue()
    {
        IsOpen = false;
        gameObject.SetActive(false);
    }


    // ----- SCENE MANAGEMENT -----
    private IEnumerator LoadSceneAsync(SceneAsset scene)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(scene.name);

        while (operation.progress < 1)
        {
            yield return null;
        }

        Continue();
    }
}
