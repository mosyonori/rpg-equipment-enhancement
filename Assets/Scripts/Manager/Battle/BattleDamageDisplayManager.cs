using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 戦闘中のダメージ表示を管理するクラス
/// モンスターやプレイヤーの位置に応じてダメージテキストを表示
/// </summary>
public class BattleDamageDisplayManager : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private DamageTextUI damageTextUI;
    [SerializeField] private PlayerBattleUI playerBattleUI;
    [SerializeField] private MonsterBattleUI monsterBattleUI;

    [Header("表示オフセット設定")]
    [SerializeField] private Vector3 playerDamageOffset = new Vector3(0, 50, 0);
    [SerializeField] private Vector3 monsterDamageOffset = new Vector3(0, 30, 0);

    private void Start()
    {
        InitializeReferences();
    }

    /// <summary>
    /// 参照の初期化
    /// </summary>
    private void InitializeReferences()
    {
        if (damageTextUI == null)
            damageTextUI = FindFirstObjectByType<DamageTextUI>();

        if (playerBattleUI == null)
            playerBattleUI = FindFirstObjectByType<PlayerBattleUI>();

        if (monsterBattleUI == null)
            monsterBattleUI = FindFirstObjectByType<MonsterBattleUI>();
    }

    /// <summary>
    /// プレイヤーにダメージ表示
    /// </summary>
    /// <param name="damageData">ダメージデータ</param>
    public void ShowPlayerDamage(DamageData damageData)
    {
        if (playerBattleUI == null || damageTextUI == null) return;

        Vector3 displayPosition = GetPlayerDamagePosition();
        damageTextUI.ShowDamageText(damageData, displayPosition);
    }

    /// <summary>
    /// モンスターにダメージ表示
    /// </summary>
    /// <param name="damageData">ダメージデータ</param>
    /// <param name="monsterIndex">モンスターのインデックス（複数モンスター対応）</param>
    public void ShowMonsterDamage(DamageData damageData, int monsterIndex = 0)
    {
        if (monsterBattleUI == null || damageTextUI == null) return;

        Vector3 displayPosition = GetMonsterDamagePosition(monsterIndex);
        damageTextUI.ShowDamageText(damageData, displayPosition);
    }

    /// <summary>
    /// 特定のモンスターUIにダメージ表示
    /// </summary>
    /// <param name="damageData">ダメージデータ</param>
    /// <param name="targetMonster">対象のモンスターUI</param>
    public void ShowMonsterDamage(DamageData damageData, MonsterBattleUI targetMonster)
    {
        if (targetMonster == null || damageTextUI == null) return;

        Vector3 displayPosition = GetMonsterDamagePosition(targetMonster);
        damageTextUI.ShowDamageText(damageData, displayPosition);
    }

    /// <summary>
    /// プレイヤーのダメージ表示位置を取得
    /// </summary>
    private Vector3 GetPlayerDamagePosition()
    {
        Vector3 basePosition = RectTransformUtility.WorldToScreenPoint(
            Camera.main,
            playerBattleUI.transform.position
        );

        return basePosition + playerDamageOffset;
    }

    /// <summary>
    /// モンスターのダメージ表示位置を取得（インデックス指定）
    /// </summary>
    private Vector3 GetMonsterDamagePosition(int monsterIndex = 0)
    {
        // MonsterBattleUIから特定のモンスターの位置を取得
        // 実装はMonsterBattleUIの構造に依存
        Vector3 basePosition = RectTransformUtility.WorldToScreenPoint(
            Camera.main,
            monsterBattleUI.transform.position
        );

        // 複数モンスターの場合は横にオフセット
        Vector3 indexOffset = new Vector3(monsterIndex * 100, 0, 0);
        return basePosition + monsterDamageOffset + indexOffset;
    }

    /// <summary>
    /// モンスターのダメージ表示位置を取得（UI指定）
    /// </summary>
    private Vector3 GetMonsterDamagePosition(MonsterBattleUI targetMonster)
    {
        Vector3 basePosition = RectTransformUtility.WorldToScreenPoint(
            Camera.main,
            targetMonster.transform.position
        );

        return basePosition + monsterDamageOffset;
    }

    /// <summary>
    /// 複数のダメージを連続表示（AOEスキル等）
    /// </summary>
    /// <param name="damageDataList">ダメージデータリスト</param>
    /// <param name="isPlayerTarget">プレイヤーが対象か</param>
    public void ShowMultipleDamages(List<DamageData> damageDataList, bool isPlayerTarget)
    {
        if (damageDataList == null || damageDataList.Count == 0) return;

        Vector3 basePosition = isPlayerTarget ?
            GetPlayerDamagePosition() :
            GetMonsterDamagePosition();

        damageTextUI.ShowMultipleDamageTexts(damageDataList, basePosition);
    }

    /// <summary>
    /// 戦闘管理クラスから呼び出されるメソッド例
    /// </summary>
    /// <param name="actionData">行動データ</param>
    public void OnBattleActionExecuted(ActionData actionData)
    {
        if (actionData.damageResults == null || actionData.damageResults.Count == 0) return;

        if (actionData.isPlayerAction)
        {
            // プレイヤーの攻撃 → モンスターにダメージ表示
            if (actionData.IsSingleTarget())
            {
                // 単体攻撃
                ShowMonsterDamage(actionData.damageResults[0]);
            }
            else
            {
                // 複数攻撃
                ShowMultipleDamages(actionData.damageResults, false);
            }
        }
        else
        {
            // モンスターの攻撃 → プレイヤーにダメージ表示
            if (actionData.IsSingleTarget())
            {
                // 単体攻撃
                ShowPlayerDamage(actionData.damageResults[0]);
            }
            else
            {
                // 複数攻撃（AOEスキル等）
                ShowMultipleDamages(actionData.damageResults, true);
            }
        }
    }

    #region テスト用メソッド

    [ContextMenu("プレイヤーダメージテスト")]
    private void TestPlayerDamage()
    {
        var testDamage = new DamageData
        {
            targetName = "プレイヤー",
            finalDamage = 150,
            isCritical = false
        };

        ShowPlayerDamage(testDamage);
    }

    [ContextMenu("モンスターダメージテスト")]
    private void TestMonsterDamage()
    {
        var testDamage = new DamageData
        {
            targetName = "モンスター",
            finalDamage = 200,
            isCritical = true
        };

        ShowMonsterDamage(testDamage);
    }

    [ContextMenu("複数ダメージテスト")]
    private void TestMultipleDamages()
    {
        var damageList = new List<DamageData>
        {
            new DamageData { targetName = "モンスター1", finalDamage = 100, isCritical = false },
            new DamageData { targetName = "モンスター2", finalDamage = 150, isCritical = true },
            new DamageData { targetName = "モンスター3", finalDamage = 80, isCritical = false }
        };

        ShowMultipleDamages(damageList, false);
    }

    #endregion
}