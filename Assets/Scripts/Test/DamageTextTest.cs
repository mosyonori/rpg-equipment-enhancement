using UnityEngine;

/// <summary>
/// DamageTextUIのテスト用スクリプト
/// BattleScene内の適当なGameObjectにアタッチしてテスト実行
/// </summary>
public class DamageTextTest : MonoBehaviour
{
    [Header("テスト設定")]
    [SerializeField] private DamageTextUI damageTextUI; // インスペクターでアサイン
    [SerializeField] private Transform testPosition; // テスト表示位置（省略可）

    [Header("テスト実行ボタン")]
    [SerializeField] private KeyCode testKey = KeyCode.Space; // スペースキーでテスト

    private void Update()
    {
        // スペースキーでテスト実行
        if (Input.GetKeyDown(testKey))
        {
            TestAllDamageTypes();
        }

        // 数字キーで個別テスト
        if (Input.GetKeyDown(KeyCode.Alpha1)) TestNormalDamage();
        if (Input.GetKeyDown(KeyCode.Alpha2)) TestCriticalDamage();
        if (Input.GetKeyDown(KeyCode.Alpha3)) TestHealDamage();
        if (Input.GetKeyDown(KeyCode.Alpha4)) TestNullifyDamage();
    }

    /// <summary>
    /// 全種類のダメージテキストをテスト
    /// </summary>
    [ContextMenu("全ダメージタイプテスト")]
    public void TestAllDamageTypes()
    {
        if (damageTextUI == null)
        {
            damageTextUI = FindFirstObjectByType<DamageTextUI>();
            if (damageTextUI == null)
            {
                Debug.LogError("DamageTextUIが見つかりません");
                return;
            }
        }

        Vector3 basePosition = GetTestPosition();

        // 通常ダメージ
        var normalDamage = CreateTestDamageData("通常ダメージ", 150, false);
        damageTextUI.ShowDamageText(normalDamage, basePosition + Vector3.left * 2);

        // クリティカルダメージ
        var criticalDamage = CreateTestDamageData("クリティカル", 300, true);
        damageTextUI.ShowDamageText(criticalDamage, basePosition + Vector3.right * 2);

        // 回復
        var healDamage = CreateTestDamageData("回復", -100, false);
        damageTextUI.ShowDamageText(healDamage, basePosition + Vector3.up * 1);

        // 無効化
        var nullifyDamage = CreateTestDamageData("無効化", 0, false, true);
        damageTextUI.ShowDamageText(nullifyDamage, basePosition + Vector3.down * 1);

        Debug.Log("全ダメージタイプテスト実行！");
    }

    /// <summary>
    /// 通常ダメージテスト
    /// </summary>
    public void TestNormalDamage()
    {
        if (damageTextUI == null) return;

        var damage = CreateTestDamageData("通常", Random.Range(500, 800), false);
        damageTextUI.ShowDamageText(damage, GetTestPosition());
        Debug.Log("通常ダメージテスト実行");
    }

    /// <summary>
    /// クリティカルダメージテスト
    /// </summary>
    public void TestCriticalDamage()
    {
        if (damageTextUI == null) return;

        var damage = CreateTestDamageData("クリティカル", Random.Range(500, 800), true);
        damageTextUI.ShowDamageText(damage, GetTestPosition());
        Debug.Log("クリティカルダメージテスト実行");
    }

    /// <summary>
    /// 回復テスト
    /// </summary>
    public void TestHealDamage()
    {
        if (damageTextUI == null) return;

        var damage = CreateTestDamageData("回復", -Random.Range(500, 800), false);
        damageTextUI.ShowDamageText(damage, GetTestPosition());
        Debug.Log("回復テスト実行");
    }

    /// <summary>
    /// 無効化テスト
    /// </summary>
    public void TestNullifyDamage()
    {
        if (damageTextUI == null) return;

        var damage = CreateTestDamageData("無効化", 0, false, true);
        damageTextUI.ShowDamageText(damage, GetTestPosition());
        Debug.Log("無効化テスト実行");
    }

    /// <summary>
    /// テスト用DamageData作成
    /// </summary>
    private DamageData CreateTestDamageData(string targetName, int damage, bool isCritical, bool hasBaseDamage = false)
    {
        return new DamageData
        {
            targetName = targetName,
            finalDamage = damage,
            baseDamage = hasBaseDamage ? 100 : damage, // 無効化用
            isCritical = isCritical,
            effectiveness = Random.value > 0.8f ? DamageEffectiveness.SuperEffective :
                           Random.value > 0.6f ? DamageEffectiveness.NotVeryEffective :
                           DamageEffectiveness.Normal
        };
    }

    /// <summary>
    /// テスト表示位置取得
    /// </summary>
    private Vector3 GetTestPosition()
    {
        if (testPosition != null)
            return testPosition.position;

        // カメラの前方にテスト位置を設定
        Camera cam = Camera.main;
        if (cam != null)
        {
            return cam.transform.position + cam.transform.forward * 5f;
        }

        return Vector3.zero;
    }

    private void OnGUI()
    {
        // テスト用GUI
        GUILayout.BeginArea(new Rect(10, 10, 200, 200));
        GUILayout.Label("DamageTextUIテスト");
        GUILayout.Label("Space: 全タイプテスト");
        GUILayout.Label("1: 通常ダメージ");
        GUILayout.Label("2: クリティカル");
        GUILayout.Label("3: 回復");
        GUILayout.Label("4: 無効化");

        if (GUILayout.Button("全タイプテスト"))
        {
            TestAllDamageTypes();
        }
        GUILayout.EndArea();
    }
}