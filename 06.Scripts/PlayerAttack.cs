using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class PlayerAttack : NetworkBehaviour
{
    [Header("コンポーネント群")]
    [Tooltip("Rigidbody"), SerializeField] Rigidbody rb;
    [Tooltip("AudioPlayer"), SerializeField] AudioPlayer audioPlayer;

    [Header("パラメータ")]
    [Tooltip("攻撃開始までのディレイ時間"), SerializeField] float delayTime = 0.1f;
    [Tooltip("攻撃判定の持続時間"), SerializeField] float activeTime = 0.05f;
    [Tooltip("攻撃中の時間"), SerializeField] float totalTime = 1f;
    [Tooltip("攻撃判定の座標や吹っ飛ばし方向の基準となるオブジェクト"), SerializeField] Transform attackPoint;
    [Tooltip("攻撃判定となる球体の半径"), SerializeField] float radius = 0.5f;
    [Tooltip("攻撃判定を取るレイヤー"), SerializeField] LayerMask attackedLayer;
    [Tooltip("攻撃で吹っ飛ばす力"), SerializeField] float knockback = 500f;

    /// <summary>
    /// ネットワーク上で共有する攻撃時のタイマー
    /// </summary>
    [Networked] private TickTimer attackTimer { get; set; }

    /// <summary>
    /// 攻撃中かどうかを示すフラグ（タイマーの時間が残っている場合は攻撃中とみなす）
    /// </summary>
    public bool IsAttacking { get { return attackTimer.ExpiredOrNotRunning(Runner) == false; } }

    /// <summary>
    /// 吹っ飛ばした相手を記憶するネットワーク共有専用配列（最大8つまで格納可能）
    /// </summary>
    [Networked, Capacity(8)] private NetworkArray<NetworkId> AttackHits => default;

    /// <summary>
    /// 吹っ飛ばした相手のカウント数
    /// </summary>
    [Networked] private int attackHitCount { get; set; }


    //攻撃判定にヒットしたコライダーを格納する配列
    Collider[] hits = new Collider[5];

    //攻撃ヒット演出回数
    int visibleAttackHitCount = 0;

    /// <summary>
    /// Runner.Spawn()などで生成された時、全ピアで呼び出される。Start()のネットワーク同期版。
    /// </summary>
    public override void Spawned()
    {
        //演出回数を現在のカウントに揃える
        visibleAttackHitCount = attackHitCount;
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
        //タイマーの残り時間が0秒 or タイマー未登録であれば処理しない
        if (attackTimer.ExpiredOrNotRunning(Runner)) { return; }

        //攻撃判定出現に必要な時間を計算
        float remainingTime = attackTimer.RemainingTime(Runner).GetValueOrDefault();      //ティックタイマーの残り時間取得
        float startTime = totalTime - delayTime;                //攻撃判定出現開始時間
        float endTime = totalTime - delayTime - activeTime;     //攻撃判定出現終了時間

        //開始～終了時間内だけ攻撃判定を出現させる
        if (startTime >= remainingTime && remainingTime >= endTime)
        {
            //OnTrigger系のネットワーク共有版はないので、Overlapで判定を取る

            //Runnerが持つ物理シーンの中で、球状の判定がオブジェクトに接触しているかを確認（hits[]配列に接触コライダーのデータが格納される）
            int hitCount = Runner.GetPhysicsScene().OverlapSphere(attackPoint.position, radius, hits, attackedLayer, QueryTriggerInteraction.Ignore);

            //hitCountが1以上なら何かと接触している
            if (hitCount > 0)
            {
                for(int i = 0; i < hitCount; i++)
                {
                    //ヒットしたコライダーにRigidbodyが付いているか確認
                    if (hits[i].attachedRigidbody != null)
                    {
                        //ヒットしたRigidbodyが自分自身のものなら何もしない
                        if(hits[i].attachedRigidbody == rb) { continue; }

                        //相手のオブジェクトからNetworkObjectコンポーネントを取得する
                        var networkOb = hits[i].attachedRigidbody.GetComponent<NetworkObject>();

                        //NetworkObjectが無いとゲーム状態を共有するためのIDを取得できないため、何もしない
                        if (networkOb == null) { continue; }

                        //ネットワーク上で共有する配列（要素は8つ）をfor文で探索して同じIDが既に存在するかを確認
                        bool exist = false;
                        for(int j = 0; j <AttackHits.Length; j++)
                        {
                            //同じIDが配列に存在していればフラグをtrueに変更
                            if (AttackHits[j] == networkOb)
                            {
                                exist = true; 
                                break;
                            }
                        }

                        //配列の中に同じIDがあれば既に吹っ飛ばしたことになるので処理しない
                        if (exist) { continue; }

                        //ネットワーク上で共有する配列（要素は8つ）をfor文で探索して空いているスロット（要素番号）を確認
                        for (int j = 0; j < AttackHits.Length; j++)
                        {
                            //スロットが空（null）ならIDを登録して吹っ飛ばす
                            if (!AttackHits[j].IsValid)
                            {
                                //attackPointの方向に吹っ飛ばす
                                var direction = attackPoint.forward;
                                hits[i].attachedRigidbody.AddForce(direction * knockback);

                                //吹っ飛ばした相手のIDを配列に記憶させておく
                                AttackHits.Set(j, networkOb.Id);

                                //攻撃ヒットカウントを加算
                                attackHitCount++;

                                //登録したらもうfor文を探索する必要は無いためbreakで抜ける
                                break;
                            }
                        }
                    }
                }
            }
        }


    }

    /// <summary>
    /// FixedUpdateNetwork()の後に各ピアで各々呼び出される。同期する必要のない処理をここで行う。
    /// </summary>
    public override void Render()
    {
        //攻撃ヒットカウントが更新されていたら、攻撃演出を行う
        if (attackHitCount > visibleAttackHitCount)
        {
            audioPlayer.PlayAudioOnce("Player_AttackHit");
            visibleAttackHitCount = attackHitCount;    //カウントを攻撃ヒットカウントと同じにする
        }
    }

    /// <summary>
    /// パンチ開始
    /// </summary>
    public void Attack()
    {
        //ネットワーク上で共有するティックタイマーを作成
        attackTimer = TickTimer.CreateFromSeconds(Runner, totalTime);

        //攻撃で吹っ飛ばしたオブジェクトのリストの中身を空にする
        AttackHits.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        //表示に必要な情報がない場合は処理させない
        if(attackPoint == null) { return; }

        //攻撃判定を画面上に水色の球体で表示
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(attackPoint.position, radius);
    }
}
