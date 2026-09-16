using Fusion;
using UnityEngine;

public struct PlayerInputData : INetworkInput
{
    /// <summary>
    /// 移動方向
    /// </summary>
    public Vector2 Move;

    /// <summary>
    /// カメラの向き
    /// </summary>
    public Vector2 Look;

    /// <summary>
    /// ジャンプや攻撃など、複数のボタン入力状態をまとめておく
    /// </summary>
    public NetworkButtons Buttons;
}

/// <summary>
/// 入力するボタンの種類
/// </summary>
public enum InputButtonType
{
    Jump,       //ジャンプ
    Attack,     //攻撃
}