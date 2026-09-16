using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class VIllagerStatusManager : MonoBehaviour
{
    public BuildingManager buildingManager;
    [HideInInspector]
    public Dictionary<GameObject, Job> villagersjob = new Dictionary<GameObject, Job>();
    public Dictionary<Job,List<GameObject>> jobandvillagers = new Dictionary<Job, List<GameObject>>();
    public List<Job> availableJobs;
    [Header("食料満足度")]
    public int NeedFoodPerVillager = 5;
    private void Start()
    {
        availableJobs = new List<Job> { Job.UnEmployer,Job.WoodCutter,Job.Builder,Job.Carrier};
        foreach(Job job in Enum.GetValues(typeof(Job)))
        {
            jobandvillagers.Add(job, new List<GameObject>());
        }
    }
    public void AssignVillagerJob(GameObject villager, Job job,GameObject jobBuilding)
    {
        VillagerBase VB = villager.GetComponent<VillagerBase>();
        if (villagersjob.ContainsKey(villager))
        {
            //職業
            if(villagersjob[villager] == job)  //変更希望のjobnumが現在と同じだったら何もしない　
            {
                Debug.Log("同じ職業が選ばれました");
                return;
            }
            villagersjob[villager] = job; //上書き

            //建物
            if (VB.MyJobBuilding)//現在の建物があったら建物とデータから村人を消す
            {
               BuildingData data = VB.MyJobBuilding.GetComponent<BuildingData>();
                data.workers.Remove(villager);
            }

            if (jobBuilding != null) {//就職する建物あったら
                BuildingData data = jobBuilding.GetComponent<BuildingData>();
                data.workers.Add(villager);
            }
            VB.jobchangeflag = true;
            VB.JobChange(job,jobBuilding);
        }
        else//最初  (自動的になる)
        {
            villagersjob.Add(villager,Job.UnEmployer);//最初の設定
            VB.jobchangeflag = true;
            VB.JobChange(job,null);
            VB.JobChangeExecute();
        }
    }
    
 }


