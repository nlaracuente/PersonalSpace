using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the tiles on screen to destroy sections as the player triggers them
/// </summary>
public class TileManager : MonoBehaviour
{
    /// <summary>
    /// The tile prefab to instantiate
    /// </summary>
    [SerializeField]
    GameObject m_tilePrefab;

    /// <summary>
    /// The prefab surrounding the level functioning as its border
    /// </summary>
    [SerializeField]
    GameObject m_edgePrefab;

    /// <summary>
    /// The prefab for the player
    /// </summary>
    [SerializeField]
    GameObject m_playerPrefab;

    /// <summary>
    /// The prefab for an enemy
    /// </summary>
    [SerializeField]
    GameObject m_enemyPrefab;

    /// <summary>
    /// The prefab for a satellite on the left
    /// </summary>
    [SerializeField]
    GameObject m_leftSatellite;


    /// <summary>
    /// The prefab for a satellite on the right
    /// </summary>
    [SerializeField]
    GameObject m_rightSatellite;

    /// <summary>
    /// The prefab for a horizontal laser
    /// </summary>
    [SerializeField]
    GameObject m_horizontalLaser;

    /// <summary>
    /// The prefab for a vertical laser
    /// </summary>
    [SerializeField]
    GameObject m_verticalLaser;

    /// <summary>
    /// Width and breadth of the map
    /// </summary>
    [SerializeField]
    Vector2 m_mapSize;

    /// <summary>
    /// The min, max range to use when determining the speed to drop the tiles at
    /// </summary>
    [SerializeField]
    Vector2 m_dropRate = new Vector2(.25f, 1f);

    /// <summary>
    /// Minimal number of supported sides to count the tile as "supported"
    /// </summary>
    [SerializeField, Range(1, 4)]
    int m_minSupport = 1;

    /// <summary>
    /// Minimum amount of tiles falling to triggle large crumble sound effect
    /// </summary>
    [SerializeField]
    int m_minTilesFalling = 6;

    /// <summary>
    /// A collections of tiles currently highlighted
    /// </summary>
    List<Tile> m_highlightedTiles = new List<Tile>();

    /// <summary>
    /// Contains all tiles located based on their coordinates
    /// </summary>
    Dictionary<Vector2, Tile> m_tilemap = new Dictionary<Vector2, Tile>();

    /// <summary>
    /// A reference to the nav mesh controller
    /// </summary>
    NavMeshController m_meshController;

    /// <summary>
    /// Maps pixle colors to prefabs
    /// </summary>
    Dictionary<Color32, PixleColorToGameObject> m_definitionTable = new Dictionary<Color32, PixleColorToGameObject>();

    /// <summary>
    /// True when everything on the map has been removed
    /// </summary>
    bool m_isMapCleared = false;

    /// <summary>
    /// Contains a list of pixle color to object definitions
    /// </summary>
    [SerializeField]
    List<PixleColorToGameObject> m_pixlesToGameObjects;
    public List<PixleColorToGameObject> TileDefinitions { get { return m_pixlesToGameObjects; } }

    /// <summary>
    /// A collection of all the textures of the levels to be played in the order they are to be played 
    /// </summary>
    [SerializeField]
    List<Texture2D> m_levelTextures;

    /// <summary>
    /// A reference to the edge controller object
    /// </summary>
    EdgeController m_edgeController;

    /// <summary>
    /// Keeps track of the total tiles for the current loaded map in x,y
    /// </summary>
    Vector2 m_currentMapSize = Vector2.zero;

    /// <summary>
    /// Builds the tile map along its properties
    /// </summary>
    public void CreateLevel()
    {
        m_edgeController = GameObject.FindGameObjectWithTag("EdgeController").GetComponent<EdgeController>();
        InitializeTileMap();
        SetTileNeighbors();

        if(m_meshController == null)
        {
            m_meshController = FindObjectOfType<NavMeshController>();
        }
        m_meshController.Build();
    }

    /// <summary>
    /// Creates the map and spawn all the tiles
    /// </summary>
    void InitializeTileMap()
    {
        ClearMap();
        m_edgeController.DestroyAllChildren();
        GameManager.instance.TotalLevels = m_levelTextures.Count;
        CreateDefinitionTable();
        CreateMap();
    }

