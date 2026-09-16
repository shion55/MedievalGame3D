using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;

#if UNITY_EDITOR
using UnityEditor;
#endif
[ExecuteInEditMode]
public class TreeGenerate : MonoBehaviour
{
    [SerializeField] private GameObject castle;
    [SerializeField] private Terrain terrain;
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Header("Prefabs")]
    [SerializeField] private GameObject[] treePrefabs;
    [SerializeField] private GameObject[] stonePrefabs;
    [SerializeField] private GameObject[] grassPrefabs;
    

    [Header("Parent for Generated Trees")]
    [SerializeField] private Transform treeParent;
    [SerializeField] private Transform stoneParent;
    [SerializeField] private Transform grassParent;
    
    [System.Serializable]
    private class ClusterSettings
    {
        [Tooltip("森（クラスタ）の数")]
        public int clusterCount = 5;
        [Tooltip("1クラスタあたりの最小木数")]
        public int minPerCluster = 4;
        [Tooltip("1クラスタあたりの最大木数")]
        public int maxPerCluster = 20;
        [Tooltip("城から木を配置する範囲の直径")]
        public float overallRadius = 50f;
        [Tooltip("クラスタ内の半径")]
        public float clusterRadius = 4f;
    }
    [SerializeField] private ClusterSettings treeSettings = new ClusterSettings();
    [SerializeField] private ClusterSettings stoneSettings = new ClusterSettings();
    [SerializeField] private ClusterSettings grassSettings = new ClusterSettings();

  

    [Header("Exclusion Zone")]
    [Tooltip("城からこの半径以内には何も生成しない")]
    [SerializeField] private float exclusionRadius = 10f;


    [Tooltip("チェックすると一度だけ森・石・草をまとめて生成")]
    [SerializeField] private bool generateEnvironment = false;

    private List<Vector3> occupiedPositions = new List<Vector3>();
    // 生成した木の情報を保持
    private Dictionary<GameObject, Vector3> treeData = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Vector3> stoneData = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Vector3> grassData = new Dictionary<GameObject, Vector3>();
  
    void Start()
    {
        
        
    }
    void GenTrees()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
        if (generateEnvironment)
        {
            generateEnvironment = false;
            ClearAll();
            // タイプごとにクラスタを生成
            for (int i = 0; i < treeSettings.clusterCount; i++)
                GenCluster(treePrefabs, treeParent, treeData, treeSettings);
            for (int i = 0; i < stoneSettings.clusterCount; i++)
                GenCluster(stonePrefabs, stoneParent, stoneData, stoneSettings);
            for (int i = 0; i < grassSettings.clusterCount; i++)
                GenCluster(grassPrefabs, grassParent, grassData, grassSettings);
        }
    }

    [ContextMenu("Generate Environment")]
    private void GenerateContextMenu()
    {
        generateEnvironment = true;
    }

    /// <summary>
    /// 既存の木・石・草をすべて削除し、データもクリア
    /// </summary>
    private void ClearAll()
    {
        occupiedPositions.Clear();
        ClearCategory(treeParent, treeData);
        ClearCategory(stoneParent, stoneData);
        ClearCategory(grassParent, grassData);
        if (navMeshSurface != null)
            navMeshSurface.BuildNavMesh();
        else
            Debug.LogWarning("NavMeshSurface が割り当てられていません。");
    }

    private void ClearCategory(Transform parent, Dictionary<GameObject, Vector3> data)
    {
        if (parent == null) return;
        var children = new List<GameObject>();
        foreach (Transform child in parent)
            children.Add(child.gameObject);

        foreach (var go in children)
        {
#if UNITY_EDITOR
            DestroyImmediate(go);
#else
            Destroy(go);
#endif
        }
        data.Clear();
    }

    /// <summary>
    /// 与えられたプレハブ群から1クラスタ分をランダム配置
    /// </summary>
    private void GenCluster(GameObject[] prefabs, Transform parent, Dictionary<GameObject, Vector3> data, ClusterSettings settings)
    {
        if (prefabs == null || prefabs.Length == 0) return;
        if (parent == null) parent = transform;
        if (castle == null) { Debug.LogWarning("castle が未設定です"); return; }

        // 1) クラスタ中心を城周辺 overallRadius 内、かつ exclusionRadius 外で決定
        Vector3 center = Vector3.zero;
        for (int retry = 0; retry < 10; retry++)
        {
            Vector2 rnd = Random.insideUnitCircle * (settings.overallRadius * 0.5f);
            center = castle.transform.position + new Vector3(rnd.x, 0, rnd.y);
            float dist = Vector3.Distance(center, castle.transform.position);
            if (dist >= exclusionRadius)
                break;
        }

        // 2) クラスタ内にランダム配置。ただし既存位置と近接しすぎないようチェック
        int count = Random.Range(settings.minPerCluster, settings.maxPerCluster + 1);
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = Vector3.zero;
            for (int retry = 0; retry < 10; retry++)
            {
                Vector2 off = Random.insideUnitCircle * settings.clusterRadius;
                pos = center + new Vector3(off.x, 0, off.y);
                // 城から近すぎない & 他オブジェクトと近すぎない
                if (Vector3.Distance(pos, castle.transform.position) < exclusionRadius) continue;
                bool ok = true;
                foreach (var prev in occupiedPositions)
                {
                    if (Vector3.Distance(pos, prev) < 1f) { ok = false; break; }
                }
                if (ok) break;
            }

            var prefab = prefabs[Random.Range(0, prefabs.Length)];
            var go = Instantiate(prefab, pos, Quaternion.identity, parent);
            
            data[go] = pos;
            occupiedPositions.Add(pos);
        }
    }


   
}
