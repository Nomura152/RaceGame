using Fusion;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Level : NetworkBehaviour
{
    [Header("ウェイポイント")]
    [Tooltip("スタートライン"), SerializeField] StartGate startGate;
    [Tooltip("ゴールライン"), SerializeField] FinishGate finishGate;
    [Tooltip("リスポーン地点リスト"), SerializeField] List<RespawnPoint> respawnPoints;

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

    }

    /// <summary>
    /// FixedUpdateNetwork()の後に各ピアで各々呼び出される。同期する必要のない処理をここで行う。
    /// </summary>
    public override void Render()
    {

    }

    /// <summary>
    /// プレイヤーキャラクターのスタートのリスポーン地点を返却する
    /// </summary>
    /// <returns></returns>
    public RespawnPoint GetRespawnPoint()
    {
        //スタート地点のリスポーン地点を返す
        return respawnPoints[0];
    }

    /// <summary>
    /// 番号にあったリスポーン地点を返却する
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public RespawnPoint GetRespawnPoint(int index)
    {
        return respawnPoints[index];
    }

    /// <summary>
    /// リスポーン地点が何番目かを取得する
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public int GetRespawnIndex(RespawnPoint point)
    {
        //IndexOf()メソッドで引数のリスポーン地点が、リストの何番目に存在するかを取得する
        int index = respawnPoints.IndexOf(point);

        //リストに存在しない場合、-1が返されるので、0に調整する
        if(index < 0) { index = 0; }

        return index;
    }

    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        //リストに登録したシーン上のリスポーン地点に番号を表示する
        for(int i = 0; i< respawnPoints.Count; i++)
        {
            Vector3 pos = respawnPoints[i].transform.position + Vector3.up;
            string text = i + "番目";

            Handles.Label(pos, text);
        }
#endif
    }
}
