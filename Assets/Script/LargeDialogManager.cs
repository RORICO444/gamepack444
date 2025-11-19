using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LargeDialogManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialogPanel; // 对话框面板
    public Text speakerName;
    public Text speakerContent;
    //public Image speakerAvatar; // 可选：说话者头像
    
    [Header("Dialog Settings")]
    public DialogManager dialogManager; 
    public int letterPerSecond = 30;
    
    [Header("Animation")]
    //public Animator dialogAnimator; // 可选：对话框动画
    
    private string filePath = Constants.STORY_PATH;
    private List<ExcelReader.ExcelData> storyData;
    private int currentLine = 0;
    
    // 打字机效果相关变量
    private bool isTyping = false;
    private string currentText = "";
    private Coroutine typingCoroutine;
    [SerializeField] private float typingSpeed;
    
    // 事件回调
    public Action OnDialogStart;
    public Action OnDialogEnd;
    public Action<string> OnSpeakerChange; // 说话者改变时的回调

    
                 

    private void Awake()
    {
        Instance = this;
        dialogManager = GetComponent<DialogManager>();
        SetTypingSpeed(letterPerSecond);
        
        // 初始化时隐藏对话框
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
    }

    public static LargeDialogManager Instance { get; set; }

    public void HandleUpdate()
    {
        if (dialogPanel != null && dialogPanel.activeInHierarchy)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                if (isTyping)
                {
                    // 如果正在打字，立即完成当前文本
                    CompleteText();
                }
                else
                {
                    // 如果打字已完成，显示下一行
                    ShowNextLine();
                }
            }
            
            // 可选：跳过整个对话
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SkipDialog();
            }
        }
    }

    /// <summary>
    /// 开始显示对话
    /// </summary>
    public void StartDialog()
    {
        
        StartDialog(filePath);
    }

    /// <summary>
    /// 开始显示指定路径的对话
    /// </summary>
    /// <param name="path">对话文件路径</param>
    public void StartDialog(string path)
    {
        load_story(path);
        currentLine = 0;
        
        // 显示对话框
        if (dialogPanel != null)
            dialogPanel.SetActive(true);
        
        // 播放显示动画
        //if (dialogAnimator != null)
        //    dialogAnimator.SetBool("IsOpen", true);
        
        // 触发开始事件
        OnDialogStart?.Invoke();
        
        // 显示第一行对话
        ShowNextLine();
    }

    /// <summary>
    /// 结束对话
    /// </summary>
    public void EndDialog()
    {
        // 停止打字协程
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        
        // 隐藏对话框
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
        
        // 播放隐藏动画
        //if (dialogAnimator != null)
        //    dialogAnimator.SetBool("IsOpen", false);
        
        // 触发结束事件
        OnDialogEnd?.Invoke();
        
        // 重置状态
        isTyping = false;
        currentLine = 0;
    }

    private void load_story(string path)
    {
        try
        {
            Debug.Log($"开始加载故事文件: {path}");
            
            // 调用修复后的 ExcelReader
            storyData = ExcelReader.ReadExcel(path);
            
            if (storyData == null)
            {
                Debug.LogError($"ExcelReader.ReadExcel 返回 null");
                storyData = new List<ExcelReader.ExcelData>();
                return;
            }
            
            if (storyData.Count == 0)
            {
                Debug.LogWarning($"故事数据为空，路径: {path}");
            }
            else
            {
                Debug.Log($"成功加载 {storyData.Count} 行对话数据");
                
                // 打印前几行数据用于调试
                for (int i = 0; i < Mathf.Min(3, storyData.Count); i++)
                {
                    Debug.Log($"第{i+1}行: 说话者='{storyData[i].speaker}', 内容='{storyData[i].content}'");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载故事失败: {e.Message}\n堆栈跟踪: {e.StackTrace}");
            storyData = new List<ExcelReader.ExcelData>();
        }
    }


    void ShowNextLine()
    {
        // 检查是否还有对话内容
        if (currentLine >= storyData.Count)
        {
            Debug.Log("Dialog finished");
            EndDialog();
            return;
        }
        
        var data = storyData[currentLine];
        
        // 更新说话者名字
        speakerName.text = data.speaker;
        
        // 触发说话者改变事件
        OnSpeakerChange?.Invoke(data.speaker);
        
        // 更新说话者头像（如果有）
        //if (speakerAvatar != null && data.avatar != null)
        //{
        //    speakerAvatar.sprite = data.avatar;
        //}
        
        currentText = data.content;
        
        // 开始打字机效果
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(currentText));
        
        currentLine++;
    }
    
    // 打字机效果协程
    IEnumerator TypeText(string text)
    {
        isTyping = true;
        speakerContent.text = "";
        
        foreach (char letter in text.ToCharArray())
        {
            speakerContent.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        
        isTyping = false;
        typingCoroutine = null;
    }
    
    // 立即完成当前文本显示
    void CompleteText()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        
        speakerContent.text = currentText;
        isTyping = false;
    }
    
    // 跳过整个对话
    public void SkipDialog()
    {
        EndDialog();
    }
    
    // 设置打字速度
    public void SetTypingSpeed(int letterPerSecond)
    {
        typingSpeed = 1.0f / letterPerSecond;
    }
    
    // 检查对话框是否正在显示
    public bool IsDialogActive()
    {
        return dialogPanel != null && dialogPanel.activeInHierarchy;
    }
    
    // 获取当前对话进度
    public (int current, int total) GetDialogProgress()
    {
        return (currentLine, storyData?.Count ?? 0);
    }
}