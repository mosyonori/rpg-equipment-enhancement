using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BattleUIController : MonoBehaviour
{
    [Header("UI参照")]
    public TextMeshProUGUI questNameText;
    public TextMeshProUGUI turnInfoText;
    public TextMeshProUGUI battleLogText;
    public ScrollRect battleLogScrollRect;

    [Header("キャラクターUI")]
    public Transform playerUIParent;
    public Transform enemyUIParent;
    public GameObject characterUIPrefab;

    [Header("設定")]
    public int maxLogLines = 20;
    public float scrollToBottomDelay = 0.1f;

    // キャラクターUIの管理
    private Dictionary<BattleCharacter, CharacterUIElement> characterUIs
        = new Dictionary<BattleCharacter, CharacterUIElement>();

    /// <summary>
    /// UI初期化
    /// </summary>
    public void InitializeUI(string questName, int turnLimit)
    {
        // クエスト名設定
        if (questNameText != null)
            questNameText.text = questName;

        // ターン情報初期化
        UpdateTurnInfo(0, turnLimit);

        // バトルログクリア
        ClearBattleLog();

        // 初期メッセージ
        AddBattleLog("<color=cyan>=== 戦闘開始 ===</color>");
    }

    /// <summary>
    /// ターン情報更新
    /// </summary>
    public void UpdateTurnInfo(int currentTurn, int turnLimit)
    {
        if (turnInfoText != null)
        {
            turnInfoText.text = $"ターン {currentTurn} / {turnLimit}";

            // ターン数が限界に近づいたら色を変更
            float turnRatio = (float)currentTurn / turnLimit;
            if (turnRatio >= 0.8f)
            {
                turnInfoText.color = Color.red;
            }
            else if (turnRatio >= 0.6f)
            {
                turnInfoText.color = Color.yellow;
            }
            else
            {
                turnInfoText.color = Color.white;
            }
        }
    }

    /// <summary>
    /// バトルログに新しいメッセージを追加
    /// </summary>
    public void AddBattleLog(string message)
    {
        if (battleLogText == null) return;

        // タイムスタンプ付きでメッセージを追加
        string timestamp = System.DateTime.Now.ToString("mm:ss");
        string formattedMessage = $"[{timestamp}] {message}";

        battleLogText.text += formattedMessage + "\n";

        // ログが長くなりすぎた場合は古い行を削除
        string[] lines = battleLogText.text.Split('\n');
        if (lines.Length > maxLogLines)
        {
            int keepLines = maxLogLines - 5; // 余裕を持って削除
            string[] newLines = new string[keepLines];
            System.Array.Copy(lines, lines.Length - keepLines, newLines, 0, keepLines);
            battleLogText.text = string.Join("\n", newLines) + "\n";
        }

        // スクロールを最下部に移動
        ScrollToBottom();
    }

    /// <summary>
    /// バトルログをクリア
    /// </summary>
    public void ClearBattleLog()
    {
        if (battleLogText != null)
            battleLogText.text = "";
    }

    /// <summary>
    /// スクロールを最下部に移動
    /// </summary>
    private void ScrollToBottom()
    {
        if (battleLogScrollRect != null)
        {
            // 次フレームでスクロール（レイアウト更新後）
            StartCoroutine(ScrollToBottomCoroutine());
        }
    }

    private System.Collections.IEnumerator ScrollToBottomCoroutine()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        if (battleLogScrollRect != null)
        {
            battleLogScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// キャラクターUIを作成
    /// </summary>
    public void CreateCharacterUI(BattleCharacter character, bool isPlayer)
    {
        if (characterUIPrefab == null)
        {
            Debug.LogWarning("CharacterUI プレハブが設定されていません");
            return;
        }

        Transform parent = isPlayer ? playerUIParent : enemyUIParent;
        if (parent == null)
        {
            Debug.LogWarning($"{(isPlayer ? "Player" : "Enemy")} UI Parent が設定されていません");
            return;
        }

        // UIオブジェクト生成
        GameObject uiObj = Instantiate(characterUIPrefab, parent);
        uiObj.name = $"{character.characterName}_UI";

        // CharacterUIElementコンポーネント取得
        CharacterUIElement uiElement = uiObj.GetComponent<CharacterUIElement>();
        if (uiElement != null)
        {
            uiElement.Initialize(character);
            characterUIs[character] = uiElement;

            Debug.Log($"キャラクターUI作成: {character.characterName}");
        }
        else
        {
            Debug.LogError("CharacterUIElement コンポーネントが見つかりません");
            Destroy(uiObj);
        }
    }

    /// <summary>
    /// キャラクターUIを更新
    /// </summary>
    public void UpdateCharacterUI(BattleCharacter character)
    {
        if (characterUIs.TryGetValue(character, out CharacterUIElement uiElement))
        {
            uiElement.UpdateDisplay();
        }
    }

    /// <summary>
    /// キャラクターのアクティブ状態を設定
    /// </summary>
    public void SetCharacterActive(BattleCharacter character, bool isActive)
    {
        if (characterUIs.TryGetValue(character, out CharacterUIElement uiElement))
        {
            uiElement.SetActiveIndicator(isActive);
        }
    }

    /// <summary>
    /// 全キャラクターUIを更新
    /// </summary>
    public void UpdateAllCharacterUIs()
    {
        foreach (var kvp in characterUIs)
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                kvp.Value.UpdateDisplay();
            }
        }
    }

    /// <summary>
    /// キャラクターUIを削除
    /// </summary>
    public void RemoveCharacterUI(BattleCharacter character)
    {
        if (characterUIs.TryGetValue(character, out CharacterUIElement uiElement))
        {
            if (uiElement != null)
            {
                Destroy(uiElement.gameObject);
            }
            characterUIs.Remove(character);
        }
    }

    /// <summary>
    /// 全キャラクターUIをクリア
    /// </summary>
    public void ClearAllCharacterUIs()
    {
        foreach (var kvp in characterUIs)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        characterUIs.Clear();
    }

    /// <summary>
    /// デバッグ情報表示
    /// </summary>
    public void ShowDebugInfo()
    {
        Debug.Log($"管理中のキャラクターUI数: {characterUIs.Count}");
        foreach (var kvp in characterUIs)
        {
            string charName = kvp.Key?.characterName ?? "Unknown";
            bool uiExists = kvp.Value != null;
            Debug.Log($"- {charName}: UI存在={uiExists}");
        }
    }

    private void OnDestroy()
    {
        // クリーンアップ
        ClearAllCharacterUIs();
    }
}