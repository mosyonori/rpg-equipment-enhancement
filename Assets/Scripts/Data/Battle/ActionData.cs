using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1回の行動データ
/// 攻撃・スキル使用・アイテム使用等の行動情報を保持
/// </summary>
[System.Serializable]
public class ActionData
{
    [Header("行動者情報")]
    public string actorId;                  // 行動者ID
    public string actorName;                // 行動者名
    public bool isPlayerAction;             // プレイヤーの行動か

    [Header("行動内容")]
    public ActionType actionType;           // 行動種別
    public int skillId;                     // 使用スキルID（通常攻撃は-1）
    public string skillName;                // スキル名

    [Header("対象情報")]
    public List<string> targetIds;          // 対象ID群
    public List<string> targetNames;        // 対象名群

    [Header("結果情報")]
    public List<DamageData> damageResults;  // ダメージ結果群
    public bool actionSucceeded;            // 行動成功フラグ
    public string resultMessage;            // 結果メッセージ

    [Header("メタ情報")]
    public int turnNumber;                  // ターン番号
    public float timestamp;                 // タイムスタンプ

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public ActionData()
    {
        targetIds = new List<string>();
        targetNames = new List<string>();
        damageResults = new List<DamageData>();
        actionSucceeded = false;
        timestamp = Time.time;
    }

    /// <summary>
    /// 通常攻撃のActionDataを作成
    /// </summary>
    public static ActionData CreateNormalAttack(string actorId, string actorName, bool isPlayer, string targetId, string targetName, int turnNumber)
    {
        var action = new ActionData
        {
            actorId = actorId,
            actorName = actorName,
            isPlayerAction = isPlayer,
            actionType = ActionType.Attack,
            skillId = -1,
            skillName = "通常攻撃",
            turnNumber = turnNumber
        };

        action.targetIds.Add(targetId);
        action.targetNames.Add(targetName);

        return action;
    }

    /// <summary>
    /// スキル使用のActionDataを作成
    /// </summary>
    public static ActionData CreateSkillUse(string actorId, string actorName, bool isPlayer, BattleSkillData skill, List<string> targetIds, List<string> targetNames, int turnNumber)
    {
        var action = new ActionData
        {
            actorId = actorId,
            actorName = actorName,
            isPlayerAction = isPlayer,
            actionType = ActionType.Skill,
            skillId = skill.skillId,
            skillName = skill.skillName,
            turnNumber = turnNumber
        };

        action.targetIds.AddRange(targetIds);
        action.targetNames.AddRange(targetNames);

        return action;
    }

    /// <summary>
    /// 通常攻撃かチェック
    /// </summary>
    public bool IsNormalAttack()
    {
        return actionType == ActionType.Attack && skillId <= 0;
    }

    /// <summary>
    /// スキル使用かチェック
    /// </summary>
    public bool IsSkillUse()
    {
        return actionType == ActionType.Skill && skillId > 0;
    }

    /// <summary>
    /// 単体対象の行動かチェック
    /// </summary>
    public bool IsSingleTarget()
    {
        return targetIds.Count == 1;
    }

    /// <summary>
    /// 複数対象の行動かチェック
    /// </summary>
    public bool IsMultiTarget()
    {
        return targetIds.Count > 1;
    }

    /// <summary>
    /// ダメージ結果を追加
    /// </summary>
    public void AddDamageResult(DamageData damageData)
    {
        if (damageData != null)
        {
            damageResults.Add(damageData);
        }
    }

    /// <summary>
    /// 総ダメージ量を取得
    /// </summary>
    public int GetTotalDamage()
    {
        int total = 0;
        foreach (var damage in damageResults)
        {
            total += damage.finalDamage;
        }
        return total;
    }

    /// <summary>
    /// クリティカル発生回数を取得
    /// </summary>
    public int GetCriticalCount()
    {
        int count = 0;
        foreach (var damage in damageResults)
        {
            if (damage.isCritical) count++;
        }
        return count;
    }

    /// <summary>
    /// 撃破した対象数を取得
    /// </summary>
    public int GetDefeatCount()
    {
        int count = 0;
        foreach (var damage in damageResults)
        {
            if (damage.targetDefeated) count++;
        }
        return count;
    }

    /// <summary>
    /// 行動結果の要約を取得
    /// </summary>
    public string GetActionSummary()
    {
        string action = IsNormalAttack() ? "通常攻撃" : skillName;
        string targets = string.Join(", ", targetNames);

        if (actionSucceeded)
        {
            int totalDamage = GetTotalDamage();
            int criticalCount = GetCriticalCount();
            int defeatCount = GetDefeatCount();

            string summary = $"{actorName}の{action} → {targets}";

            if (totalDamage > 0)
            {
                summary += $" ({totalDamage}ダメージ)";
            }

            if (criticalCount > 0)
            {
                summary += " [クリティカル!]";
            }

            if (defeatCount > 0)
            {
                summary += " [撃破!]";
            }

            return summary;
        }
        else
        {
            return $"{actorName}の{action} → 失敗";
        }
    }

    /// <summary>
    /// 戦闘ログ用の詳細メッセージを取得
    /// </summary>
    public string GetDetailedLogMessage()
    {
        string baseMessage = GetActionSummary();

        if (!actionSucceeded)
        {
            return baseMessage + (!string.IsNullOrEmpty(resultMessage) ? $" ({resultMessage})" : "");
        }

        string details = "";

        // ダメージ詳細
        foreach (var damage in damageResults)
        {
            if (damage.finalDamage > 0)
            {
                string damageInfo = $"\n  {damage.targetName}に{damage.finalDamage}ダメージ";

                if (damage.isCritical)
                {
                    damageInfo += " [クリティカル!]";
                }

                if (damage.effectiveness != DamageEffectiveness.Normal)
                {
                    damageInfo += $" [{damage.GetEffectivenessText()}]";
                }

                if (damage.targetDefeated)
                {
                    damageInfo += " [撃破!]";
                }

                details += damageInfo;
            }
        }

        return baseMessage + details;
    }

    /// <summary>
    /// 行動データの文字列表現
    /// </summary>
    public override string ToString()
    {
        return $"Action[Turn{turnNumber}] {GetActionSummary()}";
    }
}

/// <summary>
/// 行動種別
/// </summary>
public enum ActionType
{
    Attack,         // 通常攻撃
    Skill,          // スキル使用
    Item,           // アイテム使用（将来拡張用）
    Wait,           // 待機
    Escape          // 逃走（将来拡張用）
}