using UnityEngine;

/// <summary>
/// A SoundClip functions like a proxy to an AudioClip by connecting
/// the information from <see cref="AudioClipInfo"/> with its AudioSource
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SoundClip : MonoBehaviour
{
    /// <summary>
    /// Information about this specific audio clip
    /// </summary>
    [SerializeField]
    AudioClipInfo m_info;
    public AudioClipInfo Info
    {
        get { return m_info; }

        // Updates the AudioSource based on the info received
        set {
            m_info = value;

            if (m_info != null)
            {
                Source.clip = m_info.Clip;
                Source.volume = m_info.Volume;
                Source.loop = m_info.Loops;
            }
        }
    }

    /// <summary>
    /// A reference to the audio source that plays this clip
    /// </summary>
    AudioSource m_source;
    AudioSource Source
    {
        get {
            if (m_source == null)
            {
                m_source = GetComponent<AudioSource>();

                // Still missing? Then add it!
                if (m_source == null)
                {
                    m_source = gameObject.AddComponent<AudioSource>();
                }

                m_source.playOnAwake = false;
            }
            return m_source;
        }
    }

    /// <summary>
    /// The current AudioClip the AudioSource is playing
    /// </summary>
    public AudioClip Clip { get { return Source.clip; } }

    /// <summary>
    /// The name of the current sound clip
    /// </summary>
    public AudioName ClipName { get { return m_info.ClipName; } }

    /// <summary>
    /// Updates the volume of the AudioSource by the given value
    /// using the SoundClip's base volume as its foundation.
    /// This means that even when the volume is at 100%, if the 
    /// sound clip has a lower volume, it won't play at 100%
    /// </summary>
    public float Volume
    {
        set { Source.volume = m_info.Volume * value; }
    }

    // The following methods function more as proxies

    /// <summary>
    /// Returns whether or not the current clip is playing
    /// </summary>
    public bool IsPlaying { get { return m_source.isPlaying; } }

    /// <summary>
    /// Returns whether or not the clips loops
    /// </summary>
    public bool Loops { get { return m_info.Loops; } }

    /// <summary>
    /// Returns how long the clip is
    /// </summary>
    public float Length { get { return m_info.Clip.length; } }

    /// <summary>
    /// Pauses the audio clip where it is at so that it can be resumed later
    /// </summary>
    public void Pause()
    {
        Source.Pause();
    }

    /// <summary>
    /// Triggers the clip to play if not already playing
    /// </summary>
    public void Play()
    {
        if (!Source.isPlaying)
        {
            Source.Play();
        }
    }

    /// <summary>
    /// Stops the clip from playing
    /// </summary>
    public void Stop()
    {
        Source.Stop();
    }
}
