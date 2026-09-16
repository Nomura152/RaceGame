using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class TitleScreenUI : MonoBehaviour
{
    [Header("ニックネーム用テキスト入力欄"), SerializeField] TMP_InputField nicknameInput;
    [Header("ルーム名用テキスト入力欄"), SerializeField] TMP_InputField roomNameInput;

    void Start()
    {
        //デフォルトの名前を作成
        string defaultNick = "Player" + Random.Range(0, 1000);
        string defaultRoom = "Room" + Random.Range(0, 1000);

        //PlayerPrefsに登録済みのデータを読み込み、あらかじめテキスト欄に入力しておく（無ければデフォルト名を使用）
        nicknameInput.text = PlayerPrefs.GetString("Nickname", defaultNick);
        roomNameInput.text = PlayerPrefs.GetString("RoomName", defaultRoom);
    }

    /// <summary>
    /// ホスト用のセッション接続
    /// </summary>
    public void OnCreateRoomButtonClicked()
    {
        //入力情報をPlayerPrefsに登録しておく
        PlayerPrefs.SetString("Nickname", nicknameInput.text);
        PlayerPrefs.SetString("RoomName", roomNameInput.text);

        //NetworkManagerを使用して、セッション接続を行う（部屋名を引数で渡す）
        NetworkManager.Instance.ConnectAsync_Host(roomNameInput.text);
    }

    /// <summary>
    /// クライアント用のセッション接続
    /// </summary>
    public void OnJoinRoomButtonClicked()
    {
        //入力情報をPlayerPrefsに登録しておく
        PlayerPrefs.SetString("Nickname", nicknameInput.text);
        PlayerPrefs.SetString("RoomName", roomNameInput.text);

        //NetworkManagerを使用して、セッション接続を行う（部屋名を引数で渡す）
        NetworkManager.Instance.ConnectAsync_Client(roomNameInput.text);
    }

    /// <summary>
    /// ゲーム終了
    /// </summary>
    public void OnQuitGameButtonClicked()
    {
#if UNITY_EDITOR
        // エディタ上でプレイモードを終了する
        EditorApplication.isPlaying = false;
#else
        // ビルド時にアプリケーションを終了
        Application.Quit(); 
#endif
    }
}
