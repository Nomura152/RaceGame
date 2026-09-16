using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MyScriptable/AudioList")]
public class AudioList : ScriptableObject
{
    //音源ファイルを纏めるリスト
    public List<AudioClip> clipList;
}