using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 装備強化結果表示UIコントロールクラス - IDベース修正版
/// 
/// 【責任】
/// - 強化結果の演出表示
/// - 成功/失敗のビジュアル演出
/// - 結果詳細情報の表示
/// - ユーザーに対する分かりやすい結果通知
/// 
/// 【重要機能】
/// - 成功/失敗アニメーション
/// - ステータス変化の表示
/// - 結果メッセージ表示
/// - 結果画面の表示制御
/// </summary>
public class Enhance_ResultUIController : MonoBehaviour
{
    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private CanvasGroup resultCanvasGroup;

    [Header("Result Display")]
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [SerializeField] private TextMeshProUGUI resultMessageText;
    [SerializeField] private Image resultBackgroundImage;
    [SerializeField] private Image resultIconImage;

    [Header("Equipment Display")]
    [SerializeField] private Image equipmentIconImage;
    [SerializeField] private TextMeshProUGUI equipmentNameText;
    [SerializeField] private TextMeshProUGUI equipmentLevelText;

    [Header("Status Change Display")]
    [SerializeField] private Transform statusChangeContainer;
    [SerializeField] private GameObject statusChangeItemPrefab;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float displayDuration = 3.0f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private AnimationCurve fadeAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Success Settings")]
    [SerializeField] private Color successBackgroundColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
    [SerializeField] private Color successTextColor = Color.white;
    [SerializeField] private Sprite successIconSprite;
    [SerializeField] private AudioClip successSoundEffect;

    [Header("Failure Settings")]
    [SerializeField] private Color failureBackgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color failureTextColor = Color.white;
    [SerializeField] private Sprite failureIconSprite;
    [SerializeField] private AudioClip failureSoundEffect;

    [Header("Status Change Colors")]
    [SerializeField] private Color increaseColor = Color.green;
    [SerializeField] private Color decreaseColor = Color.red;
    [SerializeField] private Color noChangeColor = Color.gray;

    // Service層
    private EnhanceDataService dataService = new EnhanceDataService();

    // 内部状態
    private bool isDisplaying = false;

    #region Public Methods

    /// <summary>
    /// 強化結果表示（コルーチン）
    /// EnhanceUIControllerから呼び出される
    /// </summary>
    public IEnumerator ShowResult(EnhanceResultData resultData)
    {
        if (isDisplaying)
        {
            Debug.LogWarning("[Enhance_ResultUIController] 既に結果表示中です");
            yield break;
        }

        isDisplaying = true;

        // 結果データの検証
        if (resultData == null)
        {
            Debug.LogError("[Enhance_ResultUIController] 結果データがnullです");
            isDisplaying = false;
            yield break;
        }

        // 結果表示の準備
        PrepareResultDisplay(resultData);

        // フェードイン演出
        yield return StartCoroutine(FadeIn());

        // 結果表示保持
        yield return new WaitForSeconds(displayDuration);

        // フェードアウト演出
        yield return StartCoroutine(FadeOut());

        isDisplaying = false;
    }

    /// <summary>
    /// 結果表示を強制終了
    /// </summary>
    public void ForceClose()
    {
        if (isDisplaying)
        {
            StopAllCoroutines();
            HideResultPanel();
            isDisplaying = false;
        }
    }

    #endregion

    #region Result Display Preparation

    /// <summary>
    /// 結果表示の準備
    /// </summary>
    private void PrepareResultDisplay(EnhanceResultData resultData)
    {
        // 基本結果情報設定
        SetupBasicResultInfo(resultData);

        // 装備情報設定
        SetupEquipmentInfo(resultData);

        // ステータス変化表示設定
        SetupStatusChangeDisplay(resultData);

        // 成功/失敗に応じた見た目設定
        SetupResultAppearance(resultData.IsSuccess);

        // サウンド再生
        PlayResultSound(resultData.IsSuccess);
    }

    /// <summary>
    /// 基本結果情報設定
    /// </summary>
    private void SetupBasicResultInfo(EnhanceResultData resultData)
    {
        if (resultTitleText != null)
        {
            resultTitleText.text = resultData.IsSuccess ? "強化成功！" : "強化失敗...";
        }

        if (resultMessageText != null)
        {
            resultMessageText.text = resultData.ResultMessage;
        }
    }

    /// <summary>
    /// 装備情報設定 - IDベース修正版
    /// </summary>
    private void SetupEquipmentInfo(EnhanceResultData resultData)
    {
        if (resultData.EnhancedEquipment != null)
        {
            EquipmentMasterData masterData = dataService.GetEquipmentMaster(resultData.EnhancedEquipment.equipment_id);

            if (masterData != null)
            {
                // 装備名
                if (equipmentNameText != null)
                {
                    equipmentNameText.text = masterData.equipment_name;
                }

                // 強化レベル
                if (equipmentLevelText != null)
                {
                    equipmentLevelText.text = $"+{resultData.EnhancedEquipment.current_enhanced_value}";
                }

                // ✅ 装備アイコン - IDベース読み込み
                if (equipmentIconImage != null)
                {
                    equipmentIconImage.sprite = LoadEquipmentIcon(masterData.equipment_id);
                }
            }
        }
    }

