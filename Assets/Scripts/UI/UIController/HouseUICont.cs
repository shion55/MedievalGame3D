using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class HouseUICont : MonoBehaviour
{
    UIController UC;

    public GameObject HouseUI; // UIパネル
    public Transform VillagersPanelPa;
    public GameObject VillagerPanelPrefab;

    public GameObject VillagerMeterPanelUI;
    public Image HungerMeterImage;

    public GameObject JobChangePanelUI;
    public Transform JobChangeScrollContentPa;
    public GameObject JobChangeScrollContentPrefab;

    public Button JobBackButton;

    void Start()
    {
        UC = this.GetComponent<UIController>();
    }

    public void OpenHouseUI(GameObject house)
    {
        HouseUI.SetActive(true);     // 家UI を表示
        VillagerMeterPanelUI.SetActive(true);  //消えていた時用にメーターパネルも表示
        JobChangePanelUI.SetActive(false);//右側は非表示
        foreach (Transform child in VillagersPanelPa)
        {
            Destroy(child.gameObject);
        }
        foreach (GameObject villager in UC.houseandvillager.housevillagers[house])//家所属の村人
        {
            GameObject uiprefab = Instantiate(VillagerPanelPrefab, VillagersPanelPa);
            Job job = UC.statusManager.villagersjob[villager];//村人から仕事を取得
            HouseUIVillagerPanelPrefab villagerpanelprefab = uiprefab.GetComponent<HouseUIVillagerPanelPrefab>();
            villagerpanelprefab.HouseUICurrentJobImage.sprite = UC.jobbuildingmaster.GetDataByJob(job).jobSprite;
            villagerpanelprefab.HouseUIJobChangeButton.onClick.RemoveAllListeners();
            villagerpanelprefab.HouseUISpotVillagerButton.onClick.RemoveAllListeners();
            villagerpanelprefab.HouseUIJobChangeButton.onClick.AddListener(() => OpenJobChangeUI(villager));//jobchangeを呼ぶ
            villagerpanelprefab.HouseUISpotVillagerButton.onClick.AddListener(() => UC.SpotToVillager(villager,0));//SpotVillagerを呼ぶ  HouseUIから呼ばれたことを示すため　→　0
            VillagerHappiness VH = villager.GetComponent<VillagerHappiness>();
            float hungerlevel = VH.HungerLevel;
            HungerMeterUpdate(hungerlevel);
        }  
    }
    private void HungerMeterUpdate(float hungerlevel)
    {
        float scaledValue = hungerlevel / 100 * 0.55f;
        Vector3 scale = HungerMeterImage.rectTransform.localScale;
        scale.x = scaledValue;
        HungerMeterImage.rectTransform.localScale = scale;
    }
    
    private void OpenJobChangeUI(GameObject villager)//家の村人
    {
        //メーター部分を非表示
        VillagerMeterPanelUI.SetActive(false);
        JobChangePanelUI.SetActive(true);
        //ジョブチェンジから戻るボタン
        JobBackButton.onClick.RemoveAllListeners();
        JobBackButton.onClick.AddListener(() => BackJobChangeUI());

        foreach(Transform child in JobChangeScrollContentPa)
        {
            Destroy(child.gameObject);
        }
        //建物が必要ない職業　－　無職　ビルダー　運搬
        Job[] nobuildingjobs = { Job.UnEmployer, Job.Builder};

        foreach (Job job in nobuildingjobs) {

            if (UC.statusManager.villagersjob[villager] != job)//現在就業中のものは抜かす
            {
                GameObject panel = Instantiate(JobChangeScrollContentPrefab, JobChangeScrollContentPa);
                HouseUIJobChangeUIPrefab panelprefab = panel.GetComponent<HouseUIJobChangeUIPrefab>();
                Sprite buildsprite = UC.jobbuildingmaster.GetDataByJob(job).jobSprite;
                panelprefab.JobImage.sprite = buildsprite;
                //spotbutton下に実装してね


                panelprefab.ChangeJobButton.onClick.RemoveAllListeners();
                panelprefab.ChangeJobButton.onClick.AddListener(() => JobChangeClicked(villager,job,null));
            }   
        }

        //建物がある職業
        foreach (GameObject building in UC.buildingManager.JobBuildings)
        {
            BuildingData data = building.GetComponent<BuildingData>();
            if (data.IsRecruiting && !data.workers.Contains(villager))//就業中の建物は選択肢から消す
            {
                GameObject panel = Instantiate(JobChangeScrollContentPrefab, JobChangeScrollContentPa);
                HouseUIJobChangeUIPrefab panelprefab = panel.GetComponent<HouseUIJobChangeUIPrefab>();
                Sprite buildsprite = UC.jobbuildingmaster.GetDataByBuilding(data.buildingType).jobSprite;
                panelprefab.JobImage.sprite = buildsprite;
                //spotbutton下に実装してね


                panelprefab.ChangeJobButton.onClick.RemoveAllListeners();
                panelprefab.ChangeJobButton.onClick.AddListener(() => JobChangeClicked(villager, UC.jobbuildingmaster.GetDataByBuilding(data.buildingType).jobType,data.building));
            }
        
        }
    }
    private void BackJobChangeUI()
    {
        JobChangePanelUI.SetActive(false);
        VillagerMeterPanelUI.SetActive(true);
    }
    private void JobChangeClicked(GameObject villager,Job job,GameObject jobBuilding)
    {
        UC.statusManager.AssignVillagerJob(villager, job,jobBuilding);
        UC.CloseUI();
    }
}
