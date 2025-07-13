using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 戦闘開始時の初期設定データ
/// 戦闘開始に必要な全情報を保持
/// </summary>
[System.Serializable]
public class BattleSetupData
{
    [Header("クエスト情報")]
    public int questId;                     // 選択されたクエストID
    public string questName;               // クエスト名
    public int turnLimit;                   // ターン制限（-1=無制限）
    public List<int> spawnMonsterIds;       // 出現モンスターIDリスト

    [Header("プレイヤー情報")]
    public string playerName;               // プレイヤー名
    public int playerLevel;                 // プレイヤーレベル
    public EquipmentTotalStats playerStats; // プレイヤー合計ステータス（装備込み）
    public List<string> playerSkillIds;     // プレイヤー戦闘用スキルID（UserSaveDataのbattleSkill1Id, battleSkill2Id）
    public List<string> playerEquipmentIds; // 装備中の装備ID群

    [Header("報酬設定")]
    public int baseRewardExp;               // 基本経験値報酬
    public int baseRewardGold;              // 基本ゴールド報酬
    public string dropTableId;              // ドロップテーブルID

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public BattleSetupData()
    {
        spawnMonsterIds = new List<int>();
        playerSkillIds = new List<string>();
        playerEquipmentIds = new List<string>();
        playerStats = new EquipmentTotalStats();
    }

    /// <summary>
    /// 既存UserSaveDataからBattleSetupDataを作成するファクトリーメソッド
    /// </summary>
    public static BattleSetupData CreateFromUserData(UserSaveData userData, QuestMasterData questData)
    {
        var setupData = new BattleSetupData
        {
            questId = questData.questId,
            turnLimit = questData.turnLimit,
            spawnMonsterIds = questData.GetSpawnMonsterIds(),
            playerName = userData.playerName,
            playerLevel = userData.playerLevel,
            baseRewardExp = questData.rewardExp,
            baseRewardGold = questData.rewardGold,
            dropTableId = questData.dropItemTable
        };

        // 戦闘用スキル設定
        if (!string.IsNullOrEmpty(userData.battleSkill1Id))
            setupData.playerSkillIds.Add(userData.battleSkill1Id);
        if (!string.IsNullOrEmpty(userData.battleSkill2Id))
            setupData.playerSkillIds.Add(userData.battleSkill2Id);

        // 装備中アイテムID取得
        setupData.playerEquipmentIds.AddRange(userData.equippedWeaponIds);
        setupData.playerEquipmentIds.AddRange(userData.equippedArmorIds);
        setupData.playerEquipmentIds.AddRange(userData.equippedAccessoryIds);

        return setupData;
    }

    /// <summary>
    /// 有効な戦闘設定かチェック
    /// </summary>
    public bool IsValid()
    {
        return questId > 0 &&
               spawnMonsterIds.Count > 0 &&
               !string.IsNullOrEmpty(playerName);
    }
}