using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VillagerJobUIPrefab : MonoBehaviour
{
   
    [SerializeField] private Image jobimage;
    [SerializeField] private Button spotButton;
    //[SerializeField] private Button changeJobButton;
    public void SetData(Sprite image,System.Action onSpot)
    {
        
       
        jobimage.sprite = image;
        spotButton.onClick.RemoveAllListeners();
        spotButton.onClick.AddListener(() => onSpot?.Invoke());

 
    }
}
