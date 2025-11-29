using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "GameScene", menuName = "Scene Management/Game Scene")]
public class GameSceneSO : ScriptableObject
{
    [Header("场景信息")]
    public string sceneName;
    public GameState sceneState;
    
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
    
    
    
    
    
}