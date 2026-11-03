using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

//Persistent across scenes so the loading screen can stay up while the old level unloads.
//Drop the GameManager prefab in every level for easy testing - extra copies remove themselves.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsLoading { get; private set; }

    [SerializeField] private float restartDelay = 3f;   //time to read the console before reboot/next level

    [Space(4)]
    [Header("Loading Screen")]
    [SerializeField] private CanvasGroup loadingScreen;  //child of this object, so it persists too
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float minimumLoadTime = 1f; //keeps the screen up long enough to read / animate

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false; //skip OnEnable so the duplicate never subscribes
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetLoadingScreen(0f);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

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

    #region Level changes

    public void ReloadLevel(float delay = 0f) => LoadLevel(SceneManager.GetActiveScene().buildIndex, delay);

    public void LoadNextLevel(float delay = 0f)
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next >= SceneManager.sceneCountInBuildSettings)
        {
            //TODO: no more levels - load a credits / win scene instead
            next = SceneManager.GetActiveScene().buildIndex;
        }
        LoadLevel(next, delay);
    }

    public void LoadLevel(string sceneName, float delay = 0f)
    {
        //Accept either a plain name ("Level2") or a path ("Assets/Scenes/Level2.unity")
        int buildIndex = SceneUtility.GetBuildIndexByScenePath(sceneName);
        for (int i = 0; buildIndex < 0 && i < SceneManager.sceneCountInBuildSettings; i++)
        {
            if (Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i)) == sceneName)
                buildIndex = i;
        }

        if (buildIndex < 0)
        {
            Debug.LogError($"GameManager: scene '{sceneName}' isn't in Build Settings");
            return;
        }
        LoadLevel(buildIndex, delay);
    }

    public void LoadLevel(int buildIndex, float delay = 0f)
    {
        if (IsLoading) return;
        StartCoroutine(LoadLevelRoutine(buildIndex, delay));
    }

    IEnumerator LoadLevelRoutine(int buildIndex, float delay)
    {
        IsLoading = true;

        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        Actions.OnLevelLoadStarted?.Invoke(buildIndex);
        yield return FadeLoadingScreen(1f);

        //Load in the background, but hold activation until the minimum time has passed
        float startTime = Time.unscaledTime;
        AsyncOperation load = SceneManager.LoadSceneAsync(buildIndex);
        load.allowSceneActivation = false;

        while (load.progress < 0.9f || Time.unscaledTime - startTime < minimumLoadTime)
        {
            //Unity reports 0 - 0.9 while loading; 0.9 means "ready to activate"
            Actions.OnLevelLoadProgress?.Invoke(Mathf.Clamp01(load.progress / 0.9f));
            yield return null;
        }

        Actions.OnLevelLoadProgress?.Invoke(1f);
        load.allowSceneActivation = true;
        yield return load;

        //TODO: hook for level intro (e.g. the robot booting up in the console) before revealing
        yield return null; //let the new scene's Start() run before we fade out

        yield return FadeLoadingScreen(0f);
        Actions.OnLevelLoaded?.Invoke(buildIndex);

        IsLoading = false;
    }

    IEnumerator FadeLoadingScreen(float targetAlpha)
    {
        if (loadingScreen == null) yield break;

        float startAlpha = loadingScreen.alpha;
        for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            SetLoadingScreen(Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration));
            yield return null;
        }
        SetLoadingScreen(targetAlpha);
    }

    void SetLoadingScreen(float alpha)
    {
        if (loadingScreen == null) return;
        loadingScreen.alpha = alpha;
        loadingScreen.blocksRaycasts = alpha > 0f;
        loadingScreen.gameObject.SetActive(alpha > 0f);
    }

    #endregion

    void HandlePlayerDeath()
    {
        Actions.OnConsoleLog?.Invoke("");
        Actions.OnConsoleLog?.Invoke("CRITICAL FAILURE. HULL INTEGRITY 0.");
        Actions.OnConsoleLog?.Invoke("rebooting...");
        ReloadLevel(restartDelay);
    }

    void HandleLevelComplete()
    {
        Actions.OnConsoleLog?.Invoke("");
        Actions.OnConsoleLog?.Invoke("EXIT REACHED. UPLOADING TO NEXT SECTOR...");
        LoadNextLevel(restartDelay);
    }
}
