using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class ConstBuildingData
{
    public ConstBuildingType conbuildingtype;
    public Sprite conbuildingSprite;
    public GameObject conbuildingPrefab;
    public ConstCategory constcategory;
    public List<MaterialRequirement> requirements;

    public Dictionary<MaterialType, int> GetMaterialDict()
    {
        var dict = new Dictionary<MaterialType, int>();
        foreach (var req in requirements)
        {
            dict[req.materialType] = req.amount;
        }
        return dict;
    }

   
}


[System.Serializable]
public class MaterialRequirement
{
    public MaterialType materialType;
    public int amount;
}
[CreateAssetMenu(fileName = "ConstBuildingMasterData", menuName = "ScriptableObjects/ConstBuildingMasterData")]
public class ConstBuildingMasterDataSO : ScriptableObject
{
    public List<ConstBuildingData> conbuildingdata;

    private Dictionary<ConstBuildingType, ConstBuildingData> constbuildingDict;

    private void OnEnable()
    {
        BuildDictionary();
    }

    private void BuildDictionary()
    {
        constbuildingDict = new Dictionary<ConstBuildingType, ConstBuildingData>();
        foreach (var building in conbuildingdata)
        {
            constbuildingDict[building.conbuildingtype] = building;
        }
    }

    /// <summary>
    /// BuildingType‚É‘Î‰ž‚·‚éBuildingData‚ðŽæ“¾‚·‚é
    /// </summary>
    public ConstBuildingData GetData(ConstBuildingType type)
    {
        if (constbuildingDict == null || constbuildingDict.Count != conbuildingdata.Count)
        {
            BuildDictionary();
        }

        if (constbuildingDict.TryGetValue(type, out var data))
        {
            return data;
        }
        else
        {
            Debug.LogWarning($"BuildingData not found for {type}");
            return null;
        }
    }
}
