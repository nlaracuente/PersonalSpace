using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the game loop
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>
    /// A reference to the only instance of the GameManager
    /// </summary>
    public static GameManager instance = null;

    /// <summary>
    /// A reference to the main menu object
    /// </summary>
    [SerializeField]
    GameObject m_mainMenu;

    /// <summary>
    /// A reference to the pause menu object
    /// </summary>
    [SerializeField]
    GameObject m_pauseMenu;

    /// <summary>
    /// A reference to the win menu object
    /// </summary>
    [SerializeField]
    GameObject m_winMenu;

    /// <summary>
    /// A reference to the loss menu object
    /// </summary>
    [SerializeField]
    GameObject m_lossMenu;

    /// <summary>
    /// A reference to the game completed menu object
    /// </summary>
    [SerializeField]
    GameObject m_gameCompletedMenu;

    /// <summary>
    /// A reference to the instructions menu object`
    /// </summary>
    [SerializeField]
    GameObject m_instructionsMenu;
    /// <summary>
    /// Keeps track of the current level
    /// </summary>
    [SerializeField]
    int m_currentLevel = 0;
    public int CurrentLevel { get { return m_currentLevel; } }

    /// <summary>
    /// How long to wait when game over to display the menus
    /// </summary>
    [SerializeField]
    float m_showMenusDelay = 3f;

    /// <summary>
    /// How many cycles the enemy will wait before rechecking the player's position
    /// </summary>
    [SerializeField]
    int m_maximumPlayerTrackingDelay = 1000;

    [SerializeField]
    int m_minimumPlayerTrackingDelay = 100;

    /// <summary>
    /// How much each level will the tracking length change to make the enemies smarter
    /// </summary> 
    [SerializeField]
    int m_trackingChangeRate = 0;

    /// <summary>
    /// A reference to the player component
    /// </summary>
    Player m_player;
    public Player GetPlayer { get { return m_player; } }

    /// <summary>
    /// A collection of all the enemies on the screen
    /// </summary>
    List<Enemy> m_enemies;

    /// <summary>
    /// A reference to the tile manage object
    /// </summary>
    TileManager m_tileManager;    

    /// <summary>
    /// True when this is a brand new game
    /// </summary>
    bool m_isNewGame;

    /// <summary>
    /// True when the game has transitioned from new game to started
    /// </summary>
    bool m_isGameStarted;
    
    /// <summary>
    /// True when the gameplay is running
    /// </summary>
    bool m_isGamePlay = false;

    /// <summary>
    /// True when the current play session is over regardless of reason
    /// </summary>
    bool m_isGameOver;

    /// <summary>
    /// True when waiting for a player to decide whether to continue, rety, or quit
    /// </summary>
    bool m_isWaitingForPlayerResponse;

    /// <summary>
    /// True when the player wants to restart the current level
    /// </summary>
    bool m_isLevelReset;

    /// <summary>
    /// Stores total levels
    /// </summary>
    public int TotalLevels { get; set; }

    /// <summary>
    /// Calculate and returns the tracking delay for the current level
    /// Defaults to minimum if the rate is too low.
    /// </summary>
    int TrackingDelayForCurrentLevel
    {
        get {
            int newRate = m_maximumPlayerTrackingDelay - m_trackingChangeRate * CurrentLevel;
            return Mathf.Max(m_minimumPlayerTrackingDelay, newRate);
        }
    }

    /// <summary>
    /// Creates the GameManager instance
    /// </summary>
    void Awake()
    {
        Setup();
    }

    /// <summary>
    /// Sets this class as a singleton and the file to use to store the game data
    /// </summary>
    public void Setup()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    /// <summary>
    /// Starts the game loop
    /// </summary>
    void Start()
    {
        InitGame();
        StartCoroutine(GameLoopRoutine());
    }

    /// <summary>
    /// For debugging purposes and as a fun easter egg for those who find it
    /// a way to change levels
    /// </summary>
    void ManualLevelChange()
    {
        // Only during gameplay to allow time for the level to load
        if (!m_isGamePlay)
        {
            return;
        }

        bool levelChange = false;
        int levelIndex = CurrentLevel;

        // Skip to next level
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            levelChange = true;
            levelIndex++;

            if(levelIndex >= TotalLevels)
            {
                levelIndex = 0;
            }

        // Previous
        } else if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            levelChange = true;
            levelIndex--;

            if (levelIndex < 0)
            {
                levelIndex = TotalLevels - 1;
            }
        }

        if (levelChange)
        {
            m_currentLevel = levelIndex;
            m_isLevelReset = true;
            m_isGameOver = true;
        }
    }

    /// <summary>
    /// Main Loop which handles initializing the level, loading each level, and triggering gameplay until application is closed
    /// </summary>
    /// <returns></returns>
    IEnumerator GameLoopRoutine()
    {
        while (true)
        {
            yield return StartCoroutine(LoadLevelRoutine());
            yield return StartCoroutine(GamePlayRoutine());
            yield return StartCoroutine(GameOverRoutine());
        }
    }

    /// <summary>
    /// Loads the current level
    /// If this is a new game, then the Main Menu is displayed and we wait until the game has started
    /// </summary>
    /// <returns></returns>
    IEnumerator LoadLevelRoutine()
    {
        AudioManager.instance.PlayMusic(AudioName.LevelMusic);

        m_isGamePlay = false;
        m_isLevelReset = false;
        m_isGameOver = false;
        m_isWaitingForPlayerResponse = false;

        CreateCurrentLevel();
        TurnOffAllMenus();

        if (m_isNewGame)
        {
            m_isNewGame = false;
            m_mainMenu.SetActive(true);

            // Calculate the change rate
            m_trackingChangeRate = m_minimumPlayerTrackingDelay - m_maximumPlayerTrackingDelay / TotalLevels;

            // Wait until the game has started 
            while (!m_isGameStarted)
            {
                yield return null;
            }
        }

        // Finally find the player and the enemies
        m_player = FindObjectOfType<Player>();
        m_enemies = new List<Enemy>(FindObjectsOfType<Enemy>());
    }
    
    /// <summary>
    /// Triggers the enemies to start and allows the player to be controlled
    /// Yields until the game is over
    /// </summary>
    /// <returns></returns>
    IEnumerator GamePlayRoutine()
    {
        m_isGamePlay = true;
        m_player.ControlsEnabled = true;
        Time.timeScale = 1f;

        foreach (Enemy enemy in m_enemies)
        {
            enemy.Initialize(TrackingDelayForCurrentLevel);
        }

        while (!m_isGameOver)
        {
            yield return null;
        }        
    }

    /// <summary>
    /// Checks the reason for game over and triggers the proper response
    /// For player victory/loss it shows the appropiate menu and yields until the player choses
    /// For a restart is simply allows everything to go through as normal
    /// </summary>
    /// <returns></returns>
    IEnumerator GameOverRoutine()
    {      
        m_isGamePlay = false;
        m_player.ControlsEnabled = false;

        if (!m_isLevelReset)
        {
            // Delay showing the screens
            yield return new WaitForSeconds(m_showMenusDelay);

            AudioManager.instance.StopMusic();

            // Loss
            if (m_player.IsDead)
            {
                AudioManager.instance.PlaySound(AudioName.LostMusic);
                ShowLossMenu();
                m_isWaitingForPlayerResponse = true;

                // Victory
            } else if (m_enemies.Count < 1)
            {
                AudioManager.instance.PlaySound(AudioName.WinMusic);

                // Increase to next level
                m_currentLevel++;
                if (m_currentLevel < TotalLevels)
                {
                    ShowWinMenu();
                } else
                {
                    // Go back to the first level
                    m_currentLevel = 0;
                    ShowGameCompletedMenu();
                }

                m_isWaitingForPlayerResponse = true;
            }

            while (m_isWaitingForPlayerResponse)
            {
                yield return null;
            }
        }
    }

    /// <summary>
    /// Shows the title screen
    /// Turn all other menus off
    /// Lock the cursor in place
    /// </summary>
    void InitGame()
    {
        // Keep the cursor within the game screen
        Cursor.lockState = CursorLockMode.Confined;        
        m_isNewGame = true;
    }

    /// <summary>
    /// Disables the title screens and shows instructions
    /// </summary>
    public void StartGame()
    {        
        TurnOffAllMenus();
        m_instructionsMenu.SetActive(true);
    }

    /// <summary>
    /// Closes all menus and starts the game
    /// </summary>
    public void CloseInstructions()
    {
        TurnOffAllMenus();
        m_isGameStarted = true;
    }

    /// <summary>
    /// Sets all menus to disabled
    /// </summary>
    void TurnOffAllMenus()
    {
        m_mainMenu.SetActive(false);
        m_instructionsMenu.SetActive(false);
        m_pauseMenu.SetActive(false);
        m_winMenu.SetActive(false);
        m_lossMenu.SetActive(false);
        m_gameCompletedMenu.SetActive(false);
    }
    
    /// <summary>
    /// Triggers the creation of the current level
    /// </summary>
    void CreateCurrentLevel()
    {
        if(m_tileManager == null)
        {
            m_tileManager = FindObjectOfType<TileManager>();
        }

        m_tileManager.CreateLevel();
    }

    /// <summary>
    /// Handles opening/closing pause menu while in game play
    /// </summary>
    void Update()
    {
        if (m_isGamePlay && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }

        // Check to allow skipping of levels
        ManualLevelChange();
    }    

    /// <summary>
    /// Toggles On/Off the pause menu
    /// </summary>
    public void TogglePauseMenu()
    {
        m_pauseMenu.SetActive(!m_pauseMenu.activeSelf);

        // Freeze/unfreeze time
        if (m_pauseMenu.activeSelf)
        {
            Time.timeScale = 0f;
            m_player.ControlsEnabled = false;
        } else
        {
            Time.timeScale = 1f;
            m_player.ControlsEnabled = true;
        }
    }

    /// <summary>
    /// Shows the win menu screen
    /// </summary>
    public void ShowWinMenu()
    {
        m_winMenu.SetActive(true);
    }

    /// <summary>
    /// Shows the win menu screen
    /// </summary>
    public void ShowGameCompletedMenu()
    {
        m_gameCompletedMenu.SetActive(true);
    }

    /// <summary>
    /// Shows the game over screen
    /// </summary>
    public void ShowLossMenu()
    {
        m_lossMenu.SetActive(true);
    }

    /// <summary>
    /// Triggers a reload of the current scene
    /// </summary>
    public void RestartGame()
    {
        m_isLevelReset = true;
        m_isGameOver = true;
    }

    /// <summary>
    /// Removes the waiting continue to progress the game
    /// </summary>
    public void ContinueGame()
    {
        m_isWaitingForPlayerResponse = false;
    }

    /// <summary>
    /// Removes the waiting continue to replay the current level
    /// </summary>
    public void ReplayLevel()
    {
        m_isWaitingForPlayerResponse = false;
    }

    /// <summary>
    /// Called by an enemy when they are defeated
    /// </summary>
    /// <param name="enemy"></param>
    public void EnemyDefeated(Enemy enemy)
    {
        if (m_enemies.Contains(enemy))
        {
            m_enemies.Remove(enemy);
        }

        if (!m_isGameOver && m_enemies.Count < 1)
        {
            m_isGameOver = true;
        }
    }

    /// <summary>
    /// Look at tile being destroyed
    /// </summary>
    /// <param name="other"></param>
    public void PlayerLookAt(Transform other)
    {
        m_player.transform.LookAt(other);
    }

    /// <summary>
    /// Triggers the menu for player losing
    /// </summary>
    public void PlayerLost()
    {
        m_isGameOver = true;
    }

    /// <summary>
    /// Exits out of the application
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
    }
}
