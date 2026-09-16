using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Splines;
using Unity.Mathematics;
using TMPro;
using static UnityEngine.UI.GridLayoutGroup;

public class ConstRoadHandler : MonoBehaviour
{
    [Header("UIController")]
    public UIController UC;
    [Header("お金更新用")]
    public MoneyManager moneyManager;
    //道建設の全てやります
    [Header("親とプレハブ")]
    public Transform RoadStartEndMarkPa;
    public GameObject StartEndMarkPrefab;

    [SerializeField] private GameObject dotPrefab;    // 小さな球や円のプレハブ
    [SerializeField] private float dotSpacing = 1.5f; // 点と点の間隔(m)
    private List<GameObject> previewDots = new();     // 毎フレ消して生成する
    public Transform dotParent;                      // DOTの親オブジェクト

    public GameObject Road_Tile_Prefab;
    public Transform Road_Pa;
    [Header("道設定ボタン")]
    public Button RoadMarkSetButton;
    public Button RoadConstStopButton;

    [Header("必要金額表示")]
    public int RoadCostParDot = 10;
    public GameObject NeedMoneyPanel;
    public TextMeshProUGUI NeedMoneyText;
    private int currentCost;

    //始点と終点
    private GameObject StartMark;
    private GameObject EndMark;
    private GameObject NowMark;
    private Renderer StartMRenderer;
    private Renderer EndMRenderer;
    private Renderer NowRenderer;

    private bool StartMarkPutMode = false;//update用
    private bool EndMarkPutMode = false;//update用
    private bool StartT_EndF = false;    //MarkSet内用

    public float checkRadius = 0.2f;//建物の周りに建てられない範囲
    public GameObject Plane;

    private Vector3 snapPos;

    private Vector3[] RoadStartAndEndPos = new Vector3[2];

    [Header("Preview")]
    [SerializeField] private Material previewMat;   // 半透明 or 点線マテリアル
    private NavMeshPath previewPath;                // 使い回して GC 抑制

    private List<SplineContainer> roadSplines = new();

    [SerializeField] private float roadWidth = 2f;
    [SerializeField] private float mergeDistance = 1.2f;


    public float debugfloat = 0f;
    void Start()
    {
        //ConstUICont内のボタンにイベントをアサイン
        RoadMarkSetButton.onClick.RemoveAllListeners();
        RoadMarkSetButton.onClick.AddListener(() => ButtonClicked());
        RoadConstStopButton.onClick.RemoveAllListeners();
        RoadConstStopButton.onClick.AddListener(() => RoadGenModeOff());

        StartMark = Instantiate(StartEndMarkPrefab, Vector3.zero, Quaternion.identity);
        StartMRenderer = StartMark.GetComponent<Renderer>();
        EndMark = Instantiate(StartEndMarkPrefab, Vector3.zero, Quaternion.identity);
        EndMRenderer = EndMark.GetComponent<Renderer>();
        previewPath = new NavMeshPath();        // 毎回 new しない


    }

    void Update()
    {
        if (StartMarkPutMode || EndMarkPutMode)
        {
            SetMark();
        }
    }
    
    public void RoadGenModeOn(ConstBuildingType type)
    {

        NowMark = StartMark;
        StartMRenderer.enabled = true;
        NowRenderer = StartMRenderer;
        StartT_EndF = true;
        EndMarkPutMode = false;
        StartMarkPutMode = true;
        RoadConstStopButton.gameObject.SetActive(true);
        NeedMoneyPanel.SetActive(true);
    }
    private void EndMarkModeOn()
    {
        StartMRenderer.enabled = false;
        NowMark = EndMark;
        EndMRenderer.enabled = true;
        NowRenderer = EndMRenderer;
        StartT_EndF = false;
        StartMarkPutMode = false;
        EndMarkPutMode = true;
    }
    void RoadGenModeOff()
    {
        ClearDots();
        EndMRenderer.enabled = false;
        EndMarkPutMode = false;
        RoadMarkSetButton.gameObject.SetActive(false);
        RoadConstStopButton.gameObject.SetActive(false);
        NeedMoneyPanel.SetActive(false);
        UC.CloseUI();
    }

