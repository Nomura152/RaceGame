using Fusion;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerNameManager : MonoBehaviour
{
    [Header("シーン参照")]
    [Tooltip("プレイヤー名を表示するキャンバス"), SerializeField] Canvas canvas;

    [Header("参照")]
    [Tooltip("プレイヤー名を表示するUI"), SerializeField] GameObject namePlatePrefab;

    [Header("パラメータ")]
    [Tooltip("名前表示オフセット"), SerializeField] Vector3 offset = new Vector3(0f, 2f, 0f);
    [Tooltip("プレイヤー名表示範囲"), SerializeField] float maxDisplayRange = 20f;

    [Header("確認用")]
    [Tooltip("プレイヤー名表示UIリスト"), SerializeField] List<PlayerNameData> playerNameDatas = new List<PlayerNameData>();

    //誰でも参照可能にする
    public static PlayerNameManager Instance { get; private set; }

    //表示するUIの情報をまとめた内部クラス（プレイヤーとUIのセット）
    [Serializable]
    public class PlayerNameData
    {
        public Player Player;
        public GameObject UIObject;
        public RectTransform Rect;
        public TMP_Text Text;
    }


    void Awake()
    {
        //シングルトンにして、誰でも参照できるように登録する
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// プレイヤーの名前を表示するUIオブジェクトを生成してリストに登録
    /// </summary>
    /// <param name="player">対象プレイヤー</param>
    public void Register(Player player)
    {
        //プレイヤー名を表示するUIオブジェクトを子オブジェクトとして生成
        var ui = Instantiate(namePlatePrefab, canvas.transform);

        //プレイヤー名表示UIリストに登録するためのデータを作成
        var newData = new PlayerNameData
        {
            Player = player,
            UIObject = ui,
            Rect = ui.GetComponent<RectTransform>(),
            Text = ui.GetComponent<TMP_Text>()
        };

        //リストにデータを追加
        playerNameDatas.Add(newData);

        //初期表示更新
        UpdateNameText(newData);
        UpdateUIPosition(newData);
    }

    /// <summary>
    /// 不要になったUIオブジェクトを削除してリストから削除
    /// </summary>
    /// <param name="player">対象プレイヤー</param>
    public void Unregister(Player player)
    {
        // リストから対象を探して削除
        var data = playerNameDatas.Find(x => x.Player == player);
        if (data != null)
        {
            //UIオブジェクト削除
            Destroy(data.UIObject);

            //リストからデータを削除
            playerNameDatas.Remove(data);
        }
    }

    /// <summary>
    /// プレイヤー名表示のUIをまとめて更新
    /// </summary>
    private void LateUpdate()
    {
        //GameMainManagerがネットワークオブジェクトとして共有されているかを確認
        if (GameMainManager.Instance.Object == null) { return; }

        //GameMainManagerのオブジェクトが有効になっているかを確認
        if (GameMainManager.Instance.Object.IsValid == false) { return; }

        //カメラがない場合、処理を行わない
        if (Camera.main == null) return;

        //登録されている全プレイヤーのUIを毎フレーム更新
        foreach (var data in playerNameDatas)
        {
            UpdateNameText(data);
            UpdateUIPosition(data);

            //プレイヤーがゴールしていればUIを非表示にする
            if (data.Player.IsFinished)
            {
                data.UIObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// プレイヤーのニックネームを更新する
    /// </summary>
    /// <param name="data"></param>
    private void UpdateNameText(PlayerNameData data)
    {
        //登録されているプレイヤーオブジェクトからPlayerRefを取得
        PlayerRef playerRef = data.Player.Object.InputAuthority;

        //ディクショナリに欲しいデータが存在するかを確認（遅延によりデータが無い可能性を考慮）
        if (GameMainManager.Instance.PlayerData.ContainsKey(playerRef))
        {
            //GameMainManagerのPlayerDataから対象プレイヤーのニックネームを取得
            string nickname = GameMainManager.Instance.PlayerData[playerRef].Nickname.Value;

            //ニックネームをテキストに反映
            data.Text.text = nickname;
        }
    }

    /// <summary>
    /// プレイヤー名のUIの位置を更新
    /// </summary>
    /// <param name="data"></param>
    private void UpdateUIPosition(PlayerNameData data)
    {
        //プレイヤーの座標をオフセット込みで取得
        Vector3 targetPos = data.Player.transform.position + offset;

        //プレイヤーの座標がゲーム画面上のピクセルで見た時にどの位置に存在するかを取得
        Vector3 screenPos = Camera.main.WorldToScreenPoint(targetPos);

        // カメラの後ろ or 離れすぎていたらUIオブジェクトを非表示
        if (screenPos.z < 0 || screenPos.z > maxDisplayRange)
        {
            //UIオブジェクトを非アクティブにする
            data.UIObject.SetActive(false);
        }
        else
        {
            //UIオブジェクトをアクティブにする
            data.UIObject.SetActive(true);

            //UIの位置を対象プレイヤーの頭上に来るように位置調整
            data.Rect.position = screenPos;
        }
    }

}
