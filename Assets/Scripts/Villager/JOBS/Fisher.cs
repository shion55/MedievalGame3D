using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
public class Fisher : JobBase
{
    
    private enum State { None, Gobuilding, Building }
    private State _state;

    protected override void Awake()
    {
        myJob = Job.Fisher;   // ここだけ自分で設定 このスクリプトはfisherですよってこと
        myMaterial = MaterialType.Fish;
        myMaterialAcce = Enum.Parse<VillagerAcceType>(myMaterial.ToString());
        _state = State.Gobuilding;
        base.Awake();
    }
    public override void ArriveAtTarget(GameObject building)
    {
        
        switch (_state) {
            case State.Gobuilding:
                VB.agent.enabled = false;
                Transform fishingpoint = myJobBuilding.transform.Find("FishingPoint");
                this.gameObject.transform.position = fishingpoint.position;//釣り小屋の甲板に移動
                this.gameObject.transform.Rotate(0, -90, 0);
                StartCoroutine(FishingRoutine());
                break;      
        }
    }
    IEnumerator FishingRoutine() {
        VB.MRender.enabled = true;                                                //レンダーを付ける
        VA.AcceActive(VillagerAcceType.Fisher_Rod);
        VB.anim.Play(AnimType.Fish);
        yield return new WaitForSeconds(produceTime);
        VB.anim.Play(AnimType.FishPull);
        yield return new WaitForSeconds(1.5f);
        FishingDone();
    }

    private void FishingDone()
    {
        VB.WSUIcontroller.ShowMaterialPopUp(MaterialType.Fish, myJobBuilding);　　　　　　　　　//魚表示
        VB.buildiingmanager.BuildingStorageUpdate(myJobBuilding,myMaterial,1);//魚追加
        VA.AcceDisActive(VillagerAcceType.Fisher_Rod);     //釣り竿を消す
        if (VB.jobchangeflag)
        {
            VB.MRender.enabled = false;
            Transform entrance = myJobBuilding.transform.Find("Entrance");
            this.gameObject.transform.position = entrance.position;
            VB.agent.enabled = true;
            VB.JobChangeExecute();
        }
        else
        {
            StartCoroutine(FishingRoutine());
        }
    }
    public override void Interrupt()
    {
        if (VB.jobchangeflag)
        {
            VB.MRender.enabled = false;
            Transform entrance = myJobBuilding.transform.Find("Entrance");
            this.gameObject.transform.position = entrance.position;
            VB.agent.enabled = true;
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
    public override void ReStartMyJob()
    {
        base.ReStartMyJob();
        VB.agent.enabled = false;
        Transform fishingpoint = myJobBuilding.transform.Find("FishingPoint");
        this.gameObject.transform.position = fishingpoint.position;//釣り小屋の甲板に移動
        this.gameObject.transform.Rotate(0, -90, 0);
        StartCoroutine(FishingRoutine());
    }
}
