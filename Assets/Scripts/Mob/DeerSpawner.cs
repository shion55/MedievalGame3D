using UnityEngine;
using UnityEngine.AI;

public class DeerSpawner : MonoBehaviour
{
    [Header("Deer")]
    public GameObject deerPrefab;

    [Tooltip("生成する鹿の数")]
    public int deerCount = 20;

    [Header("Spawn Range")]
    [Tooltip("DeerPaを中心とした生成範囲")]
    public float spawnRange = 100f;

    [Tooltip("中心から最低限離す距離")]
    public float minDistanceFromCenter = 20f;

    [Tooltip("ランダム地点の周囲何mからNavMeshを探すか")]
    public float navMeshSearchRadius = 5f;

    [Tooltip("1頭あたりの最大探索回数")]
    public int maxSpawnAttempts = 100;

    [Header("Parent")]
    public Transform DeerPa;


    private int walkableAreaMask;


    private void Start()
    {
        SetupAreaMask();

        DeerSpawn();
    }


    // =========================================================
    // NavMesh Area Mask
    // =========================================================

    private void SetupAreaMask()
    {
        walkableAreaMask =
            NavMesh.AllAreas;


        // WaterGenerateで湖・川に設定している
        // Not Walkable Areaを明示的に除外
        int notWalkableArea =
            NavMesh.GetAreaFromName(
                "Not Walkable"
            );


        if (notWalkableArea >= 0)
        {
            walkableAreaMask &=
                ~(1 << notWalkableArea);
        }
    }


    // =========================================================
    // Deer Spawn
    // =========================================================

    public void DeerSpawn()
    {
        if (deerPrefab == null)
        {
            Debug.LogWarning(
                "DeerSpawner: deerPrefab が未設定です"
            );

            return;
        }


        if (DeerPa == null)
        {
            Debug.LogWarning(
                "DeerSpawner: DeerPa が未設定です"
            );

            return;
        }


        // Hunterから直接呼ばれた場合などに備える
        SetupAreaMask();


        int spawnedCount =
            0;


        for (int i = 0;
             i < deerCount;
             i++)
        {
            if (TryFindSpawnPosition(
                    out Vector3 spawnPosition))
            {
                Instantiate(
                    deerPrefab,
                    spawnPosition,
                    Quaternion.Euler(
                        0f,
                        Random.Range(0f, 360f),
                        0f
                    ),
                    DeerPa
                );


                spawnedCount++;
            }
            else
            {
                Debug.LogWarning(
                    $"DeerSpawner: 鹿 {i + 1} の生成可能地点が見つかりませんでした"
                );
            }
        }


        Debug.Log(
            $"Deer Spawn 完了: {spawnedCount}/{deerCount}"
        );
    }


    // =========================================================
    // Spawn Position
    // =========================================================

    private bool TryFindSpawnPosition(
        out Vector3 result)
    {
        result =
            Vector3.zero;


        Vector3 center =
            DeerPa.position;


        float halfRange =
            spawnRange * 0.5f;


        for (int attempt = 0;
             attempt < maxSpawnAttempts;
             attempt++)
        {
            Vector3 randomPosition =
                center +
                new Vector3(
                    Random.Range(
                        -halfRange,
                        halfRange
                    ),

                    0f,

                    Random.Range(
                        -halfRange,
                        halfRange
                    )
                );


            // 中央付近には出さない
            Vector2 centerXZ =
                new Vector2(
                    center.x,
                    center.z
                );


            Vector2 randomXZ =
                new Vector2(
                    randomPosition.x,
                    randomPosition.z
                );


            if (Vector2.Distance(
                    centerXZ,
                    randomXZ)
                <
                minDistanceFromCenter)
            {
                continue;
            }


            // NavMesh上の実際に歩ける地点を探す
            if (!NavMesh.SamplePosition(
                    randomPosition,
                    out NavMeshHit hit,
                    navMeshSearchRadius,
                    walkableAreaMask))
            {
                continue;
            }


            // 念のため中央との距離を
            // Sample後の地点でも確認
            Vector2 hitXZ =
                new Vector2(
                    hit.position.x,
                    hit.position.z
                );


            if (Vector2.Distance(
                    centerXZ,
                    hitXZ)
                <
                minDistanceFromCenter)
            {
                continue;
            }


            result =
                hit.position;


            return true;
        }


        return false;
    }
}