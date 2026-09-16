using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Analytics;
using static UnityEditor.PlayerSettings;

public class VillagerBase : MonoBehaviour
{
    [HideInInspector] public static VillagerBase VB { get; private set; }
    [HideInInspector] public TreeManager treemanager;
    [HideInInspector] public BuildingManager buildiingmanager;
    [HideInInspector] public ConstructioinManager constManager;
    [HideInInspector] public HouseAndVillager houseandvillager;
    [HideInInspector] public VIllagerStatusManager statusManager;
    [HideInInspector] public UIController uicontroller;
    [HideInInspector] public WorldSpaceUIController WSUIcontroller;
    [HideInInspector] public Anim anim;
    [HideInInspector] public MobManager mobManager;
    [HideInInspector] public FarmManager farmmanager;

    VillagerAcce VA;
    VillagerHappiness VH;
    public JobBuildingMasterData jobbuildingmaster;

    //移動Agent
    public NavMeshAgent agent;
    public float ArriveRadius = 1f;
    //村人の進むスピード
    public float agentspeed = 10f;
    public float agentroadacc = 2f;
    public float timeoutSeconds = 30f;

    private int triggerCount = 0;  // 何枚の RoadTile トリガー内にいるか
    //村人のレンダー
    public SkinnedMeshRenderer MRender;

    static readonly int ClothColorID = Shader.PropertyToID("_ClothColor");
    static MaterialPropertyBlock mpb;   // 使い回し

    
    //職業変更　
    [HideInInspector] public Job jobchangeto;
    [HideInInspector] private Job currentjob;
    [HideInInspector] public bool jobchangeflag = false;
    //職業建物
    [HideInInspector] public GameObject MyJobBuilding;
    //城
    [HideInInspector] public GameObject Castle;
    //家
    [HideInInspector] public GameObject Myhouse;
    //在宅bool
    [HideInInspector] public bool IsAtHome = true;
    //帰宅bool
    [HideInInspector] public bool IsGoHome = false;

    [HideInInspector] public enum GoState {GoJobBuilding,GoCarry,GoObject,GoLeisure}
    //仕事建物での待機時間
    public JobWaitTimeDataSO waitTimeDataSO;

    public ParticleSystem warningpartcle;

    //Jobとスクリプトを紐づけ
    private Dictionary<Job,JobBase> jobandscript;
    public UnEmployed unemployed;
    public WoodCutter woodcutter;
    public Builder builder;
    public Carrier carrier;
    public Farmer farmer;
    public SawmillWorker sawmillWorker;
    public Miner miner;
    public Hunter hunter;
    public Fisher fisher;
    //自分の村人index
    public int MyIndex;


