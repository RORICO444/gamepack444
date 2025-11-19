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

        public ExcelData()
        {
            speaker = "";
            content = "";
        }

        public ExcelData(string speaker, string content)
        {
            this.speaker = speaker ?? "";
            this.content = content ?? "";
        }

        public override string ToString()
        {
            return $"Speaker: {speaker}, Content: {content}";
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
    /// 安全地读取Excel行数据
    /// </summary>
    private static ExcelData ReadExcelRowSafely(IExcelDataReader reader, int rowNumber)
    {
        try
        {
            // 检查是否有足够的列
            if (reader.FieldCount < 2)
            {
                Debug.LogWarning($"第{rowNumber}行列数不足，需要至少2列，实际{reader.FieldCount}列");
                return null;
            }

            string speaker = "";
            string content = "";

            // 安全地读取说话者列（第0列）
            if (!reader.IsDBNull(0))
            {
                speaker = reader.GetValue(0)?.ToString() ?? "";
            }
            else
            {
                Debug.LogWarning($"第{rowNumber}行第1列为空");
            }

            // 安全地读取内容列（第1列）
            if (!reader.IsDBNull(1))
            {
                content = reader.GetValue(1)?.ToString() ?? "";
            }
            else
            {
                Debug.LogWarning($"第{rowNumber}行第2列为空");
            }

            // 如果两列都为空，跳过这一行
            if (string.IsNullOrEmpty(speaker) && string.IsNullOrEmpty(content))
            {
                Debug.LogWarning($"第{rowNumber}行数据为空，已跳过");
                return null;
            }

            return new ExcelData(speaker, content);
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
    private static List<ExcelData> CreateFallbackData()
    {
        Debug.Log("创建备用对话数据");
        
        List<ExcelData> fallbackData = new List<ExcelData>
        {
            new ExcelData("系统", "欢迎来到游戏！"),
            new ExcelData("系统", "这是备用对话内容。"),
            new ExcelData("系统", "请检查Excel文件路径和格式是否正确。")
        };

        return fallbackData;
    }

    /// <summary>
    /// 测试Excel读取功能
    /// </summary>
    public static void TestExcelReading(string filePath)
    {
        Debug.Log("=== Excel读取测试开始 ===");
        
        try
        {
            var data = ReadExcel(filePath);
            
            if (data == null)
            {
                Debug.LogError("测试失败：返回的数据为null");
                return;
            }

            Debug.Log($"成功读取 {data.Count} 行数据:");
            for (int i = 0; i < data.Count; i++)
            {
                Debug.Log($"行 {i + 1}: {data[i]}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Excel读取测试失败: {ex.Message}");
        }
        
        Debug.Log("=== Excel读取测试结束 ===");
    }
}