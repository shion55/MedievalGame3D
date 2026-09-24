using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class WorldGenerate : MonoBehaviour
{
    [Header("Generators")]
    [SerializeField] private WaterGenerate waterGenerate;
    [SerializeField] private TreeGrassGenerate treeGrassGenerate;

    [Header("NavMesh")]
    [SerializeField] private NavMeshSurface navMeshSurface;

    [SerializeField] private Skip skip;
    // =========================================================
    // Inspectorから手動生成
    // =========================================================

    [ContextMenu("Generate World")]
    public void GenerateWorld()
    {
        Debug.Log("World Generate Start");
        List<Vector3> reservedPositions =
    null;


        if (skip != null &&
            skip.SkipActive)
        {
            reservedPositions =
                skip.GetReservedBuildingPositions();
        }


        if (waterGenerate != null)
        {
            waterGenerate.SetReservedBuildingPositions(
                reservedPositions
            );
        }


        if (treeGrassGenerate != null)
        {
            treeGrassGenerate.SetReservedBuildingPositions(
                reservedPositions
            );
        }
        // 1. 水
        if (waterGenerate != null)
        {
            waterGenerate.GenerateWater();
        }
        else
        {
            Debug.LogWarning(
                "WorldGenerate: WaterGenerate が未設定です"
            );
        }


        Physics.SyncTransforms();


        // 2. 木・草
        if (treeGrassGenerate != null)
        {
            treeGrassGenerate.GenerateEnvironment();
        }
        else
        {
            Debug.LogWarning(
                "WorldGenerate: TreeGrassGenerate が未設定です"
            );
        }


        Physics.SyncTransforms();


        // 3. NavMesh
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }
        else
        {
            Debug.LogWarning(
                "WorldGenerate: NavMeshSurface が未設定です"
            );
        }


        Debug.Log("World Generate Complete");
    }

    // =========================================================
    // 木・草だけ再生成
    // =========================================================

    [ContextMenu("Regenerate Trees And Grass")]
    public void RegenerateTreesAndGrass()
    {
        if (treeGrassGenerate == null)
            return;


        Physics.SyncTransforms();

        treeGrassGenerate.GenerateEnvironment();


        Physics.SyncTransforms();


        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }
    }


    // =========================================================
    // NavMeshだけ更新
    // =========================================================

    [ContextMenu("Rebuild NavMesh")]
    public void RebuildNavMesh()
    {
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }
    }
}