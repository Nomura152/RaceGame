using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

public class GroundChecker : NetworkBehaviour
{
    [Header("コンポーネント群")]
    [Tooltip("Rigidbody"), SerializeField] Rigidbody rb;
    [Tooltip("CapsuleCollider"), SerializeField] CapsuleCollider col;

    [Header("パラメータ")]
    [Tooltip("地面判定とするレイヤー"), SerializeField] LayerMask groundLayer;
    [Tooltip("地面判定とする地面の角度"), SerializeField] float slopeLimit = 50f;
    [Tooltip("地面との接触判定範囲"), SerializeField] float extent = 0.02f;

    Collider[] hits = new Collider[5];

    /// <summary>
    /// オーバーラップを用いた地面接触判定
    /// </summary>
    /// <returns></returns>
    public bool CheckGround()
    {
        //接触判定に必要な球状の接地判定の情報を作成する
        var offset = transform.up * col.radius;   //プレイヤーがコロコロ転がるのでその分を考慮
        var pos = rb.transform.position + offset;  //カプセルの足元の中心
        var radius = col.radius + extent;    //カプセルの半径

        //Runnerが持つ物理シーンの中で、球状の判定がオブジェクトに接触しているかを確認（hits[]配列に接触コライダーのデータが格納される）
        int hitCount = Runner.GetPhysicsScene().OverlapSphere(pos, radius, hits, groundLayer, QueryTriggerInteraction.Ignore);

        //hitCountが1以上なら何かと接触している
        if (hitCount > 0)
        {
            //配列に格納された接触コライダーを全てチェック（最大で5つ）
            for (int i = 0; i < hitCount; i++)
            {
                //プレイヤー側のコライダー情報（座標・回転）
                var playerCol = col;
                var playerPos = rb.transform.position - rb.transform.up * extent; //判定をわずかに下にずらして無理やり地面に埋める
                var playerRot = rb.transform.rotation;

                //地面側のコライダー情報（座標・回転）
                var groundCol = hits[i];
                var groundPos = hits[i].transform.position;
                var groundRot = hits[i].transform.rotation;

                //取得可能な情報
                Vector3 normal = Vector3.up;
                float distance = 0f;    //今回は使用しない

                //プレイヤーのコライダーと地面のコライダーがめり込んでいるかを確認し、押し戻す方向と押し戻す距離を取得する
                bool hit = Physics.ComputePenetration(playerCol, playerPos, playerRot, groundCol, groundPos, groundRot, out normal, out distance);

                //めり込んでいたら
                if (hit)
                {
                    //押し戻す方向を地面との法線とし、坂道の角度の計算に使用する
                    float differenceAngle = Vector3.Angle(normal, Vector3.up);

                    //スロープの限界以内の値であれば、接地判定とみなす
                    if (differenceAngle <= slopeLimit)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        //表示に必要な情報がない場合は処理させない
        if (rb == null) { return; }
        if (col == null) { return; }

        //判定のベースとなる球体(黄色)
        var offset = transform.up * col.radius;   //プレイヤーがコロコロ転がるのでその分を考慮
        var pos = rb.transform.position + offset;  //カプセルの足元の中心
        var radius = col.radius;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, radius);

        //実際の判定となるライン(赤)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, radius + extent);
    }
}