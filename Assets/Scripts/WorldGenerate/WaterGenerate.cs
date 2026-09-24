using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.AI.Navigation;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WaterGenerate : MonoBehaviour
{
    // =========================================================
    // 基本
    // =========================================================

    [Header("基本")]
    [SerializeField] private Terrain terrain;
    [SerializeField] private GameObject castle;

    [Tooltip("Environment > Water を指定")]
    [SerializeField] private Transform waterParent;
    [Header("NavMesh")]
    [SerializeField] private float navMeshWaterHeight = 10f;

    // =========================================================
    // Terrain復元
    // =========================================================

    [Header("Terrain復元")]

    [Tooltip("水生成前のTerrainDataを複製したAssetを指定")]
    [SerializeField] private TerrainData baseTerrainData;


    // =========================================================
    // 生成禁止
    // =========================================================

    [Header("生成禁止")]

    [Tooltip("Building Layerを指定")]
    [SerializeField] private LayerMask obstacleMask;

    [Tooltip("建物から追加で離す距離")]
    [SerializeField] private float buildingMargin = 2f;

    private readonly List<Vector3> reservedBuildingPositions =
    new List<Vector3>();

    [SerializeField]
    private float reservedBuildingRadius = 4f;
    // =========================================================
    // 全体の生成範囲
    // =========================================================

    [Header("生成範囲")]

    [Tooltip("Castleを中心に、この半径以内に湖と川を生成")]
    [SerializeField] private float generationRadius = 45f;


    // =========================================================
    // 水位
    // =========================================================

    [Header("水位")]

    [Tooltip("元の平面Terrainから水面をどれだけ下げるか")]
    [SerializeField] private float waterLevelOffset = -0.5f;


    // =========================================================
    // 湖
    // =========================================================

    [Header("湖")]

    [Tooltip("生成する湖の数")]
    [Min(1)]
    [SerializeField] private int lakeCount = 3;

    [Tooltip("湖の最小半径")]
    [SerializeField] private float minLakeRadius = 6f;

    [Tooltip("湖の最大半径")]
    [SerializeField] private float maxLakeRadius = 10f;

    [Tooltip("湖の深さ")]
    [SerializeField] private float lakeDepth = 2f;

    [Tooltip("城からこの距離以内には湖を作らない")]
    [SerializeField] private float castleExclusionRadius = 10f;

    [Tooltip("湖同士の最低間隔")]
    [SerializeField] private float lakeSpacing = 6f;


    // =========================================================
    // 湖の形
    // =========================================================

    [Header("湖の形")]

    [Tooltip("湖の輪郭の頂点数。少ないほどLow Poly")]
    [Range(6, 32)]
    [SerializeField] private int lakeEdgePoints = 14;

    [Tooltip("湖岸の不規則さ")]
    [Range(0f, 0.5f)]
    [SerializeField] private float lakeEdgeRandomness = 0.2f;

    [Tooltip("掘った範囲に対する水面の大きさ")]
    [Range(0.6f, 1f)]
    [SerializeField] private float lakeWaterRadiusScale = 0.9f;


    // =========================================================
    // 川
    // =========================================================

    [Header("川")]

    [Tooltip("川の水面幅")]
    [SerializeField] private float riverWidth = 3f;

    [Tooltip("川の深さ")]
    [SerializeField] private float riverDepth = 1.2f;

    [Tooltip("水面の外側に作る岸の幅")]
    [SerializeField] private float riverBankWidth = 1.5f;
    [Tooltip("川を湖の内側まで何m食い込ませるか")]
    [SerializeField] private float riverLakeOverlap = 3f;

    [Tooltip("同一平面のちらつき防止。見た目だけ1cm上げる")]
    [SerializeField] private float riverRenderYOffset = 0.01f;

    // =========================================================
    // 川の蛇行
    // =========================================================

    [Header("川の蛇行")]

    [Tooltip("川の基本制御点の間隔")]
    [SerializeField] private float riverControlPointSpacing = 7f;

    [Tooltip("川が左右に蛇行する最大幅")]
    [SerializeField] private float riverMeanderStrength = 4f;

    [Tooltip("湖と湖の間で何回くらい蛇行するか")]
    [Range(0.5f, 3f)]
    [SerializeField] private float riverMeanderWaves = 1.2f;

    [Tooltip("川の角を滑らかにする回数")]
    [Range(0, 4)]
    [SerializeField] private int riverSmoothIterations = 2;

    [Tooltip("Buildingを避ける経路を何通り試すか")]
    [Range(1, 100)]
    [SerializeField] private int riverRouteAttempts = 30;

    [Tooltip("川経路の衝突チェック間隔")]
    [SerializeField] private float riverValidationStep = 1f;

    [Header("川と湖の接続")]

    [Tooltip("湖と川の接続部分を丸く馴染ませる半径")]
    [SerializeField] private float riverMouthRadius = 5f;

    // =========================================================
    // Material
    // =========================================================

    [Header("水面")]

    [SerializeField] private Material waterMaterial;


    // =========================================================
    // Random
    // =========================================================

    [Header("Random")]

    [Tooltip("毎回違う地形にする")]
    [SerializeField] private bool randomizeSeed = true;

    [SerializeField] private int seed = 12345;


    // =========================================================
    // 内部データ
    // =========================================================

    private float globalWaterY;


    private readonly List<LakeData> generatedLakes =
        new List<LakeData>();


    private readonly List<List<Vector3>> generatedRivers =
        new List<List<Vector3>>();


    private class LakeData
    {
        public Vector3 center;
        public float baseRadius;
        public float[] radii;
    }


    // =========================================================
    // Generate
    // =========================================================

    [ContextMenu("Generate Water")]
    public void GenerateWater()
    {
        if (!ValidateSettings())
            return;


#if UNITY_EDITOR
        Undo.RegisterCompleteObjectUndo(
            terrain.terrainData,
            "Generate Water"
        );
#endif


        // 前回生成した水オブジェクトを削除
        ClearWaterObjects();


        // Terrainを完全に元へ戻す
        RestoreTerrain();


        generatedLakes.Clear();
        generatedRivers.Clear();


        // =====================================================
        // 水位を一度だけ決定
        // =====================================================

        float baseGroundY =
            terrain.SampleHeight(
                castle.transform.position
            ) +
            terrain.transform.position.y;


        globalWaterY =
            baseGroundY +
            waterLevelOffset;


        // =====================================================
        // Random
        // =====================================================

        UnityEngine.Random.State oldRandomState =
            UnityEngine.Random.state;


        if (randomizeSeed)
        {
            UnityEngine.Random.InitState(
                Environment.TickCount
            );
        }
        else
        {
            UnityEngine.Random.InitState(
                seed
            );
        }


        // =====================================================
        // 湖を全部生成
        // =====================================================

        for (int i = 0; i < lakeCount; i++)
        {
            GenerateLake(i);
        }


        // =====================================================
        // すべての湖を川で接続
        // =====================================================

        ConnectAllLakes();


        UnityEngine.Random.state =
            oldRandomState;


        terrain.Flush();


#if UNITY_EDITOR
        EditorUtility.SetDirty(
            terrain.terrainData
        );
#endif


        Debug.Log(
            $"Water Generate 完了 / Lakes: {generatedLakes.Count}, Rivers: {generatedRivers.Count}"
        );
    }


    // =========================================================
    // Validate
    // =========================================================

    private bool ValidateSettings()
    {
        if (terrain == null)
        {
            Debug.LogWarning(
                "WaterGenerate: Terrainが未設定です"
            );

            return false;
        }


        if (castle == null)
        {
            Debug.LogWarning(
                "WaterGenerate: Castleが未設定です"
            );

            return false;
        }


        if (waterParent == null)
        {
            Debug.LogWarning(
                "WaterGenerate: WaterParentが未設定です"
            );

            return false;
        }


        if (baseTerrainData == null)
        {
            Debug.LogWarning(
                "WaterGenerate: Base Terrain Dataが未設定です"
            );

            return false;
        }


        if (baseTerrainData ==
            terrain.terrainData)
        {
            Debug.LogError(
                "Base Terrain Dataには現在使用中のTerrainDataではなく、複製したTerrainDataを指定してください"
            );

            return false;
        }


        if (baseTerrainData.heightmapResolution !=
            terrain.terrainData.heightmapResolution)
        {
            Debug.LogError(
                "Base Terrain Dataと現在のTerrainのHeightmap Resolutionが違います"
            );

            return false;
        }


        if (baseTerrainData.size !=
            terrain.terrainData.size)
        {
            Debug.LogError(
                "Base Terrain Dataと現在のTerrainのSizeが違います"
            );

            return false;
        }


        if (minLakeRadius >
            maxLakeRadius)
        {
            Debug.LogError(
                "Min Lake RadiusがMax Lake Radiusより大きくなっています"
            );

            return false;
        }


        return true;
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
    // =========================================================
    // 湖生成
    // =========================================================

    private void GenerateLake(int index)
    {
        float radius =
            UnityEngine.Random.Range(
                minLakeRadius,
                maxLakeRadius
            );


        if (!TryFindLakePosition(
                radius,
                out Vector3 center))
        {
            Debug.LogWarning(
                $"Lake {index}: 配置可能な場所が見つかりませんでした"
            );

            return;
        }


        float[] radii =
            new float[lakeEdgePoints];


        for (int i = 0;
             i < lakeEdgePoints;
             i++)
        {
            float scale =
                UnityEngine.Random.Range(
                    1f - lakeEdgeRandomness,
                    1f + lakeEdgeRandomness
                );


            radii[i] =
                radius * scale;
        }


        LakeData lake =
            new LakeData
            {
                center = new Vector3(
                    center.x,
                    globalWaterY,
                    center.z
                ),

                baseRadius = radius,
                radii = radii
            };


        CarveLake(
            lake
        );


        CreateLakeMesh(
            lake,
            index
        );


        generatedLakes.Add(
            lake
        );
    }


    // =========================================================
    // 湖の位置
    // =========================================================

    private bool TryFindLakePosition(
        float radius,
        out Vector3 result)
    {
        result =
            Vector3.zero;


        TerrainData data =
            terrain.terrainData;


        Vector3 terrainPos =
            terrain.transform.position;


        Vector3 terrainSize =
            data.size;


        float maxCenterDistance =
            generationRadius -
            radius -
            1f;


        float minCenterDistance =
            castleExclusionRadius +
            radius;


        if (maxCenterDistance <=
            minCenterDistance)
        {
            Debug.LogWarning(
                "Generation Radiusが小さすぎて湖を配置できません"
            );

            return false;
        }


        float terrainMargin =
            radius +
            buildingMargin +
            1f;


        for (int retry = 0;
             retry < 200;
             retry++)
        {
            float angle =
                UnityEngine.Random.Range(
                    0f,
                    Mathf.PI * 2f
                );


            // 面積が均等になるよう平方根を使う
            float distance =
                Mathf.Sqrt(
                    UnityEngine.Random.Range(
                        minCenterDistance *
                        minCenterDistance,

                        maxCenterDistance *
                        maxCenterDistance
                    )
                );


            Vector3 candidate =
                castle.transform.position +
                new Vector3(
                    Mathf.Cos(angle) *
                    distance,

                    0f,

                    Mathf.Sin(angle) *
                    distance
                );


            // =================================================
            // Terrain外
            // =================================================

            if (candidate.x <
                    terrainPos.x +
                    terrainMargin ||

                candidate.x >
                    terrainPos.x +
                    terrainSize.x -
                    terrainMargin ||

                candidate.z <
                    terrainPos.z +
                    terrainMargin ||

                candidate.z >
                    terrainPos.z +
                    terrainSize.z -
                    terrainMargin)
            {
                continue;
            }


            float groundY =
                terrain.SampleHeight(
                    candidate
                ) +
                terrainPos.y;


            candidate.y =
                groundY;


            // =================================================
            // Building
            // =================================================

            if (Physics.CheckSphere(
                    candidate,
                    radius +
                    buildingMargin,
                    obstacleMask,
                    QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            bool overlapsReservedBuilding =
    false;


            foreach (Vector3 reserved
                     in reservedBuildingPositions)
            {
                if (DistanceXZ(
                        candidate,
                        reserved)
                    <
                    radius +
                    reservedBuildingRadius +
                    buildingMargin)
                {
                    overlapsReservedBuilding =
                        true;

                    break;
                }
            }


            if (overlapsReservedBuilding)
            {
                continue;
            }
            // =================================================
            // 他の湖
            // =================================================

            bool overlapping =
                false;


            foreach (LakeData lake
                     in generatedLakes)
            {
                float dist =
                    DistanceXZ(
                        candidate,
                        lake.center
                    );


                float requiredDistance =
                    radius +
                    lake.baseRadius +
                    lakeSpacing;


                if (dist <
                    requiredDistance)
                {
                    overlapping =
                        true;

                    break;
                }
            }


            if (overlapping)
                continue;


            result =
                candidate;

            return true;
        }


        return false;
    }


    // =========================================================
    // すべての湖を接続
    //
    // Prim方式:
    // 接続済みの湖から一番近い未接続湖へ川を伸ばす
    // =========================================================

    private void ConnectAllLakes()
    {
        if (generatedLakes.Count <
            2)
        {
            return;
        }


        HashSet<int> connected =
            new HashSet<int>();


        connected.Add(0);


        int riverIndex =
            0;


        while (connected.Count <
               generatedLakes.Count)
        {
            int bestFrom =
                -1;

            int bestTo =
                -1;

            float bestDistance =
                Mathf.Infinity;


            foreach (int from
                     in connected)
            {
                for (int to = 0;
                     to <
                     generatedLakes.Count;
                     to++)
                {
                    if (connected.Contains(to))
                        continue;


                    float distance =
                        DistanceXZ(
                            generatedLakes[from].center,
                            generatedLakes[to].center
                        );


                    if (distance <
                        bestDistance)
                    {
                        bestDistance =
                            distance;

                        bestFrom =
                            from;

                        bestTo =
                            to;
                    }
                }
            }


            if (bestFrom < 0 ||
                bestTo < 0)
            {
                break;
            }


            GenerateRiverBetweenLakes(
                generatedLakes[bestFrom],
                generatedLakes[bestTo],
                riverIndex
            );


            connected.Add(
                bestTo
            );


            riverIndex++;
        }
    }


    // =========================================================
    // 湖Aと湖Bを川でつなぐ
    // =========================================================

    private void GenerateRiverBetweenLakes(
        LakeData lakeA,
        LakeData lakeB,
        int riverIndex)
    {
        GetRiverEndpoints(
            lakeA,
            lakeB,
            out Vector3 start,
            out Vector3 end
        );


        List<Vector3> bestRoute =
            null;


        int bestPenalty =
            int.MaxValue;


        // =====================================================
        // 複数パターン試してBuildingを避ける
        // =====================================================

        for (int attempt = 0;
             attempt < riverRouteAttempts;
             attempt++)
        {
            List<Vector3> route =
                BuildRiverRoute(
                    start,
                    end
                );


            int penalty =
                GetRiverRoutePenalty(
                    route
                );


            if (penalty <
                bestPenalty)
            {
                bestPenalty =
                    penalty;

                bestRoute =
                    route;
            }


            // 完全に問題なし
            if (penalty == 0)
            {
                break;
            }
        }


        if (bestRoute == null ||
            bestRoute.Count < 2)
        {
            Debug.LogWarning(
                $"River {riverIndex}: 経路生成に失敗しました"
            );

            return;
        }


        if (bestPenalty > 0)
        {
            Debug.LogWarning(
                $"River {riverIndex}: Buildingを完全には避けられないため、最も衝突の少ない経路を使用します"
            );
        }


        // 全ポイントの高さを完全に統一
        for (int i = 0;
             i < bestRoute.Count;
             i++)
        {
            Vector3 p =
                bestRoute[i];

            p.y =
                globalWaterY;

            bestRoute[i] =
                p;
        }


        CarveRiver(
            bestRoute
        );
        CarveRiverMouth(start);
        CarveRiverMouth(end);

        CreateRiverMesh(
            bestRoute,
            riverIndex
        );


        generatedRivers.Add(
            bestRoute
        );
    }


    // =========================================================
    // 湖の岸 → 湖の岸
    // =========================================================

    private void GetRiverEndpoints(
        LakeData lakeA,
        LakeData lakeB,
        out Vector3 start,
        out Vector3 end)
    {
        Vector3 direction =
            lakeB.center -
            lakeA.center;


        direction.y =
            0f;


        direction.Normalize();


        float startAngle =
            Mathf.Atan2(
                direction.z,
                direction.x
            );


        if (startAngle < 0f)
        {
            startAngle +=
                Mathf.PI * 2f;
        }


        float endAngle =
            startAngle +
            Mathf.PI;


        if (endAngle >=
            Mathf.PI * 2f)
        {
            endAngle -=
                Mathf.PI * 2f;
        }


        float startRadius =
            GetLakeRadiusAtAngle(
                lakeA,
                startAngle
            );


        float endRadius =
            GetLakeRadiusAtAngle(
                lakeB,
                endAngle
            );


        // 湖の水面の少し内側まで川を入れる
        float startWaterRadius =
     Mathf.Max(
         0f,
         startRadius *
         lakeWaterRadiusScale -
         riverLakeOverlap
     );


        float endWaterRadius =
            Mathf.Max(
                0f,
                endRadius *
                lakeWaterRadiusScale -
                riverLakeOverlap
            );


        start =
            lakeA.center +
            direction *
            startWaterRadius;


        end =
            lakeB.center -
            direction *
            endWaterRadius;


        start.y =
            globalWaterY;

        end.y =
            globalWaterY;
    }


    // =========================================================
    // 川の蛇行した経路
    // =========================================================

    private List<Vector3> BuildRiverRoute(
        Vector3 start,
        Vector3 end)
    {
        Vector3 line =
            end -
            start;


        line.y =
            0f;


        float distance =
            line.magnitude;


        if (distance <
            0.01f)
        {
            return new List<Vector3>
            {
                start,
                end
            };
        }


        Vector3 forward =
            line.normalized;


        Vector3 side =
            new Vector3(
                -forward.z,
                0f,
                forward.x
            );


        int segmentCount =
            Mathf.Max(
                3,
                Mathf.CeilToInt(
                    distance /
                    Mathf.Max(
                        1f,
                        riverControlPointSpacing
                    )
                )
            );


        List<Vector3> controlPoints =
            new List<Vector3>();


        controlPoints.Add(
            start
        );


        float phase =
            UnityEngine.Random.Range(
                0f,
                Mathf.PI * 2f
            );


        float amplitude =
            riverMeanderStrength *
            UnityEngine.Random.Range(
                0.7f,
                1.15f
            );


        float waves =
            riverMeanderWaves *
            UnityEngine.Random.Range(
                0.85f,
                1.15f
            );


        for (int i = 1;
             i < segmentCount;
             i++)
        {
            float t =
                i /
                (float)segmentCount;


            Vector3 basePoint =
                Vector3.Lerp(
                    start,
                    end,
                    t
                );


            // 湖の近くでは蛇行量を0に近づける
            float taper =
                Mathf.Sin(
                    Mathf.PI * t
                );


            // 大きな滑らかなS字
            float mainWave =
                Mathf.Sin(
                    t *
                    Mathf.PI *
                    2f *
                    waves +
                    phase
                );


            // 少しだけランダム性
            float smallVariation =
                UnityEngine.Random.Range(
                    -0.25f,
                    0.25f
                );


            float offset =
                (
                    mainWave +
                    smallVariation
                ) *
                amplitude *
                taper;


            Vector3 point =
                basePoint +
                side *
                offset;


            point.y =
                globalWaterY;


            controlPoints.Add(
                point
            );
        }


        controlPoints.Add(
            end
        );


        return SmoothRiverPoints(
            controlPoints,
            riverSmoothIterations
        );
    }


    // =========================================================
    // Chaikin Curve
    // 鋭角を丸くする
    // =========================================================

    private List<Vector3> SmoothRiverPoints(
        List<Vector3> source,
        int iterations)
    {
        if (source == null ||
            source.Count < 3 ||
            iterations <= 0)
        {
            return new List<Vector3>(
                source
            );
        }


        List<Vector3> result =
            new List<Vector3>(
                source
            );


        for (int iteration = 0;
             iteration < iterations;
             iteration++)
        {
            List<Vector3> next =
                new List<Vector3>();


            // 始点はそのまま
            next.Add(
                result[0]
            );


            for (int i = 0;
                 i <
                 result.Count - 1;
                 i++)
            {
                Vector3 a =
                    result[i];

                Vector3 b =
                    result[i + 1];


                Vector3 q =
                    Vector3.Lerp(
                        a,
                        b,
                        0.25f
                    );


                Vector3 r =
                    Vector3.Lerp(
                        a,
                        b,
                        0.75f
                    );


                q.y =
                    globalWaterY;

                r.y =
                    globalWaterY;


                next.Add(q);
                next.Add(r);
            }


            // 終点はそのまま
            next.Add(
                result[
                    result.Count - 1
                ]
            );


            result =
                next;
        }


        return result;
    }
    private void CarveRiverMouth(
    Vector3 center)
    {
        TerrainData data =
            terrain.terrainData;


        int resolution =
            data.heightmapResolution;


        float[,] heights =
            data.GetHeights(
                0,
                0,
                resolution,
                resolution
            );


        Vector3 terrainPos =
            terrain.transform.position;

        Vector3 terrainSize =
            data.size;


        float bottomY =
            globalWaterY -
            riverDepth;


        for (int z = 0;
             z < resolution;
             z++)
        {
            float worldZ =
                terrainPos.z +
                (
                    z /
                    (float)(resolution - 1)
                ) *
                terrainSize.z;


            if (Mathf.Abs(
                    worldZ -
                    center.z)
                > riverMouthRadius)
            {
                continue;
            }


            for (int x = 0;
                 x < resolution;
                 x++)
            {
                float worldX =
                    terrainPos.x +
                    (
                        x /
                        (float)(resolution - 1)
                    ) *
                    terrainSize.x;


                float dx =
                    worldX -
                    center.x;

                float dz =
                    worldZ -
                    center.z;


                float distance =
                    Mathf.Sqrt(
                        dx * dx +
                        dz * dz
                    );


                if (distance >
                    riverMouthRadius)
                {
                    continue;
                }


                float t =
                    distance /
                    riverMouthRadius;


                float strength =
                    1f -
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        t
                    );


                float currentHeight =
                    terrainPos.y +
                    heights[z, x] *
                    terrainSize.y;


                float newHeight =
                    Mathf.Lerp(
                        currentHeight,
                        bottomY,
                        strength
                    );


                heights[z, x] =
                    Mathf.Clamp01(
                        (
                            Mathf.Min(
                                currentHeight,
                                newHeight
                            ) -
                            terrainPos.y
                        ) /
                        terrainSize.y
                    );
            }
        }


        data.SetHeights(
            0,
            0,
            heights
        );
    }

    // =========================================================
    // 川経路のPenalty
    // 0 = 問題なし
    // =========================================================

    private int GetRiverRoutePenalty(
        List<Vector3> route)
    {
        int penalty =
            0;


        for (int i = 0;
             i <
             route.Count - 1;
             i++)
        {
            Vector3 a =
                route[i];

            Vector3 b =
                route[i + 1];


            float distance =
                DistanceXZ(
                    a,
                    b
                );


            int samples =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        distance /
                        Mathf.Max(
                            0.25f,
                            riverValidationStep
                        )
                    )
                );


            for (int s = 0;
                 s <= samples;
                 s++)
            {
                float t =
                    s /
                    (float)samples;


                Vector3 point =
                    Vector3.Lerp(
                        a,
                        b,
                        t
                    );


                if (!IsRiverPointInsideGenerationArea(
                        point))
                {
                    penalty +=
                        100;

                    continue;
                }


                if (IsRiverPointBlocked(
                        point))
                {
                    penalty +=
                        10;
                }
            }
        }


        return penalty;
    }


    // =========================================================
    // 生成範囲内か
    // =========================================================

    private bool IsRiverPointInsideGenerationArea(
        Vector3 point)
    {
        float distanceFromCastle =
            DistanceXZ(
                point,
                castle.transform.position
            );


        float requiredMargin =
            riverWidth *
            0.5f +
            riverBankWidth;


        if (distanceFromCastle >
            generationRadius -
            requiredMargin)
        {
            return false;
        }


        Vector3 terrainPos =
            terrain.transform.position;


        Vector3 size =
            terrain.terrainData.size;


        if (point.x <
                terrainPos.x +
                requiredMargin ||

            point.x >
                terrainPos.x +
                size.x -
                requiredMargin ||

            point.z <
                terrainPos.z +
                requiredMargin ||

            point.z >
                terrainPos.z +
                size.z -
                requiredMargin)
        {
            return false;
        }


        return true;
    }


    // =========================================================
    // Building判定
    // =========================================================

    private bool IsRiverPointBlocked(
    Vector3 point)
    {
        Vector3 checkPoint =
            point;


        checkPoint.y =
            terrain.SampleHeight(point) +
            terrain.transform.position.y;


        float radius =
            riverWidth * 0.5f +
            buildingMargin;


        // 今存在するBuilding
        if (Physics.CheckSphere(
                checkPoint,
                radius,
                obstacleMask,
                QueryTriggerInteraction.Ignore))
        {
            return true;
        }


        // Skipで今後生成されるBuilding
        foreach (Vector3 reserved
                 in reservedBuildingPositions)
        {
            if (DistanceXZ(
                    point,
                    reserved)
                <
                radius +
                reservedBuildingRadius)
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // 湖を掘る
    // =========================================================

    private void CarveLake(
        LakeData lake)
    {
        TerrainData data =
            terrain.terrainData;


        int resolution =
            data.heightmapResolution;


        float[,] heights =
            data.GetHeights(
                0,
                0,
                resolution,
                resolution
            );


        Vector3 terrainPos =
            terrain.transform.position;


        Vector3 terrainSize =
            data.size;


        float maxRadius =
            lake.baseRadius *
            (1f +
             lakeEdgeRandomness);


        for (int z = 0;
             z < resolution;
             z++)
        {
            float normalizedZ =
                z /
                (float)(resolution - 1);


            float worldZ =
                terrainPos.z +
                normalizedZ *
                terrainSize.z;


            if (Mathf.Abs(
                    worldZ -
                    lake.center.z)
                >
                maxRadius)
            {
                continue;
            }


            for (int x = 0;
                 x < resolution;
                 x++)
            {
                float normalizedX =
                    x /
                    (float)(resolution - 1);


                float worldX =
                    terrainPos.x +
                    normalizedX *
                    terrainSize.x;


                if (Mathf.Abs(
                        worldX -
                        lake.center.x)
                    >
                    maxRadius)
                {
                    continue;
                }


                float dx =
                    worldX -
                    lake.center.x;


                float dz =
                    worldZ -
                    lake.center.z;


                float distance =
                    Mathf.Sqrt(
                        dx * dx +
                        dz * dz
                    );


                float angle =
                    Mathf.Atan2(
                        dz,
                        dx
                    );


                if (angle < 0f)
                {
                    angle +=
                        Mathf.PI * 2f;
                }


                float radius =
                    GetLakeRadiusAtAngle(
                        lake,
                        angle
                    );


                if (distance >
                    radius)
                {
                    continue;
                }


                float normalizedDistance =
                    distance /
                    radius;


                float carveAmount =
                    1f -
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        normalizedDistance
                    );


                float currentHeight =
                    terrainPos.y +
                    heights[z, x] *
                    terrainSize.y;


                float bottomY =
                    globalWaterY -
                    lakeDepth;


                float targetHeight =
                    Mathf.Lerp(
                        currentHeight,
                        bottomY,
                        carveAmount
                    );


                // Terrainは下げるだけ
                float newHeight =
                    Mathf.Min(
                        currentHeight,
                        targetHeight
                    );


                heights[z, x] =
                    Mathf.Clamp01(
                        (
                            newHeight -
                            terrainPos.y
                        ) /
                        terrainSize.y
                    );
            }
        }


        data.SetHeights(
            0,
            0,
            heights
        );
    }


    // =========================================================
    // 川を掘る
    // =========================================================

    private void CarveRiver(
        List<Vector3> points)
    {
        if (points == null ||
            points.Count < 2)
        {
            return;
        }


        TerrainData data =
            terrain.terrainData;


        int resolution =
            data.heightmapResolution;


        float[,] heights =
            data.GetHeights(
                0,
                0,
                resolution,
                resolution
            );


        Vector3 terrainPos =
            terrain.transform.position;


        Vector3 terrainSize =
            data.size;


        float waterHalfWidth =
            riverWidth * 0.5f;


        float carveHalfWidth =
            waterHalfWidth +
            riverBankWidth;


        float bottomY =
            globalWaterY -
            riverDepth;


        for (int z = 0;
             z < resolution;
             z++)
        {
            float normalizedZ =
                z /
                (float)(resolution - 1);


            float worldZ =
                terrainPos.z +
                normalizedZ *
                terrainSize.z;


            for (int x = 0;
                 x < resolution;
                 x++)
            {
                float normalizedX =
                    x /
                    (float)(resolution - 1);


                float worldX =
                    terrainPos.x +
                    normalizedX *
                    terrainSize.x;


                Vector3 worldPoint =
                    new Vector3(
                        worldX,
                        globalWaterY,
                        worldZ
                    );


                float closestDistance =
                    Mathf.Infinity;


                for (int i = 0;
                     i <
                     points.Count - 1;
                     i++)
                {
                    float t;


                    float distance =
                        DistanceToSegmentXZ(
                            worldPoint,
                            points[i],
                            points[i + 1],
                            out t
                        );


                    if (distance <
                        closestDistance)
                    {
                        closestDistance =
                            distance;
                    }
                }


                if (closestDistance >
                    carveHalfWidth)
                {
                    continue;
                }


                float carveAmount;


                // 水面直下は一定の深さ
                if (closestDistance <=
                    waterHalfWidth)
                {
                    carveAmount =
                        1f;
                }
                else
                {
                    float bankT =
                        (
                            closestDistance -
                            waterHalfWidth
                        ) /
                        Mathf.Max(
                            0.01f,
                            riverBankWidth
                        );


                    carveAmount =
                        1f -
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            bankT
                        );
                }


                float currentHeight =
                    terrainPos.y +
                    heights[z, x] *
                    terrainSize.y;


                float targetHeight =
                    Mathf.Lerp(
                        currentHeight,
                        bottomY,
                        carveAmount
                    );


                // 湖や他の川を通っても
                // Terrainを持ち上げない
                float newHeight =
                    Mathf.Min(
                        currentHeight,
                        targetHeight
                    );


                heights[z, x] =
                    Mathf.Clamp01(
                        (
                            newHeight -
                            terrainPos.y
                        ) /
                        terrainSize.y
                    );
            }
        }


        data.SetHeights(
            0,
            0,
            heights
        );
    }


    // =========================================================
    // 湖Mesh
    // =========================================================

    private void CreateLakeMesh(
        LakeData lake,
        int index)
    {
        GameObject lakeObject =
            new GameObject(
                $"Lake_{index}"
            );


        lakeObject.transform.SetParent(
            waterParent
        );


        lakeObject.transform.position =
            new Vector3(
                lake.center.x,
                globalWaterY,
                lake.center.z
            );


        SetWaterLayer(
            lakeObject
        );


        MeshFilter filter =
            lakeObject.AddComponent<MeshFilter>();


        MeshRenderer renderer =
            lakeObject.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        if (waterMaterial != null)
        {
            renderer.sharedMaterial =
                waterMaterial;
        }


        Mesh mesh =
            new Mesh();


        mesh.name =
            $"LakeMesh_{index}";


        Vector3[] vertices =
            new Vector3[
                lakeEdgePoints + 1
            ];


        vertices[0] =
            Vector3.zero;


        for (int i = 0;
             i < lakeEdgePoints;
             i++)
        {
            float angle =
                Mathf.PI *
                2f *
                i /
                lakeEdgePoints;


            float radius =
                lake.radii[i] *
                lakeWaterRadiusScale;


            vertices[i + 1] =
                new Vector3(
                    Mathf.Cos(angle) *
                    radius,

                    0f,

                    Mathf.Sin(angle) *
                    radius
                );
        }


        int[] triangles =
            new int[
                lakeEdgePoints * 3
            ];


        for (int i = 0;
             i < lakeEdgePoints;
             i++)
        {
            int next =
                (i + 1) %
                lakeEdgePoints;


            triangles[i * 3] =
                0;

            triangles[i * 3 + 1] =
                next + 1;

            triangles[i * 3 + 2] =
                i + 1;
        }


        mesh.vertices =
            vertices;

        mesh.triangles =
            triangles;


        mesh.RecalculateNormals();
        mesh.RecalculateBounds();


        filter.sharedMesh =
            mesh;


        MeshCollider collider =
            lakeObject.AddComponent<MeshCollider>();


        collider.sharedMesh =
            mesh;

        NavMeshModifierVolume modifier =
    lakeObject.AddComponent<NavMeshModifierVolume>();

        modifier.area =
            NavMesh.GetAreaFromName("Not Walkable");

        float navRadius =
            lake.baseRadius *
            lakeWaterRadiusScale;

        modifier.size =
            new Vector3(
                navRadius * 2f,
                navMeshWaterHeight,
                navRadius * 2f
            );

        modifier.center =
            new Vector3(
                0f,
                -navMeshWaterHeight * 0.5f,
                0f
            );
    }


    // =========================================================
    // 川Mesh
    // =========================================================

    private void CreateRiverMesh(
        List<Vector3> points,
        int riverIndex)
    {
        GameObject riverObject =
            new GameObject(
                $"River_{riverIndex}"
            );


        riverObject.transform.SetParent(
            waterParent,
            false
        );


        SetWaterLayer(
            riverObject
        );


        MeshFilter filter =
            riverObject.AddComponent<MeshFilter>();


        MeshRenderer renderer =
            riverObject.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        if (waterMaterial != null)
        {
            renderer.sharedMaterial =
                waterMaterial;
        }


        Vector3[] vertices =
            new Vector3[
                points.Count * 2
            ];


        float halfWidth =
            riverWidth * 0.5f;


        for (int i = 0;
             i < points.Count;
             i++)
        {
            Vector3 tangent;


            if (i == 0)
            {
                tangent =
                    points[1] -
                    points[0];
            }
            else if (i ==
                     points.Count - 1)
            {
                tangent =
                    points[i] -
                    points[i - 1];
            }
            else
            {
                tangent =
                    points[i + 1] -
                    points[i - 1];
            }


            tangent.y =
                0f;


            if (tangent.sqrMagnitude <
                0.0001f)
            {
                tangent =
                    Vector3.forward;
            }


            tangent.Normalize();


            Vector3 side =
                new Vector3(
                    -tangent.z,
                    0f,
                    tangent.x
                );


            Vector3 left =
                points[i] +
                side *
                halfWidth;


            Vector3 right =
                points[i] -
                side *
                halfWidth;


            left.y =
    globalWaterY +
    riverRenderYOffset;

            right.y =
                globalWaterY +
                riverRenderYOffset;


            vertices[i * 2] =
                riverObject.transform
                    .InverseTransformPoint(
                        left
                    );


            vertices[i * 2 + 1] =
                riverObject.transform
                    .InverseTransformPoint(
                        right
                    );
        }


        int[] triangles =
            new int[
                (points.Count - 1) *
                6
            ];


        int triangleIndex =
            0;


        for (int i = 0;
             i <
             points.Count - 1;
             i++)
        {
            int a =
                i * 2;

            int b =
                i * 2 + 1;

            int c =
                i * 2 + 2;

            int d =
                i * 2 + 3;


            triangles[triangleIndex++] = a;
            triangles[triangleIndex++] = c;
            triangles[triangleIndex++] = b;

            triangles[triangleIndex++] = b;
            triangles[triangleIndex++] = c;
            triangles[triangleIndex++] = d;
        }


        Mesh mesh =
            new Mesh();


        mesh.name =
            $"RiverMesh_{riverIndex}";


        mesh.vertices =
            vertices;

        mesh.triangles =
            triangles;


        mesh.RecalculateNormals();
        mesh.RecalculateBounds();


        filter.sharedMesh =
            mesh;


        MeshCollider collider =
            riverObject.AddComponent<MeshCollider>();


        collider.sharedMesh =
            mesh;

        CreateRiverNavMeshVolumes(
    riverObject.transform,
    points
);
    }

    private void CreateRiverNavMeshVolumes(
    Transform parent,
    List<Vector3> points)
    {
        float width =
            riverWidth +
            riverBankWidth * 0.5f;


        for (int i = 0;
             i < points.Count - 1;
             i++)
        {
            Vector3 a =
                points[i];

            Vector3 b =
                points[i + 1];


            Vector3 direction =
                b - a;

            direction.y = 0f;


            float length =
                direction.magnitude;


            if (length < 0.01f)
                continue;


            Vector3 middle =
                (a + b) * 0.5f;


            GameObject volumeObject =
                new GameObject(
                    $"NavMeshBlock_{i}"
                );


            volumeObject.transform.SetParent(
                parent
            );


            volumeObject.transform.position =
                new Vector3(
                    middle.x,
                    globalWaterY,
                    middle.z
                );


            volumeObject.transform.rotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up
                );


            NavMeshModifierVolume volume =
                volumeObject.AddComponent<
                    NavMeshModifierVolume
                >();


            volume.area =
                NavMesh.GetAreaFromName(
                    "Not Walkable"
                );


            volume.size =
                new Vector3(
                    width,
                    navMeshWaterHeight,
                    length + 0.5f
                );


            volume.center =
                new Vector3(
                    0f,
                    -navMeshWaterHeight * 0.5f,
                    0f
                );
        }
    }
    // =========================================================
    // 湖岸の半径
    // =========================================================

    private float GetLakeRadiusAtAngle(
        LakeData lake,
        float angle)
    {
        float normalizedAngle =
            angle /
            (Mathf.PI * 2f);


        float point =
            normalizedAngle *
            lake.radii.Length;


        int indexA =
            Mathf.FloorToInt(
                point
            ) %
            lake.radii.Length;


        int indexB =
            (indexA + 1) %
            lake.radii.Length;


        float t =
            point -
            Mathf.Floor(
                point
            );


        return Mathf.Lerp(
            lake.radii[indexA],
            lake.radii[indexB],
            t
        );
    }


    // =========================================================
    // XZ距離
    // =========================================================

    private float DistanceXZ(
        Vector3 a,
        Vector3 b)
    {
        float dx =
            a.x -
            b.x;


        float dz =
            a.z -
            b.z;


        return Mathf.Sqrt(
            dx * dx +
            dz * dz
        );
    }


    // =========================================================
    // 点 → 線分までのXZ距離
    // =========================================================

    private float DistanceToSegmentXZ(
        Vector3 point,
        Vector3 a,
        Vector3 b,
        out float t)
    {
        Vector2 p =
            new Vector2(
                point.x,
                point.z
            );


        Vector2 a2 =
            new Vector2(
                a.x,
                a.z
            );


        Vector2 b2 =
            new Vector2(
                b.x,
                b.z
            );


        Vector2 ab =
            b2 -
            a2;


        float sqrLength =
            ab.sqrMagnitude;


        if (sqrLength <
            0.0001f)
        {
            t =
                0f;


            return Vector2.Distance(
                p,
                a2
            );
        }


        t =
            Vector2.Dot(
                p - a2,
                ab
            ) /
            sqrLength;


        t =
            Mathf.Clamp01(
                t
            );


        Vector2 closest =
            a2 +
            ab * t;


        return Vector2.Distance(
            p,
            closest
        );
    }


    // =========================================================
    // Water Layer
    // =========================================================

    private void SetWaterLayer(
        GameObject obj)
    {
        int waterLayer =
            LayerMask.NameToLayer(
                "Water"
            );


        if (waterLayer >= 0)
        {
            obj.layer =
                waterLayer;
        }
        else
        {
            Debug.LogWarning(
                "Water Layerがありません"
            );
        }
    }


    // =========================================================
    // Clear Water
    // =========================================================

    [ContextMenu("Clear Water")]
    public void ClearWater()
    {
        if (!ValidateSettings())
            return;


#if UNITY_EDITOR
        Undo.RegisterCompleteObjectUndo(
            terrain.terrainData,
            "Clear Water"
        );
#endif


        ClearWaterObjects();

        RestoreTerrain();


        generatedLakes.Clear();
        generatedRivers.Clear();


        terrain.Flush();


#if UNITY_EDITOR
        EditorUtility.SetDirty(
            terrain.terrainData
        );
#endif


        Debug.Log(
            "Waterを削除しTerrainを復元しました"
        );
    }


    // =========================================================
    // Terrainだけ復元
    // =========================================================

    [ContextMenu("Restore Terrain")]
    public void RestoreTerrainContextMenu()
    {
        if (!ValidateSettings())
            return;


#if UNITY_EDITOR
        Undo.RegisterCompleteObjectUndo(
            terrain.terrainData,
            "Restore Terrain"
        );
#endif


        RestoreTerrain();


        terrain.Flush();


#if UNITY_EDITOR
        EditorUtility.SetDirty(
            terrain.terrainData
        );
#endif


        Debug.Log(
            "Terrainを復元しました"
        );
    }


    // =========================================================
    // Waterの子を消す
    // =========================================================

    private void ClearWaterObjects()
    {
        if (waterParent == null)
            return;


        List<GameObject> children =
            new List<GameObject>();


        foreach (Transform child
                 in waterParent)
        {
            children.Add(
                child.gameObject
            );
        }


        foreach (GameObject child
                 in children)
        {
            DestroyGeneratedObject(
                child
            );
        }
    }


    private void DestroyGeneratedObject(
        GameObject obj)
    {
        if (obj == null)
            return;


        MeshFilter filter =
            obj.GetComponent<MeshFilter>();


        Mesh generatedMesh =
            null;


        if (filter != null)
        {
            generatedMesh =
                filter.sharedMesh;
        }


#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(
                obj
            );


            if (generatedMesh != null)
            {
                DestroyImmediate(
                    generatedMesh
                );
            }


            return;
        }
