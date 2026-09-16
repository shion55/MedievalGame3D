using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UIElements;

public class Miner : JobBase
{
   
    private bool IsGoMyBuilding = true;

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
        VB.MRender.enabled = false;

        StartCoroutine(ProductionProgress());
    }
    IEnumerator ProductionProgress()
    {
        yield return new WaitForSeconds(produceTime);
        FinishProduce();
    }

    private void FinishProduce()
    {

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
