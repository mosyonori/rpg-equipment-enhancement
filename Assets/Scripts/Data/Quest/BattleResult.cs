using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 戦闘結果データクラス（将来の戦闘システム用）
/// 戦闘完了時の結果情報を格納
/// </summary>
[System.Serializable]
public class BattleResult
{
    [Header("戦闘結果")]
    public bool isVictory;
    public int turnCount;
    public int remainingHp;
    public float completionTime;

    [Header("撃破情報")]
    public Dictionary<int, int> defeatedMonsters;

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public BattleResult()
    {
        isVictory = false;
        turnCount = 0;
        remainingHp = 0;
        completionTime = 0f;
        defeatedMonsters = new Dictionary<int, int>();
    }

    /// <summary>
    /// パラメータ付きコンストラクタ
    /// </summary>
    /// <param name="isVictory">勝利フラグ</param>
    /// <param name="turnCount">ターン数</param>
    /// <param name="remainingHp">残りHP</param>
    /// <param name="completionTime">完了時間</param>
    public BattleResult(bool isVictory, int turnCount, int remainingHp, float completionTime)
    {
        this.isVictory = isVictory;
        this.turnCount = turnCount;
        this.remainingHp = remainingHp;
        this.completionTime = completionTime;
        this.defeatedMonsters = new Dictionary<int, int>();
    }
}