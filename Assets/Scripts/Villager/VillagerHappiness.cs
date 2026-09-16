using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillagerHappiness : MonoBehaviour
{
    [HideInInspector] public static VillagerHappiness VH { get; private set; }
    VillagerBase VB;

    public float HungerLevel = 0;
    private void Awake()
    {
        VB = GetComponent<VillagerBase>();
    }
    
    public void HungerLevelUpdate(float value)
    {
        HungerLevel += value;
       if(HungerLevel > 100)
        {
            HungerLevel = 100f;
        }
        else if(HungerLevel < 0) {
         
            HungerLevel = 0f;
        }
    }
}
