using System;
using UnityEngine;

/// <summary>
/// ユーザーが所有するスキルデータ
/// </summary>
[System.Serializable]
public class UserSkillData
{
    [Header("基本情報")]
    public string userSkillId;      // ユーザー固有スキルID（UUID等）
    public int skillMasterId;       // スキルマスターデータID

    [Header("管理情報")]
    public DateTime acquiredDate;   // 取得日時
    public bool isLocked;          // ロック状態（誤操作防止）
    public bool isNew;             // 新規取得フラグ

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public UserSkillData()
    {
        userSkillId = Guid.NewGuid().ToString();
        acquiredDate = DateTime.Now;
        isLocked = false;
        isNew = true;
    }

    /// <summary>
    /// マスターデータから新規スキルデータを作成
    /// </summary>
    public UserSkillData(SkillMasterData masterData) : this()
    {
        if (masterData != null)
        {
            skillMasterId = masterData.skillId;
        }
    }

    /// <summary>
    /// スキルマスターID指定で作成
    /// </summary>
    public UserSkillData(int skillMasterId) : this()
    {
        this.skillMasterId = skillMasterId;
    }

    /// <summary>
    /// 新規フラグをクリア
    /// </summary>
    public void ClearNewFlag()
    {
        isNew = false;
    }

    /// <summary>
    /// ロック状態を切り替え
    /// </summary>
    public void ToggleLock()
    {
        isLocked = !isLocked;
    }

    /// <summary>
    /// デバッグ用文字列
    /// </summary>
    public override string ToString()
    {
        return $"UserSkill[ID:{userSkillId}, MasterID:{skillMasterId}, New:{isNew}, Locked:{isLocked}]";
    }
}