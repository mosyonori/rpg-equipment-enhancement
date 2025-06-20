using UnityEngine;

public class BattleEnemy : BattleCharacter
{
    [Header("敵固有データ")]
    public int enemyId;
    public int spawnOrder;

    // 敵専用メソッド
    public void InitializeFromMasterData(MonsterMasterData masterData)
    {
        if (masterData == null) return;

        // マスターデータからステータスを初期化
        characterName = masterData.monsterName;
        enemyId = masterData.monsterId;
        maxHP = currentHP = masterData.maxHP;
        attackPower = masterData.attackPower;
        defensePower = masterData.defensePower;
        speed = masterData.speed;
        criticalRate = masterData.criticalRate;

        // 属性攻撃力
        fireAttack = masterData.fireAttack;
        waterAttack = masterData.waterAttack;
        windAttack = masterData.windAttack;
        earthAttack = masterData.earthAttack;

        // スキル初期化
        InitializeSkill(ref skill1, masterData.skill1Id);
        InitializeSkill(ref skill2, masterData.skill2Id);

        Debug.Log($"敵 '{characterName}' を初期化しました。HP:{maxHP} ATK:{attackPower} DEF:{defensePower}");
    }

    private void InitializeSkill(ref BattleSkill skill, string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return;

        // スキルマスターデータからスキル情報を取得
        SkillMasterData skillData = GetSkillMasterData(skillId);
        if (skillData != null)
        {
            skill = new BattleSkill
            {
                skillId = skillData.skillId,
                skillName = skillData.skillName,
                maxCoolTime = skillData.maxCoolTime,
                currentCoolTime = 0
            };

            Debug.Log($"スキル '{skill.skillName}' を設定しました (CT:{skill.maxCoolTime})");
        }
        else
        {
            // フォールバック: デフォルトスキル設定
            skill = new BattleSkill
            {
                skillId = skillId,
                skillName = GetDefaultSkillName(skillId),
                maxCoolTime = GetDefaultCoolTime(skillId),
                currentCoolTime = 0
            };
        }
    }

    private SkillMasterData GetSkillMasterData(string skillId)
    {
        // TODO: DataManagerからスキルマスターデータを取得
        // 現在はマスターデータが直接読み込めないため、スキルIDから推測

        // Assets/GameData/Skills/ フォルダからスキルデータを検索
        var skillAssets = Resources.LoadAll<SkillMasterData>("GameData/Skills");
        foreach (var skillData in skillAssets)
        {
            if (skillData.skillId == skillId)
            {
                return skillData;
            }
        }

        return null;
    }

    private string GetDefaultSkillName(string skillId)
    {
        // スキルIDから名前を推測（フォールバック用）
        return skillId switch
        {
            "skill_fire_ball" => "ファイアボール",
            "skill_quick_strike" => "クイックストライク",
            "skill_guard" => "ガード",
            "skill_wind_claw" => "ウィンドクロー",
            "skill_howl" => "ハウル",
            "skill_earthquake" => "アースクエイク",
            "skill_stone_armor" => "ストーンアーマー",
            "skill_heal_small" => "スモールヒール",
            _ => "不明なスキル"
        };
    }

    private int GetDefaultCoolTime(string skillId)
    {
        // スキルIDからクールタイムを推測（フォールバック用）
        return skillId switch
        {
            "skill_fire_ball" => 3,
            "skill_quick_strike" => 2,
            "skill_guard" => 4,
            "skill_wind_claw" => 4,
            "skill_howl" => 5,
            "skill_earthquake" => 6,
            "skill_stone_armor" => 8,
            "skill_heal_small" => 3,
            _ => 3
        };
    }

    // 敵のレベルに応じたステータス調整
    public void AdjustStatsForLevel(int targetLevel)
    {
        if (targetLevel <= 1) return;

        float levelMultiplier = 1.0f + (targetLevel - 1) * 0.1f; // レベル1上がるごとに10%強化

        maxHP = Mathf.RoundToInt(maxHP * levelMultiplier);
        currentHP = maxHP;
        attackPower = Mathf.RoundToInt(attackPower * levelMultiplier);
        defensePower = Mathf.RoundToInt(defensePower * levelMultiplier);

        // 属性攻撃力も調整
        fireAttack = Mathf.RoundToInt(fireAttack * levelMultiplier);
        waterAttack = Mathf.RoundToInt(waterAttack * levelMultiplier);
        windAttack = Mathf.RoundToInt(windAttack * levelMultiplier);
        earthAttack = Mathf.RoundToInt(earthAttack * levelMultiplier);

        Debug.Log($"敵レベル {targetLevel} に調整: HP:{maxHP} ATK:{attackPower} DEF:{defensePower}");
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        // 敵死亡時の経験値・ドロップ処理があれば追加
        Debug.Log($"{characterName}を倒しました！");
    }
}