    /// <summary>
    /// Converts pixle color prefab relationships into a hash table
    /// </summary>
    void CreateDefinitionTable()
    {
        // Already defined - D.R.Y.
        if (m_definitionTable.Count > 0)
        {
            return;
        }

        foreach (PixleColorToGameObject definition in TileDefinitions)
        {
            if (!m_definitionTable.ContainsKey(definition.color))
            {
                m_definitionTable.Add(definition.color, definition);
            }
        }
    }

    /// <summary>
    /// Removes all childrens thus clearing the map
    /// </summary>
    public void ClearMap()
    {
        m_isMapCleared = false;
        List<GameObject> children = new List<GameObject>();

        foreach (Transform child in transform)
        {
            children.Add(child.gameObject);
        }

        children.ForEach(child => {
            if (child != null)
            {
                DestroyImmediate(child);
            }
        });

        m_tilemap.Clear();
        m_highlightedTiles.Clear();
    }

    /// <summary>
    /// Stores the texture map data from the resources folder when one is not already set
    /// </summary>
    public Texture2D GetCurentLevelTextureMap()
    {
        return m_levelTextures[GameManager.instance.CurrentLevel];
    }

    /// <summary>
    /// Spawns all the prefabs defined in the tile map texture
    /// </summary>
    void CreateMap()
    {
        Texture2D textureMap = GetCurentLevelTextureMap();

        int mapWidth = textureMap.width;
        int mapHeight = textureMap.height;

        m_currentMapSize = new Vector2(mapWidth, mapHeight);

        for (int x = -1; x <= mapWidth; x++)
        {
            for (int y = -1; y <= mapHeight; y++)
            {
                Vector2 coords = new Vector2(x, y);

                // Edge
                if (x < 0 || x >= m_mapSize.x || y < 0 || y >= m_mapSize.y)
                {
                    SpawnEdgeAt(coords, textureMap);
                    continue;
                }

                // Get the prefab associated with the current pixle color
                Color32 colorId = textureMap.GetPixel(x, y);
                PixleColorToGameObject info = GetObjectInfoByColorId(colorId);

                // Always spawn a tile
                Tile tile = SpawnTileAt(coords);
                m_tilemap.Add(coords, tile);

                // Base on the tile color we may need to either turn the tile into void
                // or spawn an enemy or player
                switch (info.type)
                {
                    case ObjectType.Enemy:
                        GameObject enemy = Instantiate(m_enemyPrefab, transform);
                        
                        enemy.transform.position = new Vector3(
                            coords.x,
                            enemy.transform.position.y,
                            coords.y
                        );

                        break;

                    case ObjectType.Player:
                        GameObject player = Instantiate(m_playerPrefab, transform);
                        player.transform.position = new Vector3(
                            coords.x,
                            player.transform.position.y,
                            coords.y
                        );
                        break;

                    case ObjectType.Void:
                        tile.State = TileState.Void;
                        break;
                }                
            }
        }
    }

    /// <summary>
    /// Returns the associated prefab if the given color id is recognize
    /// Returns NULL when a match is not found
    /// </summary>
    /// <param name="colordId"></param>
    /// <returns></returns>
    PixleColorToGameObject GetObjectInfoByColorId(Color32 colordId)
    {
        PixleColorToGameObject info = new PixleColorToGameObject();

        if (m_definitionTable.ContainsKey(colordId))
        {
            info = m_definitionTable[colordId];
        } else
        {
            Debug.Log("Color ID not found: " + colordId);
        }

        return info;
    }
   
    /// <summary>
    /// Loops through all tiles on the map and adds their adjencent neighbor based 
    /// on the cardinal points
    /// </summary>
    void SetTileNeighbors()
    {
        foreach (KeyValuePair<Vector2, Tile> kvp in m_tilemap)
        {
            Tile tile = kvp.Value;
            tile.Neighbors.Clear();

            foreach (Vector2 point in Utility.FourCardinalPoints)
            {
                Vector2 coords = tile.Coordinates + point;
                Tile neighbor = GetTileAt(coords);

                if (neighbor != null)
                {
                    tile.Neighbors.Add(neighbor);
                }
            }
        }
    }

    /// <summary>
    /// Returns the tile located at the given position
    /// </summary>
    /// <param name="coords"></param>
    /// <returns></returns>
    public Tile GetTileAt(Vector2 coords)
    {
        Tile tile = null;

        if (m_tilemap.ContainsKey(coords))
        {
            tile = m_tilemap[coords];
        }

        return tile;
    }

