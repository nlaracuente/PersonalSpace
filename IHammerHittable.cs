using UnityEngine;

/// <summary>
/// Objects that can be hit by the player's hammer
/// </summary>
public interface IHammerHittable
{
    /// <summary>
    /// Returns true when it can be hit by the hammer
    /// </summary>
    /// <returns></returns>
    bool CanBeHit();

    /// <summary>
    /// Handles being hit by the hammer
    /// </summary>
    void OnHammerHit();

    /// <summary>
    /// True when the action that gets triggerd when hit is at a stage where it can be considerd "processed"
    /// </summary>
    /// <returns></returns>
    bool HitProcessed();

    /// <summary>
    /// Returns the attached game object
    /// </summary>
    GameObject SourceGO { get; }
}
