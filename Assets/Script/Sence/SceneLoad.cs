 using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneLoad : MonoBehaviour
{
    [Header("玩家设置")]
    public Transform playerTrans;
    
    [Header("事件监听")]
    public SceneLoadEventSO loadEventSO;
    public VoidEventSO newGameEventSO;
    
    [Header("广播")]
    public VoidEventSO afterSceneLoadedEvent;
    public FadeEventSO fadeEvent;

    [Header("加载设置")]
    [SerializeField] private float fadeDuration = 1f;

    [Header("对话设置")]
    [SerializeField] private LargeDialogManager dialogManager;
    [SerializeField] private bool autoPlaySceneDialog = true;
    [SerializeField] private float dialogStartDelay = 2f;
    
    [Header("场景")]
    public GameSceneSO firstLoadScene;
    public GameSceneSO menuScene;
    public Vector3 firstPosition;
    public Vector3 menuPosition;
    
    public static SceneLoad Instance {get; set; }

    
    private GameSceneSO currentLoadScene;
    private GameSceneSO sceneToLoad;
    private Vector3 positionToGo;
    private bool fadeScreen;
    private bool isLoading;
    private bool isFirstScene = true;
    private string currentSceneName;
    private SceneInstance currentSceneInstance;
    private PlayerController playerController;

    private void Awake()
    {
        
        playerController = FindObjectOfType<PlayerController>();
        
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        // 确保对话管理器引用
        if (dialogManager == null)
        {
            dialogManager = FindObjectOfType<LargeDialogManager>();
        }

        // 注册对话结束事件
        if (dialogManager != null)
        {
            dialogManager.OnDialogEnd += OnSceneDialogEnd;
        }
    }

    private void Start()
    {
        OnLoadRequestEvent(menuScene, menuPosition, true);
    }

    private void OnEnable()
    {
        if (loadEventSO != null)
        {
            loadEventSO.LoadRequestEvent += OnLoadRequestEvent;    
            newGameEventSO.OnEventRaised += NewGame;
        }
    }

    private void OnDisable()
    {
        if (loadEventSO != null)
        {
            loadEventSO.LoadRequestEvent -= OnLoadRequestEvent;
        }

        if (dialogManager != null)
        {
            dialogManager.OnDialogEnd -= OnSceneDialogEnd;
        }
    }

    private void NewGame()
    {
        if (firstLoadScene == null)
        {
            return;
        }

        isFirstScene = true;
        sceneToLoad = firstLoadScene;
        OnLoadRequestEvent(sceneToLoad, firstPosition, true);
        
    }

    private void OnLoadRequestEvent(GameSceneSO locationToLoad, Vector3 posToGo, bool fadeScreen)
    {
        if (isLoading)
        {
            return;
        }

        if (locationToLoad == null)
        {
            return;
        }

        if (locationToLoad.sceneReference == null)
        {
            return;
        }

        isLoading = true;
        sceneToLoad = locationToLoad;
        positionToGo = posToGo;
        this.fadeScreen = fadeScreen;

        if (currentLoadScene != null)
        {
            StartCoroutine(UnloadPreviousScene());
        }
        else
        {
            LoadNewScene();
        }
    }

    private IEnumerator UnloadPreviousScene()
    {
        if (fadeScreen && fadeEvent != null)
        {
            fadeEvent.FadeIn(fadeDuration);
        }
        
        yield return new WaitForSeconds(fadeDuration);

        if (currentLoadScene != null && currentLoadScene.sceneReference != null)
        {
            try
            {
                currentLoadScene.sceneReference.UnLoadScene();
            }
            catch (Exception e)
            {
            }
        }

        if (playerTrans != null)
        {
            playerTrans.gameObject.SetActive(false);
        }
        
        LoadNewScene();
    }

    private void LoadNewScene()
    {
        if (sceneToLoad == null || sceneToLoad.sceneReference == null)
        {
            isLoading = false;
            return;
        }

        try
        {
            var loadingOption = sceneToLoad.sceneReference.LoadSceneAsync(
                LoadSceneMode.Additive, 
                true,
                0
            );
            
            loadingOption.Completed += OnLoadComplete;
        }
        catch (Exception e)
        {
            isLoading = false;
        }
    }

    private void OnLoadComplete(AsyncOperationHandle<SceneInstance> obj)
    {
        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            currentSceneInstance = obj.Result;
            currentSceneName = GetSceneNameFromSceneInstance(currentSceneInstance);
            
            currentLoadScene = sceneToLoad;
            
            if (playerTrans != null)
            {
                playerTrans.position = positionToGo;
                playerTrans.gameObject.SetActive(true);
            }
            
            if (fadeScreen && fadeEvent != null)
            {
                fadeEvent.FadeOut(fadeDuration);
            }
            
            StartCoroutine(OnSceneLoadCompleteCoroutine());
            if (currentLoadScene.sceneState != GameState.menu)
            {
                playerController.EnableMovement();
            }
        }
        else
        {
            isLoading = false;
        }
    }

    private IEnumerator OnSceneLoadCompleteCoroutine()
    {
        yield return new WaitForEndOfFrame();
        
        try
        {
            if (currentSceneInstance.Scene.IsValid())
            {
                SceneManager.SetActiveScene(currentSceneInstance.Scene);
            }
        }
        catch (Exception e)
        {
        }

        isLoading = false;
        
        if (afterSceneLoadedEvent != null)
        {
            afterSceneLoadedEvent.RaiseEvent();
        }
        
        yield return StartCoroutine(TriggerSceneDialogCoroutine());
        
        isFirstScene = false;
    }

    private IEnumerator TriggerSceneDialogCoroutine()
    {
        if (!autoPlaySceneDialog)
        {
            yield break;
        }

        if (dialogManager == null)
        {
            yield break;
        }

        if (dialogManager.IsDialogActive())
        {
            yield break;
        }

        yield return new WaitForSeconds(dialogStartDelay);

        StartSceneDialog();
    }

    private void StartSceneDialog()
    {
        if (!dialogManager)
        {
            return;
        }

        string sceneName = GetCurrentSceneName();
        string storyPath = GetStoryPathForScene(sceneName);

        if (string.IsNullOrEmpty(storyPath))
        {
            return;
        }

        try
        {
            dialogManager.StartDialog(storyPath);
        }
        catch (Exception e)
        {
        }
    }

    /// <summary>
    /// 获取当前场景名称
    /// </summary>
    private string GetCurrentSceneName()
    {
        
        
        //从当前加载的场景实例获取
        if (currentSceneInstance.Scene.IsValid())
        {
            string name = currentSceneInstance.Scene.name;
            if (!string.IsNullOrEmpty(name))
            {
                currentSceneName = name;
                return name;
            }
        }
        
        
        
        //从 SceneManager 获取活跃场景
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
        {
            string name = activeScene.name;
            if (!string.IsNullOrEmpty(name))
            {
                currentSceneName = name;
                return name;
            }
        }
        
        return "UnknownScene";
    }

    private string GetStoryPathForScene(string sceneName)
    {
        try
        {
            // 使用 Constants 类
            string path = GetPathFromConstants(sceneName);
            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }
            
            return null;
        }
        catch (System.Exception e)
        {
            return null;
        }
    }

    /// <summary>
    /// 从 Constants 类获取路径
    /// </summary>
    private string GetPathFromConstants(string sceneName)
    {
        try
        {
            // 使用 Type.GetType 获取 Constants 类型
            Type constantsType = Type.GetType("Constants");
            if (constantsType == null)
            {
                return null;
            }
            
            // 获取 StoryPaths 嵌套类型
            Type storyPathsType = constantsType.GetNestedType("StoryPaths");
            if (storyPathsType == null)
            {
                return null;
            }
            
            // 获取 GetStoryPathByScene 方法
            var method = storyPathsType.GetMethod("GetStoryPathByScene");
            if (method == null)
            {
                return null;
            }
            
            // 调用静态方法
            object result = method.Invoke(null, new object[] { sceneName });
            
            // 确保返回的是 string 类型
            if (result is string path)
            {
                return path;
            }
            else if (result != null)
            {
                return result.ToString();
            }
            else
            {
                return null;
            }
        }
        catch (System.Exception e)
        {
            return null;
        }
    }

    private void OnSceneDialogEnd()
    {
    }

    /// <summary>
    /// 从 SceneInstance 获取场景名称
    /// </summary>
    private string GetSceneNameFromSceneInstance(SceneInstance sceneInstance)
    {
        try
        {
            if (sceneInstance.Scene.IsValid())
            {
                return sceneInstance.Scene.name;
            }
        }
        catch (Exception e)
        {
        }
        return null;
    }
    
}