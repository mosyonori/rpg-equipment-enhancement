using UnityEngine;

/// <summary>
/// 状態効果データ（バフ・デバフ・状態異常）
/// 戦闘中のキャラクターに付与される一時的な効果を管理
/// </summary>
[System.Serializable]
public class StatusEffectData
{
    [Header("効果基本情報")]
    public int effectId;                    // 効果ID（SkillEffectMasterDataのID）
    public string effectName;               // 効果名
    public StatusEffectType effectType;     // 効果種別
    public bool isPositive;                 // ポジティブ効果か

    [Header("効果内容")]
    public bool preventAction;              // 行動阻害フラグ
    public float offenseMultiplier;         // 攻撃力倍率
    public float defenseMultiplier;         // 防御力倍率
    public int turnStartDamagePercent;      // ターン開始時ダメージ割合
    public int turnStartHealPercent;        // ターン開始時回復割合

    [Header("属性効果")]
    public float fireOffenseMultiplier;     // 火属性攻撃力倍率
    public float waterOffenseMultiplier;    // 水属性攻撃力倍率
    public float windOffenseMultiplier;     // 風属性攻撃力倍率
    public float earthOffenseMultiplier;    // 土属性攻撃力倍率

    [Header("持続情報")]
    public int remainingTurns;              // 残りターン数
    public int maxTurns;                    // 最大ターン数
    public bool isStackable;                // 重複可能フラグ

    [Header("付与情報")]
    public string casterId;                 // 付与者ID
    public string casterName;               // 付与者名
    public float appliedTimestamp;          // 付与時刻

    [Header("表示情報")]
    public string iconPath;                 // アイコンパス
    public string colorCode;                // 色コード
    public int displayPriority;             // 表示優先度

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public StatusEffectData()
    {
        offenseMultiplier = 1.0f;
        defenseMultiplier = 1.0f;
        fireOffenseMultiplier = 1.0f;
        waterOffenseMultiplier = 1.0f;
        windOffenseMultiplier = 1.0f;
        earthOffenseMultiplier = 1.0f;
        appliedTimestamp = Time.time;
        isPositive = true;
    }

    /// <summary>
    /// 既存SkillEffectMasterDataからStatusEffectDataを作成
    /// </summary>
    public static StatusEffectData CreateFromSkillEffectMaster(SkillEffectMasterData masterData, string casterId, string casterName, int duration)
    {
        var statusEffect = new StatusEffectData
        {
            effectId = masterData.statusEffectId,
            effectName = masterData.statusEffectName,
            effectType = masterData.statusEffectType,
            isPositive = IsPositiveEffect(masterData.statusEffectType),

            // 効果内容
            preventAction = masterData.preventAction,
            offenseMultiplier = masterData.offenseMultiplier,
            defenseMultiplier = masterData.defenseMultiplier,
            turnStartDamagePercent = masterData.turnStartDamagePercent,
            turnStartHealPercent = masterData.turnStartHealPercent,

            // 属性効果
            fireOffenseMultiplier = masterData.fireOffenseMultiplier,
            waterOffenseMultiplier = masterData.waterOffenseMultiplier,
            windOffenseMultiplier = masterData.windOffenseMultiplier,
            earthOffenseMultiplier = masterData.earthOffenseMultiplier,

            // 持続情報
            remainingTurns = duration,
            maxTurns = duration,
            isStackable = masterData.stackable,

            // 付与情報
            casterId = casterId,
            casterName = casterName,

            // 表示情報
            iconPath = masterData.skillEffectIconId,
            colorCode = masterData.colorCode,
            displayPriority = masterData.skillEffectPriority
        };

        return statusEffect;
    }

    /// <summary>
    /// 効果タイプからポジティブ効果かを判定
    /// </summary>
    private static bool IsPositiveEffect(StatusEffectType effectType)
    {
        return effectType switch
        {
            StatusEffectType.AttackUp => true,
            StatusEffectType.DefenseUp => true,
            StatusEffectType.Regen => true,
            StatusEffectType.AttackDown => false,
            StatusEffectType.DefenseDown => false,
            StatusEffectType.Stun => false,
            StatusEffectType.Poison => false,
            _ => true
        };
    }

    /// <summary>
    /// 効果が有効かチェック
    /// </summary>
    public bool IsActive()
    {
        return remainingTurns > 0;
    }

    /// <summary>
    /// ターン経過処理
    /// </summary>
    public void ProcessTurn()
    {
        remainingTurns = Mathf.Max(0, remainingTurns - 1);
    }

