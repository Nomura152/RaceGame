using Fusion;
using UnityEngine;

public class FinishGate : NetworkBehaviour
{
    /// <summary>
    /// Runner.Spawn()などで生成された時、全ピアで呼び出される。Start()のネットワーク同期版。
    /// </summary>
    public override void Spawned()
    {
        //設定ミスを防ぐためオブジェクト生成時にコライダーをトリガーに変更する
        //finishTrigger.isTrigger = true;
    }

    /// <summary>
    /// Runner.Despawn()などで削除された時、全ピアで呼び出される。OnDestroy()のネットワーク同期版。
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="hasState"></param>
    public override void Despawned(NetworkRunner runner, bool hasState)
    {

    }

    /// <summary>
    /// PhotonFusionで毎Tickごとに、ホストと入力権限持ちのピアで呼び出される。Update()のネットワーク同期版。
    /// </summary>
    public override void FixedUpdateNetwork()
    {

    }

    /// <summary>
    /// FixedUpdateNetwork()の後に各ピアで各々呼び出される。同期する必要のない処理をここで行う。
    /// </summary>
    public override void Render()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        //ホスト以外は処理をさせない
        if (!Runner.IsServer) { return; }

        //ヒットした相手のオブジェクトから「Player」コンポーネントを取得する
        Player player = other.gameObject.GetComponent<Player>();

        //取得できた（コンポーネントが付いている）ならゴール判定にする
        if (player != null)
        {
            GameMainManager.Instance.GoalPlayer(player);
        }
    }
}