    private void SetMark()
    {
       
        Ray ray = Camera.main.ScreenPointToRay(GetInputPosition());
        RaycastHit hit;
        LayerMask buildingMask = LayerMask.GetMask("Building");
        if (Physics.Raycast(ray, out hit))
        {
            snapPos = GetSnappedPosition(hit.point);
            
            NowMark.transform.position = snapPos;//オブジェクトを移動

            if (hit.collider.gameObject == Plane && !Physics.CheckSphere(snapPos, checkRadius, buildingMask))
            {
                RoadMarkSetButton.gameObject.SetActive(true);
               
                if (EndMarkPutMode)
                    UpdatePathPreview(snapPos);
                if (NowRenderer.material.color != Color.gray)
                {
                    NowRenderer.material.color = Color.gray;
                }
                if (Input.GetMouseButtonDown(0))
                {
                    ButtonClicked();
                       
                }
            }
            else//建築不可能
            {
               RoadMarkSetButton.gameObject.SetActive(false);
                //赤くする
                NowRenderer.material.color = Color.red;
            }
        }
    }
    void ButtonClicked()
    {
        if (StartT_EndF)
        {
            RoadStartAndEndPos[0] = snapPos;
            EndMarkModeOn();
        }
        else
        {
            RoadStartAndEndPos[1] = snapPos;

            RoadGenModeOff();
            RoadGenExecute();
        }
    }
    //プレビューの線を更新する
    void UpdatePathPreview(Vector3 targetPos)
    {
        // まだ始点が入っていなければ何もしない
        if (RoadStartAndEndPos[0] == Vector3.zero) return;

        if (!NavMesh.CalculatePath(RoadStartAndEndPos[0], targetPos,
                                   NavMesh.AllAreas, previewPath) ||
            previewPath.status != NavMeshPathStatus.PathComplete)
        {
            ClearDots();
            return;
        }
       
        var corners = previewPath.corners;
       
        // Chaikin スムージング（2 回反復）
        Vector3[] smooth = ChaikinSmooth(previewPath.corners, 2);
       
        // ★ 端点を既存道路にスナップ
        Vector3 snappedStart = SnapToExisting(previewPath.corners[0]);
        Vector3 snappedEnd = SnapToExisting(previewPath.corners[^1]);

        // 端点を既存道路へ吸着
        smooth[0] = SnapToExisting(smooth[0]);
        smooth[^1] = SnapToExisting(smooth[^1]);

        // 間隔 dotSpacing でポイントをサンプリング
        List<Vector3> samples = SampleBySpacing(corners, dotSpacing);

        //ドットの数によって必要金額を表示
        int need = samples.Count * RoadCostParDot;
        NeedMoneyText.text = need.ToString();
        currentCost = need;//実際に引く用

        // 前回のドットを消去し、新たに並べる
        ClearDots();
        foreach (var pos in samples)
        {
            var dot = Instantiate(dotPrefab, pos, Quaternion.Euler(90,0,0), dotParent);
            var rend = dot.GetComponent<Renderer>();
            previewDots.Add(dot);
        }
        
    }
    //スムージング
    Vector3[] ChaikinSmooth(Vector3[] pts, int iterations)
    {
        var result = pts;
        for (int it = 0; it < iterations; it++)
        {
            var list = new List<Vector3>();
            list.Add(result[0]);
            for (int i = 0; i < result.Length - 1; i++)
            {
                Vector3 p0 = result[i], p1 = result[i + 1];
                list.Add(Vector3.Lerp(p0, p1, 0.25f));
                list.Add(Vector3.Lerp(p0, p1, 0.75f));
            }
            list.Add(result[^1]);
            result = list.ToArray();
        }
        return result;
    }

    // smoothedPoints 上を dotSpacing ごとにサンプル
    List<Vector3> SampleBySpacing(Vector3[] smoothed, float spacing)
    {


        var samples = new List<Vector3>();
        if (smoothed == null || smoothed.Length == 0)
            return samples;

        // １：最初の点を追加
        Vector3 lastPoint = smoothed[0];
        samples.Add(lastPoint);

        // まだ「前回サンプリングからの累積距離」がいくらあるか
        float accumulated = 0f;

        // ２：各セグメントごとにループ
        for (int i = 1; i < smoothed.Length; i++)
        {
            Vector3 segmentStart = lastPoint;
            Vector3 segmentEnd = smoothed[i];
            float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
            Vector3 direction = (segmentEnd - segmentStart).normalized;

            // ３：このセグメント内で spacing 間隔ごとにサンプリングできるだけループ
            while (accumulated + segmentLength >= spacing)
            {
                // 次のサンプル地点まで進む距離
                float distanceToNext = spacing - accumulated;
                // セグメント開始からサンプル地点までの t 比
                float t = distanceToNext / segmentLength;
                Vector3 samplePoint = Vector3.Lerp(segmentStart, segmentEnd, t);

                samples.Add(samplePoint);

                // 「次のループ」ではこのサンプル地点を起点にする
                segmentStart = samplePoint;
                segmentLength -= distanceToNext;
                accumulated = 0f;
            }

            // セグメントを最後まで進みきって累積距離を更新
            accumulated += segmentLength;
            lastPoint = segmentEnd;
        }
        return samples;
    }
    void ClearDots()
    {
        foreach (var d in previewDots)
            Destroy(d);
        previewDots.Clear();
    }
    /*====================================================================
     *  端点スナップ (プレビュー用)：最近傍 Knot 座標を返す
     *==================================================================*/
    Vector3 SnapToExisting(Vector3 pos)
    {
        foreach (var sp in roadSplines)          // ① すべての既存道路を走査
        {
            foreach (var k in sp.Spline)         // ② その Spline 内の全 Knot を確認
            {
                Vector3 kPos = (Vector3)k.Position;      // float3 → Vector3
                if ((kPos - pos).sqrMagnitude           // ③ 距離² を計算
                     <= mergeDistance * mergeDistance)   // ④ 閾値以内？
                    return kPos;                         // ⑤ ヒット：その Knot 座標を返す
            }
        }
        return pos; // ⑥ 見つからなければ元の座標をそのまま返す
    }


