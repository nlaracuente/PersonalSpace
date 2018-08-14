using UnityEngine;

/// <summary>
/// AudioClip Info contains the basic information about a specific AudioClip such as
/// its volume, name, and whether or not it loops
/// </summary>
[CreateAssetMenu(fileName = "AudioClipInfo", menuName = "AudioClipInfo", order = 1)]
public class AudioClipInfo : ScriptableObject
{
    /// <summary>
    /// The audio clip to play
    /// </summary>
    [SerializeField]
    AudioClip m_clip;
    public AudioClip Clip { get { return m_clip; } }

    /// <summary>
    /// This is different from the master volume used by the AudioManager
    /// This allows each audio clip to be played at different levels
    /// </summary>
    [SerializeField, Range(0f, 1f)]
    float m_volume = 1f;
    public float Volume { get { return m_volume; } }

    /// <summary>
    /// The name that references this specific audio clip
    /// </summary>
    [SerializeField]
    AudioName m_name;
    public AudioName ClipName { get { return m_name; } }

    /// <summary>
    /// True: causes the audio to loop on play
    /// </summary>
    [SerializeField]
    bool m_loops = false;
    public bool Loops { get { return m_loops; } }
}
