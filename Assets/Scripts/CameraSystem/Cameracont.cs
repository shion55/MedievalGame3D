using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.GraphicsBuffer;

public class Cameracont : MonoBehaviour
{
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
    }
    void MoveCameraSwipe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastTouchPos = Input.mousePosition;
            isDragging = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastTouchPos;
            if (delta.magnitude < 100f) // ←これ以上はたぶんバグ
            {
                MoveCamera(delta);
            }
            lastTouchPos = Input.mousePosition;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            mainCamera.orthographicSize -= scroll * zoomSpeed;
            mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, minSize, maxSize);
        }
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            // ２点間の距離
            float curDist = Vector2.Distance(t0.position, t1.position);

            if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
            {
                // ピンチ開始時の距離を記録
                prevTouchDist = curDist;
            }
            else
            {
                // 距離差分を正規化してズーム量とする
                float delta = (curDist - prevTouchDist) * zoomSpeed * Time.deltaTime;
                prevTouchDist = curDist;

                Zoom(delta);
            }
        }
        // --- スマホ用 ---
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                MoveCamera(touch.deltaPosition);
            }
        }
    }
    private void Zoom(float delta)
    {
        mainCamera.orthographicSize = Mathf.Clamp(
            mainCamera.orthographicSize - delta,
            minSize,
            maxSize
        );
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
        Vector3 move = new Vector3(-delta.x, 0, -delta.y) * moveSpeed * Time.deltaTime;

        Vector3 newPos = transform.position + move;

        // Clampをかける（XZ平面上のみ）
        newPos.x = Mathf.Clamp(newPos.x, minCameraPos.x, maxCameraPos.x);
        newPos.z = Mathf.Clamp(newPos.z, minCameraPos.y, maxCameraPos.y);

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
}
