using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "GameScene", menuName = "Scene Management/Game Scene")]
public class GameSceneSO : ScriptableObject
{
    [Header("场景信息")]
    public string sceneName; // 确保这个字段存在且公开
    
    [Header("场景描述")]
    [TextArea] public string sceneDescription;
    
    [Header("Addressables 引用")]
    public AssetReference sceneReference;
    
    [Header("玩家生成位置")]
    public Vector3 playerSpawnPosition = Vector3.zero;
    
    [Header("对话设置")]
    public bool playSceneDialog = true;
    public string dialogFilePath;
    public float dialogStartDelay = 1.0f;
    
    // 验证方法
    public bool IsValid()
    {
        return sceneReference != null && sceneReference.RuntimeKeyIsValid();
    }
    
    // 获取场景名称的安全方法
    public string GetSceneName()
    {
        if (!string.IsNullOrEmpty(sceneName))
            return sceneName;
        
        // 如果 sceneName 为空，尝试从 AssetReference 获取
        if (sceneReference != null && sceneReference.editorAsset != null)
        {
            return sceneReference.editorAsset.name;
        }
        
        return this.name; // 最后返回 SO 的名称
    }
}