    /// <summary>
    /// ターン開始時効果があるかチェック
    /// </summary>
    public bool HasTurnStartEffect()
    {
        return turnStartDamagePercent > 0 || turnStartHealPercent > 0;
    }

    /// <summary>
    /// ターン開始時のダメージ/回復量を計算
    /// </summary>
    public int CalculateTurnStartHpChange(int maxHp)
    {
        int damage = (maxHp * turnStartDamagePercent) / 100;
        int heal = (maxHp * turnStartHealPercent) / 100;
        return heal - damage; // 正の値は回復、負の値はダメージ
    }

    /// <summary>
    /// ステータスに効果を適用
    /// </summary>
    public void ApplyToStats(ref int offense, ref int defense, ref int fireOffense, ref int waterOffense, ref int windOffense, ref int earthOffense)
    {
        offense = Mathf.RoundToInt(offense * offenseMultiplier);
        defense = Mathf.RoundToInt(defense * defenseMultiplier);
        fireOffense = Mathf.RoundToInt(fireOffense * fireOffenseMultiplier);
        waterOffense = Mathf.RoundToInt(waterOffense * waterOffenseMultiplier);
        windOffense = Mathf.RoundToInt(windOffense * windOffenseMultiplier);
        earthOffense = Mathf.RoundToInt(earthOffense * earthOffenseMultiplier);
    }

    /// <summary>
    /// 効果の残り時間割合を取得
    /// </summary>
    public float GetRemainingRatio()
    {
        return maxTurns > 0 ? (float)remainingTurns / maxTurns : 0f;
    }

    /// <summary>
    /// 効果の強度を取得（UI表示用）
    /// </summary>
    public float GetEffectStrength()
    {
        // 攻撃力や防御力の倍率から効果の強度を判定
        float strengthFromOffense = Mathf.Abs(offenseMultiplier - 1.0f);
        float strengthFromDefense = Mathf.Abs(defenseMultiplier - 1.0f);
        return Mathf.Max(strengthFromOffense, strengthFromDefense);
    }

    /// <summary>
    /// 効果アイコンの色を取得
    /// </summary>
    public Color GetEffectColor()
    {
        if (!string.IsNullOrEmpty(colorCode))
        {
            if (ColorUtility.TryParseHtmlString(colorCode, out Color color))
            {
                return color;
            }
        }

        // デフォルト色
        return isPositive ? Color.green : Color.red;
    }

    /// <summary>
    /// 同じ効果と重複可能かチェック
    /// </summary>
    public bool CanStackWith(StatusEffectData other)
    {
        return isStackable &&
               other.isStackable &&
               effectId == other.effectId;
    }

    /// <summary>
    /// 効果を重複させる
    /// </summary>
    public void StackWith(StatusEffectData other)
    {
        if (!CanStackWith(other)) return;

        // ターン数は長い方を採用
        remainingTurns = Mathf.Max(remainingTurns, other.remainingTurns);
        maxTurns = Mathf.Max(maxTurns, other.maxTurns);

        // 効果値は重複（上限チェックが必要な場合は追加）
        if (other.offenseMultiplier != 1.0f)
        {
            offenseMultiplier *= other.offenseMultiplier;
        }
        if (other.defenseMultiplier != 1.0f)
        {
            defenseMultiplier *= other.defenseMultiplier;
        }
    }

    /// <summary>
    /// 効果説明文を取得
    /// </summary>
    public string GetDescription()
    {
        string description = effectName;

        if (remainingTurns > 0)
        {
            description += $" (残り{remainingTurns}ターン)";
        }

        // 効果の詳細を追加
        if (offenseMultiplier != 1.0f)
        {
            float percent = (offenseMultiplier - 1.0f) * 100f;
            description += $"\n攻撃力{percent:+0}%";
        }

        if (defenseMultiplier != 1.0f)
        {
            float percent = (defenseMultiplier - 1.0f) * 100f;
            description += $"\n防御力{percent:+0}%";
        }

        if (preventAction)
        {
            description += "\n行動不能";
        }

        if (HasTurnStartEffect())
        {
            if (turnStartDamagePercent > 0)
            {
                description += $"\nターン開始時{turnStartDamagePercent}%ダメージ";
            }
            if (turnStartHealPercent > 0)
            {
                description += $"\nターン開始時{turnStartHealPercent}%回復";
            }
        }

        return description;
    }

    /// <summary>
    /// 状態効果データの文字列表現
    /// </summary>
    public override string ToString()
    {
        string type = isPositive ? "バフ" : "デバフ";
        return $"StatusEffect[{effectId}] {effectName} ({type}, 残り{remainingTurns}ターン)";
    }
}