using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshSurface))]
public class NavmeshUpdater : MonoBehaviour
{
    public SceneManager sceneManager;
    private NavMeshSurface navMeshSurface;

    void Start()
    {        
        navMeshSurface = GetComponent<NavMeshSurface>();

        if (sceneManager != null && navMeshSurface != null) 
        {
            sceneManager.OnInvalidateScene += () =>
            {
                Debug.Log("rebuild");
                navMeshSurface.BuildNavMesh();
            };
        }

        StartCoroutine(RebuildNavMesh());
    }

    IEnumerator RebuildNavMesh() {
        yield return null;
        if (navMeshSurface != null) navMeshSurface.BuildNavMesh();
    }

}
