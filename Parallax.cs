using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves the render's material offset to give the effect of a moving background
/// </summary>
[RequireComponent(typeof(Renderer))]
public class Parallax : MonoBehaviour
{
    /// <summary>
    /// How fast to move the material's offeset by its individual axis
    /// </summary>
    [SerializeField]
    Vector2 m_offsetSpeed = new Vector2(1f, 0f);

    /// <summary>
    /// A reference to the render component
    /// </summary>
    Renderer m_renderer;

	// Use this for initialization
	void Start ()
    {
        m_renderer = GetComponent<Renderer>();
	}
	
	// Update is called once per frame
	void Update ()
    {
        Vector2 currentOffset = m_renderer.material.mainTextureOffset;
        m_renderer.material.mainTextureOffset = currentOffset + m_offsetSpeed * Time.deltaTime;
	}
}
