using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum VillagerAcceType
{
    Wood,
    Axe,
    Bow,
    Venison,
    Carrot,
    Fisher_Rod,
    Fish,
    Lumber,
    Stone,
    WateringCan
};

public class VillagerAcce : MonoBehaviour
{
    [HideInInspector] public static VillagerAcce VA { get; private set; }

    [System.Serializable]
    public struct AccessoryEntry
    {
        public VillagerAcceType type;
        public GameObject obj;
    }
    [System.Serializable]
    public struct ClothingEntry
    {
        public Job type;
        public List<GameObject> objs;
    }
    public AccessoryEntry[] accessories;
    public ClothingEntry[] clothings;
  
    [HideInInspector] public Dictionary<VillagerAcceType, GameObject> AcceType_Objects = new Dictionary<VillagerAcceType,GameObject>();
    [HideInInspector] public Dictionary<VillagerAcceType, Renderer> AcceType_Renderer = new Dictionary<VillagerAcceType, Renderer>();

    [HideInInspector] public Dictionary<Job, List<GameObject>> Job_ClothingObjects = new Dictionary<Job,List<GameObject>>();
    [HideInInspector] public Dictionary<Job, List<Renderer>> Job_ClothingRenderer = new Dictionary<Job , List<Renderer>>();

    [Header("インスタンスするもの")]
    public GameObject ArrowPrefab;
    public float ArrowSpeed = 5f;

    public GameObject Vanison_TakePrefab;

    void Awake()
    {
        AcceType_Objects = accessories.ToDictionary(e => e.type, e => e.obj);
        foreach (var entry in accessories)
        {
            var rend = entry.obj.GetComponent<Renderer>();
            if (rend != null)
            {
                AcceType_Renderer[entry.type] = rend;
            }
        }

        Job_ClothingObjects = clothings.ToDictionary(e => e.type, e => e.objs);
        Job_ClothingRenderer = clothings.ToDictionary(e => e.type, e => new List<Renderer>());

        foreach (var entry in clothings)
        {
            
            foreach(GameObject obj in entry.objs)
            {
                var rend = obj.GetComponent<Renderer>();
                if (rend != null)
                {
                    Job_ClothingRenderer[entry.type].Add(rend);
                }
            }
            
        }
    }

    public void AllAcceOff()
    {
        foreach(Renderer rend in AcceType_Renderer.Values)
        {
            rend.enabled = false;
        }
    }
    public void AllClothingOff()
    {
        foreach (var rends in Job_ClothingRenderer.Values)
        {
            foreach(var rend in rends)
            {
                rend.enabled = false;
            }
        }
    }

    public void AcceActive(VillagerAcceType acceType)
    {
        AcceType_Renderer[acceType].enabled = true;
    }

    public void AcceDisActive(VillagerAcceType acceType)
    {
        AcceType_Renderer[acceType].enabled = false;
    }
}
