using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ConstSiteSetHandler : MonoBehaviour
{
    public UIController uiController;
    public ConstructioinManager constManager;
    public ConstBuildingMasterDataSO constbuildingmaster;

    [Header("設置対象")]
    public GameObject Plane;
    public GameObject Site;
    public Transform PreviewObjParent;

    [Header("釣り人小屋")]

    [SerializeField]
    private WaterGenerate waterGenerate;

    [SerializeField]
    private Terrain terrain;

    [SerializeField]
    private LayerMask waterLayer;

    [Tooltip("岸から陸側へ小屋を離す距離")]
    [SerializeField]
    private float fishermanShoreOffset = 1.5f;

    [Header("建設予定地の予約")]

    [Tooltip("建設予定地同士を少し離す余白")]
    [SerializeField]
    private float constructionSiteMargin = 0.3f;

    [Header("設置不可判定")]
    [SerializeField]
    private LayerMask hitLayers;

    [Header("プレビュー")]
    public Material previewMaterial;

    public Color canPlaceColor = Color.grey;
    public Color cannotPlaceColor = Color.red;

    [Header("ボタン")]
    public Button SiteSetButton;
    public Button SiteSetStopButton;

    // 新しく作る
    public Button SiteRotateButton;

    [Header("回転")]
    public float rotateStep = 45f;

    [Header("Camera")]
    [SerializeField] private Cameracont cameraCont;
    [HideInInspector]
    public bool SiteGenMode = false;

    private bool CanSetSite = false;

    private ConstBuildingType buildingNowType;

    private GameObject NowPreviewPrefab;

    private readonly List<Renderer> NowRenderers =
        new List<Renderer>();

    private readonly Dictionary<ConstBuildingType, GameObject>
        PreviewObjects =
        new Dictionary<ConstBuildingType, GameObject>();

    private readonly Dictionary<ConstBuildingType, Quaternion>
        PreviewBaseRotations =
        new Dictionary<ConstBuildingType, Quaternion>();


    private void Start()
    {
        // =========================
        // プレビューをあらかじめ生成
        // =========================

        foreach (
            ConstBuildingType type
            in Enum.GetValues(typeof(ConstBuildingType)))
        {
            var data = constbuildingmaster.GetData(type);

            if (data == null)
                continue;

            GameObject prefab = data.conbuildingPrefab;

            if (prefab == null)
                continue;


            GameObject building = Instantiate(
                prefab,
                new Vector3(20f, 20f, 20f),
                prefab.transform.rotation,
                PreviewObjParent
            );


            // -------------------------
            // プレビューMaterial
            // -------------------------

            foreach (
                Renderer renderer
                in building.GetComponentsInChildren<Renderer>())
            {
                Material[] materials =
                    renderer.materials;

                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = previewMaterial;
                }

                renderer.materials = materials;
                renderer.enabled = false;
            }


            // -------------------------
            // スクリプト停止
            // -------------------------

            foreach (
                MonoBehaviour comp
                in building.GetComponentsInChildren<MonoBehaviour>())
            {
                comp.enabled = false;
            }


            // -------------------------
            // プレビュー自身のColliderは判定しない
            // -------------------------

            foreach (
                Collider col
                in building.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }


            foreach (
                NavMeshObstacle nav
                in building.GetComponentsInChildren<NavMeshObstacle>())
            {
                nav.enabled = false;
            }


            PreviewObjects.Add(type, building);

            PreviewBaseRotations.Add(
                type,
                prefab.transform.rotation
            );
        }


        SiteSetButton.gameObject.SetActive(false);
        SiteSetStopButton.gameObject.SetActive(false);

        if (SiteRotateButton != null)
        {
            SiteRotateButton.gameObject.SetActive(false);
        }


        SiteSetButton.onClick.AddListener(
            ConfirmPlacement
        );

        SiteSetStopButton.onClick.AddListener(
            SiteGenModeOff
        );

        if (SiteRotateButton != null)
        {
            SiteRotateButton.onClick.AddListener(
    () => RotatePreview(rotateStep)
);
        }
    }


    private void Update()
    {
        if (!SiteGenMode)
            return;


        // 建築プレビューが画面端にあるときカメラ移動
        if (cameraCont != null &&
            TryGetPointerPosition(
                out Vector2 pointerPosition))
        {
            
            cameraCont.MoveCameraAtScreenEdge(
                pointerPosition
            );
        }


        // カメラ移動後にプレビュー位置を更新
        UpdatePreview();


        if (!Application.isMobilePlatform)
        {
            HandlePCInput();
        }
    }


    // =========================================================
    // 設置モード開始
    // =========================================================

    public void SiteGenModeOn(ConstBuildingType type)
    {
        if (!PreviewObjects.ContainsKey(type))
            return;

        cameraCont.CameraContActive = true;
        buildingNowType = type;

        CanSetSite = false;

        NowPreviewPrefab =
            PreviewObjects[type];


        // 前回回した向きをリセット
        NowPreviewPrefab.transform.rotation =
            PreviewBaseRotations[type];


        NowRenderers.Clear();

        NowRenderers.AddRange(
            NowPreviewPrefab
                .GetComponentsInChildren<Renderer>()
        );


        foreach (Renderer renderer in NowRenderers)
        {
            renderer.enabled = true;
        }


        SiteGenMode = true;

        SiteSetButton.gameObject.SetActive(false);
        SiteSetStopButton.gameObject.SetActive(true);


        if (SiteRotateButton != null)
        {
            // 釣り人小屋は湖を自動で向く
            SiteRotateButton.gameObject.SetActive(
                type != ConstBuildingType.fishmancabin
            );
        }


        DebugController.Log(
            "建設地決定モードオン"
        );
    }
    private void HandlePCInput()
    {
        // 左回転
        if (Input.GetKeyDown(KeyCode.Q))
        {
            RotatePreview(-rotateStep);
        }

        // 右回転
        if (Input.GetKeyDown(KeyCode.E))
        {
            RotatePreview(rotateStep);
        }

        // 建築決定
        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmPlacement();
        }

        // キャンセル
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SiteGenModeOff();
        }
    }

    // =========================================================
    // 設置モード終了
    // =========================================================

    public void SiteGenModeOff()
    {
        SiteGenMode = false;
        CanSetSite = false;
        cameraCont.CameraContActive = false;

        if (NowPreviewPrefab != null)
        {
            foreach (Renderer renderer in NowRenderers)
            {
                renderer.enabled = false;
            }


            NowPreviewPrefab.transform.position =
                new Vector3(20f, 20f, 20f);
        }


        SiteSetButton.gameObject.SetActive(false);
        SiteSetStopButton.gameObject.SetActive(false);


        if (SiteRotateButton != null)
        {
            SiteRotateButton.gameObject.SetActive(false);
        }


        uiController.CloseUI();


        DebugController.Log(
            "建設地決定モードオフ"
        );
    }


    // =========================================================
    // プレビュー更新
    // =========================================================

    private void UpdatePreview()
    {
        if (!TryGetPointerPosition(
                out Vector2 pointerPosition))
        {
            return;
        }


        Ray ray =
            Camera.main.ScreenPointToRay(
                pointerPosition
            );


        if (buildingNowType ==
            ConstBuildingType.fishmancabin)
        {
            UpdateFishermanCabin(ray);
        }
        else
        {
            UpdateNormalBuilding(ray);
        }
    }


    // =========================================================
    // 普通の建物
    // =========================================================

    private void UpdateNormalBuilding(Ray ray)
    {
        if (!TryRaycastObject(
                Plane,
                ray,
                out RaycastHit hit))
        {
            SetCanPlace(false);
            return;
        }


        // グリッドスナップしない
        // hit.pointをそのまま使う
        NowPreviewPrefab.transform.position =
            hit.point;


        bool blocked =
            IsPlacementBlocked();


        SetCanPlace(!blocked);
    }


    // =========================================================
    // 釣り人小屋
    // =========================================================

    private void UpdateFishermanCabin(
    Ray ray)
    {
        if (waterGenerate == null)
        {
            SetCanPlace(false);
            return;
        }


        // =====================================================
        // 実際の湖・川MeshをRaycast
        // =====================================================

        if (!Physics.Raycast(
                ray,
                out RaycastHit waterHit,
                500f,
                waterLayer,
                QueryTriggerInteraction.Collide))
        {
            SetCanPlace(false);
            return;
        }


        // =====================================================
        // クリック地点から一番近い岸を探す
        // =====================================================

        if (!waterGenerate.TryFindNearestShore(
                waterHit.point,
                out Vector3 shorePoint,
                out Vector3 directionToWater))
        {
            SetCanPlace(false);
            return;
        }


        // =====================================================
        // 岸から少し陸側へ移動
        // =====================================================

        Vector3 directionToLand =
            -directionToWater;


        Vector3 position =
            shorePoint +
            directionToLand *
            fishermanShoreOffset;


        // Terrainの高さへ合わせる
        if (terrain != null)
        {
            position.y =
                terrain.SampleHeight(
                    position
                ) +
                terrain.transform.position.y;
        }


        NowPreviewPrefab.transform.position =
            position;


        // =====================================================
        // 小屋を水側へ向ける
        // =====================================================

        if (directionToWater.sqrMagnitude >
            0.001f)
        {
            NowPreviewPrefab.transform.rotation =
                Quaternion.LookRotation(
                    directionToWater,
                    Vector3.up
                );
        }


        // =====================================================
        // 建物・木・水との衝突
        // =====================================================

        bool blocked =
            IsPlacementBlocked();


        SetCanPlace(
            !blocked
        );
    }

    // =========================================================
    // 池の縁へ移動
    // =========================================================

    private Vector3 GetPondEdgePosition(
        Vector3 pointerPosition,
        Bounds bounds)
    {
        Vector3 center = bounds.center;

        Vector3 dir =
            pointerPosition - center;

        dir.y = 0f;


        if (dir.sqrMagnitude < 0.001f)
        {
            dir = Vector3.forward;
        }


        dir.Normalize();


        float distanceX =
            Mathf.Abs(dir.x) > 0.001f
                ? bounds.extents.x /
                  Mathf.Abs(dir.x)
                : Mathf.Infinity;


        float distanceZ =
            Mathf.Abs(dir.z) > 0.001f
                ? bounds.extents.z /
                  Mathf.Abs(dir.z)
                : Mathf.Infinity;


        float distance =
            Mathf.Min(
                distanceX,
                distanceZ
            );


        Vector3 edge =
            center + dir * distance;


        edge.y =
            pointerPosition.y;


        return edge;
    }


    // =========================================================
    // 建物との重なり判定
    // =========================================================

    private bool IsPlacementBlocked()
    {
        BoxCollider placementArea =
            FindPlacementArea();


        // PlacementAreaがあるPrefab
        if (placementArea != null)
        {
            Vector3 center =
                placementArea.transform
                    .TransformPoint(
                        placementArea.center
                    );


            Vector3 scale =
                placementArea.transform
                    .lossyScale;


            Vector3 halfExtents =
                new Vector3(
                    placementArea.size.x *
                    Mathf.Abs(scale.x) * 0.5f,

                    placementArea.size.y *
                    Mathf.Abs(scale.y) * 0.5f,

                    placementArea.size.z *
                    Mathf.Abs(scale.z) * 0.5f
                );


            return Physics.CheckBox(
                center,
                halfExtents,
                placementArea.transform.rotation,
                hitLayers,
               QueryTriggerInteraction.Collide
            );
        }


        // まだPlacementAreaを作っていないPrefabは
        // Renderer全体から仮判定
        if (NowRenderers.Count == 0)
            return false;


        Bounds bounds =
            NowRenderers[0].bounds;


        for (int i = 1; i < NowRenderers.Count; i++)
        {
            bounds.Encapsulate(
                NowRenderers[i].bounds
            );
        }


        return Physics.CheckBox(
            bounds.center,
            bounds.extents,
            Quaternion.identity,
            hitLayers,
            QueryTriggerInteraction.Collide
        );
    }


    // =========================================================
    // PlacementAreaを探す
    // =========================================================

    private BoxCollider FindPlacementArea()
    {
        BoxCollider[] colliders =
            NowPreviewPrefab
                .GetComponentsInChildren<BoxCollider>(
                    true
                );


        foreach (BoxCollider collider in colliders)
        {
            if (collider.gameObject.name ==
                "PlacementArea")
            {
                return collider;
            }
        }


        return null;
    }

    private void CreateConstructionReservation(
    GameObject siteObj)
    {
        BoxCollider placementArea =
            FindPlacementArea();

        if (placementArea == null)
        {
            Debug.LogWarning(
                "PlacementArea が無いため建設予定範囲を作れませんでした"
            );

            return;
        }


        // PlacementAreaのワールド中心
        Vector3 worldCenter =
            placementArea.transform.TransformPoint(
                placementArea.center
            );


        // ワールド上での実寸
        Vector3 scale =
            placementArea.transform.lossyScale;


        Vector3 worldSize =
            new Vector3(
                placementArea.size.x *
                Mathf.Abs(scale.x),

                placementArea.size.y *
                Mathf.Abs(scale.y),

                placementArea.size.z *
                Mathf.Abs(scale.z)
            );


        // 建物同士を少し離す
        worldSize.x +=
            constructionSiteMargin * 2f;

        worldSize.z +=
            constructionSiteMargin * 2f;


        // =====================================================
        // 予約領域Object
        // =====================================================

        GameObject reservation =
            new GameObject(
                "PlacementReservation"
            );


        reservation.transform.position =
            worldCenter;

        reservation.transform.rotation =
            placementArea.transform.rotation;


        reservation.transform.SetParent(
            siteObj.transform,
            true
        );


        // 通常のBuildingと同じLayerにして
        // hitLayersで検出できるようにする
        int buildingLayer =
            LayerMask.NameToLayer(
                "Building"
            );


        if (buildingLayer >= 0)
        {
            reservation.layer =
                buildingLayer;
        }


        BoxCollider collider =
            reservation.AddComponent<BoxCollider>();


        collider.center =
            Vector3.zero;

        collider.size =
            worldSize;

        // 実際に物理的に押し返す必要はない
        collider.isTrigger =
            true;
    }
    // =========================================================
    // 設置可能状態
    // =========================================================

    private void SetCanPlace(bool canPlace)
    {
        CanSetSite = canPlace;

        SiteSetButton.gameObject.SetActive(
            canPlace
        );


        Color color =
            canPlace
                ? canPlaceColor
                : cannotPlaceColor;


        foreach (Renderer renderer in NowRenderers)
        {
            foreach (Material mat in renderer.materials)
            {
                mat.color = color;
            }
        }
    }


    // =========================================================
    // 回転
    // =========================================================

    private void RotatePreview(float angle)
    {
        if (!SiteGenMode)
            return;

        if (NowPreviewPrefab == null)
            return;

        // 釣り人小屋は湖方向へ自動回転
        if (buildingNowType == ConstBuildingType.fishmancabin)
            return;

        NowPreviewPrefab.transform.Rotate(
            0f,
            angle,
            0f,
            Space.World
        );
    }


    // =========================================================
    // 設置決定
    // =========================================================

    private void ConfirmPlacement()
    {
        if (!SiteGenMode)
            return;

        if (!CanSetSite)
            return;


        var data =
            constbuildingmaster.GetData(
                buildingNowType
            );


        int needMoney = 0;

        data.GetMaterialDict().TryGetValue(
            MaterialType.Money,
            out needMoney
        );


        // 決定時にもう一度所持金確認
        if (uiController.moneyManager.HavingMoney
            < needMoney)
        {
            DebugController.Log(
                "お金足りない"
            );

            return;
        }


        // ここで初めて支払う
        uiController.moneyManager
            .HavingMoneyUpdate(-needMoney);


        Vector3 spawnPosition =
            NowPreviewPrefab.transform.position;

        Quaternion spawnRotation =
            NowPreviewPrefab.transform.rotation;


        GameObject siteObj =
            Instantiate(
                Site,
                spawnPosition,
                Site.transform.rotation
            );

        CreateConstructionReservation(
    siteObj
);

        constManager.waitingSite.Add(
            siteObj
        );


        constManager.ConstractionSetting(
            siteObj,
            buildingNowType,
            spawnPosition,
            spawnRotation
        );


        SiteGenModeOff();
    }


    // =========================================================
    // 指定オブジェクトにRaycast
    // =========================================================

    private bool TryRaycastObject(
        GameObject target,
        Ray ray,
        out RaycastHit closestHit)
    {
        closestHit =
            new RaycastHit();


        if (target == null)
            return false;


        Collider[] colliders =
            target.GetComponentsInChildren<Collider>();


        bool found = false;
        float closestDistance =
            Mathf.Infinity;


        foreach (Collider collider in colliders)
        {
            if (!collider.enabled)
                continue;


            if (collider.Raycast(
                    ray,
                    out RaycastHit hit,
                    500f))
            {
                if (hit.distance <
                    closestDistance)
                {
                    closestDistance =
                        hit.distance;

                    closestHit = hit;

                    found = true;
                }
            }
        }


        return found;
    }


    // =========================================================
    // PC / スマホ共通入力
    // =========================================================

    private bool TryGetPointerPosition(
        out Vector2 position)
    {
        position = Vector2.zero;


        // スマホ
        if (Input.touchCount > 0)
        {
            Touch touch =
                Input.GetTouch(0);


            // UIを触っているなら
            // 建物を動かさない
            if (EventSystem.current != null &&
                EventSystem.current
                    .IsPointerOverGameObject(
                        touch.fingerId
                    ))
            {
                return false;
            }


            if (touch.phase ==
                    TouchPhase.Ended ||
                touch.phase ==
                    TouchPhase.Canceled)
            {
                return false;
            }


            position =
                touch.position;

            return true;
        }


        // 実機スマホで
        // 指を触っていないときは更新しない
        if (Application.isMobilePlatform)
        {
            return false;
        }


        // PC
        if (EventSystem.current != null &&
            EventSystem.current
                .IsPointerOverGameObject())
        {
            return false;
        }


        position =
            Input.mousePosition;

        return true;
    }
}