using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;
using System.Collections.Generic;

public class TimelineManager : MonoBehaviour
{
    [System.Serializable]
    public class SceneTimeline
    {
        public string sceneName;
        public string timelineDisplayName;
        public string directorObjectName; // 使用对象名称查找
    }

    [Header("Timeline配置")]
    public List<SceneTimeline> sceneTimelines = new List<SceneTimeline>();

    [Header("设置")]
    public bool showDebug = true;
    public float registrationDelay = 0.5f; // 场景加载后延迟注册

    // 单例实例
    public static TimelineManager Instance { get; private set; }

    // 内部数据
    private Dictionary<string, PlayableDirector> registeredDirectors = new Dictionary<string, PlayableDirector>();
    private SceneLoad sceneLoadSystem;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Log("Timeline管理器已初始化");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 获取场景加载系统引用
        sceneLoadSystem = FindObjectOfType<SceneLoad>();
        if (sceneLoadSystem != null)
        {
            // 监听场景加载完成事件
            if (sceneLoadSystem.afterSceneLoadedEvent != null)
            {
                sceneLoadSystem.afterSceneLoadedEvent.OnEventRaised += OnSceneLoaded;
            }
        }
        else
        {
            Debug.LogWarning("未找到 SceneLoad 系统，将使用手动注册");
        }
    }

    /// <summary>
    /// 场景加载完成回调
    /// </summary>
    private void OnSceneLoaded()
    {
        StartCoroutine(RegisterSceneTimelinesWithDelay());
    }

    private IEnumerator RegisterSceneTimelinesWithDelay()
    {
        yield return new WaitForSeconds(registrationDelay);
        RegisterCurrentSceneTimelines();
    }

    /// <summary>
    /// 注册当前活跃场景中的 Timeline Director
    /// </summary>
    public void RegisterCurrentSceneTimelines()
    {
        string currentSceneName = GetCurrentSceneName();
        RegisterSceneTimelines(currentSceneName);
    }

    /// <summary>
    /// 注册指定场景的 Timeline Director
    /// </summary>
    public void RegisterSceneTimelines(string sceneName)
    {
        // 查找场景配置
        List<SceneTimeline> sceneConfigs = sceneTimelines.FindAll(x => x.sceneName == sceneName);
        
        if (sceneConfigs.Count == 0)
        {
            Log($"场景 '{sceneName}' 没有配置 Timeline");
            return;
        }

        foreach (var config in sceneConfigs)
        {
            if (!registeredDirectors.ContainsKey(config.timelineDisplayName))
            {
                RegisterTimeline(config);
            }
        }
    }

    /// <summary>
    /// 注册单个 Timeline
    /// </summary>
    private void RegisterTimeline(SceneTimeline config)
    {
        PlayableDirector director = FindDirectorByName(config.directorObjectName);
        
        if (director != null)
        {
            registeredDirectors[config.timelineDisplayName] = director;
            Log($"注册 Timeline: {config.timelineDisplayName} -> {director.name}");
        }
        
    }
    
    /// <summary>
    /// 通过名称查找 Director
    /// </summary>
    private PlayableDirector FindDirectorByName(string directorName)
    {
        if (string.IsNullOrEmpty(directorName))
        {
            Debug.LogError("Director 名称为空");
            return null;
        }

        // 方法1: 直接通过名称查找
        GameObject directorObj = GameObject.Find(directorName);
        if (directorObj != null)
        {
            PlayableDirector director = directorObj.GetComponent<PlayableDirector>();
            if (director != null)
            {
                return director;
            }
            else
            {
                Debug.LogError($"对象 '{directorName}' 上没有 PlayableDirector 组件");
                return null;
            }
        }

        // 方法2: 在所有对象中查找
        PlayableDirector[] allDirectors = FindObjectsOfType<PlayableDirector>();
        foreach (PlayableDirector director in allDirectors)
        {
            if (director.name == directorName || director.gameObject.name == directorName)
            {
                return director;
            }
        }

        // 方法3: 尝试通过路径查找
        string[] nameParts = directorName.Split('/');
        if (nameParts.Length > 1)
        {
            Transform current = null;
            for (int i = 0; i < nameParts.Length; i++)
            {
                if (current == null)
                {
                    current = GameObject.Find(nameParts[i])?.transform;
                }
                else
                {
                    current = current.Find(nameParts[i]);
                }
                
                if (current == null) break;
            }
            
            if (current != null)
            {
                PlayableDirector director = current.GetComponent<PlayableDirector>();
                if (director != null)
                {
                    return director;
                }
            }
        }

        Log($"未找到 Timeline Director: {directorName}");
        return null;
    }

    /// <summary>
    /// 播放指定 Timeline
    /// </summary>
    public void PlayTimeline(string timelineName)
    {
        if (registeredDirectors.ContainsKey(timelineName))
        {
            PlayableDirector director = registeredDirectors[timelineName];
            if (director != null && director.playableAsset != null)
            {
                director.Play();
                Log($"播放 Timeline: {timelineName}");
            }
            else
            {
                Debug.LogError($"Timeline '{timelineName}' 的 Director 或资源无效");
            }
        }
        else
        {
            Debug.LogError($"未找到已注册的 Timeline: {timelineName}");
            Debug.Log($"可用 Timeline: {string.Join(", ", GetRegisteredTimelineNames())}");
        }
    }

   
    /// <summary>
    /// 注销指定 Timeline
    /// </summary>
    public void UnregisterTimeline(string timelineName)
    {
        if (registeredDirectors.ContainsKey(timelineName))
        {
            registeredDirectors.Remove(timelineName);
            Log($"注销 Timeline: {timelineName}");
        }
    }

    

    /// <summary>
    /// 获取所有已注册的 Timeline 名称
    /// </summary>
    public List<string> GetRegisteredTimelineNames()
    {
        return new List<string>(registeredDirectors.Keys);
    }

    /// <summary>
    /// 手动添加 Timeline 引用
    /// </summary>
    public void ManualRegisterTimeline(string timelineName, PlayableDirector director)
    {
        if (director != null)
        {
            registeredDirectors[timelineName] = director;
            Log($"手动注册 Timeline: {timelineName} -> {director.name}");
        }
    }
    

    // 内部方法
    private string GetCurrentSceneName()
    {
        // 如果有 SceneLoad 系统，尝试使用它
        if (sceneLoadSystem != null)
        {
            // 使用反射调用 GetCurrentSceneName 方法
            var method = sceneLoadSystem.GetType().GetMethod("GetCurrentSceneName");
            if (method != null)
            {
                return method.Invoke(sceneLoadSystem, null) as string;
            }
        }

        // 备用方法
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }

    private void Log(string message)
    {
        if (showDebug)
        {
            Debug.Log($"[TimelineManager] {message}");
        }
    }

    void OnDestroy()
    {
        // 清理事件监听
        if (sceneLoadSystem != null && sceneLoadSystem.afterSceneLoadedEvent != null)
        {
            sceneLoadSystem.afterSceneLoadedEvent.OnEventRaised -= OnSceneLoaded;
        }
        
        registeredDirectors.Clear();
    }

    // 编辑器工具
    [ContextMenu("注册当前场景的所有Timeline")]
    public void EditorRegisterCurrentScene()
    {
        RegisterCurrentSceneTimelines();
    }

    [ContextMenu("打印所有已注册的Timeline")]
    public void PrintRegisteredTimelines()
    {
        Debug.Log("=== 已注册的 Timeline ===");
        foreach (var pair in registeredDirectors)
        {
            string status = pair.Value != null ? 
                $"{pair.Value.state} (Asset: {pair.Value.playableAsset?.name})" : "NULL";
            Debug.Log($"{pair.Key}: {status}");
        }
        
        if (registeredDirectors.Count == 0)
        {
            Debug.Log("没有已注册的 Timeline");
        }
    }

    [ContextMenu("清理所有Timeline引用")]
    public void ClearAllReferences()
    {
        registeredDirectors.Clear();
        Log("已清理所有 Timeline 引用");
    }

    [ContextMenu("测试查找Director")]
    public void TestFindDirector()
    {
        string testName = "TestDirector"; // 修改为你的测试对象名
        PlayableDirector director = FindDirectorByName(testName);
        if (director != null)
        {
            Debug.Log($"找到 Director: {director.name}");
        }
        else
        {
            Debug.Log($"未找到 Director: {testName}");
        }
    }
}