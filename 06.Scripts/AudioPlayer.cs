using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioPlayer : NetworkBehaviour
{
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioList audioList;

    public void PlayAudioOnce(string clipName)
    {
        //引数として渡された曲名をリストから探す
        AudioClip clip = audioList.clipList.Find(audioClip => audioClip.name == clipName);

        if (clip != null)
        {
            //対象の音源をAudioSourceから一度だけ鳴らす
            audioSource.PlayOneShot(clip);
        }
        else
        {
            //音源が見つからなかった場合、エラーとしてログを出力
            Debug.LogError(clipName + "という曲名がリストに存在しません。音楽ファイル名を一致しているか確認して下さい。");
        }
    }
}
