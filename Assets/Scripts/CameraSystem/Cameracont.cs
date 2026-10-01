using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.GraphicsBuffer;

public class Cameracont : MonoBehaviour
{
    [Header("カメラ範囲")]
    [SerializeField] private Terrain terrain;

    [SerializeField]
    private float terrainEdgeMargin = 1f;
    public Camera thirdPersonCamera; // TPSカメラ
    public Camera mainCamera;        // 通常のカメラ

    public Vector2 minCameraPos = new Vector2(-50f, -50f);
    public Vector2 maxCameraPos = new Vector2(50f, 50f);

    public bool CameraContActive = true;

    private bool isTPSActive = false; // 現在TPSカメラが有効かどうか

    private GameObject HouseMarkPutterObjects;
    public HouseAndVillager houseandvillager;

    private int currentvillagerindex = 0;
    private GameObject currentTarget;//現在追従するキャラ

    public float moveSpeed = 10.0f;    // カメラの移動速度
    public float lookSpeed = 2.0f;    // カメラの視点移動速度
    public float boostMultiplier = 2.0f; // Shiftキーで加速

    private float rotationX = 0f; // 上下の視点回転角度
    private float rotationY = 0f; // 左右の視点回転角度

    private Vector3 moveDirection = Vector3.zero;
    //↓ドラッグ用
    public float movespeed = 0.1f;

    private Vector2 lastTouchPos;
    private bool isDragging = false;

    public float zoomSpeed = 2f;
    public float minSize = 2f;
    public float maxSize = 20f;

    [Header("画面端スクロール")]
    [SerializeField] private float edgeScrollMargin = 60f;

    [SerializeField] private float edgeScrollSpeed = 12f;

    //↓　追従モード設定
    private GameObject TargetObj;
    // 元のカメラ位置を保持
    private Vector3 originalPosition;
   

    // 追従時のオフセット
    private Vector3 positionOffset;
    private Quaternion rotationOffset;

    // フラグ：追従モード中か
    private bool isFollowing = false;

    private float prevTouchDist;

    //AudioListner用position
    public Transform audioListenerPosition;
    [Header("Audio Listener")]
    [SerializeField] private float minListenerDistance = 3f;
    [SerializeField] private float maxListenerDistance = 30f;

    [SerializeField, Range(0.005f, 0.05f)]
    private float dragThresholdRatio = 0.015f;

    [SerializeField]
    private float dragSensitivity = 1f;

    private Vector2 touchStartPos;
    private bool touchStartedOverUI;
    void Start()
    {
        SetTPSMode(false);
    }

    void Update()
    {

        if (CameraContActive)
        {
            MoveCameraSwipe();
        }
        if (isFollowing) { 
            FollowTarget();
        }
        UpdateAudioListenerPosition();
    }
    void MoveCameraSwipe()
    {
        // =========================
        // スマホ
        // =========================
        if (Input.touchCount > 0)
        {
            // ----- 2本指：ピンチズーム -----
            if (Input.touchCount >= 2)
            {
                isDragging = false;

                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);

                float currentDistance =
                    Vector2.Distance(t0.position, t1.position);

                if (t0.phase == TouchPhase.Began ||
                    t1.phase == TouchPhase.Began)
                {
                    prevTouchDist = currentDistance;
                }
                else
                {
                    float distanceDelta =
                        currentDistance - prevTouchDist;

                    prevTouchDist = currentDistance;

                    // 画面解像度に依存しないよう正規化
                    float normalizedDelta =
                        distanceDelta /
                        Mathf.Max(1f, Screen.height);

                    float zoomDelta =
                        normalizedDelta *
                        zoomSpeed *
                        mainCamera.orthographicSize;

                    Zoom(zoomDelta);
                }

                return;
            }

            // ----- 1本指：カメラ移動 -----
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touch.position;
                    lastTouchPos = touch.position;

                    isDragging = false;

                    touchStartedOverUI =
                        EventSystem.current != null &&
                        EventSystem.current.IsPointerOverGameObject(
                            touch.fingerId
                        );

                    break;

                case TouchPhase.Moved:

                    if (touchStartedOverUI)
                        break;

                    float threshold =
                        Screen.height * dragThresholdRatio;

                    // 一定距離以上動いて初めてドラッグ扱い
                    if (!isDragging)
                    {
                        if (Vector2.Distance(
                                touchStartPos,
                                touch.position
                            ) >= threshold)
                        {
                            isDragging = true;

                            // ドラッグ開始時にジャンプしないようにする
                            lastTouchPos = touch.position;
                        }

                        break;
                    }

                    Vector2 delta =
                        touch.position - lastTouchPos;

                    MoveCamera(delta);

                    lastTouchPos = touch.position;

                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    isDragging = false;
                    touchStartedOverUI = false;
                    break;
            }

