using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A tile is where both the player and enemy can stand on 
/// It can be destroyed replacing it with an invisible wall
/// All avatars on the tile when it is destroyed are killed
/// </summary>
public class Tile : MonoBehaviour, IHammerHittable
{
    /// <summary>
    /// A reference to the tile's mesh renderer component
    /// </summary>
    [SerializeField]
    Renderer m_renderer;

    /// <summary>
    /// A reference to the rigidbody component
    /// </summary>
    [SerializeField]
    Rigidbody m_rigidbody;

    /// <summary>
    /// A reference to the invisible wall object to use when the tile is destroyed
    /// </summary>
    [SerializeField]
    GameObject m_invisibleWall;

    /// <summary>
    /// Material to use when the tile is active
    /// </summary>
    [SerializeField]
    Material m_activeMat;

    /// <summary>
    /// Material to use when the tile is highlited as in "can be destroyed"
    /// </summary>
    [SerializeField]
    Material m_highlightedMat;

    /// <summary>
    /// Material to use when the tile is destroyed
    /// </summary>
    [SerializeField]
    Material m_destroyedMat;

    /// <summary>
    /// Material to use when the tile falls because the surrounding tiles are destroyed
    /// </summary>
    [SerializeField]
    Material m_fallenMat;

    /// <summary>
    /// A collections of all the possible sounds to 
    /// </summary>
    [SerializeField]
    List<AudioName> m_audioNames = new List<AudioName>() {
        AudioName.TileBreakOne,
        AudioName.TileBreakTwo,
        AudioName.TileBreakThree,
    };

    /// <summary>
    /// Where on the tile map this tile is located as
    /// </summary>
    public Vector2 Coordinates { get; set; }

    /// <summary>
    /// Stores the original 3d coordinates of this tile
    /// </summary>
    Vector3 m_startingPosition;

    /// <summary>
    /// The current state of the tile
    /// Defaults to active
    /// </summary>
    [SerializeField]
    TileState m_state = TileState.Active;
    public TileState State
    {
        get {
            return m_state;
        }

        set {
            m_state = value;
            ShowRenderer(true);

            switch (m_state)
            {
                case TileState.Active:
                    m_renderer.material = m_activeMat;
                    EnableInvisibleWall(false);
                    break;

                case TileState.Highlighted:
                    m_renderer.material = m_highlightedMat;
                    EnableInvisibleWall(false);
                    break;

                case TileState.Destroyed:
                    m_renderer.material = m_destroyedMat;
                    EnableInvisibleWall(m_state == TileState.Destroyed);
                    PlayTileDestroyedSound();
                    break;

                case TileState.Fallen:
                    m_renderer.material = m_fallenMat;
                    EnableInvisibleWall(false);
                    break;

                case TileState.Void:
                    ShowRenderer(false);
                    EnableInvisibleWall(true);
                    break;
            }
        }
    }

    /// <summary>
    /// A collection of neighbor tiles 
    /// </summary>
    [SerializeField]
    List<Tile> m_neighbors = new List<Tile>();
    public List<Tile> Neighbors
    {
        get {
            return m_neighbors;
        }

        set {
            m_neighbors = value;
        }
    }

    /// <summary>
    /// A refence to the TileMananger
    /// </summary>
    TileManager m_manager;

    /// <summary>
    /// A collections of objects that can be killed when tile is destroyed
    /// </summary>
    [SerializeField]
    List<IKillable> m_killables = new List<IKillable>();

    /// <summary>
    /// True when the hit was registered
    /// </summary>
    bool m_hitProcessed = false;

    /// <summary>
    /// How long to wait before acknowledging a player hit
    /// </summary>
    [SerializeField]
    float m_hitProcessDelay = .25f;

    /// <summary>
    /// True when the tile has not been destroyed
    /// </summary>
    public bool IsAvailable
    {
        get {
            List<TileState> invalidStates = new List<TileState>() {
                TileState.Destroyed,
                TileState.Fallen,
                TileState.Void,
            };

            return !invalidStates.Contains(m_state);
        }
    }

    /// <summary>
    /// True when the tile is available and has no objects on it
    /// </summary>
    public bool IsAvailableAndEmpty
    {
        get {
            return IsAvailable && m_killables.Count < 1;
        }
    }

