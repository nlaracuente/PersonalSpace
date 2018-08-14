using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy tracks the player
/// The speed and frequency at which is set randomly at creation
/// </summary>
public class Enemy : MonoBehaviour, IKillable, IHammerHittable
{
    /// <summary>
    /// A reference to the navigation mesh agent
    /// </summary>
    [SerializeField]
    NavMeshAgent m_agent;

    /// <summary>
    /// A reference to the animator component
    /// </summary>
    [SerializeField]
    Animator m_animator;

    /// <summary>
    /// How long to wait between cycles
    /// </summary>
    [SerializeField]
    float m_cyclesDelay = 3f;

    /// <summary>
    /// How long to wait before re-looking for the player 
    /// </summary>
    [SerializeField]
    int m_trackingDelay = 300;
    int m_trackingCount = 0;
    
    /// <summary>
    /// A nav mesh path used to determine if the path is valid
    /// </summary>
    NavMeshPath m_path;

    /// <summary>
    /// A refence to the rigidbody component
    /// </summary>
    Rigidbody m_rigidbody;

    /// <summary>
    /// A reference to the tile manager object
    /// </summary>
    TileManager m_tileManager;

    /// <summary>
    /// The current position the enemy will move to
    /// </summary>
    Vector3 m_destination;

    /// <summary>
    /// True once the enemy has been told to initialize
    /// </summary>
    bool m_isInitialized = false;

    /// <summary>
    /// True when the enemy falls
    /// </summary>
    bool m_isDead = false;

    /// <summary>
    /// True when the enemy is in a stunned phase
    /// </summary>
    bool m_isStunned = false;

    /// <summary>
    /// True when the enemy has recovered from being stunned
    /// </summary>
    bool m_hasRecovered = false;

    /// <summary>
    /// True when the hit was registered
    /// </summary>
    bool m_hitProcessed = false;

    /// <summary>
    /// How long to wait before processing the hit
    /// </summary>
    float m_hitProcessDelay = .25f;

    /// <summary>
    /// Returns the current tile map player coordinates
    /// </summary>
    public Vector2 Coordinates
    {
        get {
            Vector2 coord = new Vector2(
                Mathf.Round(transform.position.x),
                Mathf.Round(transform.position.z)
            );

            return coord;
        }
    }

    /// <summary>
    /// Returns the instance of the player
    /// </summary>
    Player m_player { get { return GameManager.instance.GetPlayer; } }

    /// <summary>
    /// Implementation from interface, returns this game object
    /// </summary>
    public GameObject SourceGO { get { return gameObject; } }

    /// <summary>
    /// Initialize
    /// </summary>
    public void Initialize(int trackingDelay)
    {
        m_path = new NavMeshPath();
        m_destination = transform.position;
        m_trackingDelay = trackingDelay;

        if(m_agent == null)
        {
            m_agent = GetComponent<NavMeshAgent>();
        }
        
        m_agent.enabled = true;
        m_tileManager = FindObjectOfType<TileManager>();
        m_rigidbody = GetComponent<Rigidbody>();

        m_isInitialized = true;

        StartCoroutine(EnemyAIRoutine());
    }

    /// <summary>
    /// Updates the animators speed to show enemy walking or idled
    /// </summary>
    public void SetMoving(bool isMoving)
    {
        float speed = 1f;
        if (!isMoving)
        {
            speed = 0f;
        }

        m_animator.SetFloat("Speed", speed);
    }

    /// <summary>
    /// Handles the behavior of the enemy
    /// </summary>
    /// <returns></returns>
    IEnumerator EnemyAIRoutine()
    {
        while (!m_isDead)
        {
            yield return StartCoroutine(GetPlayerPositionRoutine());
            yield return StartCoroutine(MoveToDestinationRoutine());
            yield return StartCoroutine(MoveToRandomTileRoutine());
        }
    }

    /// <summary>
    /// Track the player
    /// </summary>
    /// <returns></returns>
    IEnumerator GetPlayerPositionRoutine()
    {
        SetMoving(false);
        yield return null;            

        // Go after the player
        if (!m_isDead && !m_isStunned && !m_player.IsDead)
        {
            m_destination = m_player.transform.position;

            // if path to the player is not available, then choose a random tile
            // a random neighboring tile that is available
            if (!IsPathAvailable(m_destination))
            {
                SetDestinationToRandomTile();
            }
      
        // Stay where you are
        } else
        {
            m_destination = transform.position;
        }
    }

    /// <summary>
    /// Choses a random tile to move the enemy to
    /// </summary>
    void SetDestinationToRandomTile()
    {
        Tile tile = m_tileManager.GetRandomActiveTile(Coordinates);
        m_destination = tile.transform.position;
    }

