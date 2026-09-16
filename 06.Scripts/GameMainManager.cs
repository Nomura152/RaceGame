using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//ゲーム状態
public enum GameState
{
    Waiting,            // 待機中
    Countdown,          // カウントダウン
    Racing,             // レース中
    Finish,             // 全員ゴールorタイムアップ
    Result,             // リザルト表示
    NextActionSelect    // レースを続けるか部屋を抜けるかを選択する   
}

public class GameMainManager : NetworkBehaviour
{
    [Header("シーン参照")]
    [Tooltip("リザルト画面"), SerializeField] ResultScreenUI resultScreenUI;

    [Header("参照")]
    [Tooltip("プレイヤーのプレハブ"), SerializeField] NetworkPrefabRef playerPrefab;
    [Tooltip("ステージリスト"), SerializeField] List<NetworkPrefabRef> levels;

    [Header("パラメータ")]
    [Tooltip("レース開始までのカウントダウン"), SerializeField] float startCountdown = 10f;
    [Tooltip("レースの制限時間"), SerializeField] float raceTime = 180f;
    [Tooltip("リザルト表示までの時間"), SerializeField] float delayBeforeResult = 3f;
    [Tooltip("リザルト画面の表示時間"), SerializeField] float resultTime = 5.0f;
    [Tooltip("「次のレースへ」か「部屋を抜ける」かを選択する時間"), SerializeField] float nextActionSelectTime = 10.0f;


    //誰でも参照可能にする
    public static GameMainManager Instance { get; private set; }

    /// <summary>
    /// 全プレイヤーのデータをまとめたネットワーク共有ディクショナリ
    /// </summary>
    [Networked, Capacity(20)] public NetworkDictionary<PlayerRef, PlayerData> PlayerData => default;

    /// <summary>
    /// 現在プレイ中のステージ
    /// </summary>
    [Networked] public Level PlayLevel { get; private set; }

    /// <summary>
    /// ゲーム状態
    /// </summary>
    [Networked] public GameState CurrentState { get; private set; }

    /// <summary>
    /// レース開始前のカウントダウン時間（少し時間を空けてから、3、2、1、スタート）
    /// </summary>
    [Networked] public TickTimer CountdownTimer { get; private set; }

    /// <summary>
    /// レースの残り時間
    /// </summary>
    [Networked] public TickTimer RaceRemainingTimer { get; private set; }

    /// <summary>
    /// ゴールしたプレイヤー人数
    /// </summary>
    [Networked] public int FinishedPlayerCount { get; private set; }

    /// <summary>
    /// リザルト画面表示までの時間
    /// </summary>
    [Networked] public TickTimer DelayBeforeResultTimer { get; private set; }

    /// <summary>
    /// リザルト画面表示の時間
    /// </summary>
    [Networked] public TickTimer ResultTimer { get; private set; }
    
    /// <summary>
    /// ボタン選択の時間
    /// </summary>
    [Networked] public TickTimer NextActionSelectTimer { get; private set; }



    bool isNicknameSent = false;        //ニックネームを登録したかどうかのフラグ


