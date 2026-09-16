using Unity.Cinemachine;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Cinemachine追従用ターゲット"), SerializeField] Transform cameraTarget;
    [Header("Cinemachineカメラ"), SerializeField] CinemachineCamera cinemachineCamera;
    [Header("ターゲットとの座標オフセット"), SerializeField] Vector3 targetOffset = new Vector3(0f, 3f, 0f);


    //設定されたプレイヤーターゲット
    Transform player;

    /// <summary>
    /// カメラで追従するプレイヤーを設定
    /// </summary>
    /// <param name="target"></param>
    public void SetTarget(Transform target)
    {
        player = target;
    }

    /// <summary>
    /// カメラの角度を設定する
    /// </summary>
    /// <param name="eulerAngles"></param>
    public void SetEulerAngles(Vector3 eulerAngles)
    {
        //回転値を直接設定する（オイラー角からクォータニオンに変換）
        cinemachineCamera.transform.rotation = Quaternion.Euler(eulerAngles);
    }

    void LateUpdate()
    {
        //プレイヤー情報がない場合は処理終了
        if(player == null) { return; }

        //プレイヤーを追従するカメラターゲットの座標を更新する
        cameraTarget.position = player.position + targetOffset;
    }
}
