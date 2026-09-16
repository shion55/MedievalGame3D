using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class UnEmployed : JobBase
{
    protected override void Awake()
    {
        myJob = Job.UnEmployer;   // Ç±Ç±ÇæÇØé©ï™Ç≈ê›íË 
        base.Awake();
    }
    public override void StartMyJob()
    {
        VB.jobchangeflag = false;
        if (VB.IsAtHome)
        {
            StartCoroutine(InHomeCheck());
        }
        else
        {
            DepartToHouse();
        }
    }
    private void DepartToHouse()
    {
        VB.DepartToTarget(VB.Myhouse, VillagerBase.GoState.GoJobBuilding);
    }
    public override void ArriveAtTarget(GameObject house)
    {
        VB.IsAtHome = true;
        VB.IsGoHome = false;
        StartCoroutine(InHomeCheck());
    }
    IEnumerator InHomeCheck()
    {
        while (true)
        {
            if (VB.jobchangeflag)
            {
                VB.JobChangeExecute();
                yield break;
            }
                yield return null;
        }
    }
}
