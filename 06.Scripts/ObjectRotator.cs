using Fusion;
using UnityEngine;

public class ObjectRotator : NetworkBehaviour
{
    [Header("コンポーネント群")]
    [Tooltip("Rigidbody"), SerializeField] Rigidbody rb;

    [Header("パラメータ")]
    [Tooltip("各軸の回転速度"), SerializeField] Vector3 rotateVelocity;
    [Tooltip("時間のオフセット"), SerializeField] float timeOffset;

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
        //ネットワーク上での経過時間をもとに、回転で使用する時間の値を計算する
        float time = Runner.SimulationTime + timeOffset;

        //ゲームの経過時間を回転値に掛け算する
        Vector3 rotateEuler = rotateVelocity * time;

        //経過時間に合わせた角度のクォータニオンを作成
        Quaternion rot = Quaternion.Euler(rotateEuler);

        //物理挙動を考慮した回転
        rb.MoveRotation(rot);
    }

    /// <summary>
    /// FixedUpdateNetwork()の後に各ピアで各々呼び出される。同期する必要のない処理をここで行う。
    /// </summary>
    public override void Render()
    {

    }
}
