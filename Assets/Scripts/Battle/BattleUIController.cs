using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BattleUIController : MonoBehaviour
{
    [Header("UI�Q��")]
    public TextMeshProUGUI questNameText;
    public TextMeshProUGUI turnInfoText;
    public TextMeshProUGUI battleLogText;
    public ScrollRect battleLogScrollRect;

    [Header("�L�����N�^�[UI")]
    public Transform playerUIParent;
    public Transform enemyUIParent;
    public GameObject characterUIPrefab;

    [Header("�ݒ�")]
    public int maxLogLines = 20;
    public float scrollToBottomDelay = 0.1f;

    // �L�����N�^�[UI�̊Ǘ�
    private Dictionary<BattleCharacter, CharacterUIElement> characterUIs
        = new Dictionary<BattleCharacter, CharacterUIElement>();

    /// <summary>
    /// UI������
    /// </summary>
    public void InitializeUI(string questName, int turnLimit)
    {
        // �N�G�X�g���ݒ�
        if (questNameText != null)
            questNameText.text = questName;

        // �^�[����񏉊���
        UpdateTurnInfo(0, turnLimit);

        // �o�g�����O�N���A
        ClearBattleLog();

        // �������b�Z�[�W
        AddBattleLog("<color=cyan>=== �퓬�J�n ===</color>");
    }

    /// <summary>
    /// �^�[�����X�V
    /// </summary>
    public void UpdateTurnInfo(int currentTurn, int turnLimit)
    {
        if (turnInfoText != null)
        {
            turnInfoText.text = $"�^�[�� {currentTurn} / {turnLimit}";

            // �^�[���������E�ɋ߂Â�����F��ύX
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
    /// �o�g�����O�ɐV�������b�Z�[�W��ǉ�
    /// </summary>
    public void AddBattleLog(string message)
    {
        if (battleLogText == null) return;

        // �^�C���X�^���v�t���Ń��b�Z�[�W��ǉ�
        string timestamp = System.DateTime.Now.ToString("mm:ss");
        string formattedMessage = $"[{timestamp}] {message}";

        battleLogText.text += formattedMessage + "\n";

        // ���O�������Ȃ肷�����ꍇ�͌Â��s���폜
        string[] lines = battleLogText.text.Split('\n');
        if (lines.Length > maxLogLines)
        {
            int keepLines = maxLogLines - 5; // �]�T�������č폜
            string[] newLines = new string[keepLines];
            System.Array.Copy(lines, lines.Length - keepLines, newLines, 0, keepLines);
            battleLogText.text = string.Join("\n", newLines) + "\n";
        }

        // �X�N���[�����ŉ����Ɉړ�
        ScrollToBottom();
    }

    /// <summary>
    /// �o�g�����O���N���A
    /// </summary>
    public void ClearBattleLog()
    {
        if (battleLogText != null)
            battleLogText.text = "";
    }

    /// <summary>
    /// �X�N���[�����ŉ����Ɉړ�
    /// </summary>
    private void ScrollToBottom()
    {
        if (battleLogScrollRect != null)
        {
            // ���t���[���ŃX�N���[���i���C�A�E�g�X�V��j
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
    /// �L�����N�^�[UI���쐬
    /// </summary>
    public void CreateCharacterUI(BattleCharacter character, bool isPlayer)
    {
        if (characterUIPrefab == null)
        {
            Debug.LogWarning("CharacterUI �v���n�u���ݒ肳��Ă��܂���");
            return;
        }

        Transform parent = isPlayer ? playerUIParent : enemyUIParent;
        if (parent == null)
        {
            Debug.LogWarning($"{(isPlayer ? "Player" : "Enemy")} UI Parent ���ݒ肳��Ă��܂���");
            return;
        }

        // UI�I�u�W�F�N�g����
        GameObject uiObj = Instantiate(characterUIPrefab, parent);
        uiObj.name = $"{character.characterName}_UI";

        // CharacterUIElement�R���|�[�l���g�擾
        CharacterUIElement uiElement = uiObj.GetComponent<CharacterUIElement>();
        if (uiElement != null)
        {
            uiElement.Initialize(character);
            characterUIs[character] = uiElement;

            Debug.Log($"�L�����N�^�[UI�쐬: {character.characterName}");
        }
        else
        {
            Debug.LogError("CharacterUIElement �R���|�[�l���g��������܂���");
            Destroy(uiObj);
        }
    }

    /// <summary>
    /// �L�����N�^�[UI���X�V
    /// </summary>
    public void UpdateCharacterUI(BattleCharacter character)
    {
        if (characterUIs.TryGetValue(character, out CharacterUIElement uiElement))
        {
            uiElement.UpdateDisplay();
        }
    }

    /// <summary>
    /// �L�����N�^�[�̃A�N�e�B�u��Ԃ�ݒ�
    /// </summary>
    public void SetCharacterActive(BattleCharacter character, bool isActive)
    {
        if (characterUIs.TryGetValue(character, out CharacterUIElement uiElement))
        {
            uiElement.SetActiveIndicator(isActive);
        }
    }

    /// <summary>
    /// �S�L�����N�^�[UI���X�V
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
    /// �L�����N�^�[UI���폜
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
    /// �S�L�����N�^�[UI���N���A
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
    /// �f�o�b�O���\��
    /// </summary>
    public void ShowDebugInfo()
    {
        Debug.Log($"�Ǘ����̃L�����N�^�[UI��: {characterUIs.Count}");
        foreach (var kvp in characterUIs)
        {
            string charName = kvp.Key?.characterName ?? "Unknown";
            bool uiExists = kvp.Value != null;
            Debug.Log($"- {charName}: UI����={uiExists}");
        }
    }

    private void OnDestroy()
    {
        // �N���[���A�b�v
        ClearAllCharacterUIs();
    }
}