    /*====================================================================
       *  道路を生成
       *==================================================================*/
    void RoadGenExecute()
    {
        // 1) NavMesh 経路取得（失敗なら中断）
        if (!NavMesh.CalculatePath(RoadStartAndEndPos[0], RoadStartAndEndPos[1],
                                   NavMesh.AllAreas, previewPath) ||
            previewPath.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning("Path finding failed");
            return;
        }

        List<Vector3> samples = SampleBySpacing(previewPath.corners,1f);
        for (int i = 0; i < samples.Count; i++)
        {
            Vector3 pos = samples[i];
            // 少し浮かせて地形に埋まらないように
            pos.y += 0.02f;

            // 向きは先行点→次の点の方向ベクトル
            Quaternion rot = Quaternion.identity;
            if (i < samples.Count - 1)
            {
                Vector3 dir = (samples[i + 1] - pos).normalized;
                rot = Quaternion.LookRotation(dir, Vector3.left);
               
                Quaternion finalRot =   rot;
                GameObject road = Instantiate(Road_Tile_Prefab, pos, finalRot, Road_Pa);
                var rotation = road.transform.localEulerAngles;
                rotation.x = 90f ;
                rotation.y -= 95f;
                road.transform.localEulerAngles = rotation;
            }

            
        }

        StartCoroutine(RebuildNavMeshNextFrame());


        //お金を引く
        moneyManager.HavingMoneyUpdate(-currentCost);
    }

    IEnumerator RebuildNavMeshNextFrame()
    {
        yield return null;
        FindAnyObjectByType<NavMeshSurface>().BuildNavMesh();
    }

    /*====================================================================
     *  既存道路に融合：端点が近ければ Knot 共有
     *==================================================================*/
    void FuseWithExisting(SplineContainer newCont)
    {
        var newSpline = newCont.Spline;
        BezierKnot head = newSpline[0];
        BezierKnot tail = newSpline[^1];

        foreach (var ex in roadSplines)               // ★ 自前リストを走査
        {
            TrySnap(ref head, ex);
            TrySnap(ref tail, ex);
        }
        newSpline[0] = head;
        newSpline[^1] = tail;
    }

    void TrySnap(ref BezierKnot candidate, SplineContainer target)
    {
        var spline = target.Spline;
        int bestIdx = -1;
        float bestSq = float.MaxValue;

        // candidate.Position は float3 → Vector3 キャスト
        Vector3 candPos = (Vector3)candidate.Position;

        for (int i = 0; i < spline.Count; ++i)
        {
            Vector3 knotPos = (Vector3)spline[i].Position;           // ★
            float dSq = (knotPos - candPos).sqrMagnitude;      // ★

            if (dSq < bestSq)
            {
                bestSq = dSq;
                bestIdx = i;
            }
        }

        if (bestIdx >= 0 && bestSq <= mergeDistance * mergeDistance)
        {
            // スナップだけ行う（同座標に寄せる）
            candidate.Position = spline[bestIdx].Position;           // ★ float3 なのでそのまま代入
                                                                     // ※ JoinSplinesOnKnots は同一コンテナ専用のため今回は呼ばない
        }
    }

    Vector3 GetSnappedPosition(Vector3 rawPos)
    {
        float gridSize = 0.5f; // 好きなサイズに
        float x = Mathf.Round(rawPos.x / gridSize) * gridSize;
        float z = Mathf.Round(rawPos.z / gridSize) * gridSize;
        return new Vector3(x, Plane.transform.position.y + 2f, z);
    }
    Vector2 GetInputPosition()
    {
        // タッチ対応デバイスかつタッチされていればその位置
        if (Input.touchSupported && Input.touchCount > 0)
            return Input.GetTouch(0).position;
        // それ以外はマウス位置
        return Input.mousePosition;
    }
}
