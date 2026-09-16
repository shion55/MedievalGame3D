using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BuildingMaterialData
{
    public BuildingType buildingType;
    public List<MaterialType> requiredMaterials;
    public List<MaterialType> producedMaterials;
}
[CreateAssetMenu(fileName = "BuildingProductionData", menuName = "ScriptableObjects/BuildingProductionData")]
public class BuildingProductionData : ScriptableObject
{
    public List<BuildingMaterialData> buildingMaterials;

    public List<MaterialType> GetRequiredMaterials(BuildingType type)
    {
        foreach (var data in buildingMaterials)
        {
            if (data.buildingType == type)
            {
                return data.requiredMaterials;
            }
        }
        return new List<MaterialType>(); // 無かったら空リスト
    }

    // 生産素材を取得
    public List<MaterialType> GetProducedMaterials(BuildingType type)
    {
        foreach (var data in buildingMaterials)
        {
            if (data.buildingType == type)
            {
                return data.producedMaterials;
            }
        }
        return new List<MaterialType>(); // 無かったら空リスト
    }
}
