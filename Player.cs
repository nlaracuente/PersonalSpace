using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Controllers and managers the player's inputs and actions
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour, IKillable
{
    /// <summary>
    /// The length of the ray cast from the camera to the playing field
    /// when detecting mouse position
    /// </summary>
    [SerializeField]
    float m_camRayLength = Mathf.Infinity;

    /// <summary>
    /// The mask that represents the floor
    /// </summary>
    [SerializeField]
    LayerMask m_hamerHitLayer;

    /// <summary>
    /// A reference to the rigidbody component
    /// </summary>
    [SerializeField]
    Rigidbody m_rigidbody;

    /// <summary>
    /// How fast the player moves
    /// </summary>
    [SerializeField]
    float m_moveSpeed = 6f;

    /// <summary>
    /// How fast the player rotates
    /// </summary>
    [SerializeField]
    float m_rotationSpeed = 12f;

    /// <summary>
    /// When false the player can hold down the mouse button to continue to trigger tiles
    /// </summary>
    [SerializeField]
    bool m_mouseToggle = false;

    /// <summary>
    /// A reference to the animator component
    /// </summary>
    [SerializeField]
    Animator m_animator;

    /// <summary>
    /// Holds the current keyboard input value
    /// </summary>
    Vector3 m_input = Vector3.zero;

    /// <summary>
    /// A reference to the tile manager
    /// </summary>
    TileManager m_tileManager;

    /// <summary>
    /// True when the player can trigger the hammer
    /// </summary>
    bool m_canHammer = true;

    /// <summary>
    /// True when the hammer is in the hit state
    /// </summary>
    bool m_hammerHasHit;

    /// <summary>
    /// True when the action to trigger the hammer was received
    /// </summary>
    bool m_hammerTriggered = false;

    /// <summary>
    /// True when the hammer button is no longer being pressed
    /// </summary>
    bool m_hammerButtonReleased = true;

    /// <summary>
    /// True while the moving routine is running
    /// </summary>
    [SerializeField]
    bool m_isMoving;

    /// <summary>
    /// True while the rotation routine is running
    /// </summary>
    [SerializeField]
    bool m_isRotating;

    /// <summary>
    /// How long to wait for the hammer to "HIT" and trigger the hit action
    /// </summary>
    [SerializeField]
    float m_hammerHitDelay = .3f;

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
    /// When the player is no longer alive
    /// </summary>
    public bool IsDead { get; set; }

    /// <summary>
    /// True when the player is allowed to control the avatar
    /// </summary>
    public bool ControlsEnabled { set; get; }

    /// <summary>
    /// When true, player cannot move but can still hammer
    /// </summary>
    public bool IsMovementDisabled { get; set; }

    /// <summary>
    /// True when the player is an disabled starte
    /// </summary>
    public bool IsPlayerDisabled
    {
        get { return IsDead || IsMovementDisabled; }
    }

    /// <summary>
    /// Implementation from interface, returns this game object
    /// </summary>
    public GameObject SourceGO { get { return gameObject; } }

    /// <summary>
    /// Initialize
    /// </summary>
    private void Start()
    {
        m_tileManager = FindObjectOfType<TileManager>();
        m_rigidbody = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Player input
    /// </summary>
    void Update()
    {
        if (IsDead)
        {
            return;
        }

        HandleMouseInput();
        SaveKeyboardInput();
        m_tileManager.HighlightSurroundingTiles(Coordinates);
    }

    /// <summary>
    /// Handles rigidbody controls
    /// </summary>
    void FixedUpdate()
    {
        if (IsDead || IsMovementDisabled)
        {
            return;
        }

        Move(m_input);
        Rotate(m_input);
    }

    /// <summary>
    /// Stores the player's keyboard input
    /// </summary>
    void SaveKeyboardInput()
    {
        if (!ControlsEnabled)
        {
            m_animator.SetFloat("Speed", 0f);
            return;
        }

        m_input = new Vector3(
            Input.GetAxisRaw("Horizontal"),
            0f,
            Input.GetAxisRaw("Vertical")
        );
    }

    /// <summary>
    /// Handles player mouse inputs
    /// </summary>
    void HandleMouseInput()
    {
        if (!ControlsEnabled)
        {
            return;
        }

        // Not clicking on anything
        if (m_mouseToggle && !Input.GetButtonDown("Fire1"))
        {
            return;
        } else if (!m_mouseToggle && !Input.GetButton("Fire1"))
        {
            return;
        } else
        {
            m_hammerButtonReleased = true;
        }

        // Already triggered - wait until it is done
        if (!m_hammerTriggered && m_hammerButtonReleased)
        {
            // Create a ray from the mouse cursor on screen in the direction of the camera.
            Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Create a RaycastHit variable to store information about what was hit by the ray.
            RaycastHit hit;
            Debug.DrawRay(camRay.origin, camRay.direction);

            // Perform the raycast and if it hits something on the floor layer...
            if (Physics.Raycast(camRay, out hit, m_camRayLength, m_hamerHitLayer))
            {
                IHammerHittable target = hit.collider.GetComponent<IHammerHittable>();
                Tile tile = hit.collider.GetComponent<Tile>();

                if (target != null && tile != null && target.CanBeHit())
                {
                    TriggerHammerAction(target);
                }
            }
        }
    }

    /// <summary>
    /// Moves the player towards the given direction
    /// </summary>
    /// <param name="direction"></param>
    void Move(Vector3 direction)
    {
        if (direction == Vector3.zero)
        {
            if (!m_isMoving) {
                m_animator.SetFloat("Speed", 0f);
            }

            return;
        } else
        {
            m_animator.SetFloat("Speed", 1f);
        }

        Vector3 curPosition = new Vector3(Coordinates.x, m_rigidbody.position.y, Coordinates.y);
        Vector3 destination = curPosition + direction;
        Vector2 coords = new Vector2(destination.x, destination.z);

        bool canMove = true;

        // Tile itself must be available
        bool isTileAvailable = m_tileManager.TileAtPositionIsAvailable(coords, false);

        // When moving at an angle, the two adjecent tiles shared by the player and the desitination
        // must also be available to allow movement
        Vector2 dirPoint = new Vector2(direction.x, direction.z);
        if (!m_isMoving && Utility.CornerCardinalPoints.Contains(dirPoint))
        {
            canMove = CanMoveToCorner(coords);
        }

        if (!m_isMoving && canMove && isTileAvailable)
        {
            StartCoroutine(MoveRoutine(destination));
        }
    }

    /// <summary>
    /// Returns <see langword="true"/>when the corner tile is in a state the player can move to
    /// </summary>
    /// <param name="coords"></param>
    /// <returns></returns>
    bool CanMoveToCorner(Vector2 coords)
    {
        Tile destTile = m_tileManager.GetTileAt(coords);
        Tile playerTile = m_tileManager.GetTileAt(Coordinates);

        // Corners maybe at the end of the map and be null
        if (destTile == null || playerTile == null){
            return false;
        }

        List<Tile> sharedNeighbors = new List<Tile>();
        foreach (Tile neighbor in destTile.Neighbors)
        {
            if (playerTile.Neighbors.Contains(neighbor))
            {
                sharedNeighbors.Add(neighbor);
            }
        }

        int totalUnavailable = 0;
        sharedNeighbors.ForEach(t =>
        {
            if (!t.IsAvailable)
            {
                totalUnavailable++;
            }
        });

        // If all neighbors are not available then the player cannot go there
        return totalUnavailable != sharedNeighbors.Count;
    }

    /// <summary>
    /// Smoothly moves the player to the desitnation
    /// </summary>
    /// <param name="destination"></param>
    /// <returns></returns>
    IEnumerator MoveRoutine(Vector3 destination)
    {
        m_isMoving = true;

        while (Vector3.Distance(m_rigidbody.position, destination) > 0.1f)
        {
            Vector3 direction = (destination - m_rigidbody.position).normalized;
            m_rigidbody.MovePosition(m_rigidbody.position + direction * m_moveSpeed * Time.deltaTime);
            yield return new WaitForFixedUpdate();
        }

        m_rigidbody.MovePosition(destination);
        m_isMoving = false;

    }

    /// <summary>
    /// Rotates the player to face the given direction
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    void Rotate(Vector3 direction)
    {
        if (direction == Vector3.zero)
        {
            return;
        }

        if (!m_isRotating)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);          
            StartCoroutine(RotateRoutine(targetRotation));
        }
    }

    /// <summary>
    /// Smoothly rotates towards the target
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    IEnumerator RotateRoutine(Quaternion target)
    {
        m_isRotating = true;

        while (Quaternion.Angle(m_rigidbody.rotation, target) > 0.1f)
        {
            m_rigidbody.MoveRotation(
                Quaternion.Lerp(m_rigidbody.rotation,
                                target,
                                m_rotationSpeed * Time.deltaTime)
            );

            yield return new WaitForFixedUpdate();
        }

        m_rigidbody.MoveRotation(target);
        m_isRotating = false;
    }

    /// <summary>
    /// Triggers death by falling sequence
    /// </summary>
    public void TriggerDeathByFall()
    {
        if (IsDead)
        {
            return;
        }
 
        IsDead = true;
        m_animator.SetTrigger("Fall");
        AudioManager.instance.PlaySound(AudioName.PlayerFall);
        GameManager.instance.PlayerLost();
    }

    /// <summary>
    /// Triggers death by enemy sequence
    /// </summary>
    public void TriggerDeathByEnemy()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        m_animator.SetTrigger("Fall");
        AudioManager.instance.PlaySound(AudioName.SlimeTouch);
        GameManager.instance.PlayerLost();
    }

    /// <summary>
    /// Process the request to hit the given object
    /// Disables player movement
    /// 
    /// </summary>
    /// <param name="target"></param>
    public void TriggerHammerAction(IHammerHittable target)
    {
        if (m_canHammer)
        {
            m_hammerTriggered = true;
            m_hammerButtonReleased = false;
            StartCoroutine(HammerActionRoutine(target));
        }
    }

    /// <summary>
    /// Triggers the hammer animation and hitting the target
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    IEnumerator HammerActionRoutine(IHammerHittable target)
    {
        IsMovementDisabled = true;
        m_canHammer = false;
        m_hammerHasHit = false;

        // Before hammering, if we are moving we must wait until reaching the destination
        while (m_isMoving)
        {
            yield return null;
        }

        // If the target can still be hit, then hit it
        if (target.CanBeHit())
        {
            m_animator.SetTrigger("Hammer");
            target.OnHammerHit();

            // Wait until the hammer connects and triggers the expected result before resuming control
            while (!target.HitProcessed())
            {
                yield return null;
            }
        }        

        m_canHammer = true;
        m_hammerTriggered = false;
        IsMovementDisabled = false;
    }

    /// <summary>
    /// Triggered by the animator when the hammer has reached a HIT state
    /// </summary>
    public void HammerHasHit()
    {
        m_hammerHasHit = true;
    }
}