#endif


        Destroy(
            obj
        );


        if (generatedMesh != null)
        {
            Destroy(
                generatedMesh
            );
        }
    }


    // =========================================================
    // Terrain復元
    // =========================================================

    private void RestoreTerrain()
    {
        if (terrain == null ||
            baseTerrainData == null)
        {
            return;
        }


        TerrainData current =
            terrain.terrainData;


        int resolution =
            baseTerrainData
                .heightmapResolution;


        float[,] originalHeights =
            baseTerrainData.GetHeights(
                0,
                0,
                resolution,
                resolution
            );


        current.SetHeights(
            0,
            0,
            originalHeights
        );
    }


    // =========================================================
    // 指定地点が水域か
    // =========================================================

    public bool IsPointInWater(
    Vector3 worldPosition,
    float margin = 0f)
    {
        // =====================================================
        // 湖
        // =====================================================

        foreach (LakeData lake
                 in generatedLakes)
        {
            float dx =
                worldPosition.x -
                lake.center.x;

            float dz =
                worldPosition.z -
                lake.center.z;


            float distance =
                Mathf.Sqrt(
                    dx * dx +
                    dz * dz
                );


            float angle =
                Mathf.Atan2(
                    dz,
                    dx
                );


            if (angle < 0f)
            {
                angle +=
                    Mathf.PI * 2f;
            }


            float radius =
                GetLakeRadiusAtAngle(
                    lake,
                    angle
                ) *
                lakeWaterRadiusScale;


            if (distance <=
                radius + margin)
            {
                return true;
            }
        }


        // =====================================================
        // 川
        // =====================================================

        foreach (List<Vector3> river
                 in generatedRivers)
        {
            if (river == null ||
                river.Count < 2)
            {
                continue;
            }


            for (int i = 0;
                 i < river.Count - 1;
                 i++)
            {
                float t;


                float distance =
                    DistanceToSegmentXZ(
                        worldPosition,
                        river[i],
                        river[i + 1],
                        out t
                    );


                if (distance <=
                    riverWidth * 0.5f +
                    margin)
                {
                    return true;
                }
            }
        }


        return false;
    }
}