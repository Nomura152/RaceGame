using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class Player : NetworkBehaviour
{
    [Header("コンポーネント群")]
    [Tooltip("Rigidbody"), SerializeField] Rigidbody rb;
    [Tooltip("Animator"), SerializeField] Animator anim;
    [Tooltip("AudioSource"), SerializeField] AudioSource audioSource;
    [Tooltip("GroundChecker"), SerializeField] GroundChecker groundChecker;
    [Tooltip("PlayerAttack"), SerializeField] PlayerAttack playerAttack;
    [Tooltip("AudioPlayer"), SerializeField] AudioPlayer audioPlayer;
    [Tooltip("Collider"), SerializeField] List<Collider> colliders;
    [Tooltip("Renderer"), SerializeField] List<Renderer> renderers;

    [Header("参照")]
    [Tooltip("PlayerCameraのプレハブ"), SerializeField] PlayerCamera playerCameraPrefab;

    [Header("パラメータ")]
    [Tooltip("加速度"), SerializeField] float moveForce = 20f;
    [Tooltip("最高速度"), SerializeField] float maxSpeed = 2.0f;
    [Tooltip("回転力"), SerializeField] float rotForce = 2f;
    [Tooltip("回転のブレーキ"), SerializeField] float rotBrake = 1f;
    [Tooltip("最大適用トルク"), SerializeField] float maxTorque = 1f;
    [Tooltip("ジャンプ力"), SerializeField] float jumpPower = 400f;
    [Tooltip("ダイブの前進力"), SerializeField] float diveForwardForce = 200f;
    [Tooltip("ダイブ時のトルク"), SerializeField] float diveTorque = 200f;
    [Tooltip("足音SEが鳴る間隔"), SerializeField] float footstepDuration = 0.3f;
    [Tooltip("コヨーテタイム"), SerializeField] float coyoteTime = 0.05f;
    [Tooltip("強制リスポーン地点"), SerializeField] float respawnHeight = -10f;

    [Header("デバッグ用")]
    [Tooltip("ワープ先座標"), SerializeField] Vector3 warpPoint;

    /// <summary>
    /// ネットワーク上で共有される、自キャラの移動方向
    /// </summary>
    [Networked] public Vector3 MoveDirection { get; set; }

    /// <summary>
    /// プレイヤーのカメラの回転角度（オイラー角）
    /// </summary>
    [Networked] public Vector3 CameraEulerAngles { get; set; }

    /// <summary>
    /// プレイヤーのジャンプ回数
    /// </summary>
    [Networked] public int JumpCount { get; set; }

    /// <summary>
    /// ネットワーク上で共有する自キャラの接地判定
    /// </summary>
    [Networked] public bool IsGrounded { get; set; }

    /// <summary>
    /// プレイヤーの攻撃回数
    /// </summary>
    [Networked] public int AttackCount { get; set; }

    /// <summary>
    /// プレイヤーのダイブ回数
    /// </summary>
    [Networked] public int DiveCount { get; set; }

    /// <summary>
    /// プレイヤーのダイブ可能フラグ
    /// </summary>
    [Networked] public bool CanDive { get; set; }

    /// <summary>
    /// プレイヤーのコヨーテタイム計測
    /// </summary>
    [Networked] public TickTimer CoyoteTimer { get; set; }

    /// <summary>
    /// 前のTickのボタン入力情報
    /// </summary>
    [Networked] public NetworkButtons PrevButtons { get; set; }

    /// <summary>
    /// プレイヤーのゴール状況
    /// </summary>
    [Networked] public bool IsFinished { get; private set; }

    //シーン上に生成したプレイヤーのカメラ
    PlayerCamera playerCamera;

    //足音SEを鳴らすタイマー
    float footstepTimer = 0f;

    //前のTickの接地状態
    bool prevGround = false;
    //ゲーム上に反映させたジャンプ回数
    int visibleJumpCount = 0;
    //ゲーム上に反映させたダイブ回数
    int visibleDiveCount = 0;
    //ゲーム上に反映させた攻撃回数
    int visibleAttackCount = 0;

    /// <summary>
    /// Runner.Spawn()などで生成された時、全ピアで呼び出される。Start()のネットワーク同期版。
    /// </summary>
    public override void Spawned()
    {
        //演出回数を現在のカウントに揃える
        visibleJumpCount = JumpCount;
        visibleAttackCount = AttackCount;
        visibleDiveCount = DiveCount;

        //ニックネームを表示するUIオブジェクトを生成
        PlayerNameManager.Instance.Register(this);

        //入力権限を持つかどうか
        if (HasInputAuthority)
        {
            //入力権限を持つ自キャラを、ローカルでもRigidbodyをシミュレーションするように設定
            Runner.SetIsSimulated(Object, true);

            //自キャラ用にカメラを生成
            playerCamera = Instantiate(playerCameraPrefab);
            playerCamera.SetTarget(transform);

            //自キャラのオーディオソースの立体音響をOFFにする
            audioSource.spatialBlend = 0f;
        }
        else
        {
            //自キャラでない場合はオーディオソースの立体音響をONにする
            audioSource.spatialBlend = 1f;

            //音量を少し下げる
            audioSource.volume = 0.1f;
        }
    }

    /// <summary>
    /// Runner.Despawn()などで削除された時、全ピアで呼び出される。OnDestroy()のネットワーク同期版。
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="hasState"></param>
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        //ニックネームを表示するUIオブジェクトを削除
        PlayerNameManager.Instance.Unregister(this);
    }

    /// <summary>
    /// PhotonFusionで毎Tickごとに、ホストと入力権限持ちのピアで呼び出される。Update()のネットワーク同期版。
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        //ゴールしているかを確認
        if (IsFinished)
        {
            //Rigidbodyによる動きを止める
            rb.isKinematic = true;

            //リストに登録されているColliderコンポーネントを全て無効化
            foreach(Collider c in colliders)
            {
                c.enabled = false;
            }

            //処理を終了して後は何も操作させない
            return;
        }
        else    //←ロールバックによりゴール状態が巻き戻された時の対策
        {
            rb.isKinematic = false;
            foreach (Collider c in colliders)
            {
                c.enabled = true;
            }
        }

        //-------------------------------------------------------

        //1Tick前の接地状態を保存
        prevGround = IsGrounded;

        //接地判定取得
        IsGrounded = groundChecker.CheckGround();

        //入力データを取得
        bool ok = GetInput(out PlayerInputData input);

        //接地状態から空中状態に切り替わったら
        if (prevGround == true && IsGrounded == false)
        {
            //ダイブ可能にする
            CanDive = true;

            //コヨーテタイム計測開始
            CoyoteTimer = TickTimer.CreateFromSeconds(Runner, coyoteTime);
        }

        //入力データを受け取れた場合、そのデータをもとにプレイヤーを動かす
        if (ok == true)
        {
            ProcessInput(input);
        }

        //落下したら強制リスポーン
        if(transform.position.y < respawnHeight)
        {
            GameMainManager.Instance.RespawnPlayer(this);
        }
    }

    /// <summary>
    /// FixedUpdateNetwork()の後に各ピアで各々呼び出される同期する必要のない処理をここで行う。
    /// </summary>
    public override void Render()
    {
        //ゴールしているかを確認
        if (IsFinished)
        {
            //リストに登録されているColliderコンポーネントを全て無効化
            foreach (Collider c in colliders)
            {
                c.enabled = false;
            }

            //リストに登録されているRendererコンポーネントを全て無効化
            foreach (Renderer r in renderers)
            {
                r.enabled = false;
            }

            //処理を終了して後は何も操作させない
            return;
        }
        else    //←ロールバックによりゴール状態が巻き戻された時の対策
        {
            foreach (Collider c in colliders)
            {
                c.enabled = true;
            }

            foreach (Renderer r in renderers)
            {
                r.enabled = true;
            }
        }

        //-------------------------------------------------------

        //アニメーション
        anim.SetFloat("Speed", MoveDirection.magnitude, 0.1f, Time.deltaTime);  //移動アニメーション
        anim.SetBool("IsGround", IsGrounded);     //空中落下アニメーション

        //ジャンプカウントが更新されていたらジャンプ演出を行う
        if(JumpCount > visibleJumpCount)
        {
            anim.SetTrigger("Jump");
            audioPlayer.PlayAudioOnce("Player_Jump");
            visibleJumpCount = JumpCount;    //カウントをジャンプカウントと同じにする
        }

        //ダイブカウントが更新されていたら、ダイブ演出を行う
        if(DiveCount > visibleDiveCount)
        {
            audioPlayer.PlayAudioOnce("Player_Dive");
            visibleDiveCount = DiveCount;    //カウントをダイブカウントと同じにする
        }

        //攻撃カウントが更新されていたら、攻撃演出を行う
        if(AttackCount > visibleAttackCount)
        {
            anim.SetTrigger("Attack");
            audioPlayer.PlayAudioOnce("Player_Attack01");
            visibleAttackCount = AttackCount;   //カウントを攻撃カウントと同じにする
        }

        //自分が生成したカメラ情報を所持しているか確認
        if (playerCamera != null)
        {
            //カメラの角度を共有パラメータの回転値に変更する
            playerCamera.SetEulerAngles(CameraEulerAngles);
        }

        //足音SE（移動入力あり & 接地中 & 攻撃状態ではない）
        if (MoveDirection.magnitude > 0.1f && IsGrounded && !playerAttack.IsAttacking)
        {
            footstepTimer += Time.deltaTime;

            if(footstepTimer >= footstepDuration)
            {
                audioPlayer.PlayAudioOnce("Player_Footstep");

                //タイマーリセット
                footstepTimer = 0;
            }
        }
    }

    /// <summary>
    /// 受け取ったボタン入力データを、プレイヤーの動作に反映させる
    /// </summary>
    private void ProcessInput(PlayerInputData input)
    {
        //入力値を共有パラメータのカメラ回転方向に加算
        float euler_x = Mathf.Clamp(CameraEulerAngles.x - input.Look.y, -89f, 89f);         //X軸の角度を-89～89度で制限する
        float euler_y = Mathf.Repeat(CameraEulerAngles.y + input.Look.x, 360f);             //Y軸の角度を360度ごとにリピートでリセットして繰り返す
        CameraEulerAngles = new Vector3(euler_x, euler_y, 0);

        //共有パラメータのY軸の回転値を取得
        var cameraRot_Y = Quaternion.Euler(new Vector3(0f, CameraEulerAngles.y, 0f));
        //入力データを移動方向へ変換
        MoveDirection = cameraRot_Y * new Vector3(input.Move.x, 0f, input.Move.y);

        //攻撃（攻撃中の状態でないかつ接地中の場合）
        if (input.Buttons.WasPressed(PrevButtons, InputButtonType.Attack) && !playerAttack.IsAttacking && IsGrounded)
        {
            Attack();
        }

        //攻撃中は他の操作をさせない
        if(!playerAttack.IsAttacking)
        {
            //移動
            Move();

            //回転
            Rotate();

            //ジャンプ可能かを確認（接地中 or コヨーテタイムの時間内であればジャンプ可能）
            bool canJump = (IsGrounded || !CoyoteTimer.ExpiredOrNotRunning(Runner));

            //ジャンプ入力があった場合にジャンプ
            if (input.Buttons.WasPressed(PrevButtons, InputButtonType.Jump) && canJump)
            {
                Jump();
            }

            //ダイブ処理（ダイブ可能の状態かつ接地中でない場合）
            if (input.Buttons.WasPressed(PrevButtons, InputButtonType.Attack) && CanDive && !IsGrounded)
            {
                Dive();
            }
        }

        //最後に使い終わったボタン入力情報を記憶し、次のTickで使用する
        PrevButtons = input.Buttons;
    }


    /// <summary>
    /// プレイヤーの移動処理
    /// </summary>
    private void Move()
    {
        //プレイヤーの横移動(XZ)軸の速度がmaxSpeedに達していなければ、AddForceで加速させる
        var velocity_XZ = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if(velocity_XZ.magnitude < maxSpeed)
        {
            //力を加えて移動
            rb.AddForce(MoveDirection * moveForce);
        }
    }

    /// <summary>
    /// プレイヤーの回転処理
    /// </summary>
    private void Rotate()
    {
        Vector3 dir = MoveDirection;

        //入力値が小さい場合は処理終了
        if (dir.sqrMagnitude < 0.0001f) { return; }
        dir.Normalize();

        //プレイヤーの正面から目的の方向までに、角度が何度あるかを計算
        float differenceAngle = Vector3.SignedAngle(transform.forward, dir, transform.up);

        //目的の方向までの角度差が1度未満なら回転にブレーキをかける
        if (Mathf.Abs(differenceAngle) < 1f)
        {
            float yawRate = Vector3.Dot(rb.angularVelocity, transform.up);
            rb.AddTorque(-transform.up * (yawRate * rotBrake));
            return;
        }

        //角度差が大きいほど大きなトルク量になるようにトルク量を作成
        float torque = rotForce * differenceAngle;

        //最大トルク量に制限をかけて適用
        torque = Mathf.Clamp(torque, -maxTorque, maxTorque);
        rb.AddTorque(transform.up * torque);
    }

    /// <summary>
    /// プレイヤーのジャンプ処理
    /// </summary>
    private void Jump()
    {
        rb.AddForce(jumpPower * Vector3.up);

        //ジャンプ回数を記憶
        JumpCount++;
    }

    /// <summary>
    /// プレイヤーの攻撃処理
    /// </summary>
    private void Attack()
    {
        //攻撃開始
        playerAttack.Attack();

        //攻撃回数を記憶
        AttackCount++;
    }

    /// <summary>
    /// プレイヤーのダイブ処理
    /// </summary>
    private void Dive()
    {
        //前方向に前進
        rb.AddForce(transform.forward * diveForwardForce);

        //前に倒れこむようにトルクによる回転を行う
        rb.AddTorque(transform.right * diveTorque);

        //ダイブ回数を追加
        DiveCount++;

        //ダイブをしたらダイブ可能フラグを切り替える
        CanDive = false;
    }

    /// <summary>
    /// ワープ処理（ホストのみが実行する）
    /// </summary>
    /// <param name="warpPos">ワープ先の座標</param>
    /// <param name="warpRot">ワープ後の回転方向</param>
    public void Warp(Vector3 warpPos, Quaternion warpRot)
    {
        //ホスト以外は処理を行わない
        if (!HasStateAuthority) { return; }

        //速度と回転速度を0に戻す
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        //ワープ先の座標と回転の値を設定
        transform.position = warpPos;
        transform.rotation = warpRot;
    }

    /// <summary>
    /// レース終了時の処理（ホストのみが実行する）
    /// </summary>
    public void FinishRace()
    {
        //ホスト以外は処理を行わない
        if (!HasStateAuthority) { return; }

        //ゴールフラグをtrueにする（Networkedで共有されるため、全員がこのプレイヤーがゴールしていると分かる）
        IsFinished = true;
    }

    [ContextMenu("デバッグ用のワープ")]
    public void DebugWarp()
    {
        Warp(warpPoint, transform.rotation);
    }
}
