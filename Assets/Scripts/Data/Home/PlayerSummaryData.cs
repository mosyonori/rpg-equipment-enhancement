using System;
using UnityEngine;

/// <summary>
/// プレイヤーサマリーデータクラス
/// ホーム画面で表示するプレイヤー情報をまとめたデータクラス
/// </summary>
[System.Serializable]
public class PlayerSummaryData
{
    [Header("基本情報")]
    public string playerName;
    public int playerLevel;
    public int currentExp;
    public int maxExp;

    [Header("通貨・リソース")]
    public long gold;
    public int gems;
    public int currentStamina;
    public int maxStamina;

    [Header("戦闘力")]
    public int totalCombatPower;
    public int weaponPower;
    public int armorPower;
    public int accessoryPower;

    [Header("状態フラグ")]
    public bool hasNewItems;
    public bool hasCompletedQuests;
    public bool hasNewNotifications;
    public int ongoingQuestCount;

    [Header("時間情報")]
    public DateTime lastLoginDate;
    public DateTime nextStaminaRecovery;
    public TimeSpan staminaRecoveryRemaining;

    [Header("キャラクター情報")]
    public string characterImagePath;
    public int selectedCharacterId;

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public PlayerSummaryData()
    {
        playerName = "プレイヤー";
        playerLevel = 1;
        currentExp = 0;
        maxExp = 100;
        gold = 0;
        gems = 0;
        currentStamina = 0;
        maxStamina = 100;
        totalCombatPower = 0;
        weaponPower = 0;
        armorPower = 0;
        accessoryPower = 0;
        hasNewItems = false;
        hasCompletedQuests = false;
        hasNewNotifications = false;
        ongoingQuestCount = 0;
        lastLoginDate = DateTime.Now;
        nextStaminaRecovery = DateTime.Now;
        staminaRecoveryRemaining = TimeSpan.Zero;
        characterImagePath = "";
        selectedCharacterId = 1;
    }

    /// <summary>
    /// UserSaveDataからPlayerSummaryDataを作成
    /// </summary>
    /// <param name="saveData">ユーザーセーブデータ</param>
    /// <returns>プレイヤーサマリーデータ</returns>
    public static PlayerSummaryData CreateFromSaveData(UserSaveData saveData)
    {
        if (saveData == null) return new PlayerSummaryData();

        return new PlayerSummaryData
        {
            playerName = saveData.playerName,
            playerLevel = saveData.playerLevel,
            currentExp = saveData.currentExp,
            maxExp = saveData.GetRequiredExpForNextLevel(),
            gold = saveData.gold,
            gems = saveData.gems,
            currentStamina = saveData.currentStamina,
            maxStamina = saveData.maxStamina,
            hasNewItems = false, // TODO: 新アイテム判定ロジック
            hasCompletedQuests = false, // TODO: 完了クエスト判定ロジック
            hasNewNotifications = false, // TODO: 通知判定ロジック
            ongoingQuestCount = 0, // TODO: 進行中クエスト数取得
            lastLoginDate = saveData.lastLoginDate,
            nextStaminaRecovery = saveData.lastStaminaRecovery.AddMinutes(5),
            staminaRecoveryRemaining = saveData.GetTimeToNextStaminaRecovery(),
            characterImagePath = "", // TODO: キャラクター画像パス取得
            selectedCharacterId = 1 // TODO: 選択中キャラクターID取得
        };
    }

    /// <summary>
    /// 経験値の進行度（0.0～1.0）を取得
    /// </summary>
    /// <returns>経験値進行度</returns>
    public float GetExpProgress()
    {
        if (maxExp <= 0) return 0f;
        return Mathf.Clamp01((float)currentExp / maxExp);
    }

    /// <summary>
    /// スタミナの進行度（0.0～1.0）を取得
    /// </summary>
    /// <returns>スタミナ進行度</returns>
    public float GetStaminaProgress()
    {
        if (maxStamina <= 0) return 0f;
        return Mathf.Clamp01((float)currentStamina / maxStamina);
    }