            // 重要：
            // Touchを処理したフレームでは
            // GetMouseButton系を実行しない
            return;
        }


        // =========================
        // PC
        // =========================

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            lastTouchPos = Input.mousePosition;
            touchStartPos = Input.mousePosition;
            isDragging = false;
        }

        if (Input.GetMouseButton(0))
        {
            Vector2 currentPos = Input.mousePosition;

            float threshold =
                Screen.height * dragThresholdRatio;

            if (!isDragging)
            {
                if (Vector2.Distance(
                        touchStartPos,
                        currentPos
                    ) >= threshold)
                {
                    isDragging = true;
                    lastTouchPos = currentPos;
                }

                return;
            }

            Vector2 delta =
                currentPos - lastTouchPos;

            MoveCamera(delta);

            lastTouchPos = currentPos;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }


        // マウスホイール
        float scroll =
            Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            Zoom(scroll * zoomSpeed);
        }
    }
    private void Zoom(float delta)
    {
        float oldSize =
            mainCamera.orthographicSize;


        float newSize =
            Mathf.Clamp(
                oldSize - delta,
                minSize,
                maxSize
            );


        mainCamera.orthographicSize =
            newSize;


        // ズームアウトした結果、
        // 画面外にTerrainが見えるなら元に戻す
        if (newSize > oldSize &&
            !IsWholeViewInsideTerrain())
        {
            mainCamera.orthographicSize =
                oldSize;
        }
    }
    private bool IsWholeViewInsideTerrain()
    {
        if (terrain == null ||
            mainCamera == null)
        {
            return true;
        }


        Vector3 terrainPosition =
            terrain.transform.position;

        Vector3 terrainSize =
            terrain.terrainData.size;


        float minX =
            terrainPosition.x +
            terrainEdgeMargin;

        float maxX =
            terrainPosition.x +
            terrainSize.x -
            terrainEdgeMargin;

        float minZ =
            terrainPosition.z +
            terrainEdgeMargin;

        float maxZ =
            terrainPosition.z +
            terrainSize.z -
            terrainEdgeMargin;


        // Terrainの基準高さの水平面
        Plane groundPlane =
            new Plane(
                Vector3.up,
                new Vector3(
                    0f,
                    terrainPosition.y,
                    0f
                )
            );


        Vector2[] corners =
        {
        new Vector2(0f, 0f),
        new Vector2(1f, 0f),
        new Vector2(0f, 1f),
        new Vector2(1f, 1f)
    };


        foreach (Vector2 corner in corners)
        {
            Ray ray =
                mainCamera.ViewportPointToRay(
                    new Vector3(
                        corner.x,
                        corner.y,
                        0f
                    )
                );


            if (!groundPlane.Raycast(
                    ray,
                    out float distance))
            {
                return false;
            }


            Vector3 point =
                ray.GetPoint(distance);


            if (point.x < minX ||
                point.x > maxX ||
                point.z < minZ ||
                point.z > maxZ)
            {
                return false;
            }
        }


        return true;
    }
    public void MoveCameraAtScreenEdge(Vector2 screenPosition)
    {
        if (!CameraContActive)
            return;

        if (isFollowing)
            return;


        float horizontal = 0f;
        float vertical = 0f;


        // 左端
        if (screenPosition.x < edgeScrollMargin)
        {
            horizontal =
                -1f +
                screenPosition.x /
                edgeScrollMargin;
        }
        // 右端
        else if (screenPosition.x >
                 Screen.width - edgeScrollMargin)
        {
            horizontal =
                1f -
                (Screen.width - screenPosition.x) /
                edgeScrollMargin;
        }


        // 下端
        if (screenPosition.y < edgeScrollMargin)
        {
            vertical =
                -1f +
                screenPosition.y /
                edgeScrollMargin;
        }
        // 上端
        else if (screenPosition.y >
                 Screen.height - edgeScrollMargin)
        {
            vertical =
                1f -
                (Screen.height - screenPosition.y) /
                edgeScrollMargin;
        }


        if (Mathf.Approximately(horizontal, 0f) &&
            Mathf.Approximately(vertical, 0f))
        {
            return;
        }


        // カメラの向きをXZ平面へ投影
        Vector3 forward =
            mainCamera.transform.forward;

        forward.y = 0f;
        forward.Normalize();


        Vector3 right =
            mainCamera.transform.right;

        right.y = 0f;
        right.Normalize();


        Vector3 direction =
            right * horizontal +
            forward * vertical;
        //Debug.Log(direction);

        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }


        Vector3 newPos =
            transform.position +
            direction *
            edgeScrollSpeed *
            Time.deltaTime;


        newPos.x =
            Mathf.Clamp(
                newPos.x,
                minCameraPos.x,
                maxCameraPos.x
            );

        newPos.z =
            Mathf.Clamp(
                newPos.z,
                minCameraPos.y,
                maxCameraPos.y
            );


        transform.position =
            newPos;
    }
    void PlaceCameraVirragerBehind()
    {
        if (currentvillagerindex < houseandvillager.villagers.Count)
        {
            currentTarget = houseandvillager.villagers[currentvillagerindex];
            Transform cameraPoint = currentTarget.transform.Find("CameraPoint");
            if (cameraPoint != null)
            {
                thirdPersonCamera.transform.position = cameraPoint.position;
                thirdPersonCamera.transform.rotation = cameraPoint.rotation;
                thirdPersonCamera.transform.parent = cameraPoint; // カメラを追従させる
            }
        }
        else if (currentvillagerindex >= houseandvillager.villagers.Count){
            currentvillagerindex = 0;
            currentTarget = houseandvillager.villagers[currentvillagerindex];
            Transform cameraPoint = currentTarget.transform.Find("CameraPoint");
            if (cameraPoint != null)
            {
                thirdPersonCamera.transform.position = cameraPoint.position;
                thirdPersonCamera.transform.rotation = cameraPoint.rotation;
                thirdPersonCamera.transform.parent = cameraPoint; // カメラを追従させる
            }
        }

}
    void SetTPSMode(bool enableTPS)
    {
        if (enableTPS)
        {
            PlaceCameraVirragerBehind();
            thirdPersonCamera.enabled = true;
            mainCamera.enabled = false;
        }
        else
        {
            thirdPersonCamera.enabled = false;
            mainCamera.enabled = true;
        }
    }

    void MoveCamera(Vector2 delta)
    {
        if (mainCamera == null)
            return;

        // 画面1pixelがワールド上で何mに相当するか
        float worldPerPixel =
            (mainCamera.orthographicSize * 2f) /
            Mathf.Max(1f, Screen.height);

        Vector3 forward =
            mainCamera.transform.forward;

        forward.y = 0f;
        forward.Normalize();

        Vector3 right =
            mainCamera.transform.right;

        right.y = 0f;
        right.Normalize();


        // 「地図を指で掴んで動かす」方向
        Vector3 move =
            (
                -right * delta.x
                - forward * delta.y
            )
            * worldPerPixel
            * dragSensitivity;


        Vector3 newPos =
            transform.position + move;


        newPos.x =
            Mathf.Clamp(
                newPos.x,
                minCameraPos.x,
                maxCameraPos.x
            );

        newPos.z =
            Mathf.Clamp(
                newPos.z,
                minCameraPos.y,
                maxCameraPos.y
            );


        transform.position = newPos;
    }


    public void EnterFollowMode(GameObject target)
    {
        originalPosition = transform.position;

        // --- ここから「村人を中央に合わせる処理」 ---
        
        float depth = Camera.main.WorldToViewportPoint(target.transform.position).z;
        Vector3 worldCenter = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth));
        Vector3 delta = target.transform.position - worldCenter;
        Camera.main.transform.position += delta;
        // --- ここまで ---

        // ここで「現在のカメラ⇔対象の位置差」を計算
        positionOffset = transform.position - target.transform.position;
               
        TargetObj = target;
        isFollowing = true;
    }

    private void FollowTarget()
    {
        
        this.transform.position = TargetObj.transform.position + positionOffset;
    }
    public void ExitFollowMode()
    {
        isFollowing = false;

        // 元の位置・回転を復帰
        transform.position = originalPosition;
        
    }

    private void UpdateAudioListenerPosition()
    {
        if (audioListenerPosition == null ||
            mainCamera == null)
        {
            return;
        }

        // 画面中央が指している地面を求める
        Ray ray = mainCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        float groundY =
            terrain != null
                ? terrain.transform.position.y
                : 0f;

        Plane groundPlane =
            new Plane(
                Vector3.up,
                new Vector3(0f, groundY, 0f)
            );

        if (!groundPlane.Raycast(ray, out float distance))
            return;

        Vector3 groundPoint =
            ray.GetPoint(distance);

        // Terrainの実際の高さ
        if (terrain != null)
        {
            groundPoint.y =
                terrain.SampleHeight(groundPoint)
                + terrain.transform.position.y;
        }

        // orthographicSizeを
        // 0～1のズーム値へ変換
        float zoomT =
            Mathf.InverseLerp(
                minSize,
                maxSize,
                mainCamera.orthographicSize
            );

        // ズームイン → 耳が近い
        // ズームアウト → 耳が遠い
        float listenerDistance =
            Mathf.Lerp(
                minListenerDistance,
                maxListenerDistance,
                zoomT
            );

        // カメラを見る方向の逆側へListenerを移動
        audioListenerPosition.position =
            groundPoint
            - mainCamera.transform.forward
            * listenerDistance;

        // 音の左右方向もカメラに合わせる
        audioListenerPosition.rotation =
            mainCamera.transform.rotation;
    }
}
