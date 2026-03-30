using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    public AudioCollection systemCollection;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AudioManager.Instance.PlaySound(systemCollection.audioClips[0]);
    }

    
}
