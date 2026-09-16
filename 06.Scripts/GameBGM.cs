using Fusion;
using UnityEngine;

public class GameBGM : MonoBehaviour
{
    [Header("コンポーネント群")]
    [Tooltip("AudioSource"), SerializeField] AudioSource audioSource;

    [Header("BGM")]
    [Tooltip("待機中のBGM"), SerializeField] AudioClip bgm_Waiting;
    [Tooltip("レース中のBGM"), SerializeField] AudioClip bgm_Racing;
    [Tooltip("リザルト中のBGM"), SerializeField] AudioClip bgm_Result;

    GameState currentState;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //最初に待機BGMをループ設定で再生
        audioSource.clip = bgm_Waiting;
        audioSource.loop = true;
        audioSource.Play();
        currentState = GameState.Waiting;
    }

    // Update is called once per frame
    void Update()
    {
        //GameMainManagerがネットワークオブジェクトとして共有されているかを確認
        if (GameMainManager.Instance.Object == null) { return; }

        //GameMainManagerのオブジェクトが有効になっているかを確認
        if (GameMainManager.Instance.Object.IsValid == false) { return; }

        //待機終了時にBGMを止める
        if (currentState == GameState.Waiting)
        {
            if(GameMainManager.Instance.CurrentState != GameState.Waiting)
            {
                audioSource.Stop();
                currentState = GameState.Countdown;
            }
        }

        //カウントダウン終了時にレース用BGM再生
        if(currentState == GameState.Countdown)
        {
            if(GameMainManager.Instance.CurrentState == GameState.Racing)
            {
                audioSource.clip = bgm_Racing;
                audioSource.loop = true;
                audioSource.Play();
                currentState = GameState.Racing;
            }
        }

        //ゴール時にBGMを止める
        if(currentState == GameState.Racing)
        {
            NetworkRunner runner = GameMainManager.Instance.Runner;

            //プレイヤーの情報を探す
            if (GameMainManager.Instance.PlayerData.TryGet(runner.LocalPlayer, out var data))
            {
                //自分自身のデータのゴールフラグを確認する
                if (data.IsFinished)
                {
                    audioSource.Stop();
                }
            }
        }

        //レース終了時にBGMを止める
        if(currentState == GameState.Racing)
        {
            if(GameMainManager.Instance.CurrentState == GameState.Finish)
            {
                audioSource.Stop();
                currentState = GameState.Finish;
            }
        }

        //リザルト画面表示時にリザルト画面用BGM再生
        if(currentState == GameState.Finish)
        {
            if(GameMainManager.Instance.CurrentState == GameState.Result)
            {
                audioSource.clip = bgm_Result;
                audioSource.loop = true;
                audioSource.Play();
                currentState = GameState.Result;
            }
        }
    }
}
