using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class JobAndBuildingData
{
    public Job jobType;
    public Sprite jobSprite;
    public Color jobClothColor;
    public BuildingType buildingType;
    public Sprite buildingSprite;
    public List<MaterialType> requiredMaterials;
    public List<MaterialType> producedMaterials;
    public int defaultWorkerLimit;
}

[CreateAssetMenu(fileName = "JobAndBuildingMaster", menuName = "ScriptableObjects/JobAndBuildingMaster")]
public class JobBuildingMasterData : ScriptableObject
{
    public List<JobAndBuildingData> jobBuildingList;

    private Dictionary<Job, JobAndBuildingData> jobDict;
    private Dictionary<BuildingType, JobAndBuildingData> buildingDict;

    public void Init()
    {
        if (jobDict != null) return; // Ç∑Ç≈Ç…èâä˙âªçœÇ›

        jobDict = new Dictionary<Job, JobAndBuildingData>();
        buildingDict = new Dictionary<BuildingType, JobAndBuildingData>();

        foreach (var data in jobBuildingList)
        {
            if (!jobDict.ContainsKey(data.jobType))
                jobDict.Add(data.jobType, data);

            if (!buildingDict.ContainsKey(data.buildingType))
                buildingDict.Add(data.buildingType, data);
        }
    }

    public JobAndBuildingData GetDataByJob(Job job)
    {
        Init();
        return jobDict.TryGetValue(job, out var data) ? data : null;
    }

    public JobAndBuildingData GetDataByBuilding(BuildingType type)
    {
        Init();
        return buildingDict.TryGetValue(type, out var data) ? data : null;
    }
}
