using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildingUIVillagerJobPrefab : MonoBehaviour
{
    public Sprite NoEmloyeeSprite;
    public Sprite VillagerSprite;

    public Image jobimage;
    public Button SpotButton;
    public Button HireButton;
    public void SetData(Sprite jobImage,bool villagerHired,System.Action SpotAction,System.Action HireAction, System.Action FireAction)
    {
        jobimage.sprite = jobImage;

        if (villagerHired) {
            HireButton.image.sprite = VillagerSprite;
            SpotButton.onClick.RemoveAllListeners();
            SpotButton.onClick.AddListener(() => SpotAction());
            HireButton.onClick.RemoveAllListeners();
            HireButton.onClick.AddListener(() => FireAction());

        }
        else
        {
            HireButton.image.sprite = NoEmloyeeSprite;
            SpotButton.image.enabled = false;
            HireButton.onClick.RemoveAllListeners();
            HireButton.onClick.AddListener(() => HireAction());
        }
       
        
      
    }
    public void HireToFire()
    {
        HireButton.image.sprite = NoEmloyeeSprite;
    }
}
