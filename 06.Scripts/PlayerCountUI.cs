using System.Linq;
using TMPro;
using UnityEngine;

public class PlayerCountUI : MonoBehaviour
{
    [Header("画面上に表示するUI")]
    [SerializeField] TMP_Text text;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //GameMainManagerがネットワークオブジェクトとして共有されているかを確認
        if (GameMainManager.Instance.Object == null) { return; }

        //GameMainManagerのオブジェクトが有効になっているかを確認
        if (GameMainManager.Instance.Object.IsValid == false) { return; }

        //現在のプレイヤー人数を取得
        int joinedPlayers = GameMainManager.Instance.PlayerData.Count;

        //セッションの最大参加人数（レース開始するための人数）を取得
        int maxPlayers = GameMainManager.Instance.Runner.SessionInfo.MaxPlayers;

        //テキストを更新する
        text.text = "レース参加者を待っています..." + joinedPlayers + " / " + maxPlayers;
    }
}
