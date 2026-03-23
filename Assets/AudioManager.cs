using System.Collections;
using UnityEngine;


public class AudioManager : MonoBehaviour
{
    [SerializeField] public AudioSource audioSource;
    public AudioClip Eraser;
    public AudioClip Turndial;
    public AudioClip Sand;
    public AudioClip Water;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }


    public void playEraser()
    {
        audioSource.PlayOneShot(Eraser, 0.5f);
    }

    public void playTurndial()
    {
        audioSource.PlayOneShot(Turndial, 0.5f);
    }
   
    public void playSand()
    {
        audioSource.clip = Sand;
        audioSource.loop = true;
        audioSource.Play();
    }
  
    public void stopSand()
    {
        audioSource.Stop();
        audioSource.loop = false;
    }

    public void playWater()
    {
        audioSource.clip = Water;
        audioSource.loop = true;
        audioSource.volume = 0.5f;
        audioSource.Play();
    }

    public void stopWater()
    {
        audioSource.Stop();
        audioSource.loop = false;
    }
}
