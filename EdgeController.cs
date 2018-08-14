using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the behavior of the surronding edge
/// </summary>
public class EdgeController : MonoBehaviour
{
    [SerializeField]
    float m_degrees = 15.0f;

    [SerializeField]
    float m_speed = 0.5f;

    [SerializeField]
    float m_frequency = 1f;

    // Position Storage Variables
    Vector3 m_startingPos = new Vector3();
    Vector3 m_newPos = new Vector3();

    // Use this for initialization
    void Start()
    {
        // Store the starting position & rotation of the object
        m_startingPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        // Float up/down with a Sin()
        m_newPos = m_startingPos;
        m_newPos.y += Mathf.Sin(Time.fixedTime * Mathf.PI * m_frequency) * m_speed;

        transform.position = m_newPos;
    }

    public void DestroyAllChildren()
    {
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
    }
}
