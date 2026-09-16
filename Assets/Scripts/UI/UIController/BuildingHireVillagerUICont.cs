using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;
public class BuildingHireVillagerUICont : MonoBehaviour
{
    UIController UC;
    //BuildingUIから呼ばれる　　雇用モードの実装です
    public Transform HireHousePopsPa;　　　//親
    public GameObject HireHousePopUIPrefab;//子
    public GameObject VillagerPanelPrefab; //孫

    public Dictionary<GameObject,GameObject> HouseAndHirePopDic = new Dictionary<GameObject,GameObject>();

    public Job HireJob;
    public GameObject HireBuilding;
    private void Start()
    {
        UC = GetComponent<UIController>();
    }
    public void GenerateHireHousePopUI(Vector3 genposition, GameObject house)//建物が生成された時に呼ばれる,世界空間UIを生成
    {
        genposition = new Vector3(genposition.x - 1, genposition.y + 6, genposition.z);
        GameObject hirehousepoppan = Instantiate(HireHousePopUIPrefab, genposition, Quaternion.identity, HireHousePopsPa);//家の上に浮かぶ板を生成
       
        //辞書に登録
        HouseAndHirePopDic.Add(house, hirehousepoppan);
        //HireHousePOPを非表示
        hirehousepoppan.SetActive(false);
    }

    private void HireVillager(GameObject villager)
    {
        //DebugController.Log("村人を雇用");
        foreach (GameObject housepan in HouseAndHirePopDic.Values)
        {
            housepan.SetActive(false);
        }

        
        UC.tapHandler.JobChangeHouseTap = false;
        UC.statusManager.AssignVillagerJob(villager,HireJob,HireBuilding);
        UC.BackButton.SetActive(false);
        UC.CloseUI();
    }
    public void OpenHireHousePop(GameObject house)
    {
        //DebugController.Log("家POP表示");
        foreach (GameObject housepan in HouseAndHirePopDic.Values)
        {
            housepan.SetActive(false);
        }
        HouseAndHirePopDic[house].SetActive(true);
        foreach(Transform child in HouseAndHirePopDic[house].transform)
        {
            Destroy(child.gameObject);
        }
        foreach (GameObject villager in UC.houseandvillager.housevillagers[house])//家の住人を全員参照してパネルを生成
        {
            GameObject villagerpanel = Instantiate(VillagerPanelPrefab, HouseAndHirePopDic[house].transform);
           
            Job job = UC.statusManager.villagersjob[villager];
            Transform transimage = villagerpanel.transform.Find("JobImage");
            Image jobimage = transimage.GetComponent<Image>();
            jobimage.sprite = UC.jobbuildingmaster.GetDataByJob(job).jobSprite;
            Button hirebutton = villagerpanel.GetComponentInChildren<Button>();
            hirebutton.onClick.RemoveAllListeners();
            hirebutton.onClick.AddListener(() => HireVillager(villager));
        }
    }

   
}
