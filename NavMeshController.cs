using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshController : MonoBehaviour
{
    NavMeshSurface m_surface;

    public void Build()
    {
        m_surface = FindObjectOfType<NavMeshSurface>();
        m_surface.BuildNavMesh();
    }
}
