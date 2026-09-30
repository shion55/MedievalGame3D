using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class WoodCutter : JobBase
{


    
    private GameObject nearestTree;

    private bool IsGoTree = false;

    private int havingwoods = 0;

    public AudioClip chopVoice;

    public Renderer MyJobBodyRenderes;

   
    protected override void Awake()
    {
        myJob = Job.WoodCutter;   // ここだけ自分で設定 
        myMaterial = MaterialType.Wood;
        myMaterialAcce = Enum.Parse<VillagerAcceType>(myMaterial.ToString());
        base.Awake();
    }

    private void DepartToJobObject()
    {
        nearestTree = VB.FindNearestObj(VB.treemanager.currentrees);
        VB.treemanager.currentrees.Remove(nearestTree);
        VB.DepartToTarget(nearestTree, VillagerBase.GoState.GoObject);
        IsGoTree = true;
    }
    public override void ArriveAtTarget(GameObject cabinortree)
    {
        if (IsGoTree)
        {
            //木に着いた
            IsGoTree = false;
            StartCoroutine(FinishCut(cabinortree));
        }
        else
        {
            //Debug.Log("木こり小屋到着");
            //木こり小屋に着いた
            ArrCabin();
        }
    }
   
    IEnumerator FinishCut(GameObject tree)
    {
        SetActivity(VillagerBase.ActivityState.Working);

        //回転をnavmeshから奪う　
        VB.agent.updateRotation = false;

        // 1) 平面上の方向ベクトルを計算
        Vector3 dir = (tree.transform.position - transform.position);
        dir.y = 0f;  // 垂直回転はさせない

        // 2) Euler 角で直接向きをセット
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, angle, 0f);
        ///アニメ＆斧＆音声on
        VB.anim.Play(AnimType.Chop);
        VAC.AcceActive(VillagerAcceType.Axe);
       

        yield return new WaitForSeconds(produceTime);

     

        VB.treemanager.delitetree(nearestTree);//木を消す
        VB.agent.updateRotation = true;
        
        havingwoods++;
        VB.DepartToTarget(myJobBuilding, VillagerBase.GoState.GoCarry);

        VAC.AcceDisActive(VillagerAcceType.Axe);
        VAC.AcceActive(myMaterialAcce);
        VB.anim.Play(AnimType.Carry);
    }
    private void ArrCabin()
    {
        VAC.AcceDisActive(myMaterialAcce);
        if (havingwoods != 0)
        {
            AddBuildingStorage(1);
            havingwoods = 0;
            Interrupt();
        }
        else
        {
            ReStartMyJob();
        }
    }
    public override void ReStartMyJob()
    {
        base.ReStartMyJob();
        DepartToJobObject();
    }



    public void PlayChopVoice()
    {
        VAU.PlayJobAudio(chopVoice);
    }
}
