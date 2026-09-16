using System;
using System.Collections.Generic;
using UnityEngine;
public enum MaterialType
{
    Wood,
    Carrot,
    Lumber,
    Money,
    Stone,
    Venison,
    Fish
}
public class Storage
{
    public Dictionary<MaterialType, int> materials;

    public Storage()
    {
        materials = new Dictionary<MaterialType, int>();
        foreach (MaterialType material in Enum.GetValues(typeof(MaterialType)))
        {
            materials[material] = 0;  // 初期値として0を設定
        }
    }

    public void AddMaterial(MaterialType type, int amount)
    {
        if (materials.ContainsKey(type))
        {
            materials[type] += amount;
        }
        else
        {
            materials[type] = amount;
        }
    }

    public void RemoveMaterial(MaterialType type, int amount)//取り出し
    {
        if (HasMaterial(type, amount))//あるか確認
        {
            materials[type] -= amount;
        }
    }
    public bool HasMaterial(MaterialType type, int amount)
    {
        return materials.ContainsKey(type) && materials[type] >= amount;
    }
}