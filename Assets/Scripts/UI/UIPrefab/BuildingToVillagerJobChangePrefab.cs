using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingToVillagerJobChangePrefab : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image jobimage;
    [SerializeField] private TextMeshProUGUI jobText;
    [SerializeField] private Button spotButton;
    [SerializeField] private Button changeJobButton;
    public GameObject BackGround;
    public void SetData(string name,Sprite jobsprite ,string job,Sprite ChangeJobButtonImage, System.Action onSpot, System.Action onChangeJob)
    {
        nameText.text = name;
        jobimage.sprite = jobsprite;
        jobText.text = job;

     
        spotButton.onClick.RemoveAllListeners();
        spotButton.onClick.AddListener(() => onSpot?.Invoke());

        changeJobButton.image.sprite = ChangeJobButtonImage;
        changeJobButton.onClick.RemoveAllListeners();
        changeJobButton.onClick.AddListener(() => onChangeJob?.Invoke());
    }
}
