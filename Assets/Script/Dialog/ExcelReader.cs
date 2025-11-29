using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;
using ExcelDataReader;

public class ExcelReader
{
    [System.Serializable]
    public class ExcelData
    {
        public string speaker;
        public string content;
        public string eventTrigger; // 事件触发列
        public int rowNumber; // 行号，用于调试

        public ExcelData()
        {
            speaker = "";
            content = "";
            eventTrigger = "";
            rowNumber = 0;
        }

        public ExcelData(string speaker, string content, string eventTrigger = "", int rowNumber = 0)
        {
            this.speaker = speaker ?? "";
            this.content = content ?? "";
            this.eventTrigger = eventTrigger ?? "";
            this.rowNumber = rowNumber;
        }

        public override string ToString()
        {
            return $"行{rowNumber}: Speaker: {speaker}, Content: {content}, Event: {eventTrigger}";
        }
    }

    // 事件处理委托和事件
    public delegate void ExcelEventTriggerHandler(string eventName, ExcelData rowData);

    public static event ExcelEventTriggerHandler OnEventTriggered;

    /// <summary>
    /// 触发Excel中定义的事件
    /// </summary>
    public static void TriggerEvent(string eventName, ExcelData rowData)
    {
        if (string.IsNullOrEmpty(eventName))
            return;

        Debug.Log($"尝试触发事件: {eventName} (行{rowData.rowNumber})");

        if (OnEventTriggered != null)
        {
            OnEventTriggered(eventName, rowData);
        }
        else
        {
            Debug.LogWarning($"没有注册事件处理器，事件 '{eventName}' 未被处理");
        }
    }

    public static List<ExcelData> ReadExcel(string filePath)
    {
        List<ExcelData> excelData = new List<ExcelData>();

        try
        {
            Debug.Log($"开始读取Excel文件: {filePath}");

            // 检查文件是否存在
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Excel文件不存在: {filePath}");
                return CreateFallbackData();
            }

            // 注册编码提供程序
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    int rowCount = 0;

                    do
                    {
                        while (reader.Read())
                        {
                            rowCount++;
                            try
                            {
                                ExcelData data = ReadExcelRowSafely(reader, rowCount);
                                if (data != null)
                                {
                                    excelData.Add(data);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"读取第{rowCount}行时出错: {ex.Message}");
                                // 继续读取下一行
                            }
                        }
                    } while (reader.NextResult());

                    Debug.Log($"成功读取 {excelData.Count} 行数据，总行数: {rowCount}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"读取Excel文件失败: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
            return CreateFallbackData();
        }

        return excelData;
    }

    /// <summary>
    /// 安全地读取Excel行数据（读取三列：说话者、内容、事件）
    /// </summary>
    private static ExcelData ReadExcelRowSafely(IExcelDataReader reader, int rowNumber)
    {
        try
        {
            // 检查是否有足够的列
            int columnCount = reader.FieldCount;

            string speaker = "";
            string content = "";
            string eventTrigger = "";

            // 读取说话者列（第0列）
            if (columnCount > 0 && !reader.IsDBNull(0))
            {
                speaker = reader.GetValue(0)?.ToString() ?? "";
            }

            // 读取内容列（第1列）
            if (columnCount > 1 && !reader.IsDBNull(1))
            {
                content = reader.GetValue(1)?.ToString() ?? "";
            }

            // 读取事件触发列（第2列）- 可选
            if (columnCount > 2 && !reader.IsDBNull(2))
            {
                eventTrigger = reader.GetValue(2)?.ToString() ?? "";
            }

            // 如果所有列都为空，跳过这一行
            if (string.IsNullOrEmpty(speaker) && string.IsNullOrEmpty(content) && string.IsNullOrEmpty(eventTrigger))
            {
                Debug.LogWarning($"第{rowNumber}行数据为空，已跳过");
                return null;
            }

            return new ExcelData(speaker, content, eventTrigger, rowNumber);
        }
        catch (Exception ex)
        {
            Debug.LogError($"读取第{rowNumber}行数据时发生异常: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 创建备用数据（当Excel读取失败时使用）
    /// </summary>
    public static List<ExcelData> CreateFallbackData()
    {
        Debug.Log("创建备用对话数据");

        List<ExcelData> fallbackData = new List<ExcelData>
        {
            new ExcelData("系统", "欢迎来到游戏！", "GameStart", 1),
            new ExcelData("系统", "这是备用对话内容。", "BackupEvent", 2),
            new ExcelData("系统", "请检查Excel文件路径和格式是否正确。", "", 3)
        };

        return fallbackData;
    }
}