using Fusion;
using UnityEngine;

public class StartGate : NetworkBehaviour
{
    [Header("スタートをさえぎる壁オブジェクト"), SerializeField] NetworkObject startBlocker;

    /// <summary>
    /// Runner.Spawn()などで生成された時、全ピアで呼び出される。Start()のネットワーク同期版。
    /// </summary>
    public override void Spawned()
    {

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
        //ホスト以外は処理を行わない
        if (!HasStateAuthority) { return; }

        //レース中の状態になっているかを確認
        if (GameMainManager.Instance.CurrentState == GameState.Racing)
        {
            //スタートを遮る壁オブジェクトが残っていればオブジェクトを削除
            if(startBlocker != null)
            {
                Runner.Despawn(startBlocker);
            }
        }
    }

    /// <summary>
    /// FixedUpdateNetwork()の後に各ピアで各々呼び出される。同期する必要のない処理をここで行う。
    /// </summary>
    public override void Render()
    {

    }
}