    /// <summary>
    /// スタミナが満タンかどうか
    /// </summary>
    /// <returns>満タンの場合true</returns>
    public bool IsStaminaFull()
    {
        return currentStamina >= maxStamina;
    }

    /// <summary>
    /// 新しい通知があるかどうか
    /// </summary>
    /// <returns>通知がある場合true</returns>
    public bool HasAnyNewNotifications()
    {
        return hasNewItems || hasCompletedQuests || hasNewNotifications;
    }

    /// <summary>
    /// フォーマットされたゴールド文字列を取得
    /// </summary>
    /// <returns>フォーマット済みゴールド文字列</returns>
    public string GetFormattedGold()
    {
        return FormatNumber(gold);
    }

    /// <summary>
    /// フォーマットされたジェム文字列を取得
    /// </summary>
    /// <returns>フォーマット済みジェム文字列</returns>
    public string GetFormattedGems()
    {
        return FormatNumber(gems);
    }

    /// <summary>
    /// フォーマットされた戦闘力文字列を取得
    /// </summary>
    /// <returns>フォーマット済み戦闘力文字列</returns>
    public string GetFormattedCombatPower()
    {
        return FormatNumber(totalCombatPower);
    }

    /// <summary>
    /// 数値をフォーマット（K、M単位）
    /// </summary>
    /// <param name="number">数値</param>
    /// <returns>フォーマット済み文字列</returns>
    private string FormatNumber(long number)
    {
        if (number >= 1000000)
        {
            return $"{number / 1000000.0:F1}M";
        }
        else if (number >= 1000)
        {
            return $"{number / 1000.0:F1}K";
        }
        else
        {
            return number.ToString("N0");
        }
    }

    /// <summary>
    /// スタミナ回復時間の文字列を取得
    /// </summary>
    /// <returns>回復時間文字列</returns>
    public string GetStaminaRecoveryTimeString()
    {
        if (IsStaminaFull())
        {
            return "満タン";
        }

        if (staminaRecoveryRemaining.TotalSeconds <= 0)
        {
            return "回復中";
        }

        return $"{staminaRecoveryRemaining.Minutes:D2}:{staminaRecoveryRemaining.Seconds:D2}";
    }

    /// <summary>
    /// デバッグ用文字列表現
    /// </summary>
    /// <returns>デバッグ情報</returns>
    public override string ToString()
    {
        return $"Player[{playerName}] Lv.{playerLevel} " +
               $"Power:{totalCombatPower} Gold:{gold} Gems:{gems} " +
               $"Stamina:{currentStamina}/{maxStamina}";
    }

    /// <summary>
    /// データの妥当性チェック
    /// </summary>
    /// <returns>データが有効な場合true</returns>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(playerName) &&
               playerLevel > 0 &&
               maxExp > 0 &&
               maxStamina > 0 &&
               currentStamina >= 0 &&
               currentStamina <= maxStamina &&
               gold >= 0 &&
               gems >= 0;
    }

    /// <summary>
    /// データを更新
    /// </summary>
    /// <param name="saveData">最新のセーブデータ</param>
    public void UpdateFromSaveData(UserSaveData saveData)
    {
        if (saveData == null) return;

        var updated = CreateFromSaveData(saveData);

        playerName = updated.playerName;
        playerLevel = updated.playerLevel;
        currentExp = updated.currentExp;
        maxExp = updated.maxExp;
        gold = updated.gold;
        gems = updated.gems;
        currentStamina = updated.currentStamina;
        maxStamina = updated.maxStamina;
        lastLoginDate = updated.lastLoginDate;
        nextStaminaRecovery = updated.nextStaminaRecovery;
        staminaRecoveryRemaining = updated.staminaRecoveryRemaining;
    }
}