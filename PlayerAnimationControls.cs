using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A proxy to call the Player component's animation
/// </summary>
public class PlayerAnimationControls : MonoBehaviour
{
    Player m_player;

	// Use this for initialization
	void Start ()
    {
        m_player = FindObjectOfType<Player>();
		
	}
	
    /// <summary>
    /// Proxy to the player's hammer hit
    /// </summary>
	public void HammerHasHit()
    {
        m_player.HammerHasHit();
    }
}
