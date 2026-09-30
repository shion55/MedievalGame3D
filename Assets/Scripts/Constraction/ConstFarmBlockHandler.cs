using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ConstFarmBlockHandler : MonoBehaviour
{
    // =========================================================
    // References
    // =========================================================

    [Header("UIController")]
    public UIController UC;

    [Header("お金更新用")]
    public MoneyManager moneyManager;

    [Header("畑管理用")]
    public FarmManager farmManager;


    // =========================================================
    // Farm
    // =========================================================

    [Header("畑Prefab")]
    public GameObject Farm_Brock;

    [Header("実物Farmの親")]
    public Transform Farm_Brock_Pa;

    [Header("Previewの親")]
    public Transform Farm_Brock_Preview_Pa;


    // =========================================================
    // Preview
    // =========================================================

    [Header("Preview")]
    public Material previewMaterial;

    public Color canPlaceColor =
        Color.gray;

    public Color cannotPlaceColor =
        Color.red;


    // =========================================================
    // UI
    // =========================================================

    [Header("畑設定ボタン")]
    public Button FarmSetButton;
    public Button FarmConstStopButton;

    [Header("必要金額表示")]
    public int FarmCostParMas = 100;

    public GameObject NeedMoneyPanel;
    public TextMeshProUGUI NeedMoneyText;


    // =========================================================
    // Placement
    // =========================================================

    [Header("設置対象")]
    public GameObject Plane;

    [Tooltip(
        "Building・Treeなど、畑を置いてはいけないLayer。" +
        "Farm Layerは入れない。"
    )]
    public LayerMask mask;


    [Tooltip(
        "Terrainに対するFarmBlockの高さ補正"
    )]
    [SerializeField]
    private float farmYOffset = 1f;


    [Tooltip(
        "建物等との判定を少しだけ内側に縮める量"
    )]
    [SerializeField]
    private float obstacleCheckInset = 0.02f;


    // =========================================================
    // Existing Farm Snap
    // =========================================================

    [Header("既存畑へのスナップ")]

    [Tooltip(
        "既存畑の隣へ吸着する距離"
    )]
    [SerializeField]
    private float farmSnapDistance = 0.6f;


    // =========================================================
    // Preview Runtime
    // =========================================================

    private GameObject previewfarm_Start;

    private readonly List<GameObject> previewfarms =
        new List<GameObject>();


    // =========================================================
    // Farm size
    //
    // FarmBlock本体のBoxColliderから取得
    // =========================================================

    private float farmBlockSizeX;
    private float farmBlockSizeZ;


    // =========================================================
    // Placement state
    // =========================================================

    private bool StartFarmPutMode = false;
    private bool EndFarmPutMode = false;

    private bool Clickable = false;

    private Vector3 currentTargetPos;

    private readonly Vector3[] FarmStartAndEndPos =
        new Vector3[2];

    private int CurrentCost = 0;


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        if (Farm_Brock == null)
        {
            Debug.LogError(
                "Farm_Brock が設定されていません"
            );

            return;
        }


        // =====================================================
        // FarmBlock本体のColliderを確認
        // =====================================================

        BoxCollider sourceCollider =
            Farm_Brock.GetComponent<BoxCollider>();


        if (sourceCollider == null)
        {
            Debug.LogError(
                "Farm_Block のRootにBoxColliderがありません"
            );

            return;
        }


        // =====================================================
        // 実際の1マスサイズを取得
        // =====================================================

        Vector3 sourceScale =
            Farm_Brock.transform.localScale;


        farmBlockSizeX =
            sourceCollider.size.x *
            Mathf.Abs(sourceScale.x);


        farmBlockSizeZ =
            sourceCollider.size.z *
            Mathf.Abs(sourceScale.z);


        if (farmBlockSizeX <= 0f ||
            farmBlockSizeZ <= 0f)
        {
            Debug.LogError(
                "FarmBlockのColliderサイズが不正です"
            );

            return;
        }


        // =====================================================
        // 始点Preview生成
        // =====================================================

        previewfarm_Start =
            CreateFarmPreview(
                Vector3.zero
            );


        SetRenderersEnabled(
            previewfarm_Start,
            false
        );


        // =====================================================
        // UI
        // =====================================================

        FarmSetButton.gameObject.SetActive(
            false
        );

        FarmConstStopButton.gameObject.SetActive(
            false
        );

        NeedMoneyPanel.SetActive(
            false
        );


        // PCでもスマホでもボタンを使える
        FarmSetButton.onClick.AddListener(
            ConfirmFarmPlacement
        );

        FarmConstStopButton.onClick.AddListener(
            CancelFarmPlacement
        );
    }


    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        if (!StartFarmPutMode &&
            !EndFarmPutMode)
        {
            return;
        }


        UpdatePreview();


        // PCではEnter / Escも使える
        if (!Application.isMobilePlatform)
        {
            HandlePCInput();
        }
    }


    // =========================================================
    // PC Input
    // =========================================================

    private void HandlePCInput()
    {
        if (Input.GetKeyDown(
                KeyCode.Return) ||
            Input.GetKeyDown(
                KeyCode.KeypadEnter))
        {
            ConfirmFarmPlacement();
        }


        if (Input.GetKeyDown(
                KeyCode.Escape))
        {
            CancelFarmPlacement();
        }
    }


    // =========================================================
    // Start placement
    // =========================================================

    public void FarmGenModeOn()
    {
        ClearRangePreview();


        StartFarmPutMode =
            true;

        EndFarmPutMode =
            false;

        Clickable =
            false;

        CurrentCost =
            0;


        // =====================================================
        // UI
        // =====================================================

        FarmSetButton.gameObject.SetActive(
            false
        );

        FarmConstStopButton.gameObject.SetActive(
            true
        );

        NeedMoneyPanel.SetActive(
            true
        );

        NeedMoneyText.text =
            "0";


        // =====================================================
        // Start preview
        // =====================================================

        SetRenderersEnabled(
            previewfarm_Start,
            true
        );
    }


    // =========================================================
    // Preview update
    // =========================================================

    private void UpdatePreview()
    {
        if (!TryGetPointerPosition(
                out Vector2 pointerPosition))
        {
            return;
        }


        if (Camera.main == null)
        {
            return;
        }


        Ray ray =
            Camera.main.ScreenPointToRay(
                pointerPosition
            );


        // =====================================================
        // PlaneだけにRaycast
        //
        // 建物やFarm Colliderに邪魔されず
        // 地面上の位置を取得する
        // =====================================================

        if (!TryRaycastObject(
                Plane,
                ray,
                out RaycastHit hit))
        {
            return;
        }


        // =====================================================
        // 1点目
        // =====================================================

        if (StartFarmPutMode)
        {
            currentTargetPos =
                GetStartPosition(
                    hit.point
                );


            previewfarm_Start.transform.position =
                currentTargetPos;


            bool blocked =
                IsPreviewBlocked(
                    previewfarm_Start
                );


            Clickable =
                !blocked;


            SetPreviewColor(
                previewfarm_Start,
                Clickable
                    ? canPlaceColor
                    : cannotPlaceColor
            );


            CurrentCost =
                FarmCostParMas;


            NeedMoneyText.text =
                CurrentCost.ToString();


            UpdateConfirmButton();

            return;
        }


        // =====================================================
        // 2点目
        // =====================================================

        if (EndFarmPutMode)
        {
            currentTargetPos =
                GetRelativeSnappedPosition(
                    hit.point
                );


            UpdateRangePreview(
                currentTargetPos
            );
        }
    }


    // =========================================================
    // Confirm
    // =========================================================

    private void ConfirmFarmPlacement()
    {
        if (!StartFarmPutMode &&
            !EndFarmPutMode)
        {
            return;
        }


        if (!Clickable)
        {
            return;
        }


        // =====================================================
        // 1回目
        // =====================================================

        if (StartFarmPutMode)
        {
            FarmStartAndEndPos[0] =
                currentTargetPos;


            StartFarmPutMode =
                false;

            EndFarmPutMode =
                true;

            Clickable =
                false;


            SetRenderersEnabled(
                previewfarm_Start,
                false
            );


            UpdateConfirmButton();

            return;
        }


        // =====================================================
        // 2回目
        // =====================================================

        if (!EndFarmPutMode)
        {
            return;
        }


        if (previewfarms.Count == 0)
        {
            return;
        }


        // =====================================================
        // Money
        // =====================================================

        if (moneyManager.HavingMoney <
            CurrentCost)
        {
            DebugController.Log(
                "お金足りない"
            );

            return;
        }


        FarmStartAndEndPos[1] =
            currentTargetPos;


        GenerateFarmBlocks();


        FarmGenModeOff();
    }


    // =========================================================
    // Cancel
    // =========================================================

    public void CancelFarmPlacement()
    {
        FarmGenModeOff();
    }


    // =========================================================
    // End placement
    // =========================================================

    private void FarmGenModeOff()
    {
        StartFarmPutMode =
            false;

        EndFarmPutMode =
            false;

        Clickable =
            false;


        ClearRangePreview();


        if (previewfarm_Start != null)
        {
            SetRenderersEnabled(
                previewfarm_Start,
                false
            );
        }


        FarmSetButton.gameObject.SetActive(
            false
        );

        FarmConstStopButton.gameObject.SetActive(
            false
        );

        NeedMoneyPanel.SetActive(
            false
        );


        CurrentCost =
            0;


        UC.CloseUI();
    }


    // =========================================================
    // Start position
    //
    // 完全自由配置。
    // 既存Farmの近くだけ隣接位置にSnap。
    // =========================================================

    private Vector3 GetStartPosition(
        Vector3 hitPosition)
    {
        Vector3 freePosition =
            new Vector3(
                hitPosition.x,
                hitPosition.y +
                    farmYOffset,
                hitPosition.z
            );


        if (TrySnapToExistingFarm(
                freePosition,
                out Vector3 snappedPosition))
        {
            return snappedPosition;
        }


        return freePosition;
    }


    // =========================================================
    // Snap to existing Farm
    // =========================================================

    private bool TrySnapToExistingFarm(
        Vector3 rawPosition,
        out Vector3 snappedPosition)
    {
        snappedPosition =
            rawPosition;


        if (farmManager == null)
        {
            return false;
        }


        float nearestDistance =
            farmSnapDistance;


        Vector2 rawXZ =
            new Vector2(
                rawPosition.x,
                rawPosition.z
            );


        foreach (
            GameObject farm
            in farmManager.AllFarmBlocks)
        {
            if (farm == null)
            {
                continue;
            }


            Vector3 farmPosition =
                farm.transform.position;


            // =================================================
            // Colliderの実寸ぶんだけずらす
            //
            // → Farm同士がぴったり接する
            // =================================================

            Vector3[] candidates =
            {
                new Vector3(
                    farmPosition.x +
                        farmBlockSizeX,
                    farmPosition.y,
                    farmPosition.z
                ),

                new Vector3(
                    farmPosition.x -
                        farmBlockSizeX,
                    farmPosition.y,
                    farmPosition.z
                ),

                new Vector3(
                    farmPosition.x,
                    farmPosition.y,
                    farmPosition.z +
                        farmBlockSizeZ
                ),

                new Vector3(
                    farmPosition.x,
                    farmPosition.y,
                    farmPosition.z -
                        farmBlockSizeZ
                )
            };


            foreach (
                Vector3 candidate
                in candidates)
            {
                // 既にFarmがある位置は候補にしない
                if (IsFarmOccupied(
                        candidate))
                {
                    continue;
                }


                float distance =
                    Vector2.Distance(
                        rawXZ,
                        new Vector2(
                            candidate.x,
                            candidate.z
                        )
                    );


                if (distance <
                    nearestDistance)
                {
                    nearestDistance =
                        distance;

                    snappedPosition =
                        candidate;
                }
            }
        }


        return
            nearestDistance <
            farmSnapDistance;
    }


    // =========================================================
    // End position
    //
    // 1点目を基準にFarm実寸単位でSnap
    // =========================================================

    private Vector3 GetRelativeSnappedPosition(
        Vector3 rawPosition)
    {
        Vector3 start =
            FarmStartAndEndPos[0];


        float xOffset =
            Mathf.Round(
                (rawPosition.x -
                 start.x)
                /
                farmBlockSizeX
            )
            *
            farmBlockSizeX;


        float zOffset =
            Mathf.Round(
                (rawPosition.z -
                 start.z)
                /
                farmBlockSizeZ
            )
            *
            farmBlockSizeZ;


        return new Vector3(
            start.x +
                xOffset,

            start.y,

            start.z +
                zOffset
        );
    }


    // =========================================================
    // Rectangle preview
    // =========================================================

    private void UpdateRangePreview(
        Vector3 endPosition)
    {
        ClearRangePreview();


        Vector3 start =
            FarmStartAndEndPos[0];


        // =====================================================
        // Number of blocks
        // =====================================================

        int xCount =
            Mathf.RoundToInt(
                Mathf.Abs(
                    endPosition.x -
                    start.x
                )
                /
                farmBlockSizeX
            );


        int zCount =
            Mathf.RoundToInt(
                Mathf.Abs(
                    endPosition.z -
                    start.z
                )
                /
                farmBlockSizeZ
            );


        float xDirection =
            endPosition.x >=
            start.x
                ? 1f
                : -1f;


        float zDirection =
            endPosition.z >=
            start.z
                ? 1f
                : -1f;


        bool allPlaceable =
            true;


        // =====================================================
        // Generate previews
        // =====================================================

        for (int x = 0;
             x <= xCount;
             x++)
        {
            for (int z = 0;
                 z <= zCount;
                 z++)
            {
                Vector3 tilePosition =
                    new Vector3(
                        start.x +
                            x *
                            farmBlockSizeX *
                            xDirection,

                        start.y,

                        start.z +
                            z *
                            farmBlockSizeZ *
                            zDirection
                    );


                GameObject tile =
                    CreateFarmPreview(
                        tilePosition
                    );


                previewfarms.Add(
                    tile
                );


                bool blocked =
                    IsPreviewBlocked(
                        tile
                    );


                if (blocked)
                {
                    allPlaceable =
                        false;
                }
            }
        }


        // =====================================================
        // Preview color
        // =====================================================

        Color color =
            allPlaceable
                ? canPlaceColor
                : cannotPlaceColor;


        foreach (
            GameObject preview
            in previewfarms)
        {
            SetPreviewColor(
                preview,
                color
            );
        }


        Clickable =
            allPlaceable;


        CurrentCost =
            previewfarms.Count *
            FarmCostParMas;


        NeedMoneyText.text =
            CurrentCost.ToString();


        UpdateConfirmButton();
    }


    // =========================================================
    // Preview creation
    //
    // Farm_Brockそのものを複製する。
    // =========================================================

    private GameObject CreateFarmPreview(
        Vector3 position)
    {
        GameObject preview =
            Instantiate(
                Farm_Brock,
                position,
                Quaternion.identity,
                Farm_Brock_Preview_Pa
            );


        // =====================================================
        // Preview material
        // =====================================================

        Renderer[] renderers =
            preview.GetComponentsInChildren<Renderer>(
                true
            );


        foreach (
            Renderer renderer
            in renderers)
        {
            Material[] materials =
                renderer.materials;


            for (int i = 0;
                 i < materials.Length;
                 i++)
            {
                materials[i] =
                    previewMaterial;
            }


            renderer.materials =
                materials;
        }


        // =====================================================
        // PreviewなのでScriptは動かさない
        // =====================================================

        MonoBehaviour[] behaviours =
            preview.GetComponentsInChildren<MonoBehaviour>(
                true
            );


        foreach (
            MonoBehaviour behaviour
            in behaviours)
        {
            behaviour.enabled =
                false;
        }


        // =====================================================
        // Preview自身のColliderは全部OFF
        //
        // 実物FarmはONのまま。
        // =====================================================

        Collider[] colliders =
            preview.GetComponentsInChildren<Collider>(
                true
            );


        foreach (
            Collider collider
            in colliders)
        {
            collider.enabled =
                false;
        }


        return preview;
    }


    // =========================================================
    // Can this preview be placed?
    // =========================================================

    private bool IsPreviewBlocked(
        GameObject preview)
    {
        if (preview == null)
        {
            return true;
        }


        BoxCollider farmCollider =
            preview.GetComponent<BoxCollider>();


        if (farmCollider == null)
        {
            Debug.LogWarning(
                $"{preview.name} のRootにBoxColliderがありません"
            );

            return true;
        }


        // =====================================================
        // Farm Colliderのワールド中心
        // =====================================================

        Vector3 center =
            farmCollider.transform.TransformPoint(
                farmCollider.center
            );


        // =====================================================
        // Farm Colliderのワールドサイズ
        // =====================================================

        Vector3 scale =
            farmCollider.transform.lossyScale;


        Vector3 halfExtents =
            new Vector3(
                farmCollider.size.x *
                    Mathf.Abs(scale.x) *
                    0.5f,

                farmCollider.size.y *
                    Mathf.Abs(scale.y) *
                    0.5f,

                farmCollider.size.z *
                    Mathf.Abs(scale.z) *
                    0.5f
            );


        // =====================================================
        // 少しだけ縮める
        //
        // Collider同士が「接しているだけ」で
        // 重複扱いになるのを防ぐ
        // =====================================================

        halfExtents.x =
            Mathf.Max(
                0.001f,
                halfExtents.x -
                    obstacleCheckInset
            );

        halfExtents.z =
            Mathf.Max(
                0.001f,
                halfExtents.z -
                    obstacleCheckInset
            );


        // =====================================================
        // Building / Tree等
        //
        // Farm Layerはmaskに入れない。
        // =====================================================

        bool obstacleOverlap =
            Physics.CheckBox(
                center,
                halfExtents,
                farmCollider.transform.rotation,
                mask,
                QueryTriggerInteraction.Collide
            );


        // =====================================================
        // Existing Farm
        //
        // Farm同士だけは座標で判定する。
        // =====================================================

        bool farmOverlap =
            IsFarmOccupied(
                preview.transform.position
            );


        return
            obstacleOverlap ||
            farmOverlap;
    }


    // =========================================================
    // Existing farm overlap
    // =========================================================

    private bool IsFarmOccupied(
        Vector3 position)
    {
        if (farmManager == null)
        {
            return false;
        }


        // =====================================================
        // 1マス分ぴったり離れていれば隣接なのでOK。
        //
        // ほぼ同じマスに入った時だけ重複。
        // =====================================================

        float xLimit =
            farmBlockSizeX *
            0.95f;

        float zLimit =
            farmBlockSizeZ *
            0.95f;


        foreach (
            GameObject farm
            in farmManager.AllFarmBlocks)
        {
            if (farm == null)
            {
                continue;
            }


            float xDistance =
                Mathf.Abs(
                    farm.transform.position.x -
                    position.x
                );


            float zDistance =
                Mathf.Abs(
                    farm.transform.position.z -
                    position.z
                );


            if (xDistance <
                    xLimit &&
                zDistance <
                    zLimit)
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // Generate actual farms
    // =========================================================

    private void GenerateFarmBlocks()
    {
        List<Vector3> positions = new List<Vector3>();

        foreach (GameObject preview in previewfarms)
        {
            positions.Add(
                preview.transform.position
            );
        }

        farmManager.CreateFarmBlocks(
            positions
        );


        // =====================================================
        // Money
        // =====================================================

        moneyManager.HavingMoneyUpdate(
            -CurrentCost
        );


        // =====================================================
        // Preview cleanup
        // =====================================================

        ClearRangePreview();
    }


    // =========================================================
    // Clear rectangle preview
    // =========================================================

    private void ClearRangePreview()
    {
        foreach (
            GameObject preview
            in previewfarms)
        {
            if (preview != null)
            {
                Destroy(
                    preview
                );
            }
        }


        previewfarms.Clear();
    }


    // =========================================================
    // Preview color
    // =========================================================

    private void SetPreviewColor(
        GameObject preview,
        Color color)
    {
        if (preview == null)
        {
            return;
        }


        Renderer[] renderers =
            preview.GetComponentsInChildren<Renderer>(
                true
            );


        foreach (
            Renderer renderer
            in renderers)
        {
            Material[] materials =
                renderer.materials;


            foreach (
                Material material
                in materials)
            {
                material.color =
                    color;
            }
        }
    }


    // =========================================================
    // Renderer ON / OFF
    // =========================================================

    private void SetRenderersEnabled(
        GameObject obj,
        bool value)
    {
        if (obj == null)
        {
            return;
        }


        Renderer[] renderers =
            obj.GetComponentsInChildren<Renderer>(
                true
            );


        foreach (
            Renderer renderer
            in renderers)
        {
            renderer.enabled =
                value;
        }
    }


    // =========================================================
    // Confirm button
    //
    // PCでもデバッグ用に表示する
    // =========================================================

    private void UpdateConfirmButton()
    {
        FarmSetButton.gameObject.SetActive(
            Clickable
        );
    }


    // =========================================================
    // Pointer
    // =========================================================

    private bool TryGetPointerPosition(
        out Vector2 position)
    {
        position =
            Vector2.zero;


        // =====================================================
        // Mobile
        // =====================================================

        if (Application.isMobilePlatform)
        {
            if (Input.touchCount <= 0)
            {
                return false;
            }


            Touch touch =
                Input.GetTouch(0);


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


        // =====================================================
        // PC
        // =====================================================

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


    // =========================================================
    // Raycast only to Plane
    // =========================================================

    private bool TryRaycastObject(
        GameObject target,
        Ray ray,
        out RaycastHit closestHit)
    {
        closestHit =
            new RaycastHit();


        if (target == null)
        {
            return false;
        }


        Collider[] colliders =
            target.GetComponentsInChildren<Collider>(
                true
            );


        bool found =
            false;

        float closestDistance =
            Mathf.Infinity;


        foreach (
            Collider collider
            in colliders)
        {
            if (!collider.enabled)
            {
                continue;
            }


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

                    closestHit =
                        hit;

                    found =
                        true;
                }
            }
        }


        return found;
    }
}