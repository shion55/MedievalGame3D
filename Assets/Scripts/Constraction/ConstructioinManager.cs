using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Unity.Jobs.LowLevel.Unsafe;
public class ConstructioinManager : MonoBehaviour
{
    public UIController UIcont;
    public WorldSpaceUIController worldSpaceUIController;
    public HouseAndVillager houseandvillager;
    public BuildingManager buildingManager;
    public VIllagerStatusManager statusManager;
    public MoneyManager moneyManager;
    public FarmManager farmManager;
    public GameObject houseParent;
    public GameObject buildingParent;


    //まだ建築出来ない建物のタイプ
    private List<ConstBuildingType> LockedConstJobBuildingTypes = new List<ConstBuildingType>();
    //今建築できる建物のタイプを格納
    public List<ConstBuildingType> ConstableBuildingType = new List<ConstBuildingType>();

    public GameObject ConstObjs;
    public List<GameObject> waitingSite = new List<GameObject>();

    

   
    private List<GameObject> NoMaterialWaitBuilders = new List<GameObject>();
    private Dictionary<MaterialType,int> NoMaterialAndBuilders = new Dictionary<MaterialType,int>();
    public GameObject SmokePrefab;//煙のプレハブ

    public GameObject PondCollider;
   

    public float ConstDuration = 3f;

    public GameObject FarmFieldPrefab;
    private GameObject FarmField;

    public ConstBuildingMasterDataSO constbuildingmaster;
    public JobBuildingMasterData jobbuildingmaster;

