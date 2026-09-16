using TMPro;
using UnityEngine;

public class RankingEntryUI : MonoBehaviour
{
    [Header("順位のテキスト"), SerializeField] TMP_Text rankText;
    [Header("ニックネームのテキスト"), SerializeField] TMP_Text nicknameText;

    /// <summary>
    /// 外部から順位とニックネームを設定する
    /// </summary>
    /// <param name="rank">順位</param>
    /// <param name="nickname">ニックネーム</param>
    public void SetData(int rank, string nickname)
    {
        rankText.text = rank.ToString();
        nicknameText.text = nickname;
    }
}