    private Coroutine arrcheck;//バグ防止
    private void Awake()
    {
        jobandscript = new Dictionary<Job, JobBase>
        {
            {Job.UnEmployer, unemployed},
            {Job.WoodCutter,woodcutter},
            {Job.Builder, builder},
            {Job.Carrier, carrier},
            {Job.Farmer,farmer},
            {Job.SawmillWorker,sawmillWorker},
            {Job.Miner, miner},
            {Job.Hunter,hunter},
            {Job.Fisher, fisher}
        };
   
        //マネージャー系取得
        treemanager = GameObject.Find("TREEMANAGER").GetComponent<TreeManager>();
        houseandvillager = GameObject.Find("HOUSEVILLAGER").GetComponent<HouseAndVillager>();
        buildiingmanager = GameObject.Find("BUILTINGMANAGER").GetComponent<BuildingManager>();
        constManager = GameObject.Find("CONSTRUCTIONMANAGER").GetComponent<ConstructioinManager>();
        statusManager = GameObject.Find("VillagerStatusManager").GetComponent<VIllagerStatusManager>();
        WSUIcontroller = GameObject.Find("WorldSpaceUIController").GetComponent<WorldSpaceUIController>();
        uicontroller = GameObject.Find("UIController").GetComponent<UIController>();
        mobManager = GameObject.Find("MobManager").GetComponent<MobManager>();
        farmmanager = GameObject.Find("FARMMANAGER").GetComponent<FarmManager>();

        //navmesh設定
        agent.speed = uicontroller.settingUICont.currentvillagerspeed;
        agent.isStopped = true;
        agent.avoidancePriority = Random.Range(1, 100);
        //レンダー設定
        
        MRender.enabled = false;
        mpb = new MaterialPropertyBlock();

        //VAとVH取得
        VA = GetComponent<VillagerAcce>();
        VH = GetComponent<VillagerHappiness>();
        anim = GetComponent<Anim>();
        //城保存
        Castle = GameObject.Find("Castle");
    }
    private void Update()
    {
        if (agent.isActiveAndEnabled == true)
        {
            agent.velocity = (agent.steeringTarget - transform.position).normalized * agent.speed;
            Vector3 direction = agent.steeringTarget - transform.position;

            if (direction != Vector3.zero) // ゼロベクトルでないかチェック
            {
                transform.forward = direction;
            }
        }
    }
    public void JobChange(Job job,GameObject jobbuilding)//statusmanagerから呼び出し
    {
        jobchangeto = job;
        MyJobBuilding = jobbuilding;
    }
    public void JobChangeExecute()//各職業スクリプトから呼び出し
    {
        currentjob = jobchangeto;
        jobchangeflag = false;
        

        //服の色を変更
        Color clothcolor = jobbuildingmaster.GetDataByJob(currentjob).jobClothColor;
        
        MRender.GetPropertyBlock(mpb);
        
        mpb.SetColor(ClothColorID, clothcolor);
        MRender.SetPropertyBlock(mpb);
        MRender.GetPropertyBlock(mpb);
        //↑ここまで

        //付属品を一旦非表示
        VA.AllAcceOff();

        //職業服飾品のレンダラーを登録
        VA.AllClothingOff();

        jobandscript[currentjob].StartMyJob();
        DebugController.Log($"職業変更{jobchangeto}"+"-"+$"{MyIndex.ToString()}");
    }

    public void DepartToTarget(GameObject target,GoState state)
    {
        DebugController.Log("DepartTo" + $"{target.name}" + "ー" + $"{currentjob}");
        agent.isStopped = false;
        MyRenderOn();
        
        if(state == GoState.GoJobBuilding 
            || state == GoState.GoLeisure 
            || state == GoState.GoObject)
        {
            anim.Play(AnimType.Walk);
        }
        else if(state == GoState.GoCarry)
        {
            anim.Play(AnimType.Carry);
        }
        if (arrcheck != null) StopCoroutine(arrcheck);
        //入口オブジェクトがあるなら
        Transform targetent = target.transform.Find("Entrance");
        if (targetent != null)
        {
            agent.SetDestination(targetent.transform.position);
            StartCoroutine(ArrCheck(target,state));
        }
        else
        {
            agent.SetDestination(target.transform.position);
            StartCoroutine(ArrCheck(target,state));
        }
    }
    
    IEnumerator ArrCheck(GameObject target,GoState state)
    {
        yield return new WaitUntil(() => !agent.pathPending);
        if (agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            Debug.Log("経路が見つからなかったので脱出します。");
            agent.enabled=false;
           transform.position = target.transform.position;
            agent.enabled = true;
        }
        yield return new WaitUntil(() =>
             agent.remainingDistance <= ArriveRadius &&
             agent.velocity.sqrMagnitude < 0.01f);

        agent.isStopped = true;
        if (state == GoState.GoJobBuilding || state == GoState.GoCarry)
        {
            MyRenderOff();
            jobandscript[currentjob].ArriveAtTarget(target);
        }
        else if(state == GoState.GoObject)
        {
            jobandscript[currentjob].ArriveAtTarget(target);
        }
        else if(state == GoState.GoLeisure)
        {
            StartCoroutine(StartLeisure(target));
        }
        arrcheck = null;
        
    }