    [Header("NavMesh設定")]
    public NavMeshSurface surface;
  
   
    private void Start()
    {
        //建築できる建物の初期設定
        AddConstableBuildingType(ConstBuildingType.House);//最初は家だけ

        AddConstableBuildingType(ConstBuildingType.sawmill);

        AddConstableBuildingType(ConstBuildingType.road);

        AddConstableBuildingType(ConstBuildingType.farm_block);

        //JobBuildingの未開放リスト作成
        foreach (ConstBuildingType type in Enum.GetValues(typeof(ConstBuildingType)))
        {
            ConstCategory category = constbuildingmaster.GetData(type).constcategory;
            if (category == ConstCategory.JobBuilding)
            {
                LockedConstJobBuildingTypes.Add(type);
            }
        }
        LockedConstJobBuildingTypes.RemoveAll(type => ConstableBuildingType.Contains(type));

        foreach (ConstBuildingType type in LockedConstJobBuildingTypes)
        {
            Debug.Log(type);
        }
    }
    private void AddConstableBuildingType(ConstBuildingType type)//建築できる建物の種類を増やす
    {
        if (!ConstableBuildingType.Contains(type))
        {
            ConstableBuildingType.Add(type);
        }
    }
    public void ConstractionSetting(GameObject site,
        ConstBuildingType type,
        Vector3 snapPos,
        Quaternion spawnRotation)
    {
        var matdict = constbuildingmaster.GetData(type).GetMaterialDict();
        matdict.Remove(MaterialType.Money);

        GameObject SmokeObject = Instantiate(SmokePrefab, site.transform.position, Quaternion.identity, ConstObjs.transform);

        var data = site.GetComponent<ConstructionSite>();
        
        data.constBuildingType = type;
        data.requiredMaterials = matdict;
        data.spownPos = snapPos;
        data.spawnRotation = spawnRotation;
        data.smoke = SmokeObject;
    }
    public void StartConstuction(GameObject site,GameObject builder)//パーティクルの処理等
    {
        
        GameObject Smoke = site.GetComponent<ConstructionSite>().smoke;
        ParticleSystem smokeParticle = Smoke.GetComponent<ParticleSystem>();
        smokeParticle.Play();
        StartCoroutine(InterruptConstruction(site, builder));
    }
    IEnumerator InterruptConstruction(GameObject site,GameObject builder)
    {
       yield return  new WaitForSeconds(ConstDuration);
       GameObject Smoke = site.GetComponent<ConstructionSite>().smoke;
        ParticleSystem smokeParticle =  Smoke.GetComponent<ParticleSystem>();
       smokeParticle.Stop();
       builder.GetComponent<Builder>().Interrupt();
    } 
    public void NotEnoughMaterial(MaterialType material,GameObject builder,GameObject Mysite)
    {
        if (!NoMaterialWaitBuilders.Contains(builder))//足りなくて待機中の人
        {
            NoMaterialWaitBuilders.Add(builder);
        }
        if (!NoMaterialAndBuilders.ContainsKey(material))//足りない人が何人いるか
        {
            NoMaterialAndBuilders.Add(material, 1);//初めて足りないとき　　valueは人数
        }
        else
        {
            NoMaterialAndBuilders[material] += 1;
        }
    }
    public void MaterialAdded (MaterialType material,GameObject builder,GameObject Mysite)
    {
        NoMaterialWaitBuilders.Remove(builder);
        NoMaterialAndBuilders[material] -= 1;
        if (NoMaterialAndBuilders[material] == 0)
        {
            UIcont.RemoveNotEnoughString(material,"Site");
        }
    }
    public void ConstCompleted(GameObject builder,GameObject site)
    {
        var data = site.GetComponent<ConstructionSite>();
        ConstBuildingType buildingType = data.constBuildingType;//建物の種類取得
        
        //生成準備
        Vector3 constplace = data.spownPos;
        Quaternion constRotation = data.spawnRotation;
        
        //建設地も消す
        Destroy(site);
        
        GameObject buildingprefab = constbuildingmaster.GetData(buildingType).conbuildingPrefab;//SOから建物のプレハブを選択
        

        if (buildingType == ConstBuildingType.House){
            //家だった場合
            GameObject housebuilding = Instantiate(buildingprefab,
                constplace,
                 constRotation,
                houseParent.transform);     
            houseandvillager.HouseGenerated(housebuilding);
            moneyManager.GenerateCoin(housebuilding);
        }
        else
        {
            ConstCategory category = constbuildingmaster.GetData(buildingType).constcategory;
            if (category == ConstCategory.JobBuilding)
            {
                AdjustJobBuilding(buildingprefab, constplace, buildingType, constRotation);
            }
            else if(category == ConstCategory.AmuseBuilding)
            {
                AdjustAmuseBuilding(buildingprefab,constplace,buildingType, constRotation);

            }
        }

        StartCoroutine(RebuildNavMesh());
    }
    void AdjustJobBuilding(GameObject buildingprefab,Vector3 constplace,ConstBuildingType buildingType, Quaternion rotation)
    {
        GameObject building = Instantiate(buildingprefab, constplace,rotation, buildingParent.transform);
        

        BuildingType type = (BuildingType)System.Enum.Parse(typeof(BuildingType), buildingType.ToString());

        Job jobtype = jobbuildingmaster.GetDataByBuilding(type).jobType;
        BuildingData data = building.GetComponent<BuildingData>();
        data.DataInitialize(ConstCategory.JobBuilding, type, jobbuildingmaster.GetDataByBuilding(type).jobType, building, jobbuildingmaster, null);
        buildingManager.JobBuildings.Add(building);
        Vector3 pos = building.transform.position;

        //建物ごとの調整
        switch (buildingType)
        {
            case ConstBuildingType.sawmill://建設するものが　　－－　   
                break;
            case ConstBuildingType.farmbuilding:

                farmManager.FarmBuildingAdd(building);
                break;
            case ConstBuildingType.mine:
                break;
            case ConstBuildingType.huntercabin:
                pos.y += 1.2f;
                building.transform.position = pos;
                break;
            case ConstBuildingType.fishmancabin:
                Collider col = PondCollider.GetComponent<Collider>();
                Vector3 pondcenter = col.bounds.center;
                Vector3 lookTarget = new Vector3(pondcenter.x, building.transform.position.y, pondcenter.z);
                building.transform.LookAt(lookTarget);//湖の中心を向かせる
                break;
            default:
                DebugController.Log("建築不可");
                break;
        }

        if (!statusManager.availableJobs.Contains(jobtype))
        {
            statusManager.availableJobs.Add(jobtype);
        }
        //↓次に解放する建物をavailableに入れる
        if (LockedConstJobBuildingTypes.Count > 0)
        {
            ConstBuildingType nextType = LockedConstJobBuildingTypes[0];

            if (!ConstableBuildingType.Contains(nextType))
            {
                ConstableBuildingType.Add(nextType);
                LockedConstJobBuildingTypes.Remove(nextType);
            }
        }

        worldSpaceUIController.GenerateWorldSpaceBuildingUI(constplace, building);
    }
    void AdjustAmuseBuilding(GameObject buildingprefab, Vector3 constplace, ConstBuildingType buildingType, Quaternion rotation)
    {
        GameObject building = Instantiate(buildingprefab, constplace,rotation, buildingParent.transform);
        BuildingType type = (BuildingType)System.Enum.Parse(typeof(BuildingType), buildingType.ToString());

        //マテリアルはSOでまとめてbuildingtype毎に変える↓    今は市場用(マテリアル特に)
        List<MaterialType> materialTypes = new List<MaterialType>()
        {
            MaterialType.Carrot,
            MaterialType.Venison,
            MaterialType.Fish
        };
        BuildingData data = building.GetComponent<BuildingData>();
        data.DataInitialize(ConstCategory.AmuseBuilding, type, null, building, null,materialTypes);
        buildingManager.AmuseBuildings.Add(building);
    }
    IEnumerator RebuildNavMesh()
    {
        UIcont.OpenLoadingUI();
        yield return null;
        yield return new WaitForEndOfFrame();
        surface.BuildNavMesh();

        UIcont.CloseLoadingUI();
    }
}

