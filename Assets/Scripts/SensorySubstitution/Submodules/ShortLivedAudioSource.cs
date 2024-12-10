using UnityEngine;

public class ShortLivedAudioSource : MonoBehaviour
{
    private AudioSource audioS;

    public void PlayAndDie(float deathDelay)
    {
        audioS = GetComponent<AudioSource>();
        audioS.Play();
        Destroy(this.gameObject, audioS.clip.length + deathDelay);
    }

    public void PlayAndDie(AudioClip clip, float deathDelay)
    {
        audioS = GetComponent<AudioSource>();
        audioS.clip = clip;
        audioS.Play();
        Destroy(this.gameObject, audioS.clip.length + deathDelay);
    }

    public void PlayAndDie(AudioClip clip, float deathDelay, float maxTime)
    {
        audioS = GetComponent<AudioSource>();
        audioS.clip = clip;
        audioS.Play();

        if (maxTime < clip.length + deathDelay)
        {
            Destroy(this.gameObject, maxTime);
        }
        else
        {
            Destroy(this.gameObject, audioS.clip.length + deathDelay);
        }

    }
}
