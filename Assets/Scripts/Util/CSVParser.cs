using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public delegate void ParseMethod(string cellValue, int x, int y);
public delegate T ParseCellValueMethod<T>(string cellValue, int x, int y);

public static class CSVParser
{
    public static void Parse(TextAsset asset, ParseMethod parseCellValue)
    {
        if (asset != null)
        {
            var rows = asset.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            for (int y =0; y < rows.Length; y++)
            {
                var cellValues = rows[y].Split(new char[] { ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        
                for (int x = 0; x < cellValues.Length; x++)
                {
                    parseCellValue(cellValues[x].Trim(), x, y);
                }
            }
        }
    }

    public static List<List<T>> ParseGrid<T>(TextAsset asset, ParseCellValueMethod<T> parseCellValue)
    {
        var result = new List<List<T>>();

        if (asset != null)
        {
            var rows = asset.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            for (int y = 0; y < rows.Length; y++)
            {
                var rowResult = new List<T>();
                result.Add(rowResult);

                var cellValues = rows[y].Split(new char[] { ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                for (int x = 0; x < cellValues.Length; x++)
                {
                    rowResult.Add(parseCellValue(cellValues[x].Trim(), x, y));
                }
            }
        }

        return result;
    }
}


