using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
public class WorldSpaceUIController : MonoBehaviour
{
    public JobBuildingMasterData jobBuildingMaster;
    public BuildingManager buildingManager;
    [Header("進捗UI")]
    public Transform ProgressBarParent;
    public GameObject progressBarPrefab;
    private Dictionary<GameObject,GameObject> buildingandprogressBars = new Dictionary<GameObject, GameObject>();//建物と進捗UIの辞書
    public Sprite[] progressSprites;  // スプライトの配列
    [Header("生産完了PopUpUI")]
    public Transform MatPopupParent;
    public GameObject ProducedMatPopupPrefab;
    public MaterialAndSpriteSO matandsprite;
    private Dictionary<GameObject, Dictionary<MaterialType,GameObject>> buildingandMaterialPopup = new Dictionary<GameObject, Dictionary<MaterialType, GameObject>>();//建物と資材ポップアップの辞書
    public float PopupDuration = 2f;
    public void GenerateWorldSpaceBuildingUI(Vector3 genposition,GameObject building)//建物が生成された時に呼ばれる,世界空間UIを生成
    {
        genposition = new Vector3(genposition.x,genposition.y +3,genposition.z);
        GameObject progressbar = Instantiate(progressBarPrefab, genposition, Quaternion.identity, ProgressBarParent);//生産の進捗バー
        buildingandprogressBars.Add(building,progressbar);//建物と進捗UIの辞書に追加

        
        BuildingType buildingType = building.GetComponent<BuildingData>().buildingType;//建物の種類を取得
        List<MaterialType> mattypes = jobBuildingMaster.GetDataByBuilding(buildingType).producedMaterials;//↑からマテリアルの種類を取得
        List<GameObject> PopUps = new List<GameObject>();//ポップアップのリスト
        buildingandMaterialPopup.Add(building, new Dictionary<MaterialType, GameObject>());
        foreach (MaterialType mattype in mattypes) {
            GameObject MatPopup = Instantiate(ProducedMatPopupPrefab, genposition, Quaternion.identity, MatPopupParent);
            buildingandMaterialPopup[building].Add(mattype,MatPopup);//建物と マテリアルとポップアップの辞書 を 辞書に追加
            MatPopup.GetComponentInChildren<Image>().sprite = matandsprite.GetSprite(mattype);//MatとspriteのSOからsprite取得
            MatPopup.GetComponentInChildren<TextMeshProUGUI>().text = "+1";
        }     
    }
    
    public void  ChangeProgressBar(GameObject building,int progress)
    {
        GameObject progressbar = buildingandprogressBars[building];
        if (progress < progressSprites.Length)
        {
            if (progressbar.activeSelf == false)
            {
                progressbar.SetActive(true);
            }
            progressbar.GetComponent<Image>().sprite = progressSprites[progress];
        }
        else
        {
            progressbar.SetActive(false);
        }
    }

    public void ShowMaterialPopUp(MaterialType material,GameObject building)
    {
        GameObject popup = buildingandMaterialPopup[building][material];
        Vector3 originalLocalPos = building.transform.position;
        originalLocalPos.y += 3;
        popup.transform.position = originalLocalPos;
        popup.SetActive(true);
        popup.transform.DOMoveY(popup.transform.position.y + 1f,PopupDuration).OnComplete(() => popup.SetActive(false));
      
    }

}
