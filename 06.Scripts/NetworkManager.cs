using Fusion.Sockets;   //Fusion関連のプログラムに必要
using Fusion;           //Fusion関連のプログラムに必要
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// ネットワークの接続状態を管理するEnum
public enum ConnectionState
{
    Disconnected,   // 切断中（待機中）
    Connecting,     // 接続試行中（連打禁止区間）
    Connected       // 接続完了
}

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("NetworkRunnerのプレハブ"), SerializeField] NetworkRunner runnerPrefab;
    [Header("ゲームの最大参加人数（無料版は20人まで）"), SerializeField] int maxPlayers = 10;
    [Header("Runner起動後にロードするシーン"), SerializeField] SceneRef startScene;

    [Header("プレイヤーのプレハブ"), SerializeField] NetworkPrefabRef playerPrefab;

    NetworkRunner runner;

    // 現在の接続状態
    public ConnectionState CurrentState { get; private set; } = ConnectionState.Disconnected;

    //どこからでもアクセスできるようにする
    public static NetworkManager Instance { get; private set; }

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// テスト用にホストとしてセッション接続する
    /// </summary>
    [ContextMenu("テスト用にホストとしてセッション接続する")]
    public void TestConnect()
    {
        ConnectAsync_Host("TestRoom");
    }

    /// <summary>
    /// ホストのセッション接続を行う非同期メソッド
    /// </summary>
    /// <param name="roomName">作成するルーム名</param>
    public async void ConnectAsync_Host(string roomName)
    {
        //待機中以外（接続中や接続済み）なら処理を中断する
        if (CurrentState != ConnectionState.Disconnected)
        {
            Debug.LogWarning("現在は接続処理を受け付けていません。状態: " + CurrentState);
            return;
        }

        //状態を「接続中」に変更
        CurrentState = ConnectionState.Connecting;

        //--------------------------------------------

        //NetworkRunnerを生成
        runner = Instantiate(runnerPrefab);

        //NetworkRunnerに自身のインターフェースを登録する
        runner.AddCallbacks(this);

        //NetworkRunner開始時に必要な情報をまとめた構造体を作成する
        var args = new StartGameArgs();
        args.GameMode = GameMode.Host;  //参加者の状態（ホスト）
        args.SessionName = roomName;    //作成ルーム名
        args.PlayerCount = maxPlayers;   //最大参加人数
        args.Scene = startScene;        //Runner起動後にロードするシーン

        //NetworkRunnerにargsを引数で渡して起動（非同期）
        var result = await runner.StartGame(args);

        //起動した結果がresult変数に入っているので確認
        if (result.Ok)
        {
            Debug.Log("セッション接続完了");

            //runnerのプレイヤー入力権限フラグをtrueにする
            runner.ProvideInput = true;

            //成功したので状態を「接続済み」にする
            CurrentState = ConnectionState.Connected;
        }
        else
        {
            Debug.LogWarning($"セッション接続に失敗しました。失敗原因→{result.ShutdownReason}");

            //失敗したので状態を「切断中」に戻して再接続できるようにする
            CurrentState = ConnectionState.Disconnected;
        }
    }

    /// <summary>
    /// クライアントのセッション接続を行う非同期メソッド
    /// </summary>
    /// <param name="roomName">参加するルーム名</param>
    public async void ConnectAsync_Client(string roomName)
    {
        //待機中以外（接続中や接続済み）なら処理を中断する
        if (CurrentState != ConnectionState.Disconnected)
        {
            Debug.LogWarning("現在は接続処理を受け付けていません。状態: " + CurrentState);
            return;
        }

        //状態を「接続中」に変更
        CurrentState = ConnectionState.Connecting;

        //--------------------------------------------

        //NetworkRunnerを生成
        runner = Instantiate(runnerPrefab);

        //NetworkRunnerに自身のインターフェースを登録する
        runner.AddCallbacks(this);

        //NetworkRunner開始時に必要な情報をまとめた構造体を作成する
        var args = new StartGameArgs();
        args.GameMode = GameMode.Client;    //参加者の状態（クライアント）
        args.SessionName = roomName;        //作成ルーム名

        //NetworkRunnerにargsを引数で渡して起動（非同期）
        var result = await runner.StartGame(args);

        //起動した結果がresult変数に入っているので確認
        if (result.Ok)
        {
            Debug.Log("セッション接続完了");

            //runnerのプレイヤー入力権限フラグをtrueにする
            runner.ProvideInput = true;

            //成功したので状態を「接続済み」にする
            CurrentState = ConnectionState.Connected;
        }
        else
        {
            Debug.LogWarning($"セッション接続に失敗しました。失敗原因→{result.ShutdownReason}");

            //失敗したので状態を「切断中」に戻して再接続できるようにする
            CurrentState = ConnectionState.Disconnected;
        }
    }


    //============================<INetworkRunnerCallbacksインターフェースのメソッド>=======================================


    //自分を含む誰かがセッション（ルーム）に参加したときに全員呼ばれる
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log(player + "がセッションに参加しました");

        //自分がサーバー（ホスト）の役割をになっていれば、代表してプレイヤーを生成
        if (runner.IsServer)
        {
            //プレイヤーを生成して参加者全員と共有する
            Vector3 spawnPos = new Vector3(0f, 3f, 0f);
            //runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
        }
    }

    //自分を含む誰かがセッション（ルーム）を離脱/切断したときに全員呼ばれる
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log(player + "がセッションから退出しました");
    }

    //プレイヤーの入力情報を「NetworkInput」に登録して、他全プレイヤーに共有する（※入力の登録はInputController側で行う）
    public void OnInput(NetworkRunner runner, NetworkInput input) { }

    //ラグ等で入力情報が抜けてしまったときにどうするかを決める
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    //自分がセッションを終了するときに呼ばれる
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log("自分がセッションを終了しました。理由→" + shutdownReason);

        //マウスカーソルを再表示させる
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        //タイトルシーンに戻す
        SceneManager.LoadScene("TitleScene");

        //切断されたので状態を「Disconnected」に戻す
        CurrentState = ConnectionState.Disconnected;
    }

    //クライアント側がセッションに接続できた時に呼ばれる
    public void OnConnectedToServer(NetworkRunner runner) { }

    //クライアント側がセッションから離脱/切断された時に呼ばれる
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }

    //セッションに新規参加者が来た時にホストのみ呼ばれる
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    //クライアント側がセッションの接続に失敗した時に呼ばれる
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

    //小さなデータを送信した時に呼ばれる（2.1.0で廃止予定）
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    //runner.JoinSessionLobby()関数を実行後に作成済みルーム一覧が届く
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    //サーバーから追加のデータを受け取る（サーバー用）
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    //Hostモードでホストが落ちた時に呼び出される
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    //Photonで繋がっている時に、シーンの遷移が完了した時に呼ばれる
    public void OnSceneLoadDone(NetworkRunner runner) { }

    //Photonで繋がっている時に、シーンの遷移を開始した時に呼ばれる
    public void OnSceneLoadStart(NetworkRunner runner) { }

    //関心領域（シーン上のオブジェクトを同期する領域）から抜けた時に呼ばれる（あまり使わない）
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    //関心領域（シーン上のオブジェクトを同期する領域）に入った時に呼ばれる（あまり使わない）
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    //ネットワークを通して大きめのデータを送信するrunner.SendReliableData()関数より、データを受け取った時に呼ばれる
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

    //runner.SendReliableData()関数によるデータの受信進捗度
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
