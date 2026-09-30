using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static VillagerBase;

public class Farmer : JobBase
{
    private GameObject MyFarm;
    private List<GameObject> farmBlocks = new List<GameObject>();
    private List<GameObject> farmRemainBlocks = new List<GameObject>();

    private bool IsGoFarm = false;
    //private int FarmProgress = 0;
    private int havingcarrot = 0;

    public AudioClip SowVoice;
    public AudioClip WateringVoice;
    public AudioClip PlowVoice;
    private enum State { None,Working,InBuilding};
    private State state;

    protected override void Awake()
    {
        myJob = Job.Farmer;   // ここだけ自分で設定 
        myMaterial = MaterialType.Carrot;
        myMaterialAcce = Enum.Parse<VillagerAcceType>(myMaterial.ToString());
        base.Awake();
    }
    public override void StartMyJob()
    {
        base.StartMyJob();
        state = State.None;
        SetMyFarm();
    }

    private void SetMyFarm()
    {
        if (myJobBuilding != null) {
            farmBlocks.Clear();
            farmBlocks.AddRange(VB.farmmanager.FarmBuilding__FarmBlocks[myJobBuilding]);
        }
    }
    public override void ArriveAtTarget(GameObject FarmCabinorFarm)//着いた判定は全部ここ
    {
        if (IsGoFarm)
        {
            //畑に着いた
            StartCoroutine(FarmWorking(FarmCabinorFarm));
        }
        else
        {
            //農場建物に着いた
            ArrFarmCabin();
        }
    }
    IEnumerator FarmWorking(GameObject farm)
    {
        Vector3 dir =farm.transform.position - transform.position;

        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }
        SetActivity(VillagerBase.ActivityState.Working);

        farmRemainBlocks.Remove(farm);
        FarmBlock f = farm.GetComponent<FarmBlock>();
        //そのブロックの進捗状況に合わせてアニメを設定
        switch (f.currentStage)
        {
            case 0:
                VB.anim.Play(AnimType.Plow);
                VAC.AcceActive(VillagerAcceType.Hoe);
                //VAU.PlayJobAudio(PlowVoice);
                break;
            case 1:
                VB.anim.Play(AnimType.FarmSeed);
                VAU.PlayJobAudio(SowVoice);
                break;
            case 2:
                VAC.AcceActive(VillagerAcceType.WateringCan);
                VB.anim.Play(AnimType.FarmWater);
                VAU.PlayJobAudio(WateringVoice);
                break;
            case 3:
                VAC.AcceActive(VillagerAcceType.WateringCan);
                VB.anim.Play(AnimType.FarmWater);
                VAU.PlayJobAudio(WateringVoice);
                break;
            case 4:
                VB.anim.Play(AnimType.FarmPick);
                havingcarrot++;
                break;
        }
        yield return new WaitForSeconds(produceTime);
        VAC.AllAcceOff();
        //ブロックの状態を更新
        f.SetStage(f.currentStage);
        //次のブロックに行く
        if (farmRemainBlocks.Count > 0) {
            GameObject nearest = VB.FindNearestObj(farmRemainBlocks);
            VB.DepartToTarget(nearest,VillagerBase.GoState.GoObject);
        }
        else
        {
            IsGoFarm = false;
            VB.DepartToTarget(myJobBuilding,VillagerBase.GoState.GoJobBuilding);
        }
    }
    
    private void ArrFarmCabin()
    {
        VB.anim.Play(AnimType.Idle);

        VAC.AcceDisActive(myMaterialAcce);
        switch (state)
        {
            case State.None:
                //就職後すぐ　　→　畑の状態を確認
                state = State.Working;
                if (farmBlocks.Count == 0) {
                    StartCoroutine(NoFarmAndWaiting());
                    break;
                }
                else
                {
                    IsGoFarm = true;
                    farmRemainBlocks.Clear();
                    farmRemainBlocks.AddRange(farmBlocks);
                    GameObject nearestfarm = VB.FindNearestObj(farmBlocks);
                    FarmBlock farmBlock = nearestfarm.GetComponent<FarmBlock>();
                    VB.DepartToTarget(nearestfarm, VillagerBase.GoState.GoObject,farmBlock.WorkPoint.position);
                    break;
                }
            case State.Working:
                UpdateStorage();
                Interrupt();
                break;
            
            default:
                break;
        }

       
    }
    private void UpdateStorage()
    {
        if (havingcarrot != 0)//キャロットを持っていたらストレージに渡す処理  →仕事終わり　　
        {
            VB.WSUIcontroller.ShowMaterialPopUp(MaterialType.Carrot, myJobBuilding);
            VB.buildiingmanager.BuildingStorageUpdate(myJobBuilding, MaterialType.Carrot, havingcarrot);//キャビンのストレージに
            havingcarrot = 0;
        }
    }
    public override void ReStartMyJob()
    {
        base.ReStartMyJob();
        if (farmBlocks.Count == 0)
        {
            StartCoroutine(NoFarmAndWaiting());
            
        }
        else
        {
            SetMyFarm();
            IsGoFarm = true;
            farmRemainBlocks.Clear();
            farmRemainBlocks.AddRange(farmBlocks);
            GameObject nearestfarm = VB.FindNearestObj(farmBlocks);
            FarmBlock nextFarm = nearestfarm.GetComponent<FarmBlock>();
            VB.DepartToTarget(nearestfarm, VillagerBase.GoState.GoObject,nextFarm.WorkPoint.position);
        }
    }
    private IEnumerator NoFarmAndWaiting()
    {
        SetActivity(ActivityState.Idle);
        yield return new WaitForSeconds(0.5f);
        while (true)
        {
            if (VB.jobchangeflag)
            {
                state = State.None;
                VB.JobChangeExecute();
                yield break;
            }
            else 
            {
                if (VB.farmmanager.FarmBuilding__FarmBlocks[myJobBuilding].Count > 0)
                {
                    farmBlocks.Clear();
                    farmBlocks.AddRange(VB.farmmanager.FarmBuilding__FarmBlocks[myJobBuilding]);

                    IsGoFarm = true;

                    farmRemainBlocks.Clear();
                    farmRemainBlocks.AddRange(farmBlocks);

                    GameObject nearestfarm = VB.FindNearestObj(farmBlocks);
                    VB.DepartToTarget(nearestfarm, VillagerBase.GoState.GoObject);

                    yield break;
                }
            }
            yield return null;
        }
    }
}
