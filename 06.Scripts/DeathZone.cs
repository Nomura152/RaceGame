using Fusion;
using UnityEngine;

public class DeathZone : NetworkBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        //ホスト以外は処理をさせない
        if (!Runner.IsServer) { return; }

        //ヒットした相手のオブジェクトから「Player」コンポーネントを取得する
        Player player = other.gameObject.GetComponent<Player>();

        //取得できた（コンポーネントが付いている）ならリスポーン
        if(player != null )
        {
            GameMainManager.Instance.RespawnPlayer(player);
        }
    }
}
