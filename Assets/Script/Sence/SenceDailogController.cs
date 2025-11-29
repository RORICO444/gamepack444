using UnityEngine;
using System.Collections;

public class SceneDialogController : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private SceneLoad sceneLoad;
    [SerializeField] private LargeDialogManager dialogManager;
    
    [Header("设置")]
    [SerializeField] private bool autoPlaySceneDialog = true;
    [SerializeField] private float dialogStartDelay = 1.5f;
    
    private void Awake()
    {
        // 获取引用
        if (sceneLoad == null)
            sceneLoad = FindObjectOfType<SceneLoad>();
        if (dialogManager == null)
            dialogManager = FindObjectOfType<LargeDialogManager>();
            
        // 注册事件
        if (sceneLoad != null)
        {
            // 假设SceneLoad有一个场景加载完成的事件
            // sceneLoad.OnSceneLoaded += OnSceneLoaded;
        }
        
        if (dialogManager != null)
        {
            dialogManager.OnDialogEnd += OnDialogEnd;
        }
    }
    
    private void OnSceneLoaded()
    {
        if (autoPlaySceneDialog)
        {
            StartCoroutine(StartSceneDialogWithDelay());
        }
    }
    
    private IEnumerator StartSceneDialogWithDelay()
    {
        yield return new WaitForSeconds(dialogStartDelay);
        StartSceneDialog();
    }
    
    private void StartSceneDialog()
    {
        string sceneName = GetCurrentSceneName();
        string storyPath = Constants.StoryPaths.GetStoryPathByScene(sceneName);
        
        if (dialogManager != null && !string.IsNullOrEmpty(storyPath))
        {
            dialogManager.StartDialog(storyPath);
        }
    }
    
    private string GetCurrentSceneName()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }
    
    private void OnDialogEnd()
    {
        Debug.Log("场景对话结束");
        // 对话结束后的处理
    }
    
    private void OnDestroy()
    {
        if (dialogManager != null)
        {
            dialogManager.OnDialogEnd -= OnDialogEnd;
        }
    }
}