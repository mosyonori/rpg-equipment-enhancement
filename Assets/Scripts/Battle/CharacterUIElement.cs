using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CharacterUIElement : MonoBehaviour
{
    [Header("��{UI�v�f")]
    public Image characterImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;

    [Header("HP�o�[")]
    public Image hpBarBackground;
    public Image hpBarFill;
    public Slider hpSlider;

    [Header("�A�N�V�����\��")]
    public Image actionIndicator;
    public GameObject activeFrame;

    [Header("��Ԉُ�\��")]
    public Transform statusEffectParent;
    public GameObject statusEffectPrefab;

    [Header("�F�ݒ�")]
    public Color normalColor = Color.white;
    public Color playerColor = Color.cyan;
    public Color enemyColor = Color.red;
    public Color deadColor = Color.gray;

    [Header("HP�o�[�F�ݒ�")]
    public Color hpFullColor = Color.green;
    public Color hpMidColor = Color.yellow;
    public Color hpLowColor = Color.red;

    // �Ǘ��p
    private BattleCharacter targetCharacter;
    private List<GameObject> statusEffectIcons = new List<GameObject>();
    private bool isInitialized = false;

    /// <summary>
    /// �L�����N�^�[UI��������
    /// </summary>
    public void Initialize(BattleCharacter character)
    {
        targetCharacter = character;

        if (targetCharacter == null)
        {
            Debug.LogError("�^�[�Q�b�g�L�����N�^�[��null�ł�");
            return;
        }

        // ��{���ݒ�
        SetupBasicInfo();

        // �����\���X�V
        UpdateDisplay();

        // �A�N�e�B�u�\�����I�t
        SetActiveIndicator(false);

        isInitialized = true;

        Debug.Log($"CharacterUIElement����������: {targetCharacter.characterName}");
    }

    /// <summary>
    /// ��{�����Z�b�g�A�b�v
    /// </summary>
    private void SetupBasicInfo()
    {
        // ���O�ݒ�
        if (nameText != null)
        {
            nameText.text = targetCharacter.characterName;

            // �v���C���[�ƓG�ŐF��ύX
            if (targetCharacter is BattlePlayer)
            {
                nameText.color = playerColor;
            }
            else if (targetCharacter is BattleEnemy)
            {
                nameText.color = enemyColor;
            }
        }

        // �L�����N�^�[�摜�ݒ�i��������j
        if (characterImage != null)
        {
            // TODO: �L�����N�^�[�̃A�C�R���摜��ݒ�
            characterImage.color = targetCharacter is BattlePlayer ? playerColor : enemyColor;
        }

        // HP�X���C�_�[�ݒ�
        if (hpSlider != null)
        {
            hpSlider.maxValue = targetCharacter.maxHP;
            hpSlider.minValue = 0;
        }
    }

    /// <summary>
    /// �\�����X�V
    /// </summary>
    public void UpdateDisplay()
    {
        if (!isInitialized || targetCharacter == null) return;

        // HP�\���X�V
        UpdateHPDisplay();

        // ������Ԃɂ��\���ύX
        UpdateAliveState();

        // ��Ԉُ�\���X�V
        UpdateStatusEffects();
    }

    /// <summary>
    /// HP�\�����X�V
    /// </summary>
    private void UpdateHPDisplay()
    {
        // HP�e�L�X�g�X�V
        if (hpText != null)
        {
            hpText.text = $"{targetCharacter.currentHP} / {targetCharacter.maxHP}";
        }

        // HP�X���C�_�[�X�V
        if (hpSlider != null)
        {
            hpSlider.value = targetCharacter.currentHP;
        }

        // HP�o�[�X�V
        if (hpBarFill != null)
        {
            float hpRatio = (float)targetCharacter.currentHP / targetCharacter.maxHP;

            // HP�o�[�̐F�������ɉ����ĕύX
            Color barColor = GetHPBarColor(hpRatio);
            hpBarFill.color = barColor;

            // HP�o�[�̕����X�V�i�X���C�_�[���g��Ȃ��ꍇ�j
            if (hpSlider == null)
            {
                hpBarFill.fillAmount = hpRatio;
            }
        }
    }

    /// <summary>
    /// HP�����ɉ������F���擾
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
    /// ������Ԃɂ��\���X�V
    /// </summary>
    private void UpdateAliveState()
    {
        bool isAlive = targetCharacter.isAlive;

        // ���S���͑S�̂��D�F��
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

        // HP�o�[���D�F��
        if (!isAlive && hpBarFill != null)
        {
            hpBarFill.color = deadColor;
        }
    }

    /// <summary>
    /// ��Ԉُ�\�����X�V
    /// </summary>
    private void UpdateStatusEffects()
    {
        if (statusEffectParent == null) return;

        // �����̃A�C�R�����N���A
        ClearStatusEffectIcons();

        // �A�N�e�B�u�ȏ�Ԉُ��\��
        foreach (var effect in targetCharacter.activeEffects)
        {
            CreateStatusEffectIcon(effect);
        }
    }

    /// <summary>
    /// ��Ԉُ�A�C�R�����쐬
    /// </summary>
    private void CreateStatusEffectIcon(StatusEffect effect)
    {
        if (statusEffectPrefab == null) return;

        GameObject iconObj = Instantiate(statusEffectPrefab, statusEffectParent);
        statusEffectIcons.Add(iconObj);

        // �A�C�R���̐F�����ʃ^�C�v�ɉ����Đݒ�
        Image iconImage = iconObj.GetComponent<Image>();
        if (iconImage != null)
        {
            iconImage.color = effect.effectType == StatusEffectType.Buff ?
                Color.green : Color.red;
        }

        // �^�[�����\��
        TextMeshProUGUI turnText = iconObj.GetComponentInChildren<TextMeshProUGUI>();
        if (turnText != null)
        {
            turnText.text = effect.remainingTurns.ToString();
        }

        // �c�[���`�b�v���i��������j
        // TODO: �}�E�X�I�[�o�[���ɏ�Ԉُ�̏ڍׂ�\��
    }

    /// <summary>
    /// ��Ԉُ�A�C�R�����N���A
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
    /// �A�N�e�B�u�C���W�P�[�^�[��ݒ�
    /// </summary>
    public void SetActiveIndicator(bool isActive)
    {
        // �A�N�V�����C���W�P�[�^�[
        if (actionIndicator != null)
        {
            actionIndicator.gameObject.SetActive(isActive);
        }

        // �A�N�e�B�u�t���[��
        if (activeFrame != null)
        {
            activeFrame.SetActive(isActive);
        }

        // �A�N�e�B�u���̃G�t�F�N�g�i��������j
        if (isActive)
        {
            // TODO: ����G�t�F�N�g��g��k���A�j���[�V����
        }
    }

    /// <summary>
    /// �_���[�W�G�t�F�N�g�\���i��������j
    /// </summary>
    public void ShowDamageEffect(int damage)
    {
        // TODO: �_���[�W���l�̕\���A�j���[�V����
        // TODO: �L�����N�^�[�̓_�ŃG�t�F�N�g
        Debug.Log($"{targetCharacter.characterName} �Ƀ_���[�W�G�t�F�N�g�\��: {damage}");
    }

    /// <summary>
    /// �񕜃G�t�F�N�g�\���i��������j
    /// </summary>
    public void ShowHealEffect(int healAmount)
    {
        // TODO: �񕜐��l�̕\���A�j���[�V����
        // TODO: �ΐF�̌��G�t�F�N�g
        Debug.Log($"{targetCharacter.characterName} �ɉ񕜃G�t�F�N�g�\��: {healAmount}");
    }

    /// <summary>
    /// �X�L���g�p�G�t�F�N�g�\���i��������j
    /// </summary>
    public void ShowSkillEffect(string skillName)
    {
        // TODO: �X�L�����̕\��
        // TODO: �X�L���ŗL�̃G�t�F�N�g
        Debug.Log($"{targetCharacter.characterName} ���X�L���g�p: {skillName}");
    }

    private void OnDestroy()
    {
        // �N���[���A�b�v
        ClearStatusEffectIcons();
    }

    /// <summary>
    /// �f�o�b�O���\��
    /// </summary>
    public void ShowDebugInfo()
    {
        if (targetCharacter != null)
        {
            Debug.Log($"CharacterUI: {targetCharacter.characterName} HP:{targetCharacter.currentHP}/{targetCharacter.maxHP} Alive:{targetCharacter.isAlive}");
        }
    }
}