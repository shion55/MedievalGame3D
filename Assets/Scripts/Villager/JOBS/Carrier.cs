using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class Carrier : JobBase
{
    private GameObject _takeBuilding;
    private GameObject _giveBuilding;

    private MaterialType Carrying_Material;
    private VillagerAcceType Carrying_Acce;
    private enum State { None, Go_Building_Take, Go_Building_Give, Castle }
    private State _state;

    (MaterialType type,int value) _havingMaterials;
    protected override void Awake()
    {
        myJob = Job.Carrier;   // ここだけ自分で設定 このスクリプトはbuilderですよってこと
        base.Awake();
    }
    public override void StartMyJob()
    {
        Debug.Log("StartMyJob");
        SetMyJobBuilding();//城を設定
        SetTargetBuilding();
    }
    public override void ArriveAtTarget(GameObject houseorMatBuildingorCastle)
    {
        switch (_state)
        {
            case State.Go_Building_Take:
                StartCoroutine(CarryToBuilding());
                break;
            case State.Go_Building_Give:
                StartCoroutine(GiveToBuilding());
                break;
            case State.Castle:
                StartCoroutine(InCastleCheck());
                break;
            default:  break;
        } 
      
    }
    private void SetTargetBuilding()
    {
        //Amuse系建物の食材等が足りているかどうか
        Dictionary<GameObject,List<MaterialType>> Empty_Buildings_Materials  = CheckEmptyAmuseBuildings();
        if(Empty_Buildings_Materials != null)//足りていない建物がある
        {
            DebugController.Log("在庫なし娯楽施設あり");
            List<GameObject> Empty_Buildings = Empty_Buildings_Materials.Keys.ToList();
            for(int i = 0;i < Empty_Buildings.Count; i++)
            {
                GameObject nearestbuilding = VB.FindNearestObj(Empty_Buildings);//足りない建物から最も近い建物を選ぶ
                foreach(MaterialType t in Empty_Buildings_Materials[nearestbuilding])//建物の足りないマテリアル達が環境に存在するかどうか
                {
                    
                    GameObject haveBuilding = VB.FindWithMostBuilding(t);
                    if (haveBuilding != null) { //存在したら行く建物決定
               
                        　_takeBuilding = haveBuilding;
                           _giveBuilding = nearestbuilding;
                        TakeMaterial(_takeBuilding, t, 1);  //マテリアルを取得しておく
                        Carrying_Material = t;
                        if (_takeBuilding == myJobBuilding && _state == State.Castle)//行先が城で自分が城に居たら
                        {

                            StartCoroutine(CarryToBuilding());
                        }
                        else
                        {
                                 //取りに行く
                            DebugController.Log("市場用の資材を取りに行く");
                            _state = State.Go_Building_Take;
                            VB.DepartToTarget(_takeBuilding, VillagerBase.GoState.GoJobBuilding);
                        }
                        return;
                    }
                }
                Empty_Buildings.Remove(nearestbuilding);//建物が欲しいマテリアルが無いならリストから消して次に近い建物を試す繰り返し
            }  
        }
        //職業建物からマテリアルを持ってくる動き
        var pair = FindMostStorageBuilding();
        GameObject BuildingToCarry = pair.building;
        MaterialType? type = pair.matType;
        if (BuildingToCarry != null)
        {
            _state = State.Go_Building_Take;
            _takeBuilding = BuildingToCarry;
            _giveBuilding = myJobBuilding;
            TakeMaterial(_takeBuilding, type.Value, 1);
            Carrying_Material = type.Value;
            VB.DepartToTarget(BuildingToCarry, VillagerBase.GoState.GoJobBuilding);
        }
        else
        {
            
            if (_state == State.Castle)
            {
                StartCoroutine(InCastleCheck());
            }
            else
            {
               _state = State.Castle;
                VB.DepartToTarget(myJobBuilding, VillagerBase.GoState.GoJobBuilding);
            }
        }
    }

    public override void ReStartMyJob()
    {
        SetTargetBuilding();
    }
    //内部的に取るメソッド
    private void TakeMaterial(GameObject building,MaterialType type,int value)
    {
        VB.buildiingmanager.BuildingStorageUpdate(building,type,-value);//取るからマイナス
        _havingMaterials.type = type;
        _havingMaterials.value = value;
    }
  
    IEnumerator CarryToBuilding()
    {
        yield return new WaitForSeconds(takeTime);
        _state = State.Go_Building_Give;
        Carrying_Acce = Enum.Parse<VillagerAcceType>(Carrying_Material.ToString());
        VA.AcceType_Renderer[Carrying_Acce].enabled = true;
        VB.DepartToTarget(_giveBuilding, VillagerBase.GoState.GoCarry);
    }
    IEnumerator GiveToBuilding()
    {
        MaterialType type = _havingMaterials.type;
        int value = _havingMaterials.value;
        VB.buildiingmanager.BuildingStorageUpdate(_giveBuilding, type, value);
        VA.AcceType_Renderer[Carrying_Acce].enabled = false;
        yield return new WaitForSeconds(produceTime);
        Interrupt();
    }
    IEnumerator InCastleCheck()
    {
        while (true)
        {

            if (VB.jobchangeflag)
            {
                VB.JobChangeExecute();
                yield break;
            }
            else
            {
                var pair = FindMostStorageBuilding();
                GameObject BuildingToCarry = pair.building;
                MaterialType? type = pair.matType;
                if (BuildingToCarry != null)
                {
                    _state = State.Go_Building_Take;
                    _takeBuilding = BuildingToCarry;
                    _giveBuilding = myJobBuilding;
                    TakeMaterial(_takeBuilding, type.Value, 1);
                    VB.DepartToTarget(BuildingToCarry, VillagerBase.GoState.GoJobBuilding);
                    yield break;
                }
            }
            yield return null;
        }

    }
    Dictionary<GameObject, List<MaterialType>> CheckEmptyAmuseBuildings()
    {
        Dictionary<GameObject,List<MaterialType> >  Empty_Buildings_Materials = new Dictionary<GameObject,List<MaterialType>>();
        foreach (GameObject building in VB.buildiingmanager.AmuseBuildings)
        {
            BuildingData data = building.GetComponent<BuildingData>();
            Storage storage = data.storage;
            List<MaterialType> ts = new List<MaterialType>();
            foreach (var mtype in data.amuseneedmaterials)//必要なマテリアルを建物のストレージと参照
            {
                if (storage.materials[mtype] == 0)
                {
                    Debug.Log(mtype.ToString());
                    ts.Add(mtype);       
                }
            }
            if (ts.Count > 0) { //足りないマテリアルがある建物を辞書に追加
                Empty_Buildings_Materials.Add(building,ts);
            }
        }
        //建物と足りないマテリアルの辞書が出来上がっているはず
        if (Empty_Buildings_Materials.Count > 0) { 
         
            return Empty_Buildings_Materials;  
        }
        else
        {
            return null; //全てのAmuseBuildingに在庫があった場合
        }
    }

    (GameObject building,MaterialType? matType) FindMostStorageBuilding()
    {
        GameObject fullbuilding = null;
        MaterialType? fullmaterial = null;
        int maxamout = 0;
        foreach (var building in VB.buildiingmanager.JobBuildings)
        {
            if(building == myJobBuilding)//城はスキップ
            {
                continue;
            };
            
            BuildingData data = building.GetComponent<BuildingData>();
            Storage storage = data.storage;
            foreach (var str in storage.materials)
            {
                if (str.Value > maxamout && str.Value > 0)
                {
                    maxamout = str.Value;
                    fullmaterial = str.Key;
                    fullbuilding = building;
                }
            }
        }
        return (fullbuilding,fullmaterial);
    }
}
