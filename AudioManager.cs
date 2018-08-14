using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the audio of the game
/// Receives request to play sound effects and music
/// Uses object pooling to limit how many sounds can play at once
/// </summary>
public class AudioManager : MonoBehaviour
{
    /// <summary>
    /// A reference to self to ensure only one AudioManager exist per session
    /// </summary>
    public static AudioManager instance = null;

    /// <summary>
    /// This is the collection of all the audio clips available for playing
    /// Meaning this contains all the SFX and music clips that the game may
    /// play at any given time
    /// </summary>
    [SerializeField, Tooltip("All sound effects and music clips")]
    AudioClipInfo[] m_clips;

    /// <summary>
    /// Creates a relationship between the name of the audio and the
    /// associated audio clip info so that it can be triggerd by name
    /// </summary>
    Dictionary<AudioName, AudioClipInfo> m_aduioDictionary = new Dictionary<AudioName, AudioClipInfo>();

    /// <summary>
    /// The parent container that holds all SFX instances
    /// </summary>
    GameObject m_soundsGO;

    /// <summary>
    /// A container for the music player game object
    /// Currently there's only one Music Player at a time
    /// </summary>
    GameObject m_musicGO;

    /// <summary>
    /// A reference to the current music clip that is playing
    /// This allows us to change/update the music
    /// </summary>
    SoundClip m_musicClip;

    /// <summary>
    /// This clip is played when changing the volume settings for sound effects
    /// to give the player an example of what the sound level is not set to
    /// </summary>
    [SerializeField, Tooltip("To play while testing sound volume")]
    AudioClipInfo m_testSfxClipInfo;

    /// <summary>
    /// The SoundClip for the test sound effect so that we can control it later
    /// </summary>
    SoundClip m_testSfxClip;

    /// <summary>
    /// Master volume for music
    /// </summary>
    float m_musicVolume = 1f;
    public float MusicVolume { get { return m_musicVolume; } set { m_musicVolume = value; } }

    /// <summary>
    /// Master volume for sound
    /// </summary>
    float m_sfxVolume = 1f;
    public float SFXVolume { get { return m_sfxVolume; } set { m_sfxVolume = value; } }

    /// <summary>
    /// A pool of soundclips to use when playing an SFX
    /// </summary>
    Queue<SoundClip> m_soundClips;