    /// <summary>
    /// Spawns an edge tile at the given coordinates
    /// </summary>
    /// <param name="coords"></param>
    void SpawnEdgeAt(Vector2 coords, Texture2D textureMap)
    {
        // The prefab to spawn
        // Defaults to the old edge prefab to be safe
        GameObject prefab = m_edgePrefab;

        int mapWidth = textureMap.width;
        int mapHeight = textureMap.height;

        // Coordinates hash table that represent the corners
        Dictionary<string, Vector2> corners = new Dictionary<string, Vector2>() {
            { "topLeft", new Vector2(-1, mapHeight) },
            { "topRight", new Vector2(mapWidth, mapHeight) },
            { "bottomLeft", new Vector2(-1, -1) },
            { "bottomRight", new Vector2(mapWidth, -1) },
        };

        // Corner Satellites
        if (corners.ContainsValue(coords))
        {
            string location = corners.FirstOrDefault(x => x.Value == coords).Key;
            switch (location)
            {
                case "topLeft":
                case "bottomLeft":
                    prefab = m_leftSatellite;
                    break;

                case "topRight":
                case "bottomRight":
                    prefab = m_rightSatellite;
                    break;
            }

        // Lasers
        } else
        {
            // Sides
            if(coords.x < 0 || coords.x >= mapWidth)
            {
                prefab = m_verticalLaser;

            // Top/Bottom
            } else
            {
                prefab = m_horizontalLaser;
            }
        }

        GameObject edgeObject = Instantiate(prefab, m_edgeController.transform);
        edgeObject.transform.position = new Vector3(coords.x, 0f, coords.y);
        edgeObject.name = string.Format("Edge_{0}_{1}", coords.x, coords.y);
    }

    /// <summary>
    /// Spawns and returns a tile at the given coordinate
    /// </summary>
    /// <param name="coords"></param>
    /// <returns></returns>
    Tile SpawnTileAt(Vector2 coords)
    {
        GameObject tileObject = Instantiate(m_tilePrefab, transform);
        tileObject.transform.position = new Vector3(coords.x, 0f, coords.y);
        tileObject.name = string.Format("Tile_{0}_{1}", coords.x, coords.y);

        Tile tile = tileObject.GetComponent<Tile>();
        tile.Coordinates = coords;
        tile.SaveCurrentPosition();

        return tile;
    }

    /// <summary>
    /// Marks all current highlighted tiles as active before searching which ones
    /// to re-highlight
    /// </summary>
    /// <param name="fromCoords"></param>
    public void HighlightSurroundingTiles(Vector2 fromCoords)
    {
        m_highlightedTiles.ForEach(tile => {
            if(tile.IsAvailable)
            {
                tile.State = TileState.Active;
            }           
        });

        m_highlightedTiles.Clear();
    
        foreach (Vector2 point in Utility.AllCardinalPoints)
        {
            Vector2 coords = fromCoords + point;            
            if(TileAtPositionIsAvailable(coords))
            {
                Tile tile = GetTileAt(coords);
                tile.State = TileState.Highlighted;
                m_highlightedTiles.Add(tile);
            }
        }
    }

    /// <summary>
    /// True when the there's an available tile at the given coords
    /// </summary>
    /// <param name="coords"></param>
    /// <returns></returns>
    public bool TileAtPositionIsAvailable(Vector2 coords, bool mustBeEmpty = true)
    {
        bool isAvailable = false;

        Tile tile = GetTileAt(coords);
        if(tile != null)
        {
            if (mustBeEmpty)
            {
                isAvailable = tile.IsAvailableAndEmpty;
            } else {
                isAvailable = tile.IsAvailable;
            }
        }

        return isAvailable;
    }

    /// <summary>
    /// Marks the tile as destroyed and triggers all surrounding tiles
    /// to be destroyed that are not currently destroyed if they are no longer supported
    /// </summary>
    /// <param name="tile"></param>
    public void DestroyTile(Tile tile)
    {
        // Make the player face this tile
        GameManager.instance.PlayerLookAt(tile.transform);

        // Mark Tile as destroy and drop it
        tile.State = TileState.Destroyed;
        DropTile(tile);

        // Check first if a large land mass is destroyed
        if (!TriggerLandMassFallingState(tile))
        {
            // Check for anything smaller that needs to be destroyed
            TriggerNeighborTilesFallingState(tile);
        }        
    }

