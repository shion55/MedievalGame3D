using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IJob
{
    void StartMyJob(); // すべてのジョブが持つべきメソッド
    void ArriveAtTarget(GameObject building);
}

public interface IJobT
{
    void StartMyJob();
    void ArriveAtTarget(GameObject target);
    void Interrupt();          // 途中で職変更／余暇入り
    void StartLeisure();       // 余暇開始
    void EndLeisure();         // 余暇終了
}
