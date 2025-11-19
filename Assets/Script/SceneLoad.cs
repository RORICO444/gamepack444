using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneLoad : MonoBehaviour
{
    [Header("玩家设置")]
    public Transform playerTrans;
    public Vector3 firstPosition;

    [Header("事件监听")]
    public SceneLoadEventSO loadEventSO;
    public GameSceneSO firstLoadScene;

    [Header("广播")]
    public VoidEventSO afterSceneLoadedEvent;
    public FadeEventSO fadeEvent;

    [Header("加载设置")]
    [SerializeField] private float fadeDuration = 1f;

    [Header("对话设置")]
    [SerializeField] private LargeDialogManager dialogManager;
    [SerializeField] private bool autoPlaySceneDialog = true;
    [SerializeField] private float dialogStartDelay = 2f;

    // 私有变量
    private GameSceneSO currentLoadScene;
    private GameSceneSO sceneToLoad;
    private Vector3 positionToGo;
    private bool fadeScreen;
    private bool isLoading;
    private bool isFirstScene = true;
    private string currentSceneName;
    private SceneInstance currentSceneInstance;

    private void Awake()
    {
        Debug.Log("SceneLoad Awake - 初始化");
        
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
        Debug.Log("SceneLoad Start - 开始新游戏");
        NewGame();
    }

    private void OnEnable()
    {
        if (loadEventSO != null)
        {
            loadEventSO.LoadRequestEvent += OnLoadRequestEvent;
            Debug.Log("成功注册 LoadRequestEvent");
        }
        else
        {
            Debug.LogError("无法注册事件：loadEventSO 为 null");
        }
    }

    private void OnDisable()
    {
        if (loadEventSO != null)
        {
            loadEventSO.LoadRequestEvent -= OnLoadRequestEvent;
            Debug.Log("已取消注册 LoadRequestEvent");
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
            Debug.LogError("firstLoadScene 未设置！");
            return;
        }

        isFirstScene = true;
        sceneToLoad = firstLoadScene;
        
        Debug.Log($"开始新游戏，加载第一个场景: {GetSceneName(sceneToLoad)}");
        OnLoadRequestEvent(sceneToLoad, firstPosition, true);
    }

    private void OnLoadRequestEvent(GameSceneSO locationToLoad, Vector3 posToGo, bool fadeScreen)
    {
        if (isLoading)
        {
            Debug.LogWarning("场景加载正在进行中，忽略新请求");
            return;
        }

        if (locationToLoad == null)
        {
            Debug.LogError("尝试加载空场景！");
            return;
        }

        if (locationToLoad.sceneReference == null)
        {
            Debug.LogError($"场景 '{GetSceneName(locationToLoad)}' 的 sceneReference 为 null！");
            return;
        }

        isLoading = true;
        sceneToLoad = locationToLoad;
        positionToGo = posToGo;
        this.fadeScreen = fadeScreen;

        Debug.Log($"开始加载场景: {GetSceneName(sceneToLoad)} (第一个场景: {isFirstScene})");

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
                Debug.Log($"卸载场景: {GetSceneName(currentLoadScene)}");
                currentLoadScene.sceneReference.UnLoadScene();
            }
            catch (Exception e)
            {
                Debug.LogError($"卸载场景失败: {e.Message}");
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
            Debug.LogError("无法加载场景：场景引用无效");
            isLoading = false;
            return;
        }

        try
        {
            Debug.Log($"开始异步加载场景: {GetSceneName(sceneToLoad)}");
            
            var loadingOption = sceneToLoad.sceneReference.LoadSceneAsync(
                LoadSceneMode.Additive, 
                true,
                0
            );
            
            loadingOption.Completed += OnLoadComplete;
        }
        catch (Exception e)
        {
            Debug.LogError($"场景加载异常: {e.Message}");
            isLoading = false;
        }
    }

    private void OnLoadComplete(AsyncOperationHandle<SceneInstance> obj)
    {
        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            currentSceneInstance = obj.Result;
            currentSceneName = GetSceneNameFromSceneInstance(currentSceneInstance);
            
            Debug.Log($"场景加载成功: {currentSceneName}");
            
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
        }
        else
        {
            Debug.LogError($"场景加载失败: {obj.OperationException}");
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
                Debug.Log($"设置活跃场景: {currentSceneInstance.Scene.name}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"设置活跃场景失败: {e.Message}");
        }

        isLoading = false;
        
        if (afterSceneLoadedEvent != null)
        {
            afterSceneLoadedEvent.RaiseEvent();
        }
        else
        {
            Debug.LogWarning("afterSceneLoadedEvent 为 null，无法广播事件");
        }
        
        yield return StartCoroutine(TriggerSceneDialogCoroutine());
        
        isFirstScene = false;
        
        Debug.Log($"场景加载流程完成: {currentSceneName}");
    }

    private IEnumerator TriggerSceneDialogCoroutine()
    {
        if (!autoPlaySceneDialog)
        {
            Debug.Log("自动播放对话已禁用");
            yield break;
        }

        if (dialogManager == null)
        {
            Debug.LogWarning("对话管理器未找到，无法播放对话");
            yield break;
        }

        if (dialogManager.IsDialogActive())
        {
            Debug.LogWarning("已有对话正在进行中，跳过新对话");
            yield break;
        }

        Debug.Log($"等待 {dialogStartDelay} 秒后开始对话");
        yield return new WaitForSeconds(dialogStartDelay);

        StartSceneDialog();
    }

    private void StartSceneDialog()
    {
        if (dialogManager == null)
        {
            Debug.LogWarning("对话管理器未找到");
            return;
        }

        string sceneName = GetCurrentSceneName();
        string storyPath = GetStoryPathForScene(sceneName);
        
        Debug.Log($"尝试开始场景对话 - 场景: {sceneName}, 路径: {storyPath}");

        if (string.IsNullOrEmpty(storyPath))
        {
            Debug.LogWarning($"未找到场景 {sceneName} 的对话文件路径");
            return;
        }

        try
        {
            dialogManager.StartDialog(storyPath);
            Debug.Log($"对话开始: {storyPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"开始对话失败: {e.Message}");
        }
    }

    /// <summary>
    /// 获取当前场景名称 - 多种方法确保可靠性
    /// </summary>
    private string GetCurrentSceneName()
    {
        // 方法1: 使用缓存的场景名称
        if (!string.IsNullOrEmpty(currentSceneName))
        {
            return currentSceneName;
        }
        
        // 方法2: 从当前加载的场景实例获取
        if (currentSceneInstance.Scene.IsValid())
        {
            string name = currentSceneInstance.Scene.name;
            if (!string.IsNullOrEmpty(name))
            {
                currentSceneName = name;
                return name;
            }
        }
        
        // 方法3: 从 GameSceneSO 获取
        if (currentLoadScene != null)
        {
            string name = GetSceneNameFromGameSceneSO(currentLoadScene);
            if (!string.IsNullOrEmpty(name))
            {
                currentSceneName = name;
                return name;
            }
        }
        
        // 方法4: 从 SceneManager 获取活跃场景
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
        
        // 方法5: 获取所有加载的场景
        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.IsValid() && scene.name != "DontDestroyOnLoad")
            {
                string name = scene.name;
                if (!string.IsNullOrEmpty(name))
                {
                    currentSceneName = name;
                    return name;
                }
            }
        }
        
        Debug.LogWarning("无法获取当前场景名称，使用默认名称");
        return "UnknownScene";
    }

    private string GetStoryPathForScene(string sceneName)
    {
        try
        {
            Debug.Log($"获取场景对话路径 - 场景: {sceneName}");
            
            // 使用 Constants 类
            string path = GetPathFromConstants(sceneName);
            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log($"使用 Constants 路径: {path}");
                return path;
            }
            
            Debug.LogWarning($"未找到场景 {sceneName} 的对话路径");
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"获取场景对话路径异常: {e.Message}");
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
                Debug.LogWarning("Constants 类型未找到");
                return null;
            }
            
            // 获取 StoryPaths 嵌套类型
            Type storyPathsType = constantsType.GetNestedType("StoryPaths");
            if (storyPathsType == null)
            {
                Debug.LogWarning("Constants.StoryPaths 类型未找到");
                return null;
            }
            
            // 获取 GetStoryPathByScene 方法
            var method = storyPathsType.GetMethod("GetStoryPathByScene");
            if (method == null)
            {
                Debug.LogWarning("GetStoryPathByScene 方法未找到");
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
                Debug.LogWarning($"GetStoryPathByScene 返回了非字符串类型: {result.GetType()}");
                return result.ToString();
            }
            else
            {
                Debug.LogWarning("GetStoryPathByScene 返回了 null");
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"从 Constants 获取路径失败: {e.Message}");
            return null;
        }
    }

    private void OnSceneDialogEnd()
    {
        Debug.Log($"场景对话结束: {GetCurrentSceneName()}");
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
            Debug.LogWarning($"从 SceneInstance 获取名称失败: {e.Message}");
        }
        return null;
    }

    /// <summary>
    /// 从 GameSceneSO 获取场景名称
    /// </summary>
    private string GetSceneNameFromGameSceneSO(GameSceneSO sceneSO)
    {
        if (sceneSO == null) return null;
        
        try
        {
            // 方法1: 使用 GetSceneName 方法（如果存在）
            var method = sceneSO.GetType().GetMethod("GetSceneName");
            if (method != null)
            {
                object result = method.Invoke(sceneSO, null);
                if (result is string name && !string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            
            // 方法2: 直接访问 sceneName 字段
            var field = sceneSO.GetType().GetField("sceneName");
            if (field != null)
            {
                object value = field.GetValue(sceneSO);
                if (value is string name && !string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            
            // 方法3: 使用 SO 的 name 属性
            if (!string.IsNullOrEmpty(sceneSO.name))
            {
                return sceneSO.name;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"从 GameSceneSO 获取名称失败: {e.Message}");
        }
        
        return null;
    }

    // 安全的获取场景名称方法
    private string GetSceneName(GameSceneSO scene)
    {
        if (scene == null) return "Null Scene";
        
        // 尝试通过反射获取 sceneName 字段
        var field = scene.GetType().GetField("sceneName");
        if (field != null)
        {
            return field.GetValue(scene) as string ?? scene.name;
        }
        
        // 如果找不到 sceneName 字段，使用 SO 的 name
        return scene.name;
    }

    /// <summary>
    /// 获取所有加载的场景信息（用于调试）
    /// </summary>
    private string GetAllLoadedScenesInfo()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== 所有加载的场景 ===");
        
        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            sb.AppendLine($"场景 {i}: {scene.name} (有效: {scene.IsValid()}, 已加载: {scene.isLoaded})");
        }
        
        sb.AppendLine($"活跃场景: {SceneManager.GetActiveScene().name}");
        return sb.ToString();
    }

    [ContextMenu("手动触发当前场景对话")]
    public void StartDialogManually()
    {
        if (dialogManager != null && !dialogManager.IsDialogActive())
        {
            StartSceneDialog();
        }
        else
        {
            Debug.Log("对话管理器不可用或已有对话在进行");
        }
    }

    [ContextMenu("检查当前状态")]
    private void CheckCurrentState()
    {
        Debug.Log("=== SceneLoad 当前状态检查 ===");
        Debug.Log($"当前场景名称: {GetCurrentSceneName()}");
        Debug.Log($"缓存的场景名称: {currentSceneName}");
        Debug.Log($"当前加载场景SO: {(currentLoadScene ? currentLoadScene.name : "None")}");
        Debug.Log($"场景实例有效: {currentSceneInstance.Scene.IsValid()}");
        
        // 显示所有加载的场景信息
        Debug.Log(GetAllLoadedScenesInfo());
        
        Debug.Log($"对话管理器: {(dialogManager ? "已设置" : "未设置")}");
        Debug.Log($"自动播放对话: {autoPlaySceneDialog}");
        Debug.Log($"第一个场景标记: {isFirstScene}");
        Debug.Log($"loadEventSO: {(loadEventSO ? "已设置" : "未设置")}");
        
        if (currentLoadScene != null)
        {
            string sceneName = GetCurrentSceneName();
            string storyPath = GetStoryPathForScene(sceneName);
            Debug.Log($"当前场景对话路径: {storyPath}");
        }
    }

    [ContextMenu("测试事件系统")]
    public void TestEventSystem()
    {
        if (loadEventSO != null && firstLoadScene != null)
        {
            Debug.Log("测试事件系统 - 发送加载请求");
            loadEventSO.RaiseLoadRequest(firstLoadScene, firstPosition, true);
        }
        else
        {
            Debug.LogError("无法测试事件系统：loadEventSO 或 firstLoadScene 为 null");
        }
    }
}