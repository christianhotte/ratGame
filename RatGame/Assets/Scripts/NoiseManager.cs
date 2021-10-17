using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseManager : MonoBehaviour
{
    //Function: Manages everything in the game that has to do with sound

    //Objects & Components:
    public static NoiseManager noiseManager;
    private AudioSource audioSource; //The primary audio source in the scene
    public AudioClip[] audioClips; //Array of all audio clips used in game (kinda gross I know but it works)

    private void Awake()
    {
        //Get components:
        if (noiseManager == null) noiseManager = this;
        else Destroy(this);
        audioSource = GetComponent<AudioSource>();
    }

    public void PlaySound(int clipIndex)
    {
        //Function: Plays sound at given index

        if (clipIndex < 0 || clipIndex >= audioClips.Length)
        {
            Debug.LogError("Tried to play non-existent clip");
            return;
        }
        audioSource.clip = audioClips[clipIndex];
        audioSource.Play();
    }
}
