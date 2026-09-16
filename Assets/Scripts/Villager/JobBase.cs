using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public abstract class JobBase : MonoBehaviour, IJob
{
    protected VillagerBase VB;
    protected VillagerAcce VA;
    protected VillagerHappiness VH;
    protected Job myJob;
    protected GameObject myJobBuilding;
    protected MaterialType myMaterial;
    protected VillagerAcceType myMaterialAcce;
    protected float takeTime;
    protected float produceTime;
    protected float shortBreakTime;


    float leisureChance = 0.99f;   // 25 % で余暇へ

   

    protected virtual void Awake()
    {
        VB = GetComponent<VillagerBase>();
        VA = GetComponent<VillagerAcce>();
        VH = GetComponent<VillagerHappiness>();
        InitWaitTimes();
    }
    public virtual void StartMyJob()
    {
        if (VB.IsAtHome)
        {
            VB.IsAtHome = false;
        }
        SetMyJobBuilding();
        VB.DepartToTarget(VB.MyJobBuilding, VillagerBase.GoState.GoJobBuilding);

    }
   
    protected void InitWaitTimes()
    {
        takeTime = VB.waitTimeDataSO.GetWaitTime(myJob, WaitType.Take);
        produceTime = VB.waitTimeDataSO.GetWaitTime(myJob, WaitType.Produce);
        shortBreakTime = VB.waitTimeDataSO.GetWaitTime(myJob, WaitType.ShortBreak);
       
    }
    protected void SetMyJobBuilding()
    {
        myJobBuilding = VB.MyJobBuilding;
    }

    public virtual void Interrupt()
    {
        if (VB.jobchangeflag)
        {
            VB.JobChangeExecute();
        }
        else
        {
            if (TryStartLeisure())
            {
                StartLeisure();
            }
            else
            {
                StartCoroutine(TakeAShortBreak());
            }
        }
    }
  
    public virtual void AddBuildingStorage(int value)
    {
        VB.buildiingmanager.BuildingStorageUpdate(myJobBuilding, myMaterial, value);
        VB.WSUIcontroller.ShowMaterialPopUp(myMaterial, myJobBuilding);
    }
    //小休憩(職業建物で呼ばれる)
    public virtual IEnumerator TakeAShortBreak()
    {
        VB.MRender.enabled = false;
        Debug.Log("takeashortbreak");
        yield return new WaitForSeconds(shortBreakTime);
        VB.MRender.enabled = true;
        ReStartMyJob();
    }
    public virtual void ReStartMyJob()
    {
        Debug.Log("RestartJob");
        VH.HungerLevelUpdate(-10);
    }

    /* ====== 余暇 ====== */
    protected bool TryStartLeisure()
    {

        float value = Random.value;
        
        if (value < leisureChance)
        {
            return true;
        }
        return false;
    }
    
    public virtual void StartLeisure()
    {
        
        StopAllCoroutines();
       
        if (VB.buildiingmanager.AmuseBuildings.Count == 0)
        {
            //余暇建物無し
            DebugController.Log("NoAmuseBuildingCancel");
            CancelLeisure();
            return;
        }
        var rand = new System.Random();
        var index = rand.Next(VB.buildiingmanager.AmuseBuildings.Count-1);
        GameObject building = VB.buildiingmanager.AmuseBuildings[index];
        BuildingData data = building.GetComponent<BuildingData>();
        if (!data.IsVacant)
        {
            DebugController.Log("NoVacantCancel");
            CancelLeisure();
            return;
        }
        data.OccupantVillagers.Add(VB.gameObject);
        VB.DepartToTarget(building, VillagerBase.GoState.GoLeisure);
    }

    public virtual void CancelLeisure()
    {
        StartCoroutine(TakeAShortBreak());                    
    }

    /* ====== IJob 共通インターフェイス ====== */
    public virtual void ArriveAtTarget(GameObject building) { }
    
    
}
