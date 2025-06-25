using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ユーザーのセーブデータ全体
/// </summary>
[System.Serializable]
public class UserSaveData
{
    [Header("プレイヤー基本情報")]
    public string playerId;             // プレイヤーID
    public string playerName;           // プレイヤー名
    public int playerLevel;             // プレイヤーレベル
    public long totalExp;               // 総経験値
    public DateTime lastLoginDate;      // 最終ログイン日時
    public DateTime createDate;         // アカウント作成日時

    [Header("ゲーム進行状況")]
    public int currentStage;            // 現在のステージ
    public int highestStage;            // 最高到達ステージ
    public long totalPlayTime;          // 総プレイ時間（秒）

    [Header("通貨・リソース")]
    public long gold;                   // ゴールド
    public int gems;                    // ジェム
    public int stamina;                 // スタミナ
    public DateTime staminaLastRecoveryTime; // スタミナ最終回復時刻

    [Header("装備データ")]
    public List<UserEquipmentData> equipments;          // 所持装備リスト
    public List<string> equippedWeaponIds;              // 装備中の武器ID（複数キャラ対応）
    public List<string> equippedArmorIds;               // 装備中の防具ID
    public List<string> equippedAccessoryIds;           // 装備中のアクセサリーID

    [Header("アイテムデータ")]
    public List<UserItemData> items;                    // 所持アイテムリスト

    [Header("設定・フラグ")]
    public GameSettings gameSettings;                   // ゲーム設定
    public List<string> clearedStages;                  // クリア済みステージリスト
    public List<int> unlockedSkills;                    // 解放済みスキルリスト
    public List<string> unlockedCharacters;             // 解放済みキャラクターリスト

    [Header("統計情報")]
    public GameStatistics statistics;                   // ゲーム統計情報

    /// <summary>
    /// デフォルトコンストラクタ（新規ユーザー用）
    /// </summary>
    public UserSaveData()
    {
        playerId = Guid.NewGuid().ToString();
        playerName = "新規プレイヤー";
        playerLevel = 1;
        totalExp = 0;
        lastLoginDate = DateTime.Now;
        createDate = DateTime.Now;

        currentStage = 1;
        highestStage = 1;
        totalPlayTime = 0;

        gold = 1000;        // 初期ゴールド
        gems = 10;          // 初期ジェム
        stamina = 100;      // 初期スタミナ
        staminaLastRecoveryTime = DateTime.Now;

        equipments = new List<UserEquipmentData>();
        equippedWeaponIds = new List<string>();
        equippedArmorIds = new List<string>();
        equippedAccessoryIds = new List<string>();

        items = new List<UserItemData>();

        gameSettings = new GameSettings();
        clearedStages = new List<string>();
        unlockedSkills = new List<int>();
        unlockedCharacters = new List<string>();

        statistics = new GameStatistics();
    }

    /// <summary>
    /// 装備を追加
    /// </summary>
    public void AddEquipment(UserEquipmentData equipment)
    {
        if (equipment == null) return;
        equipments.Add(equipment);
    }

    /// <summary>
    /// 装備を削除
    /// </summary>
    public bool RemoveEquipment(string userEquipmentId)
    {
        var equipment = GetEquipment(userEquipmentId);
        if (equipment == null) return false;

        // 装備中の場合は装備を外す
        if (equipment.isEquipped)
        {
            UnEquipItem(userEquipmentId);
        }

        return equipments.RemoveAll(e => e.userEquipmentId == userEquipmentId) > 0;
    }

    /// <summary>
    /// 装備を取得
    /// </summary>
    public UserEquipmentData GetEquipment(string userEquipmentId)
    {
        return equipments.Find(e => e.userEquipmentId == userEquipmentId);
    }

    /// <summary>
    /// アイテムを追加
    /// </summary>
    public void AddItem(UserItemData item)
    {
        if (item == null) return;

        // 同じマスターIDのアイテムが既に存在するかチェック
        var existingItem = items.Find(i => i.itemType == item.itemType && i.itemMasterId == item.itemMasterId);

        if (existingItem != null)
        {
            // 既存アイテムに追加
            existingItem.AddItem(item.quantity);
        }
        else
        {
            // 新規アイテムとして追加
            items.Add(item);
        }
    }

    /// <summary>
    /// アイテムを使用
    /// </summary>
    public bool UseItem(ItemType itemType, int itemMasterId, int quantity)
    {
        var item = items.Find(i => i.itemType == itemType && i.itemMasterId == itemMasterId);
        if (item == null || !item.CanUse(quantity)) return false;

        bool success = item.UseItem(quantity);

        // アイテムが0個になった場合はリストから削除
        if (item.IsEmpty())
        {
            items.Remove(item);
        }

        return success;
    }

    /// <summary>
    /// アイテム所持数を取得
    /// </summary>
    public int GetItemQuantity(ItemType itemType, int itemMasterId)
    {
        var item = items.Find(i => i.itemType == itemType && i.itemMasterId == itemMasterId);
        return item?.quantity ?? 0;
    }

