using Fusion;
using UnityEngine;


/// <summary>
/// ゲーム中のプレイヤーデータ
/// </summary>
public struct PlayerData : INetworkStruct
{
    /// <summary>
    /// プレイヤーのID
    /// </summary>
    public PlayerRef Ref;

    /// <summary>
    /// プレイヤーのニックネーム（16文字）
    /// </summary>
    public NetworkString<_16> Nickname;

    /// <summary>
    /// リスポーン地点番号
    /// </summary>
    public int respawnIndex;

    /// <summary>
    /// プレイヤーのゴールフラグ
    /// </summary>
    public bool IsFinished;

    /// <summary>
    /// プレイヤーのレースの順位
    /// </summary>
    public int Place;

    /// <summary>
    /// 次のレースへの準備完了フラグ
    /// </summary>
    public bool IsReady;

    //ほかにも必要なデータがあれば追加してください
    //チーム番号、使用キャラクター、所持アイテムなど
}
