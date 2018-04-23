using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour {

    public AudioClip[] clips;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void Play(int index)
    {
        Debug.Log(index + " " + clips[index]);
        audioSource.PlayOneShot(clips[index]);
        //audioSource.clip = clips[index];
        //audioSource.Play();
    }

    public void PlayTune(Tune tune)
    {
        StartCoroutine(PlayTuneCoroutine(tune));
    }


    private IEnumerator PlayTuneCoroutine(Tune tune)
    {
        foreach (Tune.Note note in tune.notes)
        {
            yield return new WaitForSecondsRealtime(note.delay / tune.tempo);
            audioSource.PlayOneShot(clips[note.key]);
        }

    }
}
