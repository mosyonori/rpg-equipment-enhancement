using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 戦闘結果データ
/// 戦闘終了後の全結果情報を保持
/// </summary>
[System.Serializable]
public class BattleResultData
{
    [Header("戦闘結果")]
    public bool isVictory;                  // 勝利フラグ
    public int totalTurns;                  // 総ターン数
    public float battleDuration;            // 戦闘時間（秒）
    public BattleEndReason endReason;       // 戦闘終了理由

    [Header("獲得報酬")]
    public int gainedExp;                   // 獲得経験値
    public int gainedGold;                  // 獲得ゴールド
    public List<DropResult> dropItems;      // ドロップアイテム

    [Header("戦闘統計")]
    public int totalDamageDealt;            // 与えた総ダメージ
    public int totalDamageReceived;         // 受けた総ダメージ
    public int skillsUsed;                  // 使用スキル数
    public int criticalHits;                // クリティカル回数

    [Header("レベルアップ情報")]
    public bool leveledUp;                  // レベルアップしたか
    public int newLevel;                    // 新しいレベル
    public int newExp;                      // 新しい経験値

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public BattleResultData()
    {
        dropItems = new List<DropResult>();
        endReason = BattleEndReason.Victory;
    }

    /// <summary>
    /// 既存UserSaveDataへ戦闘結果を適用するメソッド
    /// </summary>
    public void ApplyResultToUserData(UserSaveData userData)
    {
        if (!isVictory)
        {
            // 敗北時の統計更新
            userData.statistics.totalBattles++;
            userData.statistics.totalDefeats++;
            return;
        }

        // 経験値とゴールド追加
        userData.AddExperience(gainedExp);
        userData.gold += gainedGold;

        // ドロップアイテム追加
        foreach (var dropItem in dropItems)
        {
            var itemType = dropItem.itemType switch
            {
                "EnhanceItem" => ItemType.EnhanceItem,
                "SupportItem" => ItemType.SupportItem,
                _ => ItemType.EnhanceItem
            };

            // 既存アイテムに追加またはスタック
            var existingItem = userData.items.Find(i =>
                i.itemType == itemType && i.itemMasterId == dropItem.itemId);

            if (existingItem != null)
            {
                existingItem.AddItem(dropItem.quantity);
            }
            else
            {
                // アイテムが存在しない場合は新規作成
                var newItem = new UserItemData
                {
                    itemType = itemType,
                    itemMasterId = dropItem.itemId,
                    quantity = dropItem.quantity
                };
                userData.AddItem(newItem);
            }
        }

        // 統計情報更新
        userData.statistics.totalBattles++;
        userData.statistics.totalWins++;
        userData.statistics.totalGoldEarned += gainedGold;

        // レベルアップ情報更新
        if (leveledUp)
        {
            newLevel = userData.playerLevel;
            newExp = userData.currentExp;
        }
    }

    /// <summary>
    /// 戦闘結果の文字列表現
    /// </summary>
    public override string ToString()
    {
        string result = isVictory ? "勝利" : "敗北";
        return $"戦闘結果: {result} ({totalTurns}ターン, {battleDuration:F1}秒)";
    }
}

/// <summary>
/// 戦闘終了理由
/// </summary>
public enum BattleEndReason
{
    Victory,        // 敵全滅による勝利
    Defeat,         // プレイヤー敗北
    TurnLimit,      // ターン制限到達
    Timeout,        // 時間切れ
    Disconnect      // 接続切れ
}