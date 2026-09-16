using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Fusion;

public class ResultScreenUI : MonoBehaviour
{
    [Header("シーン参照")]
    [Tooltip("見出しのテキスト"), SerializeField] TMP_Text resultText;
    [Tooltip("スクロールビュー"), SerializeField] ScrollRect scrollView;
    [Tooltip("「次のレースへ」ボタン"), SerializeField] Button nextRaceButton;
    [Tooltip("「部屋を抜ける」ボタン"), SerializeField] Button leaveButton;

    [Header("参照")]
    [Tooltip("ランキング表示UI"), SerializeField] RankingEntryUI rankingEntryUI;

    void Awake()
    {
        //UIを全て非表示にする
        resultText.gameObject.SetActive(false);
        scrollView.gameObject.SetActive(false);
        nextRaceButton.gameObject.SetActive(false);
        leaveButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// リザルトの結果を集計して画面の表示を行う処理
    /// </summary>
    public void ShowRanking()
    {
        //------------------------<データ（順位・ニックネーム）の取得>----------------------

        //新しいソート用のリストを作成
        List<PlayerData> sortList = new List<PlayerData>();

        //GameMainManagerのPlayerDataからデータを一つずつソート用のリストに追加する
        foreach(KeyValuePair<PlayerRef, PlayerData> pair in GameMainManager.Instance.PlayerData)
        {
            sortList.Add(pair.Value);
        }

        //取得したデータを順位の小さい順にソートする
        List<PlayerData> sortedList = sortList.OrderBy(data => data.Place).ToList();

        //------------------------------------<UIの表示>------------------------------------

        //UIを全て表示する
        resultText.gameObject.SetActive(true);
        scrollView.gameObject.SetActive(true);

        //各プレイヤーの順位をもとにランキング表示UIを生成
        int rank = 1;
        foreach(var data in sortedList)
        {
            //UIをContentの子オブジェクトとして生成
            RankingEntryUI entryUI = Instantiate(rankingEntryUI, scrollView.content);

            //ニックネームを取得
            string nickname = data.Nickname.ToString();

            //未ゴールあれば最下位にする
            if(!data.IsFinished)
            {
                rank = sortedList.Count;
            }
            else
            {
                //データをUIに流し込む
                entryUI.SetData(rank, nickname);

                //次のプレイヤー用に順位を更新
                rank++;
            }
        }
    }

    /// <summary>
    /// ゲームを続けるか止めるかのボタンを表示させる
    /// </summary>
    public void ShowButtons()
    {
        nextRaceButton.gameObject.SetActive(true);
        leaveButton.gameObject.SetActive(true);
    }
}
