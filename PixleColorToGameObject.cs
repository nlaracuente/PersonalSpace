using UnityEngine;

/// <summary>
/// Contains the convertion of pixle color to game object
/// </summary>
[System.Serializable]
public struct PixleColorToGameObject
{
    public Color32 color;
    public GameObject prefab;
    public ObjectType type;
}

/// <summary>
/// Holds the different types of objects to spawn
/// </summary>
public enum ObjectType
{
    Tile,
    Void,
    Player,
    Enemy,
}