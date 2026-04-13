using UnityEngine;

// background music manager
// plays theme music on loop, sound effects play separately and dont interrupt it
public class MusicManager : MonoBehaviour
{
    [SerializeField] private AudioClip themeMusic;
    [SerializeField] private float musicVolume = 0.3f; // keep it quiet so sfx stand out

    private AudioSource musicSource;

    private void Start()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = themeMusic;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.spatialBlend = 0f; // 2d so it sounds the same everywhere
        musicSource.playOnAwake = false;
        musicSource.Play();
    }
}