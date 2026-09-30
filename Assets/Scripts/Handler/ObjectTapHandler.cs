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
        if (Input.GetMouseButtonDown(0)) // タップ or クリック
        {
            if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit,Mathf.Infinity) && !uiController.UIOPEN)
            {
                switch (hit.collider.gameObject.tag)
                {
                    case "House":
                        HouseIsTapped(hit.collider.gameObject);
                        break;
                    case "Building":
                        BuildingIsTapped(hit.collider.gameObject);
                        break ;
                    case "Castle":
                        CastleIsTapped(hit.collider.gameObject);
                        break;
                    default:
                        break;
                }
            }
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
        if (Input.GetMouseButtonDown(0)) // タップ or クリック
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject.tag == "House") // "House" タグがついたオブジェクトを判定
                {
                    House house = hit.collider.gameObject.GetComponentInParent<House>();

                    if (house == null)
                        return;

                    GameObject houseRoot = house.myobj;
                    uiController.buildinghirevillagerUIcont.OpenHireHousePop(houseRoot);
                }               
            }
        }
    }
}
