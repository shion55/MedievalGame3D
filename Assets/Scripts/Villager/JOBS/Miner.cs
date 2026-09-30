using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UIElements;

public class Miner : JobBase
{
   
    private bool IsGoMyBuilding = true;
    public AudioClip MineVoice;
    protected override void Awake()
    {
        myJob = Job.Miner;   // ここだけ自分で設定 
        myMaterial = MaterialType.Stone;
        base.Awake();
    }
    
    public override void ArriveAtTarget(GameObject building)
    {
        StartProduce();
    }
    private void StartProduce()
    {
        SetActivity(VillagerBase.ActivityState.Working);

        VB.MRender.enabled = false;

        VAU.StartJobAudioLoop(MineVoice);
        StartCoroutine(ProductionProgress());
    }
    IEnumerator ProductionProgress()
    {
        yield return new WaitForSeconds(produceTime);
        FinishProduce();
    }

    private void FinishProduce()
    {
        VAU.StopJobAudio();

        VB.buildiingmanager.BuildingStorageUpdate(myJobBuilding,myMaterial,1);
        //ポップアップ
        VB.WSUIcontroller.ShowMaterialPopUp(myMaterial, myJobBuilding);
        Interrupt();
    }
    public override void ReStartMyJob()
    {
        base.ReStartMyJob();
        StartProduce();
    }
}
