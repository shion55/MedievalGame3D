using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CastleUICont : MonoBehaviour
{
    public GameObject CastleUI;
    public Transform CastleStoragePanelTransform;
    public GameObject MaterialStorage_CastlePrefab;

    UIController UC;

    void Start()
    {
        UC = GetComponent<UIController>();
    }

    public void OpenCastleUI()
    {
        CastleUI.SetActive(true);
        foreach (Transform child in CastleStoragePanelTransform)
        {
            Destroy(child.gameObject);
        }
        foreach (MaterialType materialType in Enum.GetValues(typeof(MaterialType)))
        {
            if (materialType == MaterialType.Money) continue;
            GameObject matLabel = Instantiate(MaterialStorage_CastlePrefab, CastleStoragePanelTransform);
            MaterialStorageUIPrefab uiprefab = matLabel.GetComponent<MaterialStorageUIPrefab>();
            uiprefab.MaterialImage.sprite = UC.MaterialspriteData.GetSprite(materialType);
            BuildingData data = UC.buildingManager.Castle.GetComponent<BuildingData>();
            
            uiprefab.StorageText.text = data.storage.materials[materialType].ToString();
        }
    }
}
