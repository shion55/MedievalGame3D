using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class SawmillWorker : JobBase
{
    private bool IsGoMyBuilding = true;
    private int Progressint = 0;

    private MaterialType MyNeedMaterial = MaterialType.Wood;

    protected override void Awake()
    {
        myJob = Job.SawmillWorker;   // Ç±Ç±ÇæÇØé©ï™Ç≈ê›íË 
        myMaterial = MaterialType.Lumber;
        myMaterialAcce = Enum.Parse<VillagerAcceType>(myMaterial.ToString());
        base.Awake();
    }
    public override void ArriveAtTarget(GameObject Building)//íÖÇ¢ÇΩîªíËÇÕëSïîÇ±Ç±
    {
        if (IsGoMyBuilding)
        {
            //êªî¬èäÇ…íÖÇ¢ÇΩ
            VA.AcceDisActive(VillagerAcceType.Wood);
            IsGoMyBuilding = false;
            
            BuildingData data = Building.GetComponent<BuildingData>();
            if (data.storage.materials[MaterialType.Wood] > 0)
            {
               StartProduce();
            }
            else
            {
                GameObject toBuilding = VB.FindWithMostBuilding(MyNeedMaterial);
                if(toBuilding != null)
                {
                    StartCoroutine(GoWithMaterialBuilding(toBuilding));
                }
                else
                {
                    VB.NoMaterialAndWait(myMaterial, data.buildingType.ToString());
                    StartCoroutine(NoMaterialWaiting());
                }
            }
        }
        else
        {
            //ñÿÇ™Ç†ÇÈåöï®Ç…íÖÇ¢ÇΩ
            StartCoroutine(TakeMaterial());
        }
    }
    IEnumerator GoWithMaterialBuilding(GameObject toBuilding)
    {
        VB.buildiingmanager.BuildingStorageUpdate(toBuilding,MaterialType.Wood,-1);//ÇªÇÃåöï®ÇÃñÿÇè¡ÇµÇƒ
        yield return new WaitForSeconds(shortBreakTime);
        VB.DepartToTarget(toBuilding, VillagerBase.GoState.GoJobBuilding);
    }
    IEnumerator TakeMaterial()
    {
        yield return new WaitForSeconds(takeTime);
        VB.buildiingmanager.BuildingStorageUpdate(myJobBuilding,MaterialType.Wood,1);
        IsGoMyBuilding = true;
        VA.AcceActive(VillagerAcceType.Wood);
        VB.DepartToTarget(myJobBuilding, VillagerBase.GoState.GoCarry);      
    }
    private void StartProduce()
    {
        VB.MRender.enabled = false;
        VB.buildiingmanager.BuildingStorageUpdate(myJobBuilding, MaterialType.Wood, -1);
        StartCoroutine(ProductionProgress());
    }
    //êiíªUIÇëÄçÏ
    IEnumerator ProductionProgress()
    {
        yield return new WaitForSeconds(produceTime/6);
        if(Progressint < 6)
        {
            VB.CallChangeProgressUI(myJobBuilding, Progressint);
            Progressint++;
            StartCoroutine(ProductionProgress());
        }
        else
        {
            VB.CallChangeProgressUI(myJobBuilding, Progressint);//è¡Ç∑
            Progressint = 0;
            FinishProduce();
        }
    }
    private void FinishProduce()
    {
      
        VB.buildiingmanager.BuildingStorageUpdate(myJobBuilding,myMaterial,1);
        VB.WSUIcontroller.ShowMaterialPopUp(myMaterial,myJobBuilding);

        Interrupt();
    }
    public override void ReStartMyJob()
    {
        BuildingData data = myJobBuilding.GetComponent<BuildingData>();
        if (data.storage.materials[MaterialType.Wood] > 0)
        {
            StartProduce();
        }
        else
        {
            GameObject toBuilding = VB.FindWithMostBuilding(MyNeedMaterial);
            if (toBuilding != null)
            {
                StartCoroutine(GoWithMaterialBuilding(toBuilding));
            }
            else
            {
                VB.NoMaterialAndWait(myMaterial, data.buildingType.ToString());
                StartCoroutine(NoMaterialWaiting());
            }
        }
    }
    private IEnumerator NoMaterialWaiting()
    {
        yield return new WaitForSeconds(0.5f);
        BuildingData data = myJobBuilding.GetComponent<BuildingData>();
        while (true)
        {
            if (VB.jobchangeflag)
            {
                VB.warningpartcle.Stop();
                VB.EnoughMaterialorQuitJob(myMaterial, data.buildingType.ToString());
                VB.JobChangeExecute();
                yield break;
            }
            else
            {
                GameObject toBuilding = VB.FindWithMostBuilding(MyNeedMaterial);
                if(toBuilding != null)
                {
                    VB.warningpartcle.Stop();
                    VB.EnoughMaterialorQuitJob(myMaterial, data.buildingType.ToString());

                    VB.buildiingmanager.BuildingStorageUpdate(toBuilding,MaterialType.Wood,-1);//ÇªÇÃåöï®ÇÃñÿÇè¡ÇµÇƒ
                    VB.DepartToTarget(toBuilding, VillagerBase.GoState.GoJobBuilding); 
                    yield break;
                }
            }
            yield return null;
        }
    }

    
}
