using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;


public class InputController : NetworkBehaviour, IBeforeUpdate
{
    [Header("PlayerInput"), SerializeField] PlayerInput playerInput;

    [Header("パラメータ")]
    [Tooltip("マウス視点移動の操作感度"), SerializeField] float MouseLookSensitivity = 0.3f;
    [Tooltip("ゲームパッドの右スティックによる視点移動の操作感度"), SerializeField] float GamepadLookSensitivity = 200f;

    //入力データ
    Vector2 moveInput = Vector2.zero;
    Vector2 lookInput = Vector2.zero;
    bool jumpButtonPressed;
    bool attackButtonPressed;

    /// <summary>
    /// Runner.Spawn()などで生成された時、全ピアで呼び出される。Start()のネットワーク同期版。
    /// </summary>
    public override void Spawned()
    {
        //自分が入力権限を持っていないなら処理終了
        if (HasInputAuthority == false) {  return; }

        //NetworkEventsにOnInput()を登録して、自動で呼び出されるようにする
        var networkEvents = Runner.GetComponent<NetworkEvents>();
        networkEvents.OnInput.AddListener(OnInput);

        //マウスカーソルを非表示にする
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Runner.Despawn()などで削除された時、全ピアで呼び出される。OnDestroy()のネットワーク同期版。
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="hasState"></param>
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        //自分が入力権限を持っていないなら処理終了
        if (HasInputAuthority == false) {  return; }

        //NetworkEventsに登録したOnInput()を削除する
        var networkEvents = runner.GetComponent<NetworkEvents>();
        networkEvents.OnInput.RemoveListener(OnInput);
    }

    /// <summary>
    /// ネットワーク上の更新処理が実行される前に呼び出される
    /// ネットワーク上の更新処理が遅れている場合、その間に何度も呼び出される
    /// </summary>
    public void BeforeUpdate()
    {
        //入力権限がない場合は処理終了
        if (HasInputAuthority == false) { return; }

        //移動方向入力
        moveInput = playerInput.currentActionMap["Move"].ReadValue<Vector2>();

        //マウスによるカメラ入力（感度を掛け算）
        lookInput += playerInput.currentActionMap["Look"].ReadValue<Vector2>() * MouseLookSensitivity;

        //ゲームパッドの右スティックによるカメラ入力（感度とデルタタイムを掛け算）
        lookInput += playerInput.currentActionMap["Look_Gamepad"].ReadValue<Vector2>() * GamepadLookSensitivity * Time.deltaTime;

        //ジャンプ入力
        jumpButtonPressed = playerInput.currentActionMap["Jump"].IsPressed();

        //攻撃入力
        attackButtonPressed = playerInput.currentActionMap["Attack"].IsPressed();
    }

    /// <summary>
    /// INetworkRunnerCallbacksで定義されているOnInput()と同じメソッド
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="networkInput"></param>
    private void OnInput(NetworkRunner runner, NetworkInput networkInput)
    {
        //入力データ作成
        var data = new PlayerInputData();
        data.Move = moveInput;
        data.Look = lookInput;
        data.Buttons.Set(InputButtonType.Jump, jumpButtonPressed);
        data.Buttons.Set(InputButtonType.Attack, attackButtonPressed);

        //入力データをNetworkInputに登録
        networkInput.Set(data);

        //入力データをリセット
        lookInput = Vector2.zero;
    }
}
