using System;

/// <summary>
/// ステータスプレビューデータクラス
/// 
/// 【責任】
/// - 強化後のステータス予想値と変化量を保持
/// - プレビュー表示用のデータ構造
/// 
/// 【使用箇所】
/// - Enhance_StatusDisplayController（プレビュー表示）
/// - StatusPreviewCalculator（計算結果格納）
/// 
/// 【設計原則】
/// - Data層：UIに依存しない純粋なデータクラス
/// - Immutable：作成後は変更不可
/// - 変化量の正負で色分け表示に対応
/// </summary>
[Serializable]
public class StatusPreviewData
{
    #region Properties

    /// <summary>
    /// ステータス項目名（例：「HP」「攻撃力」「火属性攻撃」など）
    /// </summary>
    public string name { get; private set; }

    /// <summary>
    /// 強化後の予想値
    /// </summary>
    public int afterValue { get; private set; }

    /// <summary>
    /// 変化量（正数：増加、負数：減少、0：変化なし）
    /// </summary>
    public int change { get; private set; }

    /// <summary>
    /// 強化前の値（計算用）
    /// </summary>
    public int beforeValue => afterValue - change;

    /// <summary>
    /// 増加かどうか
    /// </summary>
    public bool IsIncrease => change > 0;

    /// <summary>
    /// 減少かどうか
    /// </summary>
    public bool IsDecrease => change < 0;

    /// <summary>
    /// 変化なしかどうか
    /// </summary>
    public bool IsNoChange => change == 0;

    #endregion

    #region Constructor

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="name">ステータス項目名</param>
    /// <param name="afterValue">強化後の予想値</param>
    /// <param name="change">変化量</param>
    public StatusPreviewData(string name, int afterValue, int change)
    {
        this.name = name ?? throw new ArgumentNullException(nameof(name));
        this.afterValue = afterValue;
        this.change = change;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 表示用文字列を生成（変化量付き）
    /// </summary>
    /// <returns>「{afterValue} (+{change})」形式の文字列</returns>
    public string GetDisplayText()
    {
        if (change == 0)
        {
            return afterValue.ToString();
        }

        string changeText = change > 0 ? $"(+{change})" : $"({change})";
        return $"{afterValue} {changeText}";
    }

    /// <summary>
    /// 変化量のみの表示用文字列を生成
    /// </summary>
    /// <returns>「+{change}」または「{change}」形式の文字列</returns>
    public string GetChangeText()
    {
        if (change == 0) return "";
        return change > 0 ? $"+{change}" : change.ToString();
    }

    /// <summary>
    /// 文字列表現を取得
    /// </summary>
    /// <returns>「{name}: {beforeValue} → {afterValue} ({change})」形式の文字列</returns>
    public override string ToString()
    {
        return $"{name}: {beforeValue} → {afterValue} ({GetChangeText()})";
    }

    /// <summary>
    /// オブジェクトの等価性判定
    /// </summary>
    /// <param name="obj">比較対象オブジェクト</param>
    /// <returns>等価な場合true</returns>
    public override bool Equals(object obj)
    {
        if (obj is StatusPreviewData other)
        {
            return name == other.name &&
                   afterValue == other.afterValue &&
                   change == other.change;
        }
        return false;
    }

    /// <summary>
    /// ハッシュコード取得
    /// </summary>
    /// <returns>ハッシュコード</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(name, afterValue, change);
    }

    #endregion

    #region Validation

    /// <summary>
    /// データの妥当性検証
    /// </summary>
    /// <returns>妥当な場合true</returns>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(name) &&
               afterValue >= 0 &&
               beforeValue >= 0;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// 変化なしのプレビューデータを作成
    /// </summary>
    /// <param name="name">ステータス項目名</param>
    /// <param name="value">現在値（変化後も同じ）</param>
    /// <returns>変化なしのStatusPreviewData</returns>
    public static StatusPreviewData CreateNoChange(string name, int value)
    {
        return new StatusPreviewData(name, value, 0);
    }

    /// <summary>
    /// 増加プレビューデータを作成
    /// </summary>
    /// <param name="name">ステータス項目名</param>
    /// <param name="beforeValue">強化前の値</param>
    /// <param name="increaseAmount">増加量</param>
    /// <returns>増加のStatusPreviewData</returns>
    public static StatusPreviewData CreateIncrease(string name, int beforeValue, int increaseAmount)
    {
        if (increaseAmount < 0)
            throw new ArgumentException("増加量は0以上である必要があります", nameof(increaseAmount));

        return new StatusPreviewData(name, beforeValue + increaseAmount, increaseAmount);
    }

    /// <summary>
    /// 減少プレビューデータを作成
    /// </summary>
    /// <param name="name">ステータス項目名</param>
    /// <param name="beforeValue">強化前の値</param>
    /// <param name="decreaseAmount">減少量（正数で指定）</param>
    /// <returns>減少のStatusPreviewData</returns>
    public static StatusPreviewData CreateDecrease(string name, int beforeValue, int decreaseAmount)
    {
        if (decreaseAmount < 0)
            throw new ArgumentException("減少量は0以上で指定してください", nameof(decreaseAmount));

        int afterValue = Math.Max(0, beforeValue - decreaseAmount);
        int actualChange = afterValue - beforeValue;

        return new StatusPreviewData(name, afterValue, actualChange);
    }

    #endregion
}