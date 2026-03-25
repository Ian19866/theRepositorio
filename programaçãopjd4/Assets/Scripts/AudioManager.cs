using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    private AudioSource systemSource;
    private List<AudioSource> activeSources;
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else 
        {
                Destroy(gameObject);
        }
    }

    public void PlaySound(AudioClip clip)
    {
        systemSource.Stop();
        systemSource.clip = clip;
        systemSource.Play();
    }

    public void PauseSound()
    {
        systemSource.Pause();
    }

    public void ResumeSound()
    {
        systemSource.UnPause();
    }
    public void PlayOneShot(AudioClip clip)
    {
        systemSource.PlayOneShot(clip);
    }
    public void PlaySound(AudioClip clip, AudioSource source)
    {
        if (!activeSources.Contains(source)) activeSources.Add(source);
        source.clip = clip;
        source.Play();
        
    }

    public void StopSound3d(AudioSource source)
    {
        systemSource.Stop();
        activeSources.Remove(source);
    }
    public void pauseSound3d(AudioSource source)
    {
        systemSource.Pause();
        
    }
    public void resumeSound3d(AudioSource source)
    {
        systemSource.UnPause();
        
    }
    
    }

