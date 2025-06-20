using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BattlePlayer : BattleCharacter
{
    [Header("プレイヤー固有データ")]
    public int playerId;
    public int level;
    public int experience;

    [Header("装備情報")]
    public List<int> equippedItems = new List<int>();

    // プレイヤー専用メソッド
    public void ApplyEquipmentStats(UserEquipment equipment)
    {
        if (equipment == null) return;

        // 装備からステータスを計算してBattleCharacterのプロパティに設定
        var equipmentData = DataManager.Instance?.GetEquipmentData(equipment.equipmentId);
        if (equipmentData == null) return;

        // 基本ステータス適用（floatからintにキャスト）
        attackPower = (int)equipment.GetTotalAttack();
        defensePower = (int)equipment.GetTotalDefense();
        maxHP = (int)equipment.GetTotalHP();
        currentHP = maxHP;
        criticalRate = equipment.GetTotalCriticalRate(); // floatのまま

        // 属性攻撃力適用（floatからintにキャスト）
        fireAttack = (int)equipment.GetTotalFireAttack();
        waterAttack = (int)equipment.GetTotalWaterAttack();
        windAttack = (int)equipment.GetTotalWindAttack();
        earthAttack = (int)equipment.GetTotalEarthAttack();

        // スキル設定（装備強化で解放されるスキル）
        InitializePlayerSkills(equipment);
    }

    private void InitializePlayerSkills(UserEquipment equipment)
    {
        // 装備の強化値に応じてスキルを設定
        if (equipment.enhancementLevel >= 5)
        {
            // 強化値+5でスキル1解放
            skill1 = new BattleSkill
            {
                skillId = "skill_player_attack",
                skillName = "強化攻撃",
                maxCoolTime = 3,
                currentCoolTime = 0
            };
        }

        if (equipment.enhancementLevel >= 10)
        {
            // 強化値+10でスキル2解放
            skill2 = new BattleSkill
            {
                skillId = "skill_player_heal",
                skillName = "回復",
                maxCoolTime = 5,
                currentCoolTime = 0
            };
        }
    }

    // プレイヤーの基本情報を設定
    public void InitializePlayerData()
    {
        characterName = "プレイヤー";
        playerId = 1;
        level = 1;
        position = 1;

        // ユーザーデータから装備情報を取得してステータス適用
        var userEquipment = DataManager.Instance?.GetUserEquipment(0); // 仮で最初の装備を取得
        if (userEquipment != null)
        {
            ApplyEquipmentStats(userEquipment);
        }
        else
        {
            // デフォルトステータス
            maxHP = currentHP = 100;
            attackPower = 25;
            defensePower = 15;
            speed = 10;
            criticalRate = 5.0f;
        }
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        // プレイヤー死亡時の特別処理があれば追加
        Debug.Log("プレイヤーが倒れました...");
    }
}