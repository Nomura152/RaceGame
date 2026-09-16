using Fusion;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class FinishReasonUI : MonoBehaviour
{
    [Header("表示するテキストUI"), SerializeField] TMP_Text text;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //GameMainManagereから必要なデータを取得
        NetworkRunner runner = GameMainManager.Instance.Runner;                 //NetworkRunner
        PlayerRef playerRef = runner.LocalPlayer;                               //自身のプレイヤーID

        //自分が既にゴールしていれば「レース終了」のみを伝える
        if (GameMainManager.Instance.PlayerData.TryGet(GameMainManager.Instance.Runner.LocalPlayer, out var data))
        {
            text.text = "レースが終了しました";
        }
        else
        {
            //GameMainManagerからゴールしていないプレイヤーが何人いるかを確認
            int racingPlayers = 0;
            foreach (KeyValuePair<PlayerRef, PlayerData> pair in GameMainManager.Instance.PlayerData)
            {
                if (!pair.Value.IsFinished)
                {
                    racingPlayers++;
                }
            }

            //ゴールしていない人数が、一人以下であれば理由は「順位確定」によるレース終了だと分かる
            if (racingPlayers <= 1)
            {
                text.text = "順位が確定したためレースを終了しました";
            }
            else
            {
                //それ以外であればタイムアップ
                text.text = "制限時間を超えたためレースを終了しました";
            }
        }
    }
}
