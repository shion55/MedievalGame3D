using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Hunter : JobBase
{
    public GameObject hunterpositionprefab;

    private GameObject hunterposition;
   
    private bool IsGoMyBuilding = true;
    private bool FinishHunt = false;

    private GameObject mydeer;
    

    private List<GameObject> DeersList = new List<GameObject>();
    protected override void Awake()
    {
        myJob = Job.Hunter;   // ここだけ自分で設定 
        myMaterial = MaterialType.Venison;
        myMaterialAcce = Enum.Parse<VillagerAcceType>(myMaterial.ToString());
        base.Awake();
    }
    public override void StartMyJob()
    {
        base.StartMyJob();
        hunterposition = Instantiate(hunterpositionprefab, Vector3.zero, Quaternion.identity);
    }
    public override void ArriveAtTarget(GameObject building)
    {
        if (IsGoMyBuilding && !FinishHunt) {
            VB.MRender.enabled = false;
            VA.AcceDisActive(myMaterialAcce);
            StartProduce();
            IsGoMyBuilding = false;
        }
        else if(!IsGoMyBuilding && !FinishHunt)
        {
            StartCoroutine(ProductionProgress());
        }
        else if(IsGoMyBuilding && FinishHunt)
        {
            VA.AcceDisActive(myMaterialAcce);
            FinishProduce();
        }
    }
    private void StartProduce()
    {
        VB.MRender.enabled = true;
        DeersList.Clear();
        DeersList = VB.mobManager.CountDeers();
        if (DeersList.Count == 0) { //鹿がいなかったらMobManager付のDeerSpawnerに再生成してもらう

            VB.mobManager.deerSpawner.DeerSpawn();
            DeersList.Clear();
            DeersList = VB.mobManager.CountDeers();
        }
        mydeer = VB.FindNearestObj(DeersList);
        Vector3 dir = (this.transform.position - mydeer.transform.position).normalized;
        Vector3 offsetPos = mydeer.transform.position + dir * 5f;

        hunterposition.transform.position = offsetPos;

        VB.DepartToTarget(hunterposition, VillagerBase.GoState.GoObject);
    }
    //鹿前に到着
    IEnumerator ProductionProgress()
    {
        //回転をnavmeshから奪う　→　鹿の方に弓を向ける
        VB.agent.updateRotation = false;

        // 1) 平面上の方向ベクトルを計算
        Vector3 dir = (mydeer.transform.position - transform.position);
        dir.y = 0f;  // 垂直回転はさせない

        // 2) Euler 角で直接向きをセット
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        angle += 90f;
        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        GameObject bow = VA.AcceType_Objects[VillagerAcceType.Bow];
        VA.AcceActive(VillagerAcceType.Bow);
        bow.GetComponentInChildren<Animator>().SetTrigger("Shoot");
        VB.anim.Play(AnimType.Shoot);

        yield return new WaitForSeconds(2f);

        GameObject arrow = Instantiate(VA.ArrowPrefab,bow.transform.position,VA.ArrowPrefab.transform.rotation);
        yield return StartCoroutine(FlyArrowTo(arrow.transform, mydeer.transform.position, VA.ArrowSpeed, 0.5f));
        VA.AcceDisActive(VillagerAcceType.Bow); ;

        Vector3 vanisonPos = mydeer.transform.position;
        vanisonPos.y -= 1.5f;
        Destroy(mydeer.gameObject);
        GameObject vanison = Instantiate(VA.Vanison_TakePrefab,vanisonPos,VA.Vanison_TakePrefab.transform.rotation);

        VB.agent.updateRotation = true;
        //鹿肉まで移動
        // 1) 目的地セット
        VB.agent.SetDestination(vanisonPos);
        VB.anim.Play(AnimType.Walk);
        VB.agent.isStopped = false;
        // 2) 到着判定まで待つ
        //    pathPending が false かつ remainingDistance が閾値以下、
        //    かつほぼ停止状態になるまで
        yield return new WaitUntil(() =>
            !VB.agent.pathPending &&
             VB.agent.remainingDistance <= 0.5f &&  // お好みの閾値
             VB.agent.velocity.sqrMagnitude < 0.01f);

        VB.anim.Play(AnimType.Pick);
        yield return new WaitForSeconds(1f);
        Destroy(vanison.gameObject);
        FinishHunt = true;
        IsGoMyBuilding = true;
        VA.AcceDisActive(myMaterialAcce ); ;
        VB.DepartToTarget(myJobBuilding, VillagerBase.GoState.GoCarry);
    }
    private IEnumerator FlyArrowTo(Transform arrow, Vector3 targetPos, float speed, float arriveThreshold)
    {
        while (true)
        {
            // １フレームで移動
            arrow.position = Vector3.MoveTowards(arrow.position, targetPos, speed * Time.deltaTime);
            // 向きも補正する
            Vector3 look = targetPos - arrow.position;
            if (look.sqrMagnitude > 0.001f)
                arrow.rotation = Quaternion.LookRotation(look);

            // 閾値内に入ったら終了
            if (Vector3.Distance(arrow.position, targetPos) <= arriveThreshold)
                break;

            yield return null;
        }

        // 矢を消す
        Destroy(arrow.gameObject);
    }
    private void FinishProduce()
    {

        VB.buildiingmanager.BuildingStorageUpdate(myJobBuilding,myMaterial,1);
        //ポップアップ
        VB.WSUIcontroller.ShowMaterialPopUp(myMaterial, myJobBuilding);

        Destroy(hunterposition.gameObject);

        Interrupt();
    }
    public override void ReStartMyJob()
    {
        base.ReStartMyJob();
        hunterposition = Instantiate(hunterpositionprefab, Vector3.zero, Quaternion.identity);
        FinishHunt = false;
        StartProduce();
    }
}
