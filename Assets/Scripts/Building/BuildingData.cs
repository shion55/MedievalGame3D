using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingData : MonoBehaviour
{
    public GameObject building;
    public ConstCategory buildingcategory;
    public BuildingType buildingType;

    //JobBuilding‚Ìê‡
    public Job buildingjob;
    public int workerLimit;
    public List<GameObject> workers = new List<GameObject>();
    public List<GameObject> OccupantVillagers = new List<GameObject>();
    //AmuseBuilding‚Ìê‡
    public int capacity;
    
    public Storage storage;
    public List<MaterialType> amuseneedmaterials = new List<MaterialType>();
    public void DataInitialize(ConstCategory category,BuildingType type, Job? job,GameObject buildingobj,JobBuildingMasterData data,List<MaterialType> materials)
    {
        if(category == ConstCategory.JobBuilding)
        {
            if(job != null)
            {
                buildingjob = job.Value;
            }
            else
            {
                Debug.Log("E‹ÆŒš•¨‚È‚Ì‚Éd–‚ª“o˜^‚³‚ê‚Ä‚¢‚Ü‚¹‚ñ");
            }
            InitDefaultLimit(data);
        }
        else if(category == ConstCategory.AmuseBuilding)
        {
            capacity = 3;
            amuseneedmaterials.Clear();
            amuseneedmaterials = materials;
        }
        buildingcategory = category;
        buildingType = type;
        building = buildingobj;
        storage = new Storage();
    }


    public bool IsVacant => OccupantVillagers.Count < capacity;
    public bool IsRecruiting => workers.Count < workerLimit;
    private void InitDefaultLimit(JobBuildingMasterData data)
    {
        workerLimit = data.GetDataByBuilding(buildingType).defaultWorkerLimit;
    }
}
