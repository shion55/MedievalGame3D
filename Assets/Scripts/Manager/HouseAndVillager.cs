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
            //–³E‚Éİ’è
            statusManager.AssignVillagerJob(villager, Job.UnEmployer,null);//‚±‚ê‚ªæ
            jobchangeuicont.GenerateHireHousePopUI(houses[houseindex].transform.position, houses[houseindex]);//‰Æ‚Ìã‚Ìjobchangepop
            houseindex++;
            
        }
    }
   public void HouseGenerated(GameObject house)//‘ºl‚Æ‰Æ‚ğ¶¬
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
        jobchangeuicont.GenerateHireHousePopUI(house.transform.position, house);//‰Æ‚Ìã‚Ìjobchangepop
    }
    public void RemoveHouse(GameObject house)
    {
        if (!housevillagers.TryGetValue(
                house,
                out List<GameObject> residents))
        {
            return;
        }

        // foreach’†‚ÉƒŠƒXƒg‚ğ‘‚«Š·‚¦‚é‚Ì‚ÅƒRƒs[
        List<GameObject> residentsCopy =
            new List<GameObject>(residents);


        foreach (GameObject villager in residentsCopy)
        {
            if (villager == null)
                continue;

            VillagerBase vb =
                villager.GetComponent<VillagerBase>();


            // Eê‚ÉŠ‘®‚µ‚Ä‚¢‚é‚È‚ç
            // ‚»‚ÌŒš•¨‚Ìworkers‚©‚çŠO‚·
            if (vb != null &&
                vb.MyJobBuilding != null)
            {
                BuildingData buildingData =
                    vb.MyJobBuilding
                        .GetComponentInChildren<BuildingData>();

                if (buildingData != null)
                {
                    buildingData.workers.Remove(
                        villager
                    );
                }
            }


            // E‹ÆŠÇ—‚©‚çíœ
            if (statusManager.villagersjob.TryGetValue(
                    villager,
                    out Job job))
            {
                statusManager.villagersjob.Remove(
                    villager
                );

                if (statusManager
                    .jobandvillagers
                    .TryGetValue(
                        job,
                        out List<GameObject> jobVillagers))
                {
                    jobVillagers.Remove(
                        villager
                    );
                }
            }


            // ‰Æ‚Æ‚Ì‘Î‰ŠÖŒW‚ğíœ
            villagerhouse.Remove(
                villager
            );

            villagers.Remove(
                villager
            );


            Destroy(villager);
        }

        //ã‚ÌŒÙ—ppopup‚ğÁ‚·
        jobchangeuicont.RemoveHireHousePopUI(house);

        // ‰Æ‘¤‚Ì“o˜^íœ
        housevillagers.Remove(
            house
        );

        houses.Remove(
            house
        );


        Destroy(house);
    }
}
