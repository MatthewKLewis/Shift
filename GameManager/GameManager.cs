using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private float restartDelay = 3f;

    private void OnEnable()
    {
        Actions.OnPlayerDeath += HandlePlayerDeath;
        Actions.OnLevelComplete += HandleLevelComplete;
    }

    private void OnDisable()
    {
        Actions.OnPlayerDeath -= HandlePlayerDeath;
        Actions.OnLevelComplete -= HandleLevelComplete;
    }

    void HandlePlayerDeath()
    {
        Actions.OnConsoleLog?.Invoke("");
        Actions.OnConsoleLog?.Invoke("CRITICAL FAILURE. HULL INTEGRITY 0.");
        Actions.OnConsoleLog?.Invoke("rebooting...");
        StartCoroutine(LoadSceneAfterDelay(SceneManager.GetActiveScene().buildIndex));
    }

    void HandleLevelComplete()
    {
        Actions.OnConsoleLog?.Invoke("");
        Actions.OnConsoleLog?.Invoke("EXIT REACHED. UPLOADING TO NEXT SECTOR...");

        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next >= SceneManager.sceneCountInBuildSettings) next = SceneManager.GetActiveScene().buildIndex;
        StartCoroutine(LoadSceneAfterDelay(next));
    }

    IEnumerator LoadSceneAfterDelay(int buildIndex)
    {
        yield return new WaitForSeconds(restartDelay);
        SceneManager.LoadScene(buildIndex);
    }
}
