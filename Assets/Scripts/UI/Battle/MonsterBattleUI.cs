using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// モンスター群の戦闘UI制御
/// 役割：モンスター画像・名前表示、HPバー・状態表示、撃破時のアニメーション
/// データアクセス統一ルール: UI層 → Manager層 → データ層
/// </summary>
public class MonsterBattleUI : MonoBehaviour
{
    [Header("モンスター表示エリア")]
    [SerializeField] private Transform monsterGridParent;
    [SerializeField] private GridLayoutGroup monsterGridLayout;
    [SerializeField] private GameObject monsterSlotPrefab;

    [Header("モンスター共通UI設定")]
    [SerializeField] private Vector2 monsterSlotSize = new Vector2(150f, 200f);
    [SerializeField] private Vector2 monsterSpacing = new Vector2(10f, 10f);
    [SerializeField] private int maxMonstersPerRow = 3;

    [Header("撃破エフェクト")]
    [SerializeField] private GameObject defeatEffectPrefab;
    [SerializeField] private float defeatAnimationDuration = 1.0f;
    [SerializeField] private AnimationCurve defeatFadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("行動順位表示")]
    [SerializeField] private Color activeTurnColor = Color.yellow;
    [SerializeField] private Color inactiveTurnColor = Color.gray;
    [SerializeField] private float turnIndicatorScale = 1.2f;

    [Header("ダメージエフェクト")]
    [SerializeField] private float damageShakeStrength = 5f;
    [SerializeField] private float damageShakeDuration = 0.3f;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float damageFlashDuration = 0.2f;

    // イベント
    public static event Action<string> OnMonsterSelected;
    public static event Action<string> OnMonsterDefeated;

    // 内部状態
    private bool isInitialized = false;
    private Dictionary<string, MonsterSlotUIComponent> monsterSlots;
    private List<BattleCharacterData> currentMonsters;
    private string currentActiveMonster = "";

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeCollections();
        ValidateComponents();
    }

    private void Start()
    {
        SetupGridLayout();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// UIコンポーネントの初期化
    /// </summary>
    public void Initialize()
    {
        try
        {
            Log("MonsterBattleUI初期化開始");

            // コレクション初期化
            InitializeCollections();

            // グリッドレイアウト設定
            SetupGridLayout();

            // 既存のモンスタースロットをクリア
            ClearMonsterSlots();

            isInitialized = true;
            Log("MonsterBattleUI初期化完了");
        }
        catch (Exception e)
        {
            LogError($"MonsterBattleUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// コレクション初期化
    /// </summary>
    private void InitializeCollections()
    {
        monsterSlots = new Dictionary<string, MonsterSlotUIComponent>();
        currentMonsters = new List<BattleCharacterData>();
    }

    /// <summary>
    /// コンポーネント検証
    /// </summary>
    private void ValidateComponents()
    {
        if (monsterGridParent == null)
            LogWarning("monsterGridParentが設定されていません");

        if (monsterSlotPrefab == null)
            LogWarning("monsterSlotPrefabが設定されていません");

        if (monsterGridLayout == null)
            LogWarning("monsterGridLayoutが設定されていません");
    }

    /// <summary>
    /// グリッドレイアウト設定
    /// </summary>
    private void SetupGridLayout()
    {
        if (monsterGridLayout == null) return;

        try
        {
            monsterGridLayout.cellSize = monsterSlotSize;
            monsterGridLayout.spacing = monsterSpacing;
            monsterGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            monsterGridLayout.constraintCount = maxMonstersPerRow;
            monsterGridLayout.childAlignment = TextAnchor.MiddleCenter;

            Log("グリッドレイアウト設定完了");
        }
        catch (Exception e)
        {
            LogError($"グリッドレイアウト設定エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - イベントハンドラ

    /// <summary>
    /// 戦闘開始時の処理
    /// </summary>
    public void OnBattleStart(BattleSetupData setupData)
    {
        if (!isInitialized) Initialize();

        try
        {
            Log("戦闘開始 - モンスターUI初期化");

            // BattleManager経由でモンスターデータを取得
            if (BattleManager.Instance != null)
            {
                var allCharacters = BattleManager.Instance.GetAllCharacters();
                var enemies = allCharacters.FindAll(c => !c.isPlayer);

                CreateMonsterSlots(enemies);
                Log($"モンスタースロット作成完了: {enemies.Count}体");
            }
        }
        catch (Exception e)
        {
            LogError($"戦闘開始処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ターン開始時の処理
    /// </summary>
    public void OnTurnStart(BattleCharacterData character)
    {
        try
        {
            // 前回のアクティブ状態をリセット
            SetMonsterActive(currentActiveMonster, false);

            if (!character.isPlayer)
            {
                // モンスターのターン開始
                currentActiveMonster = character.characterId;
                SetMonsterActive(currentActiveMonster, true);
                Log($"モンスターターン開始: {character.characterName}");
            }
            else
            {
                // プレイヤーのターン
                currentActiveMonster = "";
            }
        }
        catch (Exception e)
        {
            LogError($"ターン開始処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 行動実行時の処理
    /// </summary>
    public void OnActionExecuted(ActionData action)
    {
        try
        {
            // ダメージを受けたモンスターの処理
            foreach (var damage in action.damageResults)
            {
                if (monsterSlots.ContainsKey(damage.targetId))
                {
                    var monsterSlot = monsterSlots[damage.targetId];

                    // HP更新
                    monsterSlot.UpdateMonsterData();

                    // ダメージエフェクト表示
                    if (damage.finalDamage > 0)
                    {
                        StartCoroutine(PlayDamageEffect(monsterSlot));
                    }

                    // 撃破チェック
                    if (damage.targetDefeated)
                    {
                        StartCoroutine(PlayDefeatAnimation(damage.targetId));
                    }

                    Log($"モンスターダメージ処理: {damage.targetName} - {damage.finalDamage}");
                }
            }
        }
        catch (Exception e)
        {
            LogError($"行動実行処理エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - データ更新

    /// <summary>
    /// モンスターデータ更新
    /// </summary>
    public void UpdateMonstersData(List<BattleCharacterData> monsters)
    {
        try
        {
            currentMonsters = new List<BattleCharacterData>(monsters);

            // 各モンスタースロットのデータ更新
            foreach (var monster in monsters)
            {
                if (monsterSlots.ContainsKey(monster.characterId))
                {
                    monsterSlots[monster.characterId].UpdateMonsterData();
                }
            }

            Log($"モンスターデータ更新: {monsters.Count}体");
        }
        catch (Exception e)
        {
            LogError($"モンスターデータ更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// モンスター撃破処理
    /// </summary>
    public void OnMonsterDefeatedExternal(string monsterId)
    {
        try
        {
            if (monsterSlots.ContainsKey(monsterId))
            {
                StartCoroutine(PlayDefeatAnimation(monsterId));
                Log($"モンスター撃破: {monsterId}");
            }
        }
        catch (Exception e)
        {
            LogError($"モンスター撃破処理エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - モンスタースロット管理

    /// <summary>
    /// モンスタースロット作成
    /// </summary>
    private void CreateMonsterSlots(List<BattleCharacterData> monsters)
    {
        if (monsterSlotPrefab == null || monsterGridParent == null) return;

        try
        {
            // 既存スロットクリア
            ClearMonsterSlots();

            // 新しいスロット作成
            foreach (var monster in monsters)
            {
                CreateSingleMonsterSlot(monster);
            }

            currentMonsters = new List<BattleCharacterData>(monsters);
            Log($"モンスタースロット作成完了: {monsters.Count}体");
        }
        catch (Exception e)
        {
            LogError($"モンスタースロット作成エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 単体モンスタースロット作成
    /// </summary>
    private void CreateSingleMonsterSlot(BattleCharacterData monster)
    {
        try
        {
            GameObject slotObj = Instantiate(monsterSlotPrefab, monsterGridParent);
            var slotUI = slotObj.GetComponent<MonsterSlotUIComponent>();

            if (slotUI != null)
            {
                slotUI.SetMonster(monster);
                slotUI.OnMonsterClicked += OnMonsterSlotClicked;
                monsterSlots[monster.characterId] = slotUI;
            }
            else
            {
                // MonsterSlotUIComponentがない場合の基本表示
                SetupBasicMonsterSlot(slotObj, monster);
                LogWarning($"MonsterSlotUIComponentが見つかりません: {monster.characterName}");
            }
        }
        catch (Exception e)
        {
            LogError($"単体モンスタースロット作成エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 基本モンスタースロット設定
    /// </summary>
    private void SetupBasicMonsterSlot(GameObject slotObj, BattleCharacterData monster)
    {
        try
        {
            // 基本的なテキスト表示
            var textComponents = slotObj.GetComponentsInChildren<TextMeshProUGUI>();
            if (textComponents.Length > 0)
            {
                textComponents[0].text = monster.characterName;
            }
            if (textComponents.Length > 1)
            {
                textComponents[1].text = $"HP: {monster.currentHp}/{monster.maxHp}";
            }

            // クリックイベント設定
            var button = slotObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnMonsterSlotClicked(monster.characterId));
            }
        }
        catch (Exception e)
        {
            LogError($"基本モンスタースロット設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// モンスタースロットクリア
    /// </summary>
    private void ClearMonsterSlots()
    {
        if (monsterGridParent == null) return;

        try
        {
            // イベント登録解除
            foreach (var slot in monsterSlots.Values)
            {
                if (slot != null)
                {
                    slot.OnMonsterClicked -= OnMonsterSlotClicked;
                }
            }

            // スロット削除
            for (int i = monsterGridParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(monsterGridParent.GetChild(i).gameObject);
            }

            monsterSlots.Clear();
            currentMonsters.Clear();
            currentActiveMonster = "";

            Log("モンスタースロットクリア完了");
        }
        catch (Exception e)
        {
            LogError($"モンスタースロットクリアエラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - 状態更新

    /// <summary>
    /// モンスターアクティブ状態設定
    /// </summary>
    private void SetMonsterActive(string monsterId, bool isActive)
    {
        if (string.IsNullOrEmpty(monsterId) || !monsterSlots.ContainsKey(monsterId))
            return;

        try
        {
            var monsterSlot = monsterSlots[monsterId];
            monsterSlot.SetActive(isActive);

            if (isActive)
            {
                StartCoroutine(ActiveMonsterEffect(monsterSlot));
            }
        }
        catch (Exception e)
        {
            LogError($"モンスターアクティブ状態設定エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - エフェクト・アニメーション

    /// <summary>
    /// ダメージエフェクト再生
    /// </summary>
    private IEnumerator PlayDamageEffect(MonsterSlotUIComponent monsterSlot)
    {
        if (monsterSlot == null) yield break;

        // 振動エフェクト
        yield return StartCoroutine(DamageShakeEffect(monsterSlot.transform));

        // フラッシュエフェクト
        yield return StartCoroutine(DamageFlashEffect(monsterSlot));
    }

    /// <summary>
    /// ダメージ振動エフェクト
    /// </summary>
    private IEnumerator DamageShakeEffect(Transform target)
    {
        Vector3 originalPosition = target.localPosition;
        float elapsed = 0f;

        while (elapsed < damageShakeDuration)
        {
            elapsed += Time.deltaTime;
            float intensity = Mathf.Lerp(damageShakeStrength, 0f, elapsed / damageShakeDuration);

            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-intensity, intensity),
                UnityEngine.Random.Range(-intensity, intensity),
                0f
            );

            target.localPosition = originalPosition + randomOffset;
            yield return null;
        }

        target.localPosition = originalPosition;
    }

    /// <summary>
    /// ダメージフラッシュエフェクト
    /// </summary>
    private IEnumerator DamageFlashEffect(MonsterSlotUIComponent monsterSlot)
    {
        var imageComponent = monsterSlot.GetComponent<Image>();
        if (imageComponent == null) yield break;

        Color originalColor = imageComponent.color;
        float elapsed = 0f;

        while (elapsed < damageFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / damageFlashDuration;
            imageComponent.color = Color.Lerp(damageFlashColor, originalColor, t);
            yield return null;
        }

        imageComponent.color = originalColor;
    }

    /// <summary>
    /// アクティブモンスターエフェクト
    /// </summary>
    private IEnumerator ActiveMonsterEffect(MonsterSlotUIComponent monsterSlot)
    {
        Transform target = monsterSlot.transform;
        Vector3 originalScale = target.localScale;
        Vector3 targetScale = originalScale * turnIndicatorScale;

        float duration = 0.3f;
        float elapsed = 0f;

        // スケールアップ
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            target.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        target.localScale = targetScale;

        // 少し待つ
        yield return new WaitForSeconds(0.2f);

        // スケールダウン
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            target.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        target.localScale = originalScale;
    }

    /// <summary>
    /// 撃破アニメーション再生
    /// </summary>
    private IEnumerator PlayDefeatAnimation(string monsterId)
    {
        if (!monsterSlots.ContainsKey(monsterId)) yield break;

        var monsterSlot = monsterSlots[monsterId];

        // 撃破エフェクト生成
        if (defeatEffectPrefab != null)
        {
            GameObject effectObj = Instantiate(defeatEffectPrefab, monsterSlot.transform.position, Quaternion.identity);
            Destroy(effectObj, defeatAnimationDuration);
        }

        // フェードアウトアニメーション
        yield return StartCoroutine(DefeatFadeOutAnimation(monsterSlot));

        // モンスタースロット無効化
        monsterSlot.SetDefeated(true);

        // イベント発行
        OnMonsterDefeated?.Invoke(monsterId);

        Log($"撃破アニメーション完了: {monsterId}");
    }

    /// <summary>
    /// 撃破フェードアウトアニメーション
    /// </summary>
    private IEnumerator DefeatFadeOutAnimation(MonsterSlotUIComponent monsterSlot)
    {
        CanvasGroup canvasGroup = monsterSlot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = monsterSlot.gameObject.AddComponent<CanvasGroup>();
        }

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < defeatAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / defeatAnimationDuration;
            float curveValue = defeatFadeCurve.Evaluate(t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, curveValue);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// モンスタースロットクリックハンドラ
    /// </summary>
    private void OnMonsterSlotClicked(string monsterId)
    {
        try
        {
            OnMonsterSelected?.Invoke(monsterId);
            Log($"モンスター選択: {monsterId}");
        }
        catch (Exception e)
        {
            LogError($"モンスタースロットクリックエラー: {e.Message}");
        }
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        Debug.Log($"[MonsterBattleUI] {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[MonsterBattleUI] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[MonsterBattleUI] {message}");
    }

    #endregion

    #region エディタ用ツール

#if UNITY_EDITOR
    [ContextMenu("モンスター表示テスト")]
    private void TestMonsterDisplay()
    {
        Log("モンスター表示テスト実行");

        if (BattleManager.Instance != null)
        {
            var enemies = BattleManager.Instance.GetEnemyCharacters();
            CreateMonsterSlots(enemies);
        }
        else
        {
            LogWarning("BattleManagerが見つかりません");
        }
    }

    [ContextMenu("撃破アニメーションテスト")]
    private void TestDefeatAnimation()
    {
        if (monsterSlots.Count > 0)
        {
            var firstMonster = new List<string>(monsterSlots.Keys)[0];
            StartCoroutine(PlayDefeatAnimation(firstMonster));
            Log($"撃破アニメーションテスト: {firstMonster}");
        }
        else
        {
            LogWarning("テスト用のモンスタースロットがありません");
        }
    }
#endif

    #endregion
}

/// <summary>
/// モンスタースロット基本コンポーネント（簡易実装）
/// 将来的に専用クラスファイルで詳細実装予定
/// </summary>
public class MonsterSlotUIComponent : MonoBehaviour
{
    public event Action<string> OnMonsterClicked;

    private BattleCharacterData monsterData;
    private Button slotButton;

    private void Awake()
    {
        slotButton = GetComponent<Button>();
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }
    }

    public void SetMonster(BattleCharacterData monster)
    {
        monsterData = monster;
        UpdateDisplay();
    }

    public void UpdateMonsterData()
    {
        if (monsterData != null)
        {
            UpdateDisplay();
        }
    }

    public void SetActive(bool isActive)
    {
        // 基本実装：後で拡張予定
    }

    public void SetDefeated(bool isDefeated)
    {
        if (slotButton != null)
        {
            slotButton.interactable = !isDefeated;
        }
    }

    private void UpdateDisplay()
    {
        if (monsterData == null) return;

        // 基本的なテキスト表示（TextMeshProUGUIコンポーネントを検索）
        var textComponents = GetComponentsInChildren<TextMeshProUGUI>();
        if (textComponents.Length > 0)
        {
            textComponents[0].text = monsterData.characterName;
        }
        if (textComponents.Length > 1)
        {
            textComponents[1].text = $"HP: {monsterData.currentHp}/{monsterData.maxHp}";
        }

        // HPバー更新
        var hpBar = GetComponentInChildren<Slider>();
        if (hpBar != null)
        {
            hpBar.value = monsterData.GetHpRatio();
        }
    }

    private void OnSlotClicked()
    {
        if (monsterData != null)
        {
            OnMonsterClicked?.Invoke(monsterData.characterId);
        }
    }
}