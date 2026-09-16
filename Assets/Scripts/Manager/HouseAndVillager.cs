using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class HouseAndVillager : MonoBehaviour
{
    public VIllagerStatusManager statusManager;
    public BuildingHireVillagerUICont jobchangeuicont;
    public GameObject villagerprefab;
    public GameObject villagerParent;
    public List<GameObject> villagers = new List<GameObject>();
    public List<GameObject> houses = new List<GameObject>();
    public Dictionary<GameObject, GameObject> villagerhouse = new Dictionary<GameObject, GameObject>();
    [HideInInspector]
    public Dictionary<GameObject, List<GameObject>> housevillagers = new Dictionary<GameObject, List<GameObject>>();
    void Start()
    {
        int houseindex = 0;
        foreach (GameObject villager in villagers)
        {
            villagerhouse.Add(villager,houses[houseindex]);
            housevillagers.Add(houses[houseindex],new List<GameObject>());
            housevillagers[houses[houseindex]].Add(villager);
            villager.GetComponent<VillagerBase>().Myhouse = houses[houseindex];
            //ñ≥êEÇ…ê›íË
            statusManager.AssignVillagerJob(villager, Job.UnEmployer,null);//Ç±ÇÍÇ™êÊ
            jobchangeuicont.GenerateHireHousePopUI(houses[houseindex].transform.position, houses[houseindex]);//â∆ÇÃè„ÇÃjobchangepop
            houseindex++;
            
        }
    }
   public void HouseGenerated(GameObject house)//ë∫êlÇ∆â∆Çê∂ê¨
    {
        GameObject villager = Instantiate(villagerprefab, house.transform.position, Quaternion.identity,villagerParent.transform);
        villager.GetComponent<VillagerBase>().Myhouse = house;
        villagerhouse.Add(villager, house);
        housevillagers.Add(house,new List<GameObject>());
        housevillagers[house].Add(villager);
        villagers.Add(villager);
        houses.Add(house);
        statusManager.AssignVillagerJob(villager, Job.UnEmployer,null);
        villager.GetComponent<VillagerBase>().MyIndex = villagers.IndexOf(villager) ;
        jobchangeuicont.GenerateHireHousePopUI(house.transform.position, house);//â∆ÇÃè„ÇÃjobchangepop
    }
}