    /// <summary>
    /// Implementation from interface, returns this game object
    /// </summary>
    public GameObject SourceGO { get { return gameObject; } }

    /// <summary>
    /// Initializes
    /// </summary>
    void Start()
    {
        m_killables = new List<IKillable>();
        m_manager = FindObjectOfType<TileManager>();
    }

    /// <summary>
    /// Stores the current position as the starting position
    /// </summary>
    public void SaveCurrentPosition()
    {
        m_startingPosition = transform.position;
    }

    /// <summary>
    /// Triggers the tile rto be destroyed
    /// </summary>
    public void OnHammerHit()
    {
        // if there are things on this tile, try hitting them first
        List<IHammerHittable> hitables = new List<IHammerHittable>();
        m_killables.ForEach(k=> {
            IHammerHittable hitable = k.SourceGO.GetComponent<IHammerHittable>();

            if(hitable != null)
            {
                hitables.Add(hitable);
            }
        });

        // Don't destroy the tile but whats on it instead
        if(hitables.Count > 0)
        {
            hitables.ForEach( h => {
                // Only if they can be hit
                if (h.CanBeHit())
                {
                    h.OnHammerHit();
                }                    
            });

            // Have to process the hit otherwise the player sits there and waits
            StartCoroutine(HitProcessDelay());
        } else
        {
            m_manager.DestroyTile(this);
        }        
    }


    /// <summary>
    /// True when the tile is not in a destroyed state and is adjacent to the player
    /// </summary>
    /// <returns></returns>
    public bool CanBeHit()
    {
        bool canBeHit = IsAvailable && Utility.CoordinatesAreAdjacent(Coordinates, GameManager.instance.GetPlayer.Coordinates);
        if (canBeHit)
        {
            m_hitProcessed = false;
        }
        return canBeHit;
    }

    /// <summary>
    /// Enables/Disables the invisible wall
    /// </summary>
    /// <param name="active"></param>
    void EnableInvisibleWall(bool active)
    {
        m_invisibleWall.SetActive(active);
    }

    /// <summary>
    /// Enables/Disables the renderer component to show/hide the tile model
    /// </summary>
    /// <param name="active"></param>
    void ShowRenderer(bool active)
    {
        m_renderer.gameObject.SetActive(active);
    }

    /// <summary>
    /// Triggers the tile fall killing anything that is one it
    /// </summary>
    /// <param name="length"></param>
    /// <param name="time"></param>
    public void DropTile(float drag)
    {
        m_rigidbody.drag = drag;
        m_rigidbody.isKinematic = false;

        m_killables.ForEach(k => k.TriggerDeathByFall());
        m_killables.Clear();

        StartCoroutine(HitProcessDelay());
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
    /// Randomly chooses a sound to play that simulates the tile being destroyed
    /// </summary>
    void PlayTileDestroyedSound()
    {
        System.Random random = new System.Random();
        int randInx = random.Next(m_audioNames.Count);
        AudioManager.instance.PlaySound(m_audioNames[randInx]);
    }

    /// <summary>
    /// Stop moving once the death plane is reached and disables the renderer
    /// </summary>
    /// <param name="other"></param>
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeathPlane"))
        {
            m_rigidbody.isKinematic = true;
            ShowRenderer(false);
        }
    }

    /// <summary>
    /// Save the killable objects if nott already stored
    /// </summary>
    /// <param name="other"></param>
    void OnTriggerStay(Collider other)
    {        
        IKillable killable = other.GetComponent<IKillable>();
        if (killable != null && !m_killables.Contains(killable))
        {
            m_killables.Add(killable);
        }
    }

    /// <summary>
    /// Removes the killable object form the list
    /// </summary>
    /// <param name="other"></param>
    void OnTriggerExit(Collider other)
    {
        IKillable killable = other.GetComponent<IKillable>();
        if (killable != null && m_killables.Contains(killable))
        {
            m_killables.Remove(killable);
        }
    }

    /// <summary>
    /// True when the hit was processed
    /// </summary>
    /// <returns></returns>
    public bool HitProcessed()
    {
        return m_hitProcessed;
    }
}

/// <summary>
/// The different states a tile can be in
/// </summary>
public enum TileState
{
    Active,
    Highlighted,
    Destroyed,
    Fallen, // when it was triggered not by the player to be destroyed
    Void, // A state similar to Destroyed/Fallen but making it be none existent to the player
}
