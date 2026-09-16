using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeerSpawner : MonoBehaviour
{
    public GameObject deerPrefab;   // 鹿のプレハブをInspectorで指定
    public int deerCount = 20;       // 生成する鹿の数
    public float spawnRange = 100f;  // 生成範囲（±）
    public float minDistanceFromCenter = 20f; // 中心からこの距離以内には生成しない

    public Transform DeerPa;
    void Start()
    {
        DeerSpawn();
    }
    public void DeerSpawn()
    {
        for (int i = 0; i < deerCount; i++)
        {
            Vector3 spawnPos;

            // 中心からの距離が十分な位置が見つかるまでループ
            do
            {
                spawnPos = new Vector3(
                    Random.Range(-spawnRange / 2f, spawnRange / 2f),
                    0f,
                    Random.Range(-spawnRange / 2f, spawnRange / 2f)
                );
            }
            while (Vector3.Distance(DeerPa.position, spawnPos) < minDistanceFromCenter);

            Instantiate(deerPrefab, spawnPos, Quaternion.identity, DeerPa);
        }
    }
}