    /// <summary>
    /// Singleton setup
    /// </summary>
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            SetupManager();
        } else
        {
            Destroy(this.gameObject);
        }

        DontDestroyOnLoad(this.gameObject);
    }

    /// <summary>
    /// Creates the audio hash table, resource pool, and game objects
    /// that have the audio sources we can use to play audio
    /// </summary>
    void SetupManager()
    {
        CreateAudioDictionary();
        CreateParentContainersAndPlayers();
        CreateSoundPool();
    }

    /// <summary>
    /// Creates the relationship between AudioName and AudioClipInfo
    /// </summary>
    void CreateAudioDictionary()
    {
        foreach (AudioClipInfo clip in m_clips)
        {
            m_aduioDictionary[clip.ClipName] = clip;
        }
    }

    /// <summary>
    /// Creates the parent containers where music and sound lives
    /// as well as the music player and sound test player
    /// </summary>
    void CreateParentContainersAndPlayers()
    {
        // Sounds
        if (m_soundsGO == null)
        {
            m_soundsGO = new GameObject("_SFXPlayer");

            m_testSfxClip = m_soundsGO.AddComponent<SoundClip>();
            m_testSfxClip.Info = m_testSfxClipInfo;
        }

        // Music
        if (m_musicGO == null)
        {
            m_musicGO = new GameObject("_MusicPlayer");
            m_musicClip = m_musicGO.AddComponent<SoundClip>();
        }

        // Child the players to the AudioManager
        m_soundsGO.transform.SetParent(transform);
        m_musicGO.transform.SetParent(transform);
    }

    /// <summary>
    /// Creates the sound clip pool
    /// The pool is currently based on the total amount of clips to play
    /// However, this can be easily changed by updating the for loop condition
    /// </summary>
    void CreateSoundPool()
    {
        m_soundClips = new Queue<SoundClip>();

        for (int i = 0; i < m_clips.Length; i++)
        {
            GameObject go = new GameObject(string.Format("sfx_{0}", i));
            go.transform.SetParent(m_soundsGO.transform);

            m_soundClips.Enqueue(go.AddComponent<SoundClip>());
        }
    }

    /// <summary>
    /// Triggers the routine to play the given sound
    /// </summary>
    /// <param name="soundName"></param>
    public void PlaySound(AudioName soundName)
    {
        // Can't play what we don't know about
        if (!m_aduioDictionary.ContainsKey(soundName))
        {
            Debug.LogWarningFormat("Unknown Sound Effect {0}", soundName);
            return;
        }

        // Make sure we have enough resources to play this sound
        if (m_soundClips.Count < 1)
        {
            Debug.LogFormat("Not enough resources to play {0}", soundName);
            return;
        }

        StartCoroutine(PlaySoundRoutine(soundName));
    }

    /// <summary>
    /// Plays the given sound
    /// Waits until the sound is done playing
    /// Stops the sound and adds it back to the resource pool
    /// If the sound is set to loop then it won't add it back to the queue
    /// until it is done looping
    /// </summary>
    /// <param name="soundName"></param>
    /// <returns></returns>
    IEnumerator PlaySoundRoutine(AudioName soundName)
    {
        AudioClipInfo info = m_aduioDictionary[soundName];
        SoundClip clip = m_soundClips.Dequeue();

        clip.Info = info;
        clip.Volume = m_sfxVolume;
        clip.Play();

        // Will keep checking until the sound no longer plays
        if (clip.Loops)
        {
            // Check again next frame to see if we are done
            while (clip.IsPlaying)
            {
                yield return new WaitForEndOfFrame();
            }
        } else
        {
            // Wait until the sound is done playing to re-queue it
            yield return new WaitForSeconds(clip.Length);
        }

        // For safety ensure the sound is stopped
        clip.Stop();
        clip.Info = null;

        m_soundClips.Enqueue(clip);
    }

    /// <summary>
    /// Pauses the current music from playing if it's not already paused
    /// </summary>
    public void PauseMusic()
    {
        if (m_musicClip.Info == null || !m_musicClip.IsPlaying)
        {
            return;
        }

        m_musicClip.Pause();
    }

    /// <summary>
    /// Resumes playing the current music if it's not already playing
    /// </summary>
    public void ResumeMusic()
    {
        if (m_musicClip.Info == null || m_musicClip.IsPlaying)
        {
            return;
        }

        m_musicClip.Play();
    }

    /// <summary>
    /// Stops the current music from playing (if any)
    /// and play the given music
    /// </summary>
    /// <param name="musicName"></param>
    public void PlayMusic(AudioName musicName)
    {
        // Don't know it
        if (!m_aduioDictionary.ContainsKey(musicName))
        {
            Debug.LogWarningFormat("Unknown Music {0}", musicName);
            return;
        }

        // Already playing no need to trigger it
        if (m_musicClip.Info != null && m_musicClip.ClipName == musicName && m_musicClip.IsPlaying)
        {
            return;
        }

        m_musicClip.Stop();

        // Update the music and play it
        m_musicClip.Info = m_aduioDictionary[musicName];
        m_musicClip.Volume = m_musicVolume;
        m_musicClip.Play();
    }

    /// <summary>
    /// Stops the current music from playing
    /// </summary>
    public void StopMusic()
    {
        m_musicClip.Stop();
        m_musicClip.Info = null;
    }

    /// <summary>
    /// Changes the volume of the music currently playing
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        m_musicClip.Volume = volume;
    }

    /// <summary>
    /// Sets the master volume for the music
    /// Triggers a change in volume for the music currently playing
    /// </summary>
    /// <param name="volume"></param>
    public void SetMasterMusicVolume(float volume)
    {
        m_musicVolume = volume;
        SetMusicVolume(volume);
    }

    /// <summary>
    /// Sets the master volume for the sound effects
    /// </summary>
    /// <param name="volume"></param>
    public void SetMasterSFXVolume(float volume)
    {
        m_sfxVolume = volume;
        m_testSfxClip.Volume = volume;

        // Play the sample sfx
        if (!m_testSfxClip.IsPlaying)
        {
            m_testSfxClip.Play();
        }
    }
}

public enum AudioName
{
    SlimeJump,
    SlimeTouch,
    PlayerFall,
    CrumbleBig,
    CrumbleSmall,
    Misc,
    LevelTransition,
    SlimeFall,
    SlimeHit,
    TileBreakOne,
    TileBreakTwo,
    TileBreakThree,
    ButtonHover,
    ButtonClick,
    LevelMusic,
    LostMusic,
    WinMusic,
}