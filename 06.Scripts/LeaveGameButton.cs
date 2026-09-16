using UnityEngine;
using UnityEngine.UI;

public class LeaveGameButton : MonoBehaviour
{
    [Tooltip("「次のレースへ」ボタン"), SerializeField] Button nextRaceButton;
    [Tooltip("「部屋を抜ける」ボタン"), SerializeField] Button leaveButton;

    /// <summary>
    /// ボタンが押されたときに呼び出すイベント
    /// </summary>
    public void OnClick()
    {
        //GameMainManagerのレース退出処理を呼び出す
        GameMainManager.Instance?.LeaveGame();

        //ボタンを押しても反応しないようにする
        nextRaceButton.interactable = false;
        leaveButton.interactable = false;
    }
}