    /// <summary>
    /// 装備をキャラクターに装着
    /// </summary>
    public bool EquipItem(string userEquipmentId, string characterId, EquipmentMasterData masterData)
    {
        var equipment = GetEquipment(userEquipmentId);
        if (equipment == null || equipment.isEquipped) return false;

        // 同じタイプの装備が既に装着されている場合は外す
        UnEquipSameTypeItem(characterId, masterData.equipmentType);

        // 装備を装着
        equipment.EquipToCharacter(characterId);

        // 装備IDを対応するリストに追加
        switch (masterData.equipmentType)
        {
            case EquipmentType.Weapon:
                equippedWeaponIds.Add(userEquipmentId);
                break;
            case EquipmentType.Armor:
                equippedArmorIds.Add(userEquipmentId);
                break;
            case EquipmentType.Accessory:
                equippedAccessoryIds.Add(userEquipmentId);
                break;
        }

        return true;
    }

    /// <summary>
    /// 装備を外す
    /// </summary>
    public bool UnEquipItem(string userEquipmentId)
    {
        var equipment = GetEquipment(userEquipmentId);
        if (equipment == null || !equipment.isEquipped) return false;

        equipment.UnEquip();

        // 装備IDを対応するリストから削除
        equippedWeaponIds.RemoveAll(id => id == userEquipmentId);
        equippedArmorIds.RemoveAll(id => id == userEquipmentId);
        equippedAccessoryIds.RemoveAll(id => id == userEquipmentId);

        return true;
    }

    /// <summary>
    /// 同じタイプの装備を外す
    /// </summary>
    private void UnEquipSameTypeItem(string characterId, EquipmentType equipmentType)
    {
        var equippedItems = equipments.FindAll(e => e.isEquipped && e.equippedCharacterId == characterId);

        foreach (var item in equippedItems)
        {
            // マスターデータから装備タイプを確認する必要があるが、簡易的に装備IDリストから判定
            bool shouldUnEquip = equipmentType switch
            {
                EquipmentType.Weapon => equippedWeaponIds.Contains(item.userEquipmentId),
                EquipmentType.Armor => equippedArmorIds.Contains(item.userEquipmentId),
                EquipmentType.Accessory => equippedAccessoryIds.Contains(item.userEquipmentId),
                _ => false
            };

            if (shouldUnEquip)
            {
                UnEquipItem(item.userEquipmentId);
                break; // 同じタイプは1つだけ装備可能
            }
        }
    }

    /// <summary>
    /// 最終ログイン日時を更新
    /// </summary>
    public void UpdateLastLoginDate()
    {
        lastLoginDate = DateTime.Now;
    }

    /// <summary>
    /// スタミナを回復
    /// </summary>
    public void RecoverStamina(int maxStamina, int recoveryRate)
    {
        TimeSpan timeSinceLastRecovery = DateTime.Now - staminaLastRecoveryTime;
        int recoveryMinutes = (int)timeSinceLastRecovery.TotalMinutes;

        if (recoveryMinutes > 0)
        {
            int recoveredStamina = recoveryMinutes * recoveryRate;
            stamina = Mathf.Min(maxStamina, stamina + recoveredStamina);
            staminaLastRecoveryTime = DateTime.Now;
        }
    }

    /// <summary>
    /// アイテム所持状況の集計を取得
    /// </summary>
    public ItemInventorySummary GetItemSummary()
    {
        return ItemInventorySummary.CreateFromUserItems(items);
    }
}

/// <summary>
/// ゲーム設定
/// </summary>
[System.Serializable]
public class GameSettings
{
    public float bgmVolume = 1.0f;          // BGM音量
    public float seVolume = 1.0f;           // SE音量
    public bool isVibrationEnabled = true;  // バイブレーション有効
    public bool isNotificationEnabled = true; // 通知有効
    public int graphicsQuality = 2;         // グラフィック品質（0:低 1:中 2:高）
    public bool isAutoSaveEnabled = true;   // オートセーブ有効
}

/// <summary>
/// ゲーム統計情報
/// </summary>
[System.Serializable]
public class GameStatistics
{
    public int totalBattles;                // 総戦闘回数
    public int totalWins;                   // 総勝利回数
    public int totalDefeats;                // 総敗北回数
    public int totalEnhancements;           // 総強化回数
    public int successfulEnhancements;      // 成功した強化回数
    public int failedEnhancements;          // 失敗した強化回数
    public long totalGoldEarned;            // 総獲得ゴールド
    public long totalGoldSpent;             // 総消費ゴールド
    public int totalItemsAcquired;          // 総取得アイテム数
    public int totalEquipmentsAcquired;     // 総取得装備数

    /// <summary>
    /// 勝率を計算
    /// </summary>
    public float GetWinRate()
    {
        return totalBattles > 0 ? (float)totalWins / totalBattles : 0f;
    }

    /// <summary>
    /// 強化成功率を計算
    /// </summary>
    public float GetEnhancementSuccessRate()
    {
        return totalEnhancements > 0 ? (float)successfulEnhancements / totalEnhancements : 0f;
    }
}