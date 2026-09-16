using NUnit.Framework.Internal.Execution;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;
public class Builder : JobBase
{
    private GameObject _site;
    private enum State { None, GoingMat, GoingSite, Building }
    private State _state;

    protected override void Awake()
    {
        myJob = Job.Builder;   // ここだけ自分で設定 このスクリプトはbuilderですよってこと
        base.Awake();
    }
    /* ---------- 仕事開始 ---------- */
    public override void StartMyJob()
    {
        if (!TrySetSite()) { GoHome(); return; }
        VB.IsAtHome = false;
        _state = State.GoingMat;
        Debug.Log("GoforMaterials");
        GoForMaterials();
    }

    bool TrySetSite()
    {
        if (VB.constManager.waitingSite.Count > 0) {
            _site = VB.FindNearestObj(VB.constManager.waitingSite);
            VB.constManager.waitingSite.Remove(_site);
           return true;
        }
        else
        {
            return false;
        }
    }
    void GoHome()
    {
        // もう家にいるならそのまま待機コルーチンへ
        if (VB.IsAtHome)
        {
            StartCoroutine(InHomeCheck());
        }
        else
        {
            // 家まで歩かせて、到着したら ArriveAtTarget → VB.IsAtHome = true にして InHomeCheck() へ
            VB.DepartToTarget(VB.Myhouse,VillagerBase.GoState.GoJobBuilding);
        }
    }
    /// <summary>
    /// 建築現場に必要な資材を確認し、取得ルート or 現場直行 or 待機 を決める。
    /// 呼び出し元: StartMyJob(), TakeAShortBreak() など
    /// </summary>
    void GoForMaterials()
    {
        // ① 現場に必要な資材を 1 個だけ“先取り”してくれるメソッド
        var (materialBuilding, material) = CheckAndSearchMaterial();

        if (materialBuilding != null)
        {
            // → 資材が入った建物が見つかった
            VB.warningpartcle.Stop();
            _state = State.GoingMat;
            VB.DepartToTarget(materialBuilding, VillagerBase.GoState.GoJobBuilding);
        }
        else if (material != null)
        {
            // → 必要資材は判明したけど在庫ゼロ
            VB.constManager.NotEnoughMaterial(material.Value, gameObject, _site);
            VB.NoMaterialAndWait(material.Value, "Site");
            StartCoroutine(NoMaterialWaiting(material.Value));   // 元コードの待機ループ
        }
       
    }
    (GameObject, MaterialType?) CheckAndSearchMaterial()
    {
        var SiteMaterial = _site.GetComponent<ConstructionSite>().requiredMaterials;//建設地に必要なマテリアルを取得
        GameObject MaterialHavingBuilding = null;
        foreach (var material in SiteMaterial.Keys)//マテリアル毎に見ていく
        {
            if (SiteMaterial[material] > 0)//必要な資材に行き当たったら
            {
                MaterialHavingBuilding = VB.FindWithMostBuilding(material);
                if (MaterialHavingBuilding != null)
                {
                    VB.buildiingmanager.BuildingStorageUpdate(MaterialHavingBuilding,material,-1);
                    SiteMaterial[material] -= 1;
                    return (MaterialHavingBuilding, material);
                }
                else
                {
                    //資源が足りない
                    return (null, material);
                }
            }
        }
        //資源はもういらない
        return (null, null);
    }
    IEnumerator NoMaterialWaiting(MaterialType notenoughmaterial)
    {
        yield return new WaitForSeconds(0.5f);
        while (true)
        {
            if (VB.jobchangeflag)
            {
                VB.warningpartcle.Stop();
                VB.constManager.waitingSite.Add(_site);//完成できていないので戻す
                VB.EnoughMaterialorQuitJob(notenoughmaterial, "Site");
                VB.JobChangeExecute();
                yield break;
            }
            else
            {
                var (MaterialHavingBuilding, material) = CheckAndSearchMaterial();
                if (MaterialHavingBuilding != null)
                {
                    VB.warningpartcle.Stop();
                    VB.constManager.MaterialAdded(material.Value, this.gameObject, _site);
                    VB.DepartToTarget(MaterialHavingBuilding, VillagerBase.GoState.GoJobBuilding);
                    yield break;
                }
            }
            yield return null;
        }
    }


    /* ---------- 到着 ---------- */
    public override void ArriveAtTarget(GameObject target)
    {
        switch (_state)
        {
            case State.GoingMat:
                StartCoroutine(TakeMaterial());
                break;

            case State.GoingSite:
                BeginConstruction();
                break;

            default:
                VB.IsAtHome = true;
                StartCoroutine(InHomeCheck());  //家での待機コールチン
                break;
        }
    }
    IEnumerator TakeMaterial()
    {
        yield return new WaitForSeconds(takeTime);
        VB.MyRenderOff();
        VA.AcceActive(VillagerAcceType.Wood);
        _state = State.GoingSite;
        VB.DepartToTarget(_site, VillagerBase.GoState.GoCarry);//すぐもどる
    }
    void BeginConstruction()
    {
        // 手押し車モデルを消して作業アニメへ
        VA.AcceDisActive(VillagerAcceType.Wood);
        _state = State.Building;               //   ← enum State に Building を追加
        VB.anim.Play(AnimType.Chop);

        // ConstManager に「この現場をこのビルダーが担当」と教える
        VB.constManager.StartConstuction(_site,this.gameObject);
    }
    IEnumerator InHomeCheck()
    {
        while (true)
        {
            // 1) 職変更キューが立ったら即座に切り替え
            if (VB.jobchangeflag)
            {
                VB.jobchangeflag = false;
                VB.JobChangeExecute(); // VillagerBase 側で職変更
                yield break;
            }
            // 2) 新しい建設依頼が来たら仕事を再開
            else if (VB.constManager.waitingSite.Count > 0)
            {
                StartMyJob();
                yield break;
            }
            yield return null;    // 毎フレーム様子を見る
        }
    }

    public override void Interrupt()//constructionmanagerから呼ばれる
    {

        if (VB.jobchangeflag)
        {
            //完成しているかチェック
            if (CheckConstDone())
            {
                VB.constManager.ConstCompleted(this.gameObject,_site);
            }
            else
            {
                VB.constManager.waitingSite.Add(_site);//待機中建物に戻す
            }
            VB.JobChangeExecute();
        }
        else
        {
            if (CheckConstDone())//仕事変更ではなく完成したら
            {
                VB.constManager.ConstCompleted(this.gameObject,_site);
                DebugController.LogVil("建築完成", VB.MyIndex);
                if (TryStartLeisure())
                {
                    StartLeisure();
                } 
                else {
                    StartCoroutine(TakeAShortBreak());                         // 次の現場へ
                }
                    
            }
            else
            {
                GoForMaterials(); ;//完成していない場合
            }
        }
    }
   

    public override void ReStartMyJob()
    {
        base.ReStartMyJob();
        StartMyJob();
    }
    bool CheckConstDone()
    {
        var SiteMaterial = _site.GetComponent<ConstructionSite>().requiredMaterials;
        foreach (var material in SiteMaterial.Keys)//木や石毎に
        {
            if (SiteMaterial[material] > 0)
            {
                return false;
            }
        }
        return true;
    }

}

