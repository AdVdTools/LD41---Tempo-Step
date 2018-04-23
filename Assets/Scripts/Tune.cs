using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tune")]
public class Tune : ScriptableObject {
    [System.Serializable]
    public struct Note
    {
        public int key;
        public float delay;
        //float duration?
    }

    public float tempo = 1f;
    public Note[] notes;
    
    [ContextMenu("Play Tune")]
    public void PlayTune()
    {
        FindObjectOfType<SoundPlayer>().PlayTune(this);
    }

}
