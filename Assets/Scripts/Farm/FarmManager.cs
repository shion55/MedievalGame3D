using System.Collections;
using System.Collections.Generic;
//using Unity.Android.Gradle;
using UnityEngine;

public class FarmManager : MonoBehaviour
{
    public Dictionary<GameObject,List<GameObject>> FarmBuilding__FarmBlocks = new Dictionary<GameObject, List<GameObject>>();
    private List<GameObject> FarmBuildings = new List<GameObject>();
    private List<GameObject> FarmBlocks = new List<GameObject>();
    private List<GameObject> WaitingFarms = new List<GameObject>();
    private int FarmBlock_PerBuilding = 20;
    public void FarmBlockAdd(List<GameObject> blocks)
    {
        Debug.Log(blocks.Count);
        List<GameObject> AddBlockRemains = new List<GameObject>(blocks);
        if (FarmBuildings.Count == 0) {
            WaitingFarms.AddRange(blocks);
        }
        else if(WaitingFarms.Count > 0 )//既に待ち状態の畑があったらどうしようもないので戻る
        {
            WaitingFarms.AddRange(blocks);
            return;
        }
       
        //待ち状態の畑は無い状態でforeachに入る
        foreach (GameObject building in FarmBuildings)
        {
            //既に建物が持っている畑達
            List<GameObject> buildingBlocks = FarmBuilding__FarmBlocks[building];
            
            //建物所属の畑の数に余裕があったら
            if(buildingBlocks.Count < FarmBlock_PerBuilding)
            {
                //建物所属の畑のリストに満杯になるかAddBlockRemainsが無くなるまでまで入れていく
                for (int i = buildingBlocks.Count; i < FarmBlock_PerBuilding; i++)
                {
                    GameObject Block = AddBlockRemains[0];
                    buildingBlocks.Add(Block);
                    AddBlockRemains.Remove(Block);
                    if (AddBlockRemains.Count <= 0)
                    {
                        break;
                    }
                }
            }
            FarmBuilding__FarmBlocks[building] = buildingBlocks;//置き換え
        }
       
    }
    //農場追加
    public void FarmBuildingAdd(GameObject building)
    {
        FarmBuildings.Add(building);
        FarmBuilding__FarmBlocks.Add(building,new List<GameObject>());//辞書を作る
        if(WaitingFarms.Count > 0)//畑があるなら
        {
            for(int i = 0;i < FarmBlock_PerBuilding; i++)//上限分for分でwaitinglist→FarmBuilding__FarmBlocks
            {
                if (WaitingFarms.Count == 0)//Waitingが無くなったら消す
                {
                    break;
                }
                GameObject farm = WaitingFarms[0];
                WaitingFarms.Remove(farm);
                FarmBuilding__FarmBlocks[building].Add(farm);
            }
        }
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            foreach (var pair in FarmBuilding__FarmBlocks)
            {
                Debug.Log(pair.Key);
                Debug.Log(pair.Value.Count);
            }
        }
    }
}
