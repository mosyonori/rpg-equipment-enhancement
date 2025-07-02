using System;
using UnityEngine;

[System.Serializable]
public class UserEquipmentData
{
    [Header("基本情報")]
    public string userEquipmentId;  // ユーザー固有の装備ID（UUID等）
    public int equipmentMasterId;   // マスターデータのID

    [Header("強化状態")]
    public int currentEnhancedValue;    // 現在の強化値
    public int currentEnhanceStamina;   // 現在の強化耐久値
    public AttributeType currentAttributeType; // 現在の属性（強化により変更される可能性）

    [Header("強化によるステータス上昇")]
    public int enhancedHp;
    public int enhancedOffense;
    public int enhancedDefense;
    public int enhancedSpeed;
    public int enhancedCriticalRate;
    public int enhancedCriticalDamageRate;
    public int enhancedFireOffence;
    public int enhancedWaterOffence;
    public int enhancedWindOffence;
    public int enhancedEarthOffence;

    [Header("装備状態")]
    public bool isEquipped;             // 装備中かどうか
    public string equippedCharacterId;  // 装備しているキャラクターID（未装備時は空文字）

    [Header("装備スキル")]
    public string equippedSkillId;      // 装備中のスキルID（UserSkillDataのID）

    [Header("取得・管理情報")]
    public DateTime acquiredDate;       // 取得日時
    public bool isLocked;               // ロック状態（誤操作防止）
    public bool isFavorite;             // お気に入り登録

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public UserEquipmentData()
    {
        userEquipmentId = Guid.NewGuid().ToString();
        currentAttributeType = AttributeType.None;
        acquiredDate = DateTime.Now;
        isLocked = false;
        isFavorite = false;
        isEquipped = false;
        equippedCharacterId = "";
        equippedSkillId = "";  // スキル未装備
    }

    /// <summary>
    /// マスターデータから新規装備データを作成
    /// </summary>
    public UserEquipmentData(EquipmentMasterData masterData) : this()
    {
        equipmentMasterId = masterData.equipmentId;
        currentEnhancedValue = masterData.baseEnhancedValue;
        currentEnhanceStamina = masterData.baseEnhanceStamina;
        currentAttributeType = masterData.GetAttributeType();

        // 強化による追加ステータスは初期状態では0
        ResetEnhancedStats();
    }

    /// <summary>
    /// 強化による追加ステータスをリセット
    /// </summary>
    public void ResetEnhancedStats()
    {
        enhancedHp = 0;
        enhancedOffense = 0;
        enhancedDefense = 0;
        enhancedSpeed = 0;
        enhancedCriticalRate = 0;
        enhancedCriticalDamageRate = 0;
        enhancedFireOffence = 0;
        enhancedWaterOffence = 0;
        enhancedWindOffence = 0;
        enhancedEarthOffence = 0;
    }

    /// <summary>
    /// 総合ステータスを計算（マスターデータ + 強化分）
    /// </summary>
    public EquipmentTotalStats CalculateTotalStats(EquipmentMasterData masterData)
    {
        if (masterData == null || masterData.equipmentId != equipmentMasterId)
        {
            Debug.LogError($"マスターデータが一致しません。UserEquipmentId: {userEquipmentId}, MasterId: {equipmentMasterId}");
            return new EquipmentTotalStats();
        }

        return new EquipmentTotalStats
        {
            hp = masterData.hp + enhancedHp,
            offense = masterData.offense + enhancedOffense,
            defense = masterData.defense + enhancedDefense,
            speed = masterData.speed + enhancedSpeed,
            criticalRate = masterData.criticalRate + enhancedCriticalRate,
            criticalDamageRate = masterData.criticalDamageRate + enhancedCriticalDamageRate,
            fireOffence = masterData.fireOffence + enhancedFireOffence,
            waterOffence = masterData.waterOffence + enhancedWaterOffence,
            windOffence = masterData.windOffence + enhancedWindOffence,
            earthOffence = masterData.earthOffence + enhancedEarthOffence
        };
    }

    /// <summary>
    /// 現在の属性攻撃力を取得
    /// </summary>
    public int GetCurrentAttributeOffence()
    {
        return currentAttributeType switch
        {
            AttributeType.Fire => enhancedFireOffence,
            AttributeType.Water => enhancedWaterOffence,
            AttributeType.Wind => enhancedWindOffence,
            AttributeType.Earth => enhancedEarthOffence,
            _ => 0
        };
    }

    /// <summary>
    /// 装備を外す
    /// </summary>
    public void UnEquip()
    {
        isEquipped = false;
        equippedCharacterId = "";
    }

    /// <summary>
    /// 装備をキャラクターに装着
    /// </summary>
    public void EquipToCharacter(string characterId)
    {
        isEquipped = true;
        equippedCharacterId = characterId;
    }

    /// <summary>
    /// スキルを装備
    /// </summary>
    public void EquipSkill(string skillId)
    {
        equippedSkillId = skillId;
    }

    /// <summary>
    /// スキルを解除
    /// </summary>
    public void UnequipSkill()
    {
        equippedSkillId = "";
    }

    /// <summary>
    /// 装備中のスキルを取得
    /// </summary>
    public string GetEquippedSkill()
    {
        return equippedSkillId;
    }

    /// <summary>
    /// スキル装備チェック
    /// </summary>
    public bool HasEquippedSkill()
    {
        return !string.IsNullOrEmpty(equippedSkillId);
    }

    /// <summary>
    /// 強化可能かどうかを判定
    /// </summary>
    public bool CanEnhance(EquipmentMasterData masterData)
    {
        if (masterData == null || masterData.equipmentId != equipmentMasterId)
            return false;

        // 強化値が最大値に達している場合は強化不可
        if (currentEnhancedValue >= masterData.maxEnhancedValue)
            return false;

        // 強化耐久値が0の場合は強化不可（耐久減少系のアイテムではNGは除く）
        if (currentEnhanceStamina <= 0)
            return false;

        return true;
    }

    /// <summary>
    /// デバッグ用文字列
    /// </summary>
    public override string ToString()
    {
        string skillInfo = HasEquippedSkill() ? $", Skill:{equippedSkillId}" : ", Skill:None";
        return $"UserEquipment[ID:{userEquipmentId}, MasterID:{equipmentMasterId}, Enhanced:{currentEnhancedValue}, Stamina:{currentEnhanceStamina}, Attribute:{currentAttributeType}, Equipped:{isEquipped}{skillInfo}]";
    }
}

/// <summary>
/// 装備の総合ステータス（マスターデータ + 強化分）
/// </summary>
[System.Serializable]
public struct EquipmentTotalStats
{
    public int hp;
    public int offense;
    public int defense;
    public int speed;
    public int criticalRate;
    public int criticalDamageRate;
    public int fireOffence;
    public int waterOffence;
    public int windOffence;
    public int earthOffence;

    /// <summary>
    /// 指定された属性の攻撃力を取得
    /// </summary>
    public int GetAttributeOffence(AttributeType attributeType)
    {
        return attributeType switch
        {
            AttributeType.Fire => fireOffence,
            AttributeType.Water => waterOffence,
            AttributeType.Wind => windOffence,
            AttributeType.Earth => earthOffence,
            _ => 0
        };
    }
}