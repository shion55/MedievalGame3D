using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FarmBlock : MonoBehaviour
{
    public GameObject[] vegetableStages;

    public int currentStage = 0;

    public void SetStage(int stage)
    {
        // 全部非表示にしてから
        for (int i = 0; i < vegetableStages.Length; i++)
            vegetableStages[i].SetActive(false);

        // 指定されたステージだけ表示 →　０は耕した後なので何もしない
        if(stage == 0)   //0
        {
            currentStage++;
        }
        else if (stage >= 1 && stage < vegetableStages.Length + 1) //1→0 2→1 3→2 
        {
            stage -= 1;
            vegetableStages[stage].SetActive(true);
            currentStage++;
        }
        else //4→収穫
        {
            currentStage = 0;
            ResetStage();
        }
    }
    public void ResetStage()
    {
        // 全部非表示に
        for (int i = 0; i < vegetableStages.Length; i++)
            vegetableStages[i].SetActive(false);
    }
}