    /// <summary>
    /// Checks if the player has cut large land masses and decides which ones to 
    /// destroy if it meets the requirements. 
    /// Returns true when this was triggerd
    /// </summary>
    /// <param name="tile"></param>
    /// <returns></returns>
    bool TriggerLandMassFallingState(Tile tile)
    {
        bool triggered = false;

        List<Tile> allNeighbors = GetAllDestroyedNeighbors(tile, new List<Tile>());

        if (allNeighbors.Count < 1)
        {
            return triggered;
        }

        bool left = false;
        bool right = false;
        bool top = false;
        bool bottom = false;

        Tile tileA = null;
        Tile tileB = null;

        allNeighbors.ForEach(n => {
            if (!left){
                left = n.Coordinates.x == 0;
            }

            if (!right){
                right = n.Coordinates.x == m_currentMapSize.x - 1;
            }

            if (!top){
                top = n.Coordinates.y == m_currentMapSize.y - 1;
            }

            if (!bottom){
                bottom = n.Coordinates.y == 0;
            }
        });

        // The list of tiles adjacent
        List<Tile> adjacent = new List<Tile>() {null, null};

        // Get tiles above/below
        Vector2 axis = new Vector2(0, 1);
        List<Tile> yAxis = GetFirstInstanceOfAdjacentTiles(allNeighbors, axis);

        // Get tiles left/right
        axis = new Vector2(1, 0);
        List<Tile> xAxis = GetFirstInstanceOfAdjacentTiles(allNeighbors, axis);

        // Wild axis represents an axis that can go positive or negative
        // when comparing neighbors to ensure we don't have the same list
        Tile wildAxis = null;

        // Horizontal cut
        if (left && right)
        {
            adjacent = yAxis;
        }                     
            
        // Vertical cut
        else if (top && bottom)
        {
            adjacent = xAxis;
        }

        // A combination of a vertical and horizontal cut
        else if( (left && bottom) || (right && bottom) || (left && top) || (right && top) )
        {
            // The axis always come back with
            // [0] == positive axis
            // [1] == negative axis

            // x, y (-1, 1)
            if (left && bottom)
            {
                adjacent[0] = xAxis[1];
                adjacent[1] = yAxis[0];
            }

            // x, y (-1, -1)
            if (left && top)
            {
                adjacent[0] = xAxis[1];
                adjacent[1] = yAxis[1];
            }

            // x, y (1, 1)
            if (right && bottom)
            {
                adjacent[0] = xAxis[0];
                adjacent[1] = yAxis[0];
            }

            // x, y (1, -1)
            if (right && top)
            {
                adjacent[0] = xAxis[0];
                adjacent[1] = yAxis[1];
            }

        // We may have a u like shape that starts and ends on the same side
        // in this case, we will try to find both unique land masses
        // * == means this axist could go either positive/negative to get that we need
        // adjacent[0] == will hold the axis we known to always be unique for the direction
        } else
        {
            // Top: (*, 1)
            if (top)
            {
                adjacent[0] = yAxis[0];
                adjacent[1] = xAxis[0];
                wildAxis = xAxis[1];
            }

            // bottom: (*, -1)
            if (bottom)
            {
                adjacent[0] = yAxis[1];
                adjacent[1] = xAxis[0];
                wildAxis = xAxis[1];
            }

            // Left: (-1, *)
            if (left)
            {
                adjacent[0] = xAxis[1];
                adjacent[1] = yAxis[0];
                wildAxis = yAxis[1];
            }

            // Right: (1, *)
            if (right)
            {
                adjacent[0] = xAxis[0];
                adjacent[1] = yAxis[0];
                wildAxis = yAxis[1];
            }
        }

        // Save the tiles
        tileA = adjacent[0];
        tileB = adjacent[1];

        // We have tiles to play with
        if (tileA != null && tileB != null)
        {
            triggered = true;

            // Keeps track of all the neighbors for each of the land masses
            List<Tile> landMassA = GetAllAvailableNeighbors(tileA, new List<Tile>());
            List<Tile> landMassB = GetAllAvailableNeighbors(tileB, new List<Tile>());

            // If they are both the same land mass then try the wild axis
            bool sameLandMass = landMassA.All(landMassB.Contains) && landMassA.Count == landMassB.Count;

            if(sameLandMass && wildAxis != null)
            {
                landMassB = GetAllAvailableNeighbors(wildAxis, new List<Tile>());

                // Re-test to be safe
                sameLandMass = landMassA.All(landMassB.Contains) && landMassA.Count == landMassB.Count;
            }

            // As long as they are not the same then trigger the destroy
            // also make sure we have non-zero tiles land masses
            if (!sameLandMass && landMassA.Count > 0 && landMassB.Count > 0)
            {
                BreakLandMasses(landMassA, landMassB);
            }            
        }

        return triggered;
    }

