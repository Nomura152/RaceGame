using Fusion;
using UnityEngine;

public class ObjectMover : NetworkBehaviour
{
    [Header("コンポーネント群")]
    [Tooltip("Rigidbody"), SerializeField] Rigidbody rb;

    [Header("パラメータ")]
    [Tooltip("目的地座標"), SerializeField] Vector3 destination;
    [Tooltip("片道の移動時間"), SerializeField, Min(0.1f)] float moveTime;
    [Tooltip("往復時間のオフセット"), SerializeField] float timeOffset;

    //ゲーム開始時のオブジェクトの初期位置
    [Networked] private Vector3 InitPosition { get; set; }

    /// <summary>
    /// Runner.Spawn()などで生成された時、全ピアで呼び出される。Start()のネットワーク同期版。
    /// </summary>
    public override void Spawned()
    {
        //ホスト以外は処理を行わない
        if (!HasStateAuthority) { return; }

        InitPosition = transform.position;
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
        //ネットワーク上での経過時間をもとに、Pingpongメソッドで使用する時間の値を計算する
        float time = (Runner.SimulationTime + timeOffset) / moveTime;

        //計算した時間をもとに、往復する0～1の割合の数値を取得する
        float ratio = Mathf.PingPong(time , 1f);

        //スタート地点～目的地への距離を、0～1の割合にした時の位置を取得する
        Vector3 pos = Vector3.Lerp(InitPosition, InitPosition + destination, ratio);

        //物理挙動を考慮した座標移動
        rb.MovePosition(pos);
    }

    /// <summary>
    /// FixedUpdateNetwork()の後に各ピアで各々呼び出される。同期する必要のない処理をここで行う。
    /// </summary>
    public override void Render()
    {

    }

    void OnDrawGizmos()
    {


        Vector3 startPos = transform.position;
        Vector3 endPos = transform.position + destination;

        //ゲーム再生時は、initPositionの値を使用する
        if(Application.isPlaying)
        {
            //オブジェクトがネットワーク上で共有されていなければ処理しない
            if (Object != null && Object.IsValid)
            {
                startPos = InitPosition;
                endPos = InitPosition + destination;
            }
        }

        //スタート地点からゴール地点までの道のりを画面上に描画
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startPos, endPos);
        Gizmos.DrawSphere(startPos, 0.3f);
        Gizmos.DrawSphere(endPos, 0.3f);
    }
}
