using UnityEngine;

public class WalkSound : MonoBehaviour
{
    [SerializeField]AudioClip[] FootstepsSteps = new AudioClip[0];
    [SerializeField] float footstepsVolume;

    AudioSource audioSource;

    int stepSound;
    int lastSound;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }
    internal void UpdateFootsteps()
    {
        do
        {
            stepSound = Random.Range(0, FootstepsSteps.Length);     // берётся рандомный элемент массива
        } while (stepSound == lastSound);
        audioSource.PlayOneShot(FootstepsSteps[stepSound], footstepsVolume);
        lastSound = stepSound;
    }
}