    /// <summary>
    /// Returns the first to instances of tiles adjacents to any of the tiles in the given list
    /// for the given axis (1, 0) for X [left/right] (0, 1) for Y [up/down]
    /// </summary>
    /// <param name="allNeighbors"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    List<Tile> GetFirstInstanceOfAdjacentTiles(List<Tile> allNeighbors, Vector2 axis)
    {
        Tile tileA = null;
        Tile tileB = null;
        List<Tile> adjacents = new List<Tile>();

        Queue<Tile> neighbors = new Queue<Tile>(allNeighbors);

        // Try to find a tile above and bellow within the list of neighbors
        while ((tileA == null || tileB == null) && neighbors.Count > 0)
        {
            Vector2 coords = neighbors.Dequeue().Coordinates;

            // Positive Axis
            if (tileA == null)
            {
                Vector2 offset = coords + (axis * 1);
                tileA = GetTileAt(offset);

                // Looking for non-destroyed tiles
                if (tileA != null && !tileA.IsAvailable)
                {
                    tileA = null;
                }
            }

            // Negagtive Axis
            if (tileB == null)
            {
                Vector2 offset = coords + (axis * -1);
                tileB = GetTileAt(offset);

                // Looking for non-destroyed tiles
                if (tileB != null && !tileB.IsAvailable)
                {
                    tileB = null;
                }
            }
        }

        adjacents.Add(tileA);
        adjacents.Add(tileB);

        return adjacents;
    }

    /// <summary>
    /// Checks for all tiles that neighbor this tile and triggers them to fall
    /// should they be in a state where this should happen
    /// </summary>
    /// <param name="tile"></param>
    void TriggerNeighborTilesFallingState(Tile tile)
    {
        // Get all neighbors that are in a state that could pontentially remove them
        List<Tile> potentialNeighbors = GetNeighborToPotentiallyDestroy(tile);

        // Neigbors are not in a pontential destroyable state
        if (potentialNeighbors.Count < 1)
        {
            return;
        }

        // For each neighbor in a destroyable state, ensure all of its neighboring tiles are in the same state
        foreach(Tile potential in potentialNeighbors)
        {
            // Get a full list of neighboring tiles until the end of the list is reached or a destroyed tile is found
            // Utility.DepthFirstTreeTraversal(potential, c=>c.Neighbors).ToList(); //             
            List<Tile> allNeighbors = GetAllAvailableNeighbors(potential, new List<Tile>());

            // Clean the list to ensure we only have unique tiles
            allNeighbors.Distinct();

            // Has no neighbors therefore it should be dropped!
            if (allNeighbors.Count < 1)
            {
                MakeTileFall(potential);
                continue;
            }

            List<bool> supported = new List<bool>();

            // if all the neighbors are destroyable then we will destroy them all
            foreach (Tile neighbor in allNeighbors)
            {
                supported.Add(IsTileSupported(neighbor));
            }

            // Drop the tiles
            if (!supported.Contains(true))
            {
                BreakLandMass(allNeighbors);
            }
        }
    }

    /// <summary>
    /// Determines which the land masses given to destroy and triggers their destruction
    /// </summary>
    /// <param name="landMassA"></param>
    /// <param name="landMassB"></param>
    void BreakLandMasses(List<Tile> landMassA, List<Tile> landMassB)
    {
        // Landmass A is smaller, destroy that one
        if (landMassA.Count < landMassB.Count)
        {
            BreakLandMass(landMassA);

        // Landmass B is smaller, destroy that one
        } else if (landMassB.Count < landMassA.Count)
        {
            BreakLandMass(landMassB);

        // Both are the same
        } else
        {
            Tile playerTile = GetTileAt(GameManager.instance.GetPlayer.Coordinates);

            // Favor the mass the player is on first
            if (landMassA.Contains(playerTile))
            {
                BreakLandMass(landMassB);
            } else
            {
                BreakLandMass(landMassA);
            }
        }
    }

    /// <summary>
    /// Triggers all the tiles in the land mass to "fall"
    /// </summary>
    /// <param name="landMass"></param>
    void BreakLandMass(List<Tile> landMass)
    {
        // Audio is based on total neiborgs
        if (landMass.Count > m_minTilesFalling)
        {
            AudioManager.instance.PlaySound(AudioName.CrumbleBig);
        } else
        {
            AudioManager.instance.PlaySound(AudioName.CrumbleSmall);
        }

        landMass.ForEach(t => { MakeTileFall(t); });
    }

