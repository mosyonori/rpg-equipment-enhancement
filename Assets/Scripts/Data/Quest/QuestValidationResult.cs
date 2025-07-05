using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// クエスト妥当性検証結果データクラス
/// クエスト開始条件の検証結果を格納
/// </summary>
[System.Serializable]
public class QuestValidationResult
{
    [Header("検証対象")]
    public int questId;

    [Header("検証結果")]
    public bool isValid;
    public List<string> errors;

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public QuestValidationResult()
    {
        questId = 0;
        isValid = false;
        errors = new List<string>();
    }

    /// <summary>
    /// エラーを追加
    /// </summary>
    /// <param name="error">エラーメッセージ</param>
    public void AddError(string error)
    {
        if (string.IsNullOrEmpty(error)) return;

        errors.Add(error);
        isValid = false;
    }
}