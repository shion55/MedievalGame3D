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

    public bool JobChangeHouseTap = false;
    void Start()
    {
        mainCamera = Camera.main;
    }
    void Update()
    {
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
            if (Physics.Raycast(ray, out RaycastHit hit) && !uiController.UIOPEN)
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
    private void HouseIsTapped(GameObject house)
    {
        if (JobChangeHouseTap)
        {
            uiController.buildinghirevillagerUIcont.OpenHireHousePop(house);
        }
        else
        {
            uiController.OpenUIFromTap(house, UIENUM.House);
        }
    }
    private void BuildingIsTapped(GameObject building)
    {
        uiController.OpenUIFromTap(building, UIENUM.Building);
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
                    uiController.buildinghirevillagerUIcont.OpenHireHousePop(hit.collider.gameObject);
                }               
            }
        }
    }
}
