using Fusion;
using TMPro;
using UnityEngine;

public class GameMainScreenUI : MonoBehaviour
{
    [Header("シーン参照")]
    [Tooltip("参加者を表示するUI"), SerializeField] PlayerCountUI playerCountUI;
    [Tooltip("レース開始告知テキスト"), SerializeField] TMP_Text readyText;
    [Tooltip("カウントダウン3"), SerializeField] GameObject countdown_3;
    [Tooltip("カウントダウン2"), SerializeField] GameObject countdown_2;
    [Tooltip("カウントダウン1"), SerializeField] GameObject countdown_1;
    [Tooltip("レース開始パネル"), SerializeField] GameObject goPanel;
    [Tooltip("レース終了パネル"), SerializeField] GameObject finishPanel;
    [Tooltip("レース終了理由テキスト"), SerializeField] FinishReasonUI finishReasonUI;

    void Awake()
    {
        //全てのUIを非表示にする
        playerCountUI.gameObject.SetActive(false);
        readyText.gameObject.SetActive(false);
        countdown_3.SetActive(false);
        countdown_2.SetActive(false);
        countdown_1.SetActive(false);
        goPanel.SetActive(false);
        finishPanel.SetActive(false);
        finishReasonUI.gameObject.SetActive(false);
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //GameMainManagerがネットワークオブジェクトとして共有されているかを確認
        if(GameMainManager.Instance.Object == null) { return; }

        //GameMainManagerのオブジェクトが有効になっているかを確認
        if(GameMainManager.Instance.Object.IsValid == false) {  return; }

        //GameMainManagerの状態が待機中であれば、参加者人数を表示するオブジェクトをアクティブにする
        playerCountUI.gameObject.SetActive(GameMainManager.Instance.CurrentState == GameState.Waiting);

        //レース開始カウントダウン
        if(GameMainManager.Instance.CurrentState == GameState.Countdown)
        {
            //レース開始告知テキストを表示（アニメーションによって3秒後に透過されて見えなくなる）
            readyText.gameObject.SetActive(true);

            //GameMainManagerからカウントダウンの残り時間を確認
            NetworkRunner runner = GameMainManager.Instance.Runner;
            float timer = GameMainManager.Instance.CountdownTimer.RemainingTime(runner).GetValueOrDefault();

            //カウントダウンのタイマーが残り3秒以下であれば「3」の画像を表示（アニメーションによって1秒後に透過されて見えなくなる）
            if(timer <= 3f)
            {
                countdown_3.SetActive(true);
            }
            //2秒以下であれば「2」の画像を表示
            if (timer <= 2f)
            {
                countdown_2.SetActive(true);
            }
            //1秒以下であれば「1」の画像を表示
            if (timer <= 1f)
            {
                countdown_1.SetActive(true);
            }
        }

        //GameMainManagerがレース状態になっていればGoのパネルを表示（アニメーションによって1秒後に透過されて見えなくなる）
        if(GameMainManager.Instance.CurrentState == GameState.Racing)
        {
            goPanel.SetActive(true);
        }

        //GameMainManagerのPlayerDataから自分のデータを取得
        PlayerRef playerRef = GameMainManager.Instance.Runner.LocalPlayer;

        //プレイヤーの情報を探す
        if (GameMainManager.Instance.PlayerData.TryGet(GameMainManager.Instance.Runner.LocalPlayer, out var data))
        {
            //自分がゴールしていればFinishのパネルを表示（アニメーションによって1.5秒後に縮小されて見えなくなる）
            //レースが終了した場合でも表示させるようにする
            if (data.IsFinished || GameMainManager.Instance.CurrentState == GameState.Finish)
            {
                finishPanel.SetActive(true);
            }

            //レース終了状態であれば、レース終了テキストを表示
            finishReasonUI.gameObject.SetActive(GameMainManager.Instance.CurrentState == GameState.Finish);
        }
    }
}