    void Awake()
    {
        //シングルトンにして、誰でも参照できるように登録する
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Runner.Spawn()などで生成された時、全ピアで呼び出される。Start()のネットワーク同期版。
    /// </summary>
    public override void Spawned()
    {
        //ホスト以外は処理を行わない
        if (!HasStateAuthority) { return; }

        //初期化処理
        Initialize();
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

        //プレイヤーの生成と破棄を常に行う
        UpdatePlayers();

        switch (CurrentState)
        {
            case GameState.Waiting:
                //現在の参加人数がNetworkManagerで設定した参加人数と同じになっているか確認
                if(Runner.ActivePlayers.Count() == Runner.SessionInfo.MaxPlayers)
                {
                    Debug.Log("必要参加人数が集まりました。");

                    StartCountdown();
                }

                break;
            case GameState.Countdown:
                //カウントダウンタイマーが終了すればレース開始
                if (CountdownTimer.ExpiredOrNotRunning(Runner))
                {
                    StartRace();
                }

                break;
            case GameState.Racing:

                //レース終了フラグ
                bool isFinished = false;

                //PlayerDataを確認し、まだレース中の人数を確認
                int racingPlayers = 0;
                foreach (KeyValuePair<PlayerRef, PlayerData> pair in PlayerData)
                {
                    if (!pair.Value.IsFinished)
                    {
                        racingPlayers++;
                    }
                }

                //レース中のプレイヤーが1人以下なら順位が確定しているのでレース終了
                if(racingPlayers <= 1)
                {
                    isFinished = true;
                }

                //時間切れならレース終了
                if (RaceRemainingTimer.ExpiredOrNotRunning(Runner))
                {
                    isFinished = true;
                }

                //レース終了フラグがtrueならレース終了処理を行う
                if (isFinished)
                {
                    FinishRace();
                }

                break;
            case GameState.Finish:
                //リザルト表示までのタイマーが終了すればリザルト状態へ移行
                if (DelayBeforeResultTimer.ExpiredOrNotRunning(Runner))
                {
                    Debug.Log("リザルト画面へ移行します");

                    ShowResult();
                }

                break;
            case GameState.Result:
                //リザルト表示時間のタイマーが終了すれば次のアクション選択画面に移行
                if(ResultTimer.ExpiredOrNotRunning(Runner))
                {
                    StartNextActionSelect();
                }

                break;
            case GameState.NextActionSelect:
                //PlayerDataを確認し、次のレース準備OKな人数を確認
                int readyPlayers = 0;
                foreach(KeyValuePair<PlayerRef, PlayerData> pair in PlayerData)
                {
                    if (pair.Value.IsReady)
                    {
                        readyPlayers++;
                    }
                }

                //現在セッションに参加しているプレイヤーの人数と比較して、全員が準備OKであれば次のレースへ移行
                if(readyPlayers >= Runner.ActivePlayers.Count())
                {
                    GoToNextRace();
                }
                //ボタン選択の制限時間をオーバーしていれば強制的に次のレースへ移行
                else if (NextActionSelectTimer.ExpiredOrNotRunning(Runner))
                {
                    Debug.Log("時間切れ");

                    GoToNextRace();
                }

                break;
        }
    }

    /// <summary>
    /// FixedUpdateNetwork()の後に各ピアで各々呼び出される。同期する必要のない処理をここで行う。
    /// </summary>
    public override void Render()
    {
        //ゲーム開始時、すべてのプレイヤーはホストに対してニックネームを送信する
        if (isNicknameSent == false)
        {
            //TitleScreenUIスクリプトでPlayerPrefsに登録したニックネームを読み込む
            string nickname = PlayerPrefs.GetString("Nickname", Runner.LocalPlayer.ToString());

            //ホストに向けてメソッドを実行するように送信
            RPC_SetPlayerNickname(Runner.LocalPlayer, nickname);
            isNicknameSent = true;
        }
    }

    /// <summary>
    /// ホストのみが行うゲーム初期化処理
    /// </summary>
    private void Initialize()
    {
        //ネットワーク上のシーン内に存在するPlayerオブジェクトを全て取得
        var allPlayerObjects = Runner.GetAllBehaviours<Player>();

        //シーン上のプレイヤーキャラクターを全て削除
        foreach (var playerOb in allPlayerObjects)
        {
            Runner.Despawn(playerOb.Object);
        }

        //ランダム生成器を作成（現在のTickをシード値にして初期化）
        var rng = new NetworkRNG(Runner.Tick);

        //ステージをランダムに生成する
        int random = rng.RangeInclusive(0, levels.Count - 1);
        var spawnedObject = Runner.Spawn(levels[random]);

        //スポーンしたオブジェクトからLevelスクリプトを取得
        PlayLevel = spawnedObject.GetComponent<Level>();

        //プレイヤーの情報をリセット
        PlayerData.Clear();

        //ゴールしたプレイヤーの数をリセット
        FinishedPlayerCount = 0;

    }

    /// <summary>
    /// プレイヤーの入退室時の更新処理
    /// </summary>
    private void UpdatePlayers()
    {
        //===============================<退出プレイヤーの削除>=============================

        //ネットワーク上のシーン内に存在するPlayerオブジェクトを全て取得
        var allPlayerObjects = Runner.GetAllBehaviours<Player>();

        for (int i = 0; i < allPlayerObjects.Count; i++)
        {
            Player player = allPlayerObjects[i];
            PlayerRef playerRef = player.Object.InputAuthority;

            //プレイヤーキャラの操作権限を持っている人のPlayerRefが、存在しているかを確認
            if (!Runner.IsPlayerValid(playerRef))
            {
                //プレイヤーデータ削除
                PlayerData.Remove(playerRef);

                //オブジェクト削除
                Runner.Despawn(player.Object);
            }
        }

        //===============================<新規参加プレイヤーの生成>=============================

        //ネットワーク接続中のプレイヤーを全て確認
        var activePlayers = Runner.ActivePlayers;

        foreach (var playerRef in activePlayers)
        {
            //プレイヤーディクショナリにない場合はプレイヤーを生成してデータを追加
            if (!PlayerData.ContainsKey(playerRef))
            {
                //新しいデータを作成
                var data = new PlayerData();
                data.Ref = playerRef;                   //プレイヤーのIDとなるPlayerRef
                data.Nickname = playerRef.ToString();   //ニックネームを仮登録
                data.IsFinished = false;                //まだゴールしていないのでfalse
                data.Place = int.MaxValue;              //順位が確定していないので適当な値を設定

                //作成データをディクショナリに追加
                PlayerData.Add(playerRef, data);  

                //プレイヤーが生成される場所を取得する
                var spawnPoint = PlayLevel.GetRespawnPoint();
                Vector3 pos = spawnPoint.transform.position;
                Quaternion rot = spawnPoint.transform.rotation;

                //プレイヤーを生成する
                Runner.Spawn(playerPrefab, pos, rot, playerRef);
            }
        }
    }

    private void StartCountdown()
    {
        Debug.Log("カウントダウン開始！");

        //ルームへの参加を受付停止する
        Runner.SessionInfo.IsOpen = false;
        Runner.SessionInfo.IsVisible = false;

        //カウントダウン用のタイマー作成
        CountdownTimer = TickTimer.CreateFromSeconds(Runner, startCountdown);

        //ステート変更
        CurrentState = GameState.Countdown;
    }

    private void StartRace()
    {
        Debug.Log("レース開始！");

        //カウントダウン用のタイマー作成
        RaceRemainingTimer = TickTimer.CreateFromSeconds(Runner, raceTime);

        //ステート変更
        CurrentState = GameState.Racing;
    }

    /// <summary>
    /// レース終了処理
    /// </summary>
    private void FinishRace()
    {
        Debug.Log("レース終了！");

        //ネットワーク上のシーン内に存在するPlayerオブジェクトを全て取得
        var allPlayerObjects = Runner.GetAllBehaviours<Player>();

        //すべてのプレイヤーを強制的にゴール状態にする
        foreach (Player p in allPlayerObjects)
        {
            p.FinishRace();
        }

        //リザルト表示までのディレイタイマー作成
        DelayBeforeResultTimer = TickTimer.CreateFromSeconds(Runner, delayBeforeResult);

        CurrentState = GameState.Finish;
    }

    /// <summary>
    /// リザルト表示処理
    /// </summary>
    private void ShowResult()
    {
        Debug.Log("リザルト表示");

        //RPCメソッドで、ホストから全プレイヤーに向けてリザルト画面を表示するメソッドを呼び出す
        RPC_ShowResultScreen();

        //リザルト画面だけを表示するタイマー作成
        ResultTimer = TickTimer.CreateFromSeconds(Runner, resultTime);

        //ステート変更
        CurrentState = GameState.Result;
    }

    /// <summary>
    /// 次のレースを行うかどうかの選択行うフェーズへの移行処理
    /// </summary>
    private void StartNextActionSelect()
    {
        Debug.Log("次のアクションを選択する");

        //RPCメソッドで、ホストから全プレイヤーに向けてボタンを表示するメソッドを呼び出す
        RPC_ShowNextActionButtons();

        //ボタン選択の制限時間タイマー作成
        NextActionSelectTimer = TickTimer.CreateFromSeconds(Runner, nextActionSelectTime);

        //ステート変更
        CurrentState = GameState.NextActionSelect;
    }

    /// <summary>
    /// プレイヤーのリスポーン地点を更新（ホストのみが実行する）
    /// </summary>
    /// <param name="player">チェックポイントに触れたプレイヤー</param>
    /// <param name="respawnPoint">触れたチェックポイント</param>
    public void UpdateRespawnPoint(Player player, RespawnPoint respawnPoint)
    {
        //チェックポイントに触れたキャラの入力権限を持つプレイヤーIDを取得
        PlayerRef playerRef = player.Object.InputAuthority;

        //チェックポイントに触れたプレイヤーのデータを取得する
        PlayerData data = PlayerData.Get(playerRef);

        //触れたチェックポイントが何番目のリスポーン地点かをLevelから取得する
        int index = PlayLevel.GetRespawnIndex(respawnPoint);

        //触れたチェックポイントが今のリスポーン地点よりも大きい番号なら更新する
        if (index > data.respawnIndex)
        {
            data.respawnIndex = index;
            PlayerData.Set(playerRef, data);

            Debug.Log(data.Nickname + "のリスポーン地点が" + data.respawnIndex + "番目に更新されました");
        }
    }

    /// <summary>
    /// プレイヤーのリスポーン処理（ホストのみが実行する）
    /// </summary>
    /// <param name="player">対象のプレイヤーキャラ</param>
    public void RespawnPlayer(Player player)
    {
        //落下プレイヤーのIDを取得
        PlayerRef playerRef = player.Object.InputAuthority;

        //現在のリスポーン地点の番号を取得
        PlayerData data = PlayerData.Get(playerRef);
        int index = data.respawnIndex;

        //IDを元に、現在のリスポーン地点のスクリプトを取得
        RespawnPoint respawnPoint = PlayLevel.GetRespawnPoint(index);

        //スクリプトのトランスフォーム情報を元に、プレイヤーの位置をリセットする
        Vector3 position = respawnPoint.transform.position;
        Quaternion rotation = respawnPoint.transform.rotation;
        player.Warp(position, rotation);
    }

    /// <summary>
    /// プレイヤーのゴール処理（ホストのみが実行する）
    /// </summary>
    /// <param name="player">対象のプレイヤーキャラ</param>
    public void GoalPlayer(Player player)
    {
        //ゴールしたキャラの入力権限を持つプレイヤーIDを取得
        PlayerRef playerRef = player.Object.InputAuthority;

        //ゴールしたプレイヤーのデータを取得する
        PlayerData data = PlayerData.Get(playerRef);

        //既にゴールしていれば処理を行わない
        if (data.IsFinished) { return; }

        //ゴールした人数を更新
        FinishedPlayerCount++;

        //ゴールしたプレイヤーのデータを書き換える
        data.IsFinished = true;
        data.Place = FinishedPlayerCount;
        PlayerData.Set(playerRef, data);

        //プレイヤーのゴール処理を呼び出す
        player.FinishRace();

        Debug.Log(data.Nickname + "が" + FinishedPlayerCount + "位でゴールしました！");
    }

    /// <summary>
    /// レースから抜けてセッションを終了する
    /// </summary>
    [ContextMenu("セッション終了")]
    public void LeaveGame()
    {
        //Runnerを使用してセッションを終了する
        Runner.Shutdown();
    }

    /// <summary>
    /// シーンを再読み込みして新しいレースを始める
    /// </summary>
    [ContextMenu("次のレースへ移行")]
    public void GoToNextRace()
    {
        //シーン管理の権限を持っているかを確認（ホストモードではデフォルトではホストのみ）
        if (!Runner.IsSceneAuthority)
        {
            Debug.Log("ホスト以外はシーンを読み込めません");
            return;
        }

        //ルームへの参加を受付を開始し、ルーム一覧にも表示させる
        Runner.SessionInfo.IsOpen = true;
        Runner.SessionInfo.IsVisible = true;

        //NetworkRunnerを通してシーンを読み込む
        Runner.LoadScene("GameScene");
    }


    /// <summary>
    /// ニックネームを登録するRPCメソッド
    /// </summary>
    /// <param name="playerRef">登録するプレイヤーのID</param>
    /// <param name="nickname">登録するニックネーム</param>
    /// <param name="info">送信したプレイヤーの情報（Photonが自動的に作成してくれる）</param>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    private void RPC_SetPlayerNickname(PlayerRef playerRef, string nickname, RpcInfo info = default)
    {
        //送信者のIDと引数のIDが一致しているかを確認（不正対策）
        if (info.Source != playerRef) { return; }

        //ディクショナリ中にデータが存在するかを確認
        if (!PlayerData.ContainsKey(playerRef)) { return; }

        //ディクショナリの中から、ニックネームを登録したいプレイヤーのデータを取得（コピー）
        var playerData = PlayerData.Get(playerRef);

        //ニックネーム登録
        playerData.Nickname = nickname;

        //ニックネームを登録したデータを上書きで再登録
        PlayerData.Set(playerRef, playerData);

        Debug.Log($"{playerRef}の名前「{nickname}」を登録");
    }

    /// <summary>
    /// 全プレイヤーにリザルト画面の表示をさせるRPCメソッド
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowResultScreen()
    {
        //マウスカーソルを再表示させる
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        //各プレイヤーのローカル上に配置されているResultScreenUIを表示させる
        resultScreenUI.ShowRanking();
    }

    /// <summary>
    /// 全プレイヤーにボタンを表示させるRPCメソッド
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowNextActionButtons()
    {
        //各プレイヤーのローカル上に配置されているResultScreenUIを操作する
        resultScreenUI.ShowButtons();
    }

    /// <summary>
    /// 次のレースへの参加準備OKを記録するRPCメソッド
    /// </summary>
    /// <param name="info">送信したプレイヤーの情報（Photonが自動的に作成してくれる）</param>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_SetReady(PlayerRef playerRef, RpcInfo info = default)
    {
        //送信者のIDと引数のIDが一致しているかを確認（不正対策）
        if (info.Source != playerRef) { return; }

        //ディクショナリ中にデータが存在するかを確認
        if (!PlayerData.ContainsKey(playerRef)) { return; }

        //ディクショナリの中から、記録したいプレイヤーのデータを取得（コピー）
        var playerData = PlayerData.Get(playerRef);

        // 3. データを書き換える
        playerData.IsReady = true;

        // 4. 書き換えたデータをセットし直す（これで共有される）
        PlayerData.Set(playerRef, playerData);

        Debug.Log($"{playerRef} は次のレースへ参加OK");
    }

    //void OnGUI()
    //{
    //    //画面上に表示するテキストのサイズと色をGUIStyleに登録
    //    GUIStyle style = new GUIStyle();
    //    style.fontSize = 15;
    //    style.normal.textColor = Color.white;

    //    //登録したGUIStyleを元に画面上にデバッグを表示
    //    GUI.Label(new Rect(10, 10, 500, 30), "プレイヤー参加人数： " + Runner.ActivePlayers.Count() + "/" + Runner.SessionInfo.MaxPlayers, style);
    //    GUI.Label(new Rect(10, 30, 500, 30), "ゲーム状態: " + CurrentState, style);
    //    GUI.Label(new Rect(10, 50, 500, 30), "カウントダウン時間: " + CountdownTimer.RemainingTime(Runner), style);
    //    GUI.Label(new Rect(10, 70, 500, 30), "レース残り時間: " + RaceRemainingTimer.RemainingTime(Runner), style);
    //}
}