    /// <summary>
    /// ステータス変化表示設定
    /// </summary>
    private void SetupStatusChangeDisplay(EnhanceResultData resultData)
    {
        // 既存の表示をクリア
        ClearStatusChangeDisplay();

        if (!resultData.IsSuccess || resultData.EnhancedEquipment == null)
        {
            // 失敗時はステータス変化なし
            return;
        }

        // 成功時のステータス変化を表示
        DisplayStatusChanges(resultData);
    }

    /// <summary>
    /// ステータス変化の詳細表示
    /// </summary>
    private void DisplayStatusChanges(EnhanceResultData resultData)
    {
        // 強化値増加は必ず表示
        CreateStatusChangeItem("強化値", "+1", increaseColor);

        // 使用したアイテム情報表示（装備種類別プロパティを使用）
        if (resultData.ConsumedEnhanceItemId > 0 && resultData.EnhancedEquipment != null)
        {
            EnhanceItemMasterData enhanceItem = dataService.GetEnhanceItemMaster(resultData.ConsumedEnhanceItemId);
            EquipmentMasterData equipmentMaster = dataService.GetEquipmentMaster(resultData.EnhancedEquipment.equipment_id);

            if (enhanceItem != null && equipmentMaster != null)
            {
                // 装備種類に応じたステータス変化表示
                switch (equipmentMaster.equipment_type)
                {
                    case EquipmentType.Weapon:
                        DisplayWeaponStatusChanges(enhanceItem);
                        break;
                    case EquipmentType.Armor:
                        DisplayArmorStatusChanges(enhanceItem);
                        break;
                    case EquipmentType.Accessory:
                        DisplayAccessoryStatusChanges(enhanceItem);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// 武器ステータス変化表示
    /// </summary>
    private void DisplayWeaponStatusChanges(EnhanceItemMasterData enhanceItem)
    {
        if (enhanceItem.weapon_hp > 0)
            CreateStatusChangeItem("HP", $"+{enhanceItem.weapon_hp}", increaseColor);
        if (enhanceItem.weapon_offense > 0)
            CreateStatusChangeItem("攻撃力", $"+{enhanceItem.weapon_offense}", increaseColor);
        if (enhanceItem.weapon_defense > 0)
            CreateStatusChangeItem("防御力", $"+{enhanceItem.weapon_defense}", increaseColor);
        if (enhanceItem.weapon_speed > 0)
            CreateStatusChangeItem("速度", $"+{enhanceItem.weapon_speed}", increaseColor);
        if (enhanceItem.weapon_critical_rate > 0)
            CreateStatusChangeItem("クリティカル率", $"+{enhanceItem.weapon_critical_rate}%", increaseColor);
        if (enhanceItem.weapon_critical_damage_rate > 0)
            CreateStatusChangeItem("クリティカルダメージ", $"+{enhanceItem.weapon_critical_damage_rate}%", increaseColor);

        // 武器属性攻撃
        if (enhanceItem.weapon_fire_offence > 0)
            CreateStatusChangeItem("火属性攻撃", $"+{enhanceItem.weapon_fire_offence}", increaseColor);
        if (enhanceItem.weapon_water_offence > 0)
            CreateStatusChangeItem("水属性攻撃", $"+{enhanceItem.weapon_water_offence}", increaseColor);
        if (enhanceItem.weapon_wind_offence > 0)
            CreateStatusChangeItem("風属性攻撃", $"+{enhanceItem.weapon_wind_offence}", increaseColor);
        if (enhanceItem.weapon_earth_offence > 0)
            CreateStatusChangeItem("土属性攻撃", $"+{enhanceItem.weapon_earth_offence}", increaseColor);
    }

    /// <summary>
    /// 防具ステータス変化表示
    /// </summary>
    private void DisplayArmorStatusChanges(EnhanceItemMasterData enhanceItem)
    {
        if (enhanceItem.armor_hp > 0)
            CreateStatusChangeItem("HP", $"+{enhanceItem.armor_hp}", increaseColor);
        if (enhanceItem.armor_offense > 0)
            CreateStatusChangeItem("攻撃力", $"+{enhanceItem.armor_offense}", increaseColor);
        if (enhanceItem.armor_defense > 0)
            CreateStatusChangeItem("防御力", $"+{enhanceItem.armor_defense}", increaseColor);
        if (enhanceItem.armor_speed > 0)
            CreateStatusChangeItem("速度", $"+{enhanceItem.armor_speed}", increaseColor);
        if (enhanceItem.armor_critical_rate > 0)
            CreateStatusChangeItem("クリティカル率", $"+{enhanceItem.armor_critical_rate}%", increaseColor);
        if (enhanceItem.armor_critical_damage_rate > 0)
            CreateStatusChangeItem("クリティカルダメージ", $"+{enhanceItem.armor_critical_damage_rate}%", increaseColor);

        // 防具属性攻撃
        if (enhanceItem.armor_fire_offence > 0)
            CreateStatusChangeItem("火属性攻撃", $"+{enhanceItem.armor_fire_offence}", increaseColor);
        if (enhanceItem.armor_water_offence > 0)
            CreateStatusChangeItem("水属性攻撃", $"+{enhanceItem.armor_water_offence}", increaseColor);
        if (enhanceItem.armor_wind_offence > 0)
            CreateStatusChangeItem("風属性攻撃", $"+{enhanceItem.armor_wind_offence}", increaseColor);
        if (enhanceItem.armor_earth_offence > 0)
            CreateStatusChangeItem("土属性攻撃", $"+{enhanceItem.armor_earth_offence}", increaseColor);
    }

    /// <summary>
    /// アクセサリーステータス変化表示
    /// </summary>
    private void DisplayAccessoryStatusChanges(EnhanceItemMasterData enhanceItem)
    {
        if (enhanceItem.accessory_hp > 0)
            CreateStatusChangeItem("HP", $"+{enhanceItem.accessory_hp}", increaseColor);
        if (enhanceItem.accessory_offense > 0)
            CreateStatusChangeItem("攻撃力", $"+{enhanceItem.accessory_offense}", increaseColor);
        if (enhanceItem.accessory_defense > 0)
            CreateStatusChangeItem("防御力", $"+{enhanceItem.accessory_defense}", increaseColor);
        if (enhanceItem.accessory_speed > 0)
            CreateStatusChangeItem("速度", $"+{enhanceItem.accessory_speed}", increaseColor);
        if (enhanceItem.accessory_critical_rate > 0)
            CreateStatusChangeItem("クリティカル率", $"+{enhanceItem.accessory_critical_rate}%", increaseColor);
        if (enhanceItem.accessory_critical_damage_rate > 0)
            CreateStatusChangeItem("クリティカルダメージ", $"+{enhanceItem.accessory_critical_damage_rate}%", increaseColor);

        // アクセサリー属性攻撃
        if (enhanceItem.accessory_fire_offence > 0)
            CreateStatusChangeItem("火属性攻撃", $"+{enhanceItem.accessory_fire_offence}", increaseColor);
        if (enhanceItem.accessory_water_offence > 0)
            CreateStatusChangeItem("水属性攻撃", $"+{enhanceItem.accessory_water_offence}", increaseColor);
        if (enhanceItem.accessory_wind_offence > 0)
            CreateStatusChangeItem("風属性攻撃", $"+{enhanceItem.accessory_wind_offence}", increaseColor);
        if (enhanceItem.accessory_earth_offence > 0)
            CreateStatusChangeItem("土属性攻撃", $"+{enhanceItem.accessory_earth_offence}", increaseColor);
    }

    /// <summary>
    /// ステータス変化項目作成
    /// </summary>
    private void CreateStatusChangeItem(string statusName, string changeValue, Color textColor)
    {
        if (statusChangeItemPrefab == null || statusChangeContainer == null) return;

        GameObject itemObj = Instantiate(statusChangeItemPrefab, statusChangeContainer);
        StatusChangeItem changeItem = itemObj.GetComponent<StatusChangeItem>();

        if (changeItem != null)
        {
            changeItem.Setup(statusName, changeValue, textColor);
        }
        else
        {
            // フォールバック：直接テキストコンポーネントを探す
            Text[] texts = itemObj.GetComponentsInChildren<Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = statusName;
                texts[1].text = changeValue;
                texts[1].color = textColor;
            }
        }
    }

    /// <summary>
    /// 結果表示の見た目設定
    /// </summary>
    private void SetupResultAppearance(bool isSuccess)
    {
        Color backgroundColor = isSuccess ? successBackgroundColor : failureBackgroundColor;
        Color textColor = isSuccess ? successTextColor : failureTextColor;
        Sprite iconSprite = isSuccess ? successIconSprite : failureIconSprite;

        // 背景色設定
        if (resultBackgroundImage != null)
        {
            resultBackgroundImage.color = backgroundColor;
        }

        // アイコン設定
        if (resultIconImage != null)
        {
            resultIconImage.sprite = iconSprite;
        }

        // テキスト色設定
        if (resultTitleText != null)
        {
            resultTitleText.color = textColor;
        }

        if (resultMessageText != null)
        {
            resultMessageText.color = textColor;
        }
    }

    #endregion

    #region Animation

    /// <summary>
    /// フェードイン演出
    /// </summary>
    private IEnumerator FadeIn()
    {
        ShowResultPanel();

        if (resultCanvasGroup != null)
        {
            float elapsedTime = 0f;
            resultCanvasGroup.alpha = 0f;

            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / fadeInDuration;
                float curveValue = fadeAnimationCurve.Evaluate(progress);
                resultCanvasGroup.alpha = curveValue;
                yield return null;
            }

            resultCanvasGroup.alpha = 1f;
        }
        else
        {
            // CanvasGroupがない場合は単純に表示
            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// フェードアウト演出
    /// </summary>
    private IEnumerator FadeOut()
    {
        if (resultCanvasGroup != null)
        {
            float elapsedTime = 0f;
            resultCanvasGroup.alpha = 1f;

            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / fadeOutDuration;
                float curveValue = fadeAnimationCurve.Evaluate(1f - progress);
                resultCanvasGroup.alpha = curveValue;
                yield return null;
            }

            resultCanvasGroup.alpha = 0f;
        }

        HideResultPanel();
    }

    #endregion

    #region UI Control

    /// <summary>
    /// 結果パネル表示
    /// </summary>
    private void ShowResultPanel()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 結果パネル非表示
    /// </summary>
    private void HideResultPanel()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    /// <summary>
    /// ステータス変化表示クリア
    /// </summary>
    private void ClearStatusChangeDisplay()
    {
        if (statusChangeContainer == null) return;

        foreach (Transform child in statusChangeContainer)
        {
            Destroy(child.gameObject);
        }
    }

    #endregion

    #region Audio

    /// <summary>
    /// 結果音再生
    /// </summary>
    private void PlayResultSound(bool isSuccess)
    {
        AudioClip soundClip = isSuccess ? successSoundEffect : failureSoundEffect;

        if (soundClip != null)
        {
            // AudioSourceがアタッチされている場合
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(soundClip);
            }
            else
            {
                // 一時的なAudioSourceを作成して再生
                GameObject audioObj = new GameObject("TempAudio");
                AudioSource tempAudioSource = audioObj.AddComponent<AudioSource>();
                tempAudioSource.clip = soundClip;
                tempAudioSource.Play();

                // 音声再生後にオブジェクトを削除
                Destroy(audioObj, soundClip.length + 0.1f);
            }
        }
    }

    #endregion

    #region Utility

    /// <summary>
    /// 装備アイコン読み込み - IDベース修正版
    /// ✅ Phase 1パターン適用：CSVパス依存からIDベースに変更
    /// </summary>
    private Sprite LoadEquipmentIcon(int equipmentId)
    {
        try
        {
            return Resources.Load<Sprite>($"Icons/Equipments/equipment_{equipmentId:D3}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Enhance_ResultUIController] アイコン読み込み失敗: equipment_{equipmentId:D3}, {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 現在表示中かどうか
    /// </summary>
    public bool IsDisplaying()
    {
        return isDisplaying;
    }

    #endregion

    #region Debug

    /// <summary>
    /// デバッグ用：成功結果テスト
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestSuccessResult()
    {
        EnhanceResultData testData = new EnhanceResultData
        {
            IsSuccess = true,
            ResultMessage = "装備が強力になりました！",
            ConsumedEnhanceItemId = 1
        };

        StartCoroutine(ShowResult(testData));
    }

    /// <summary>
    /// デバッグ用：失敗結果テスト
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestFailureResult()
    {
        EnhanceResultData testData = new EnhanceResultData
        {
            IsSuccess = false,
            ResultMessage = "強化に失敗しました...",
            ConsumedEnhanceItemId = 1
        };

        StartCoroutine(ShowResult(testData));
    }

    #endregion
}

#region Supporting Classes

/// <summary>
/// ステータス変化項目UI（プレハブ用）
/// </summary>
public class StatusChangeItem : MonoBehaviour
{
    [Header("UI Elements")]
    public Text statusNameText;
    public Text changeValueText;
    public Image changeIcon;

    [Header("Change Icons")]
    public Sprite increaseIcon;
    public Sprite decreaseIcon;

    public void Setup(string statusName, string changeValue, Color textColor)
    {
        if (statusNameText != null)
        {
            statusNameText.text = statusName;
        }

        if (changeValueText != null)
        {
            changeValueText.text = changeValue;
            changeValueText.color = textColor;
        }

        // 変化アイコンの設定
        if (changeIcon != null)
        {
            if (changeValue.StartsWith("+"))
            {
                changeIcon.sprite = increaseIcon;
                changeIcon.color = Color.green;
            }
            else if (changeValue.StartsWith("-"))
            {
                changeIcon.sprite = decreaseIcon;
                changeIcon.color = Color.red;
            }
            else
            {
                changeIcon.gameObject.SetActive(false);
            }
        }
    }
}

#endregion