using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CharacterUIElement : MonoBehaviour
{
    [Header("基本UI要素")]
    public Image characterImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;

    [Header("HPバー")]
    public Image hpBarBackground;
    public Image hpBarFill;
    public Slider hpSlider;

    [Header("アクション表示")]
    public Image actionIndicator;
    public GameObject activeFrame;

    [Header("状態異常表示")]
    public Transform statusEffectParent;
    public GameObject statusEffectPrefab;

    [Header("色設定")]
    public Color normalColor = Color.white;
    public Color playerColor = Color.cyan;
    public Color enemyColor = Color.red;
    public Color deadColor = Color.gray;

    [Header("HPバー色設定")]
    public Color hpFullColor = Color.green;
    public Color hpMidColor = Color.yellow;
    public Color hpLowColor = Color.red;

    // 管理用
    private BattleCharacter targetCharacter;
    private List<GameObject> statusEffectIcons = new List<GameObject>();
    private bool isInitialized = false;

    /// <summary>
    /// キャラクターUIを初期化
    /// </summary>
    public void Initialize(BattleCharacter character)
    {
        targetCharacter = character;

        if (targetCharacter == null)
        {
            Debug.LogError("ターゲットキャラクターがnullです");
            return;
        }

        // 基本情報設定
        SetupBasicInfo();

        // 初期表示更新
        UpdateDisplay();

        // アクティブ表示をオフ
        SetActiveIndicator(false);

        isInitialized = true;

        Debug.Log($"CharacterUIElement初期化完了: {targetCharacter.characterName}");
    }

    /// <summary>
    /// 基本情報をセットアップ
    /// </summary>
    private void SetupBasicInfo()
    {
        // 名前設定
        if (nameText != null)
        {
            nameText.text = targetCharacter.characterName;

            // プレイヤーと敵で色を変更
            if (targetCharacter is BattlePlayer)
            {
                nameText.color = playerColor;
            }
            else if (targetCharacter is BattleEnemy)
            {
                nameText.color = enemyColor;
            }
        }

        // キャラクター画像設定（今後実装）
        if (characterImage != null)
        {
            // TODO: キャラクターのアイコン画像を設定
            characterImage.color = targetCharacter is BattlePlayer ? playerColor : enemyColor;
        }

        // HPスライダー設定
        if (hpSlider != null)
        {
            hpSlider.maxValue = targetCharacter.maxHP;
            hpSlider.minValue = 0;
        }
    }

    /// <summary>
    /// 表示を更新
    /// </summary>
    public void UpdateDisplay()
    {
        if (!isInitialized || targetCharacter == null) return;

        // HP表示更新
        UpdateHPDisplay();

        // 生存状態による表示変更
        UpdateAliveState();

        // 状態異常表示更新
        UpdateStatusEffects();
    }

    /// <summary>
    /// HP表示を更新
    /// </summary>
    private void UpdateHPDisplay()
    {
        // HPテキスト更新
        if (hpText != null)
        {
            hpText.text = $"{targetCharacter.currentHP} / {targetCharacter.maxHP}";
        }

        // HPスライダー更新
        if (hpSlider != null)
        {
            hpSlider.value = targetCharacter.currentHP;
        }

        // HPバー更新
        if (hpBarFill != null)
        {
            float hpRatio = (float)targetCharacter.currentHP / targetCharacter.maxHP;

            // HPバーの色を割合に応じて変更
            Color barColor = GetHPBarColor(hpRatio);
            hpBarFill.color = barColor;

            // HPバーの幅を更新（スライダーを使わない場合）
            if (hpSlider == null)
            {
                hpBarFill.fillAmount = hpRatio;
            }
        }
    }

    /// <summary>
    /// HP割合に応じた色を取得
    /// </summary>
    private Color GetHPBarColor(float hpRatio)
    {
        if (hpRatio > 0.6f)
            return hpFullColor;
        else if (hpRatio > 0.3f)
            return hpMidColor;
        else
            return hpLowColor;
    }

    /// <summary>
    /// 生存状態による表示更新
    /// </summary>
    private void UpdateAliveState()
    {
        bool isAlive = targetCharacter.isAlive;

        // 死亡時は全体を灰色に
        if (characterImage != null)
        {
            characterImage.color = isAlive ?
                (targetCharacter is BattlePlayer ? playerColor : enemyColor) :
                deadColor;
        }

        if (nameText != null)
        {
            nameText.color = isAlive ?
                (targetCharacter is BattlePlayer ? playerColor : enemyColor) :
                deadColor;
        }

        // HPバーも灰色に
        if (!isAlive && hpBarFill != null)
        {
            hpBarFill.color = deadColor;
        }
    }

    /// <summary>
    /// 状態異常表示を更新
    /// </summary>
    private void UpdateStatusEffects()
    {
        if (statusEffectParent == null) return;

        // 既存のアイコンをクリア
        ClearStatusEffectIcons();

        // アクティブな状態異常を表示
        foreach (var effect in targetCharacter.activeEffects)
        {
            CreateStatusEffectIcon(effect);
        }
    }

    /// <summary>
    /// 状態異常アイコンを作成
    /// </summary>
    private void CreateStatusEffectIcon(StatusEffect effect)
    {
        if (statusEffectPrefab == null) return;

        GameObject iconObj = Instantiate(statusEffectPrefab, statusEffectParent);
        statusEffectIcons.Add(iconObj);

        // アイコンの色を効果タイプに応じて設定
        Image iconImage = iconObj.GetComponent<Image>();
        if (iconImage != null)
        {
            iconImage.color = effect.effectType == StatusEffectType.Buff ?
                Color.green : Color.red;
        }

        // ターン数表示
        TextMeshProUGUI turnText = iconObj.GetComponentInChildren<TextMeshProUGUI>();
        if (turnText != null)
        {
            turnText.text = effect.remainingTurns.ToString();
        }

        // ツールチップ情報（今後実装）
        // TODO: マウスオーバー時に状態異常の詳細を表示
    }

    /// <summary>
    /// 状態異常アイコンをクリア
    /// </summary>
    private void ClearStatusEffectIcons()
    {
        foreach (var icon in statusEffectIcons)
        {
            if (icon != null)
                Destroy(icon);
        }
        statusEffectIcons.Clear();
    }

    /// <summary>
    /// アクティブインジケーターを設定
    /// </summary>
    public void SetActiveIndicator(bool isActive)
    {
        // アクションインジケーター
        if (actionIndicator != null)
        {
            actionIndicator.gameObject.SetActive(isActive);
        }

        // アクティブフレーム
        if (activeFrame != null)
        {
            activeFrame.SetActive(isActive);
        }

        // アクティブ時のエフェクト（今後実装）
        if (isActive)
        {
            // TODO: 光るエフェクトや拡大縮小アニメーション
        }
    }

    /// <summary>
    /// ダメージエフェクト表示（今後実装）
    /// </summary>
    public void ShowDamageEffect(int damage)
    {
        // TODO: ダメージ数値の表示アニメーション
        // TODO: キャラクターの点滅エフェクト
        Debug.Log($"{targetCharacter.characterName} にダメージエフェクト表示: {damage}");
    }

    /// <summary>
    /// 回復エフェクト表示（今後実装）
    /// </summary>
    public void ShowHealEffect(int healAmount)
    {
        // TODO: 回復数値の表示アニメーション
        // TODO: 緑色の光エフェクト
        Debug.Log($"{targetCharacter.characterName} に回復エフェクト表示: {healAmount}");
    }

    /// <summary>
    /// スキル使用エフェクト表示（今後実装）
    /// </summary>
    public void ShowSkillEffect(string skillName)
    {
        // TODO: スキル名の表示
        // TODO: スキル固有のエフェクト
        Debug.Log($"{targetCharacter.characterName} がスキル使用: {skillName}");
    }

    private void OnDestroy()
    {
        // クリーンアップ
        ClearStatusEffectIcons();
    }

    /// <summary>
    /// デバッグ情報表示
    /// </summary>
    public void ShowDebugInfo()
    {
        if (targetCharacter != null)
        {
            Debug.Log($"CharacterUI: {targetCharacter.characterName} HP:{targetCharacter.currentHP}/{targetCharacter.maxHP} Alive:{targetCharacter.isAlive}");
        }
    }
}