    /// <summary>
    /// Allows the enemy to move towards the destination
    /// as long as it is not dead, stunned, or has not reached the destination
    /// </summary>
    /// <returns></returns>
    IEnumerator MoveToDestinationRoutine()
    {
        if(!m_isDead && !m_isStunned && !m_player.IsDead)
        {
            if (m_agent.enabled)
            {
                SetMoving(true);
                m_agent.SetDestination(m_destination);
            }

            while (IsPathAvailable(m_destination) && !HasReachedDestination() && !m_isDead && !m_isStunned && !m_player.IsDead)
            {
                // if the path becomes invalid, then choose a random tile
                m_trackingCount++;
                if (m_trackingCount >= m_trackingDelay)
                {
                    // Stop the agent by setting our destination to be the current position
                    // Re-calculate path
                    m_agent.SetDestination(transform.position);
                    m_trackingCount = 0;
                }

                yield return null;
            }
        }
    }

    /// <summary>
    /// Waits before starting the routine all over
    /// </summary>
    /// <returns></returns>
    IEnumerator MoveToRandomTileRoutine()
    {
        SetMoving(false);
        SetDestinationToRandomTile();
        yield return StartCoroutine(MoveToDestinationRoutine());
    }

    /// <summary>
    /// Returns true when the enemy has reached their destination
    /// </summary>
    /// <returns></returns>
    bool HasReachedDestination()
    {
        bool hasReached = false;

        if (!m_agent.enabled)
        {
            return true;
        }

        if (m_agent.remainingDistance <= m_agent.stoppingDistance)
        {
            hasReached = true;
        }

        return hasReached;
    }

    /// <summary>
    /// Returns true so long as there's a walkable path to the given destination
    /// </summary>
    /// <param name="m_destination"></param>
    /// <returns></returns>
    bool IsPathAvailable(Vector3 m_destination)
    {
        m_agent.CalculatePath(m_destination, m_path);
        return m_path.status == NavMeshPathStatus.PathComplete;
    }

    /// <summary>
    /// Trigger the hurt routine
    /// </summary>
    public void OnHammerHit()
    {
        m_hitProcessed = false;
        StartCoroutine(EnemyHurtRoutine());
    }

    /// <summary>
    /// True when the enemy is adjacent to the player and not already stunned or dead
    /// </summary>
    /// <returns></returns>
    public bool CanBeHit()
    {
        return !m_isStunned && !m_isDead;
    }

    /// <summary>
    /// Stuns the enemy for a given time before resuming 
    /// </summary>
    /// <returns></returns>
    IEnumerator EnemyHurtRoutine()
    {
        m_isStunned = true;

        // Player look at the tile the enemy is on
        Tile tile = m_tileManager.GetTileAt(Coordinates);
        if(tile != null)
        {
            GameManager.instance.PlayerLookAt(tile.transform);
        }

        m_animator.SetTrigger("Flatten");
        AudioManager.instance.PlaySound(AudioName.SlimeHit);
        m_agent.enabled = false;

        // Trigger the hit processed with a delay
        StartCoroutine(HitProcessDelay());

        // Wait for the enemy to recover
        m_hasRecovered = false;    
        while (!m_hasRecovered)
        {
            yield return null;
        }

        m_isStunned = false;
        m_agent.enabled = true;
    }

    /// <summary>
    /// Delays the acknowledgement of processing the hit so that it is visually processed first
    /// </summary>
    /// <returns></returns>
    IEnumerator HitProcessDelay()
    {
        yield return new WaitForSeconds(m_hitProcessDelay);
        m_hitProcessed = true;
    }

    /// <summary>
    /// True when the hit was processed
    /// </summary>
    /// <returns></returns>
    public bool HitProcessed()
    {
        return m_hitProcessed;
    }

    /// <summary>
    /// Triggered when the enemy recovers from being flatten
    /// </summary>
    public void HasRecovered()
    {
        m_hasRecovered = true;
    }

    /// <summary>
    /// Stops enemy from moving and turns on their rigidbody to trigger them to fall
    /// </summary>
    public void TriggerDeathByFall()
    {
        m_isDead = true;
        if (m_agent.isActiveAndEnabled)
        {
            m_agent.isStopped = true;
            m_agent.enabled = false;
        }

        m_rigidbody.useGravity = true;
        m_animator.SetTrigger("Fall");
        AudioManager.instance.PlaySound(AudioName.SlimeFall);
        GameManager.instance.EnemyDefeated(this);        
    }

    /// <summary>
    /// Kill the player
    /// </summary>
    /// <param name="other"></param>
    void OnTriggerEnter(Collider other)
    {
        // Enemy can't hurt the player when stunned
        if (!m_isStunned && other.CompareTag("Player"))
        {
            other.GetComponent<Player>().TriggerDeathByEnemy();
        }
    }
}
