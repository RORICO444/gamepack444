using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExcelDataReader;
using System.IO;
using System.Text;
using Unity.Collections;

public class ExcelReader
{
    public struct ExcelData
    {
        public string speaker;
        public string content;
    }

    public static List<ExcelData> ReadExcel(string filePath)
    {
        List<ExcelData> excelData = new List<ExcelData>();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
        {
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                do
                {
                    while (reader.Read())
                    {
                        ExcelData data = new ExcelData();
                        data.speaker = reader.GetString(0);
                        data.content = reader.GetString(1);
                        excelData.Add(data);
                    }
                    
                } while (reader.NextResult());
            }
        } ;

        return excelData;
    }
    
}
