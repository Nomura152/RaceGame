using Fusion;
using UnityEngine;

public class Gimmick : NetworkBehaviour
{
    [Header("オブジェクト削除の位置"), SerializeField] float despawnHeight = -10f;

    /// <summary>
    /// Runner.Spawn()などで生成された時、全ピアで呼び出される。Start()のネットワーク同期版。
    /// </summary>
    public override void Spawned()
    {
        //ホスト以外でもローカル上でRigidbodyをシミュレーションするように設定
        Runner.SetIsSimulated(Object, true);
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
        if (!HasStateAuthority) { return; }

        //座標が指定した値未満になった場合は、オブジェクトを削除する
        if(transform.position.y < despawnHeight)
        {
            Runner.Despawn(Object);
        }
    }

    /// <summary>
    /// FixedUpdateNetwork()の後に各ピアで各々呼び出される同期する必要のない処理をここで行う。
    /// </summary>
    public override void Render()
    {

    }
}
