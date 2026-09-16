using System.Collections;
using System.Collections.Generic;
//using Unity.Android.Gradle;
using UnityEngine;
using UnityEngine.UI;

public class BuildingUICont : MonoBehaviour
{
    UIController UC;
    public GameObject BuildingUI;
    public Transform BuildingMaterialPanelPa;
    public GameObject MaterialStoragePrefab;

    public Transform BuildingUIWorkerPanelPa;
    public GameObject BuildingUIWorkerPrefab;



    public Sprite UnEmploySprite;
    public Sprite ViilagerSprite;
    private void Start()
    {
        UC = GetComponent<UIController>();
    }
    public void OpenBuildingUI(GameObject building)
    {
        
        BuildingUI.SetActive(true);
        //マテリアル画面(左側)
        foreach (Transform child in BuildingMaterialPanelPa)//一旦破壊
        {
            Destroy(child.gameObject);
        }
        BuildingData data = building.GetComponent<BuildingData>();
        BuildingType buildtype = data.buildingType;　　　//GameObjectからenum(その建物のtype)を取得
        List<MaterialType> MatType = UC.jobbuildingmaster.GetDataByBuilding(buildtype).producedMaterials;//その建物のマテリアルタイプ取得(これは種類なのでenumから取得)
        foreach (MaterialType matType in MatType)
        {
            GameObject matLabel = Instantiate(MaterialStoragePrefab, BuildingMaterialPanelPa);//Gridを持っている親に付ける
            MaterialStorageUIPrefab uiprefab = matLabel.GetComponent<MaterialStorageUIPrefab>();
            uiprefab.MaterialImage.sprite = UC.MaterialspriteData.GetSprite(matType);   //画像
            uiprefab.StorageText.text = data.storage.materials[matType].ToString();//いくらはいっているか
        }
        //worker画面(右側)
        
        foreach (Transform child in BuildingUIWorkerPanelPa)//一旦破壊
        {
            Destroy(child.gameObject);
        }
        int workerlimit = data.workerLimit;//働ける人数
        int workingvil = data.workers.Count;//現在の就職人数
        for (int i = 0; i < workerlimit; i++)//建物で働ける人数分パネルを作る
        {
            GameObject workerlabel = Instantiate(BuildingUIWorkerPrefab, BuildingUIWorkerPanelPa);
            BuildingUIVillagerJobPrefab vuiprefab = workerlabel.GetComponent<BuildingUIVillagerJobPrefab>();
            bool hired;
            GameObject vil;
            if(i < workingvil)//働いている→ボタンを村人の顔にする
            {
                hired = true;
                vil = data.workers [i];
            }
            else
            {
                hired = false;
                vil = null;
            }
            vuiprefab.SetData(UC.jobbuildingmaster.GetDataByBuilding(buildtype).jobSprite,
                              hired,　　　　　　　　　　　　　//hiredか否か
                              () => UC.SpotToVillager(vil, 1),//村人とBuildingUIから呼ばれたというindex
                              () => UC.StartHireMode(building),
                              () => FireVillager(building,buildtype, vuiprefab, vil));
        }
      
    }
   public void FireVillager(GameObject building,BuildingType buildtype, BuildingUIVillagerJobPrefab vuiprefab,GameObject villager)//prefabの村人ボタンから呼ばれます
    {
        UC.statusManager.AssignVillagerJob(villager, Job.UnEmployer, null);
        vuiprefab.SetData(UC.jobbuildingmaster.GetDataByBuilding(buildtype).jobSprite,
                              false,　　　　　　　　　　　　　//hiredか否か
                              null,
                              () => UC.StartHireMode(building),
                             null);
    }
}