    /// <summary>
    /// Triggers the tile to drop
    /// </summary>
    /// <param name="tile"></param>
    void DropTile(Tile tile)
    {
        float drag = Random.Range(m_dropRate.x, m_dropRate.y);       
        tile.DropTile(drag);
    }

    /// <summary>
    /// Updates the state of the tile to Fallen and drops it
    /// </summary>
    /// <param name="tile"></param>
    void MakeTileFall(Tile tile)
    {
        tile.State = TileState.Fallen;
        DropTile(tile);
    }

    /// <summary>
    /// Returns a collection of all the neighbors associated with the given tile and their neighbors
    /// </summary>
    /// <param name="root"></param>
    /// <param name="current"></param>
    /// <returns></returns>
    List<Tile> GetAllAvailableNeighbors(Tile root, List<Tile> current)
    {
        foreach (Tile child in root.Neighbors)
        {
            if (!current.Contains(child) && child.IsAvailable)
            {
                current.Add(child);
                GetAllAvailableNeighbors(child, current);
            }
        }

        // Clean the list to ensure we only have unique values
        return current;
    }

    /// <summary>
    /// Returns a recursive list of all neighbors currently disabled
    /// </summary>
    /// <param name="root"></param>
    /// <param name="current"></param>
    /// <returns></returns>
    List<Tile> GetAllDestroyedNeighbors(Tile root, List<Tile> current)
    {
        foreach (Tile child in root.Neighbors)
        {
            if (!current.Contains(child) && !child.IsAvailable)
            {
                current.Add(child);
                GetAllDestroyedNeighbors(child, current);
            }
        }

        // Clean the list to ensure we only have unique values
        return current;
    }

    /// <summary>
    /// Returns the first instance of a neighboring tile that is no longer supported
    /// Meaning that towards each cardinal point the last tile is destroyed
    /// </summary>
    /// <param name="tile"></param>
    /// <returns></returns>
    List<Tile> GetNeighborToPotentiallyDestroy(Tile tile)
    {
        List<Tile> potentialTiles = new List<Tile>();

        foreach (Tile neighbor in tile.Neighbors)
        {
            if (neighbor.IsAvailable && !IsTileSupported(neighbor))
            {
                potentialTiles.Add(neighbor);
            }
        }

        return potentialTiles;
    }

    /// <summary>
    /// Tiles that have three or more structures connected to an edge will remain
    /// All others will need to fall
    /// </summary>
    /// <param name="tile"></param>
    /// <returns></returns>
    bool IsTileSupported(Tile tile)
    {
        // Destroyed so skip
        if(!tile.IsAvailable)
        {
            return false;
        }

        // How many sides of the tile are supported
        int supportedCount = 0;

        foreach (Vector2 point in Utility.FourCardinalPoints)
        {
            Tile neighbor = tile;
            Tile lastTile = null;

            do
            {
                neighbor = GetTileAt(neighbor.Coordinates + point);

                // Found the last tile or a tile that is destroyed
                if (neighbor == null || !neighbor.IsAvailable)
                {
                    lastTile = neighbor;
                    neighbor = null;
                }

            } while (neighbor != null);

            // Last tile is an edge or still available
            if (lastTile == null || lastTile.IsAvailable)
            {
                supportedCount++;
            }
        }

        // At least 3 sides must be supported
        return supportedCount >= m_minSupport;
    }

    /// <summary>
    /// Returns a randomly selected tile from the tile map that is available
    /// </summary>
    /// <returns></returns>
    public Tile GetRandomActiveTile(Vector2 coords)
    {
        // Get all the neighbor tiles for this tile
        Tile tile = GetTileAt(coords);
        List<Tile> listToShuffle = m_tilemap.Values.ToList();

        if (tile != null)
        {
            List<Tile> neighbors = GetAllAvailableNeighbors(tile, new List<Tile>());

            if(neighbors.Count > 0)
            {
                listToShuffle = neighbors;
            }
        }

        System.Random rand = new System.Random();
        Tile[] tiles = Utility.ShuffleArray<Tile>(listToShuffle.ToArray(), rand.Next(99));
        int maxValue = tiles.Length;

        Tile finalTile = null;
        while(finalTile == null)
        {
            Tile t = tiles[rand.Next(maxValue)];
            if (t.IsAvailable)
            {
                finalTile = t;
            }
        }

        return finalTile;
    }
}
