using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class LargeDialogManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialogPanel; // 对话框面板
    public GameObject CG1;
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
    public int interacteCount = 0;
    
    // 打字机效果相关变量
    private bool isTyping = false;
    private string currentText = "";
    private Coroutine typingCoroutine;
    [SerializeField] private float typingSpeed;
    
    // 事件回调
    public Action OnDialogStart;
    public Action OnDialogEnd;
    public Action OnDialogPause;
    public Action<string> OnSpeakerChange; // 说话者改变时的回调
    public Action<string, ExcelReader.ExcelData> OnEventTriggered; // 新增：事件触发回调
    
    

    private void Awake()
    {
        Instance = this;
        dialogManager = GetComponent<DialogManager>();
        SetTypingSpeed(letterPerSecond);
        
        // 初始化时隐藏对话框
        if (dialogPanel != null)
            dialogPanel.SetActive(false);

        // 注册Excel事件处理器
        ExcelReader.OnEventTriggered += HandleExcelEvent;
    }

    void OnDestroy()
    {
        // 取消注册事件处理器
        ExcelReader.OnEventTriggered -= HandleExcelEvent;
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
                    CompleteText();
                }
                else
                {
                    ShowNextLine();
                }
            }
            
            //跳过整个对话
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
        
            //调用ExcelReader
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
    }

    public void ShowNextLine()
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
        typingCoroutine = StartCoroutine(TypeText(currentText, data));
        
        currentLine++;
    }
    
    // 打字机效果协程（修改：添加事件触发）
    IEnumerator TypeText(string text, ExcelReader.ExcelData data)
    {
        isTyping = true;
        speakerContent.text = "";

        // 在开始打字前触发事件（如果需要立即触发）
        // TriggerLineEvents(data);

        foreach (char letter in text.ToCharArray())
        {
            speakerContent.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        typingCoroutine = null;

        // 在文本显示完成后触发事件
        TriggerLineEvents(data);
    }
    
    /// <summary>
    /// 触发当前行的事件
    /// </summary>
    private void TriggerLineEvents(ExcelReader.ExcelData data)
    {
        // 触发Excel中定义的事件
        if (!string.IsNullOrEmpty(data.eventTrigger))
        {
            
            // 触发Excel事件系统
            ExcelReader.TriggerEvent(data.eventTrigger, data);
            
            // 触发本地事件回调
            OnEventTriggered?.Invoke(data.eventTrigger, data);
        }
    }

    /// <summary>
    /// 处理Excel事件
    /// </summary>
    private void HandleExcelEvent(string eventName, ExcelReader.ExcelData rowData)
    {

        // 这里可以添加特定的事件处理逻辑
        switch (eventName)
        {
            case "ShowImageCG1":
                // 处理显示图片事件
                HandleShowImageCG1(rowData);
                break;
            case "PlaySound":
                // 处理播放声音事件
                HandlePlaySound(rowData);
                break;
            case "PauseDialog":
                // 处理暂停对话事件
                HandlePauseDialog();
                break;
            case "HideImageCG1":
                HandleHideImageCG1(rowData);
                break;
            case "StartTL1":
                HandleStartTL1();
                break;
        }
    }

    // 具体的事件处理方法示例
    private void HandleShowImageCG1(ExcelReader.ExcelData data)
    {
        Debug.Log($"显示图片事件，说话者: {data.speaker}, 内容: {data.content}");
        CG1.gameObject.SetActive(true);
    }

    private void HandleHideImageCG1(ExcelReader.ExcelData data)
    {
        CG1.gameObject.SetActive(false);
    }

    private void HandleStartTL1()
    {
        TimelineManager.Instance.PlayTimeline("TL1");
        StartCoroutine(HideANDShow());
    }

    IEnumerator HideANDShow()
    {
        dialogPanel.SetActive(false);
        yield return new WaitForSeconds(5f);
        dialogPanel.SetActive(true);
    }

    private void HandlePlaySound(ExcelReader.ExcelData data)
    {
        Debug.Log($"播放声音事件，说话者: {data.speaker}, 内容: {data.content}");
        // 在这里添加播放声音的逻辑
        // 例如：根据data.content中的声音文件名播放音效
    }
    
    private void HandlePauseDialog()
    {
        Debug.Log($"暂停对话事件");
        dialogPanel.SetActive(false);
        OnDialogPause?.Invoke();
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

        // 完成文本后触发事件（如果之前没有触发）
        if (currentLine > 0 && currentLine <= storyData.Count)
        {
            var data = storyData[currentLine - 1];
            TriggerLineEvents(data);
        }
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

    /// <summary>
    /// 跳转到指定行
    /// </summary>
    public void JumpToLine(int lineNumber)
    {
        if (lineNumber >= 0 && lineNumber < storyData.Count)
        {
            currentLine = lineNumber;
            if (IsDialogActive())
            {
                ShowNextLine();
            }
        }
        else
        {
            Debug.LogWarning($"跳转行号 {lineNumber} 超出范围，有效范围: 0-{storyData.Count - 1}");
        }
    }

    /// <summary>
    /// 跳转到指定事件的行
    /// </summary>
    public void JumpToEvent(string eventName)
    {
        for (int i = 0; i < storyData.Count; i++)
        {
            if (storyData[i].eventTrigger == eventName)
            {
                JumpToLine(i);
                return;
            }
        }
        Debug.LogWarning($"未找到事件: {eventName}");
    }
}