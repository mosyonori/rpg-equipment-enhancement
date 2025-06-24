using System;

/// <summary>
/// ステータス表示データクラス
/// 
/// 【責任】
/// - ステータス名と値の組み合わせを保持
/// - 現在ステータス表示用のデータ構造
/// 
/// 【使用箇所】
/// - Enhance_StatusDisplayController（現在ステータス表示）
/// - 各種ステータス一覧表示
/// 
/// 【設計原則】
/// - Data層：UIに依存しない純粋なデータクラス
/// - Immutable：作成後は変更不可
/// - Serializable：Unityエディタでの表示対応
/// </summary>
[Serializable]
public class StatusDisplayData
{
    #region Properties

    /// <summary>
    /// ステータス項目名（例：「HP」「攻撃力」「火属性攻撃」など）
    /// </summary>
    public string name { get; private set; }

    /// <summary>
    /// ステータス値（数値）
    /// </summary>
    public int value { get; private set; }

    #endregion

    #region Constructor

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="name">ステータス項目名</param>
    /// <param name="value">ステータス値</param>
    public StatusDisplayData(string name, int value)
    {
        this.name = name ?? throw new ArgumentNullException(nameof(name));
        this.value = value;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 文字列表現を取得
    /// </summary>
    /// <returns>「{name}: {value}」形式の文字列</returns>
    public override string ToString()
    {
        return $"{name}: {value}";
    }

    /// <summary>
    /// オブジェクトの等価性判定
    /// </summary>
    /// <param name="obj">比較対象オブジェクト</param>
    /// <returns>等価な場合true</returns>
    public override bool Equals(object obj)
    {
        if (obj is StatusDisplayData other)
        {
            return name == other.name && value == other.value;
        }
        return false;
    }

    /// <summary>
    /// ハッシュコード取得
    /// </summary>
    /// <returns>ハッシュコード</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(name, value);
    }

    #endregion

    #region Validation

    /// <summary>
    /// データの妥当性検証
    /// </summary>
    /// <returns>妥当な場合true</returns>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(name) && value >= 0;
    }

    #endregion
}