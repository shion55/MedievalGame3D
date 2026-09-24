using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstructionSite : MonoBehaviour
{
    public Dictionary<MaterialType,int> requiredMaterials
        　　　　= new Dictionary<MaterialType,int>();
    public GameObject smoke;
    public ConstBuildingType constBuildingType;
    public Vector3 spownPos;
    public Quaternion spawnRotation;
}
