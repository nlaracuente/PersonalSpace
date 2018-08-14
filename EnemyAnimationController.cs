using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimationController : MonoBehaviour
{
    /// <summary>
    /// A reference to the parent enemy component
    /// </summary>
    Enemy m_enemy;

	// Use this for initialization
	void Start () {
        m_enemy = GetComponentInParent<Enemy>();
	}
	
    /// <summary>
    /// Proxy to has recover
    /// </summary>
	public void HasRecover()
    {
        m_enemy.HasRecovered();
    }
}
