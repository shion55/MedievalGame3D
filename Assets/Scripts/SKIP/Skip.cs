using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Skip : MonoBehaviour
{
    [Header("マネージャー系")]
    public ConstructioinManager constructioinManager;
    public BuildingManager buildingManager;
    public HouseAndVillager houseandvillager;
    public MoneyManager moneyManager;
    public VIllagerStatusManager statusmanager;
    public UIController uicontroller;
    public WorldSpaceUIController worldSpaceUIController;
    public FarmManager farmManager;

    [Header("スキップするか")]
    public bool SkipActive;
    [Header("データ")]
    public ConstBuildingMasterDataSO constbuildingmaster;
    public JobBuildingMasterData jobbuildingmaster;
    [Header("親")]
    public GameObject buildingParent;
    public GameObject HouseParent;

    [Header("初期建物")]
    public GameObject House1;
    public GameObject WoodCabin1;

    [Header("既存オブジェクトのレイヤー")]
    public LayerMask obstacleMask;        // 既存オブジェクトのレイヤー
    public float spawnRadius = 0.5f;      // 当たり判定半径

    public int areaMask = NavMesh.AllAreas;
    public NavMeshSurface surface;
    private bool hasExecuted = false;

    private IEnumerator Start()
    {
        if (!SkipActive)
            yield break;

        // 他ManagerのStartを待つ
        yield return new WaitForSeconds(0.2f);

        ExecuteSkip();
    }

    public void ExecuteSkip()
    {
        if (!SkipActive || hasExecuted)
            return;

        hasExecuted = true;

        SkipBuilding();
        SkipJob();

        // Runtimeで追加した建物をNavMeshへ反映
        Physics.SyncTransforms();

        if (surface != null)
        {
            surface.BuildNavMesh();
        }

        if (uicontroller != null)
        {
            uicontroller.CloseLoadingUI();
        }

        Debug.Log("Skip completed.");
    }
    void SkipBuilding()
    {
        if (SkipActive)
        {
            //constableに追加
            ConstBuildingType[] SkipConstable 
                = { ConstBuildingType.farmbuilding,
                    ConstBuildingType.mine, 
                    ConstBuildingType.huntercabin,
                    ConstBuildingType.fishmancabin,
                    ConstBuildingType.market};
            constructioinManager.ConstableBuildingType.AddRange(SkipConstable);

            //元から建設しておく↓
            Vector3 cameraDirection = Camera.main.transform.forward;
            cameraDirection.y = 0;

            Vector3 Housespos = House1.transform.position;
            Vector3 Buildingpos = WoodCabin1.transform.position;
            foreach (ConstBuildingType type in constructioinManager.ConstableBuildingType)
            {
                //SKIPで生成しないtype
                if (type == ConstBuildingType.road || type == ConstBuildingType.road2 || type == ConstBuildingType.farm_block
                    || type == ConstBuildingType.farmbuilding　|| type == ConstBuildingType.market)
                {
                    continue;
                }

                //生成するもののprefab
                GameObject prefab = constructioinManager.constbuildingmaster.GetData(type).conbuildingPrefab;

                if (type == ConstBuildingType.House)  //家
                {

                    for (int i = 0; i < 3; i++)
                    {
                        Housespos.x += 3f;
                        GameObject housebuilding = Instantiate(prefab, Housespos, Quaternion.LookRotation(cameraDirection), HouseParent.transform);
                        houseandvillager.HouseGenerated(housebuilding);
                        moneyManager.GenerateCoin(housebuilding);
                    }

                }
                else　　　　　　　　　　　　　　　　　　//職業建物
                {
                    //最初から置いてある木こり小屋を基準にしてスキップの建物を並べる
                    Buildingpos.z -= 10f;

                    Vector3 pos = Buildingpos;
                    GameObject building = Instantiate(prefab, pos, prefab.transform.rotation, buildingParent.transform);
                    
                    //ConstBuildingTypeからBuildingTypeへの変換処理
                    BuildingType btype = (BuildingType)System.Enum.Parse(typeof(BuildingType), type.ToString());
 
                    BuildingData data = building.GetComponent<BuildingData>();
                    data.DataInitialize(ConstCategory.JobBuilding,
                                         btype,
                                         jobbuildingmaster.GetDataByBuilding(btype).jobType,
                                         building,
                                         jobbuildingmaster,
                                         null);
                    buildingManager.JobBuildings.Add(building);
                    worldSpaceUIController.GenerateWorldSpaceBuildingUI(Buildingpos, building);

                }

            }
        }
    }
    void SkipJob()
    {
        if (SkipActive)
        {
            Job[] skipjob = {Job.SawmillWorker,Job.Miner,Job.Farmer,Job.Hunter 
                                      ,Job.Fisher};
            statusmanager.availableJobs.AddRange(skipjob);

        }
    }

    public List<Vector3> GetReservedBuildingPositions()
    {
        List<Vector3> positions =
            new List<Vector3>();


        // SkipBuilding() で追加されるものも含めて
        // 実際の建築予定リストを再現する
        List<ConstBuildingType> types =
            new List<ConstBuildingType>(
                constructioinManager.ConstableBuildingType
            );


        ConstBuildingType[] skipConstable =
        {
        ConstBuildingType.farmbuilding,
        ConstBuildingType.mine,
        ConstBuildingType.huntercabin,
        ConstBuildingType.fishmancabin,
        ConstBuildingType.market
    };


        foreach (ConstBuildingType type
                 in skipConstable)
        {
            if (!types.Contains(type))
            {
                types.Add(type);
            }
        }


        Vector3 housePos =
            House1.transform.position;

        Vector3 buildingPos =
            WoodCabin1.transform.position;


        foreach (ConstBuildingType type
                 in types)
        {
            // Skipで生成しないもの
            if (type == ConstBuildingType.road ||
                type == ConstBuildingType.road2 ||
                type == ConstBuildingType.farm_block ||
                type == ConstBuildingType.farmbuilding ||
                type == ConstBuildingType.market)
            {
                continue;
            }


            // 家3軒
            if (type == ConstBuildingType.House)
            {
                for (int i = 0; i < 3; i++)
                {
                    housePos.x += 3f;

                    positions.Add(
                        housePos
                    );
                }
            }
            else
            {
                // 職業建物
                buildingPos.z -= 10f;

                positions.Add(
                    buildingPos
                );
            }
        }


        return positions;
    }
}
