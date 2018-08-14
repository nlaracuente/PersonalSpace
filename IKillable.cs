using UnityEngine;

/// <summary>
/// Killable objects are those who have a death sequence that can be triggered by an external source
/// </summary>
public interface IKillable
{
    void TriggerDeathByFall();

    /// <summary>
    /// Returns the attached game object
    /// </summary>
    GameObject SourceGO { get; }
}
