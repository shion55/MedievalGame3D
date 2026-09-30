using UnityEngine;

public class VillagerAudio : MonoBehaviour
{
    //âπê∫
    public AudioSource jobAudioSource;
    public AudioSource bodyaudioSource;
    public AudioSource songaudioSource;

    [SerializeField] private AudioClip[] footstepClips;
    public void PlayJobAudio(AudioClip clip)
    {
        if (clip == null)
            return;

        jobAudioSource.PlayOneShot(clip);
    }
    //ÉãÅ[ÉvÅïí‚é~
    public void StartJobAudioLoop(AudioClip clip)
    {
        if (clip == null)
            return;

        jobAudioSource.clip = clip;
        jobAudioSource.loop = true;
        jobAudioSource.Play();
    }

    public void StopJobAudio()
    {
        jobAudioSource.Stop();
        jobAudioSource.loop = false;
        jobAudioSource.clip = null;
    }
    public void PlayFootstep()
    {
        if (bodyaudioSource == null ||
            footstepClips == null ||
            footstepClips.Length == 0)
        {
            return;
        }

        AudioClip clip =
            footstepClips[
                Random.Range(0, footstepClips.Length)
            ];

        bodyaudioSource.pitch =
            Random.Range(0.95f, 1.05f);

        bodyaudioSource.PlayOneShot(clip);
    }
}