    IEnumerator StartLeisure (GameObject leisure)
    {
        List<Transform> hubpoints = new List<Transform>();
        foreach(Transform child in leisure.transform)
        {
            if(child.name == "HubPoint") { 
                hubpoints.Add(child);
            }
        }
        agent.enabled = false;
        //marketの挙動↓
        if (hubpoints.Count > 0) {
            Transform pos = hubpoints[Random.Range(0, hubpoints.Count - 1)];
            anim.Play(AnimType.Walk);
            Vector3 dir = pos.position - this.transform.position;
            dir.y= 0f;
            this.transform.rotation = Quaternion.LookRotation(dir);
            this.transform.DOMove(new Vector3(pos.position.x, this.transform.position.y, pos.position.z), 3f).OnComplete(() =>
            {
                anim.Play(AnimType.LookPoint);
            });
            
        }
        
        yield return new WaitForSeconds(5);//↑で設定している移動時間も含む
        if(Random.Range(0f,1f) > 0.5f)//50％の確率で休憩終了
        {
            BuildingData data = leisure.GetComponent<BuildingData>();
            data.OccupantVillagers.Remove(this.gameObject);
            anim.Play(AnimType.Walk);
            Transform entrance = leisure.transform.Find("Entrance");
            this.transform.LookAt(entrance,transform.up);
            this.transform.DOMove(new Vector3(entrance.position.x, this.transform.position.y, entrance.position.z), 3f).OnComplete(() =>
            {
                agent.enabled = true;
                jobandscript[currentjob].StartMyJob();
            });
            VH.HungerLevelUpdate(50f);//一度に増やす満腹度
        }
        else
        {
            StartCoroutine(StartLeisure(leisure));  //休憩続行
        }
    }


    //進捗UIの操作
    public void CallChangeProgressUI(GameObject building,int progress)
    {
        WSUIcontroller.ChangeProgressBar(building, progress);
    }
    public void NoMaterialAndWait(MaterialType material,string placestring)
    {
        MyRenderOn();
        //正面を向かせる
        Vector3 targetPos = new Vector3(0, transform.position.y,0); // 高さを無視
        transform.LookAt(targetPos);
        anim.Play(AnimType.Idle);
        warningpartcle.Play();

        //警告表示
        uicontroller.MakeNotEnoughString(material, placestring);
    }

    public void EnoughMaterialorQuitJob(MaterialType material,string placestring)
    {
        uicontroller.RemoveNotEnoughString(material, placestring);
    }
    public void BreakWait()
    {
        MyRenderOn();
        //正面を向かせる
        Vector3 targetPos = new Vector3(0, transform.position.y, 0); // 高さを無視
        transform.LookAt(targetPos);
        anim.Play(AnimType.Idle);
    }

    //RenderのOnOff
    public void MyRenderOn()
    {
        MRender.enabled = true;
        foreach(var rend in VA.Job_ClothingRenderer[currentjob])
        {
            rend.enabled = true;
        }
        
    }
    public void MyRenderOff()
    {
        MRender.enabled = false;
        foreach (var rend in VA.Job_ClothingRenderer[currentjob])
        {
            rend.enabled = false;
        }

    }

    //道で速度を上げるためのcollidertrigger
    void OnTriggerEnter(Collider other)
    {
        // タグ比較 or Layer 比較で道タイルを判定
        if (other.CompareTag("Road"))
        {
            triggerCount++;
            if (triggerCount == 1)
                agent.speed += agentroadacc; // 初めて道タイルに入った瞬間だけ上げる
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("RoadTile"))
        {
            triggerCount = Mathf.Max(0, triggerCount - 1);
            if (triggerCount == 0)
                agent.speed -= agentroadacc; // 全部出たら戻す
        }
    }

    public GameObject FindNearestObj(List<GameObject> objs)
    {
        if (objs.Count == 0) return null;
        GameObject closest = null;
        float minDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;
        foreach (GameObject obj in objs)
        {
            if (obj == null) continue; // Nullチェック
            float distance = Vector3.Distance(currentPosition, obj.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = obj;
            }
        }
        return closest;
    }
    public GameObject FindWithMostBuilding(MaterialType type)
    {
        GameObject buildingWithMostMat = null;
        int maxMatAmount = 0;

        foreach (GameObject building in buildiingmanager.JobBuildings)
        {
            BuildingData data = building.GetComponent<BuildingData>();
            int MatAmount = data.storage.materials[type];
            
            if (MatAmount > maxMatAmount)
            {
                maxMatAmount = MatAmount;
                buildingWithMostMat = building;
            }
        }
        return buildingWithMostMat;
    }
}
