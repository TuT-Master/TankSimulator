using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private SceneAsset Scene_ShootingRange;


    // ----- BUTTONS -----
    public void StartGame()
    {
        StartCoroutine(LoadSceneAsync(Scene_ShootingRange));
    }
    public void ExitGame()
    {
        Application.Quit();
    }


    // ----- SCENE MANAGEMENT -----
    private IEnumerator LoadSceneAsync(SceneAsset scene)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(scene.name);

        while(operation.progress < 1)
        {
            yield return null;
        }
    }
}
