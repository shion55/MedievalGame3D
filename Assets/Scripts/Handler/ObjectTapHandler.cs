using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine.EventSystems;
using UnityEngine;

public class ObjectTapHandler : MonoBehaviour
{
    public HouseAndVillager houseandvillager;
    public VIllagerStatusManager statusManager;
    public UIController uiController;
    public ObjectOutlineManager outlineManager;
    private Camera mainCamera;
    public DestroyObjectHandler destroyObjectHandler;

    public bool JobChangeHouseTap = false;

    [SerializeField, Range(0.005f, 0.05f)]
    private float tapThresholdRatio = 0.015f;

    private Vector2 pointerDownPosition;
    private bool pointerStartedOverUI;
    private bool pointerIsDown;
    void Start()
    {
        mainCamera = Camera.main;
    }
    void Update()
    {
        if (destroyObjectHandler != null &&
       destroyObjectHandler.IsDestroyMode)
        {
            return;
        }
        if (JobChangeHouseTap)
        {
            CheckHouseTapped();
        }
        else
        {
            CheckObjectTapped();
        }
        
        
    }

    private void CheckObjectTapped()
    {
        if (!TryGetTapPosition(
                out Vector2 tapPosition))
        {
            return;
        }


        if (uiController.UIOPEN)
            return;


        Ray ray =
            mainCamera.ScreenPointToRay(
                tapPosition
            );


        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                Mathf.Infinity))
        {
            return;
        }


        switch (hit.collider.gameObject.tag)
        {
            case "House":
                HouseIsTapped(
                    hit.collider.gameObject
                );
                break;

            case "Building":
                BuildingIsTapped(
                    hit.collider.gameObject
                );
                break;

            case "Castle":
                CastleIsTapped(
                    hit.collider.gameObject
                );
                break;
        }
    }
    private void HouseIsTapped(GameObject tappedObject)
    {
        House house =
         tappedObject.GetComponentInParent<House>();

        if (house == null)
            return;

        GameObject houseRoot = house.myobj;

        if (JobChangeHouseTap)
        {
            uiController.buildinghirevillagerUIcont
                .OpenHireHousePop(houseRoot);
        }
        else
        {
            uiController.OpenUIFromTap(
                houseRoot,
                UIENUM.House
            );
        }

    }
    private void BuildingIsTapped(GameObject tappedObject)
    {
        BuildingData data =
        tappedObject.GetComponentInParent<BuildingData>();

        if (data == null)
            return;

        GameObject buildingRoot = data.building;

        Debug.Log("buildingistapped " + buildingRoot.name);

        uiController.OpenUIFromTap(
            buildingRoot,
            UIENUM.Building
        );
    }
    private void CastleIsTapped(GameObject castle)
    {
        uiController.OpenUIFromTap(castle, UIENUM.Castle);
    }
    private void EnvironmentIsTapped()
    {

    }
    private void CheckHouseTapped()
    {
        if (!TryGetTapPosition(
                out Vector2 tapPosition))
        {
            return;
        }


        Ray ray =
            mainCamera.ScreenPointToRay(
                tapPosition
            );


        if (!Physics.Raycast(
                ray,
                out RaycastHit hit))
        {
            return;
        }


        if (!hit.collider.CompareTag("House"))
            return;


        House house =
            hit.collider
                .GetComponentInParent<House>();


        if (house == null)
            return;


        GameObject houseRoot =
            house.myobj;


        uiController
            .buildinghirevillagerUIcont
            .OpenHireHousePop(houseRoot);
    }

    private bool TryGetTapPosition(out Vector2 screenPosition)
    {
        screenPosition = Vector2.zero;


        // =========================
        // スマホ
        // =========================
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                pointerDownPosition = touch.position;
                pointerIsDown = true;

                pointerStartedOverUI =
                    EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject(
                        touch.fingerId
                    );

                return false;
            }


            if (touch.phase == TouchPhase.Canceled)
            {
                pointerIsDown = false;
                return false;
            }


            if (touch.phase != TouchPhase.Ended)
                return false;


            if (!pointerIsDown)
                return false;


            pointerIsDown = false;


            if (pointerStartedOverUI)
                return false;


            float threshold =
                Screen.height * tapThresholdRatio;


            float movedDistance =
                Vector2.Distance(
                    pointerDownPosition,
                    touch.position
                );


            // スワイプだった
            if (movedDistance >= threshold)
                return false;


            screenPosition = touch.position;
            return true;
        }


        // =========================
        // PC
        // =========================
        if (Input.GetMouseButtonDown(0))
        {
            pointerDownPosition =
                Input.mousePosition;

            pointerIsDown = true;

            pointerStartedOverUI =
                EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject();

            return false;
        }


        if (Input.GetMouseButtonUp(0))
        {
            if (!pointerIsDown)
                return false;

            pointerIsDown = false;


            if (pointerStartedOverUI)
                return false;


            Vector2 currentPosition =
                Input.mousePosition;

            float threshold =
                Screen.height * tapThresholdRatio;


            if (Vector2.Distance(
                    pointerDownPosition,
                    currentPosition
                ) >= threshold)
            {
                return false;
            }


            screenPosition = currentPosition;
            return true;
        }


        return false;
    }
}
