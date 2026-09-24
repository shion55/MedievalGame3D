using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;


public class TreeGrassGenerate : MonoBehaviour
{
    [SerializeField] private GameObject castle;
    [SerializeField] private Terrain terrain;
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Header("生成禁止レイヤー")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Prefabs")]
    [SerializeField] private GameObject[] treePrefabs;
    [SerializeField] private GameObject[] grassPrefabs;

    [Header("Parents")]
    [SerializeField] private Transform treeParent;
    [SerializeField] private Transform grassParent;

    [SerializeField] private WaterGenerate waterGenerate;
    [System.Serializable]
    private class ClusterSettings
    {
        [Tooltip("クラスタの数")]
        public int clusterCount = 5;

        [Tooltip("1クラスタあたりの最小生成数")]
        public int minPerCluster = 4;

        [Tooltip("1クラスタあたりの最大生成数")]
        public int maxPerCluster = 20;

        [Tooltip("城を中心とした生成範囲の直径")]
        public float overallRadius = 50f;

        [Tooltip("1クラスタの広がり")]
        public float clusterRadius = 4f;

        [Tooltip("同士を生成するときの最低間隔")]
        public float minSpacing = 1f;

        [Tooltip("建物などから離す距離")]
        public float obstacleRadius = 1f;
    }


    [Header("木")]
    [SerializeField]
    private ClusterSettings treeSettings =
        new ClusterSettings
        {
            clusterCount = 5,
            minPerCluster = 5,
            maxPerCluster = 15,
            overallRadius = 50f,
            clusterRadius = 5f,
            minSpacing = 1.5f,
            obstacleRadius = 1f
        };


    [Header("草")]
    [SerializeField]
    private ClusterSettings grassSettings =
        new ClusterSettings
        {
            clusterCount = 12,
            minPerCluster = 20,
            maxPerCluster = 50,
            overallRadius = 50f,
            clusterRadius = 4f,
            minSpacing = 0.25f,
            obstacleRadius = 0.3f
        };


    [Header("Exclusion Zone")]
    [Tooltip("城からこの半径以内には生成しない")]
    [SerializeField]
    private float exclusionRadius = 10f;




    private readonly List<Vector3> occupiedPositions =
        new List<Vector3>();


    private readonly Dictionary<GameObject, Vector3> treeData =
        new Dictionary<GameObject, Vector3>();

    private readonly Dictionary<GameObject, Vector3> grassData =
        new Dictionary<GameObject, Vector3>();

    private readonly List<Vector3> reservedBuildingPositions =
    new List<Vector3>();

    [SerializeField]
    private float reservedBuildingRadius = 4f;


    [ContextMenu("Generate Environment")]
    public void GenerateEnvironment()
    {
        ClearAll();

        // 木
        for (int i = 0; i < treeSettings.clusterCount; i++)
        {
            GenCluster(
                treePrefabs,
                treeParent,
                treeData,
                treeSettings
            );
        }

        // 草
        for (int i = 0; i < grassSettings.clusterCount; i++)
        {
            GenCluster(
                grassPrefabs,
                grassParent,
                grassData,
                grassSettings
            );
        }
    }


    private void ClearAll()
    {
        occupiedPositions.Clear();

        ClearCategory(
            treeParent,
            treeData
        );

        ClearCategory(
            grassParent,
            grassData
        );
    }
    public void SetReservedBuildingPositions(
    List<Vector3> positions)
    {
        reservedBuildingPositions.Clear();

        if (positions != null)
        {
            reservedBuildingPositions.AddRange(
                positions
            );
        }
    }

    private void ClearCategory(
        Transform parent,
        Dictionary<GameObject, Vector3> data)
    {
        if (parent == null)
            return;

        List<GameObject> children =
            new List<GameObject>();

        foreach (Transform child in parent)
        {
            children.Add(child.gameObject);
        }

        foreach (GameObject go in children)
        {
#if UNITY_EDITOR
            DestroyImmediate(go);
#else
            Destroy(go);
#endif
        }

        data.Clear();
    }


    private void GenCluster(
        GameObject[] prefabs,
        Transform parent,
        Dictionary<GameObject, Vector3> data,
        ClusterSettings settings)
    {
        if (prefabs == null || prefabs.Length == 0)
            return;

        if (parent == null)
            parent = transform;

        if (castle == null)
        {
            Debug.LogWarning("castle が未設定です");
            return;
        }


        // =========================
        // クラスタ中心を決める
        // =========================

        Vector3 center = Vector3.zero;
        bool foundCenter = false;


        for (int retry = 0; retry < 20; retry++)
        {
            Vector2 rnd =
                Random.insideUnitCircle *
                (settings.overallRadius * 0.5f);

            center =
                castle.transform.position +
                new Vector3(rnd.x, 0f, rnd.y);


            if (Vector3.Distance(
                    center,
                    castle.transform.position)
                < exclusionRadius)
            {
                continue;
            }


            foundCenter = true;
            break;
        }


        if (!foundCenter)
            return;


        // =========================
        // クラスタ内に生成
        // =========================

        int count =
            Random.Range(
                settings.minPerCluster,
                settings.maxPerCluster + 1
            );


        for (int i = 0; i < count; i++)
        {
            Vector3 pos = Vector3.zero;

            bool foundPosition = false;


            for (int retry = 0; retry < 30; retry++)
            {
                Vector2 off =
                    Random.insideUnitCircle *
                    settings.clusterRadius;


                pos =
                    center +
                    new Vector3(
                        off.x,
                        0f,
                        off.y
                    );
                // Terrainの高さに合わせる
                pos.y =
                    terrain.SampleHeight(pos) +
                    terrain.transform.position.y;

                bool nearReservedBuilding =
    false;


                foreach (Vector3 reserved
                         in reservedBuildingPositions)
                {
                    Vector2 pos2D =
                        new Vector2(
                            pos.x,
                            pos.z
                        );

                    Vector2 reserved2D =
                        new Vector2(
                            reserved.x,
                            reserved.z
                        );


                    if (Vector2.Distance(
                            pos2D,
                            reserved2D)
                        <
                        reservedBuildingRadius +
                        settings.obstacleRadius)
                    {
                        nearReservedBuilding =
                            true;

                        break;
                    }
                }


                if (nearReservedBuilding)
                {
                    continue;
                }
                // 水の中・水際には生成しない
                if (waterGenerate != null &&
                    waterGenerate.IsPointInWater(
                        pos,
                        settings.obstacleRadius))
                {
                    continue;
                }

                // 城の周囲
                if (Vector3.Distance(
                        pos,
                        castle.transform.position)
                    < exclusionRadius)
                {
                    continue;
                }


                // 建物・水など
                if (Physics.CheckSphere(
                        pos,
                        settings.obstacleRadius,
                        obstacleMask,
                        QueryTriggerInteraction.Ignore))
                {
                    continue;
                }


                // 他の生成物との距離
                bool tooClose = false;


                foreach (Vector3 previous
                         in occupiedPositions)
                {
                    if (Vector3.Distance(
                            pos,
                            previous)
                        < settings.minSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }


                if (tooClose)
                    continue;


                foundPosition = true;
                break;
            }


            if (!foundPosition)
                continue;


            GameObject prefab =
                prefabs[
                    Random.Range(
                        0,
                        prefabs.Length
                    )
                ];


            GameObject go =
                Instantiate(
                    prefab,
                    pos,
                    Quaternion.Euler(
                        0f,
                        Random.Range(0f, 360f),
                        0f
                    ),
                    parent
                );


            data[go] = pos;

            occupiedPositions.Add(pos);
        }
    }
}