using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public HouseAndVillager houseAndvillager;
    public VIllagerStatusManager statusManager;
    public WorldSpaceUIController worldSpaceUIController;
    
    public List<GameObject> JobBuildings = new List<GameObject>();
    public List<GameObject> AmuseBuildings = new List<GameObject>();


    #region     城
    public GameObject Castle;
    #endregion
    #region 木こり小屋設定
    public List <GameObject> woodmanCabins = new List <GameObject> ();
    #endregion
    
    //建物と必要・生産マテリアルを記録しているSO
    public BuildingProductionData buildingProductionData;

    public JobBuildingMasterData jobbuildingmaster;
    void Start()
    {
        BuildingData castledata = Castle.GetComponent<BuildingData>();
        castledata.DataInitialize(ConstCategory.JobBuilding, BuildingType.castle, Job.Carrier,Castle, jobbuildingmaster,null);
        JobBuildings.Add(Castle);

        //最初の木こり小屋の設定
        foreach (GameObject cabin in woodmanCabins) {

            BuildingData buildingdata = cabin.GetComponent<BuildingData>();
            buildingdata.DataInitialize(ConstCategory.JobBuilding, BuildingType.woodcabin, Job.WoodCutter, cabin, jobbuildingmaster,null);
            JobBuildings.Add(cabin);
            worldSpaceUIController.GenerateWorldSpaceBuildingUI(cabin.transform.position, cabin);
            BuildingStorageUpdate(cabin, MaterialType.Wood, 1);
        }
        
    }
    public void BuildingStorageUpdate(GameObject building,MaterialType type,int  value)
    {
        BuildingData data = building.GetComponent<BuildingData>();
        data.storage.AddMaterial(type, value);
    }
    public int MaterialAllAmountCheck(List<MaterialType> types) { 
         int amount = 0; 
        foreach(GameObject bd in JobBuildings)
        {
            BuildingData data = bd.GetComponent<BuildingData>();
            foreach(var mat  in types)
            {
                amount += data.storage.materials[mat];
            }
        }
        foreach (GameObject bd in AmuseBuildings)
        {
            BuildingData data2 = bd.GetComponent<BuildingData>();
            foreach (var mat2 in types)
            {
                amount += data2.storage.materials[mat2];
            }
        }
        return amount;
    }
}




public class JobBuildingData
{
    public GameObject building;
    public BuildingType buildingType;
    public Job buildingjob;
    public Storage storage;
    public int workerLimit;
    public List<GameObject> workers = new List<GameObject>();

    public bool IsRecruiting => workers.Count < workerLimit;
    public void InitDefaultLimit(JobBuildingMasterData data)
    {
        workerLimit = data.GetDataByBuilding(buildingType).defaultWorkerLimit;
    }
}
public class AmuseBuildingData
{
    public GameObject building;
    public BuildingType buildingType;
    public int capacity;
    public List<GameObject> OccupantVillagers = new List<GameObject>();
    public bool IsVacant => OccupantVillagers.Count < capacity;

    public Storage storage;

    public List<MaterialType> needMaterials = new List<MaterialType>();
}
