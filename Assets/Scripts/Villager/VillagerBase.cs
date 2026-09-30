using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Analytics;
using static UnityEditor.PlayerSettings;



public class VillagerBase : MonoBehaviour
{
    public enum ActivityState
    {
        Idle,
        Moving,
        Working,
        Resting,
        Blocked
    }
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

    //現在の状態
    public ActivityState CurrentActivity { get; private set; }

    VillagerAcce VA;
    VillagerHappiness VH;
    public JobBuildingMasterData jobbuildingmaster;

    //移動Agent
    public NavMeshAgent agent;
    public float ArriveRadius = 1f;
    //村人の進むスピード
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
    private GameObject jobBuildingChangeTo;
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

    //leisure
    private Transform currentLeisurePoint;
    private bool leisureActive = false;

    private Coroutine arrcheck;//バグ防止
    private Coroutine blockedJobChangeWait;

   
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
        constManager = GameObject.Find("ConstructionManager").GetComponent<ConstructioinManager>();
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
    /*private void Update()
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
    }*/

    #region 職業変更
    public void JobChange(Job job,GameObject jobbuilding)//statusmanagerから呼び出し
    {
        jobchangeto = job;
        jobBuildingChangeTo = jobbuilding;
    }
    public void JobChangeExecute()//各職業スクリプトから呼び出し
    {
        currentjob = jobchangeto;
        jobchangeflag = false;
        MyJobBuilding = jobBuildingChangeTo;

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
    #endregion

    public void SetActivity(ActivityState state)
    {
        CurrentActivity = state;
    }


    public void DepartToTarget(GameObject target, GoState state, Vector3? overrideDestination = null)
    {
        DebugController.Log("DepartTo" + target.name + "ー" + currentjob);
        SetActivity(ActivityState.Moving);

        

        Vector3 destination;
        if (overrideDestination.HasValue)
        {
            destination = overrideDestination.Value;
        }
        else if (target.tag == "Building"||target.tag == "Castle" || target.tag == "House"){
            BuildingAccessPoints accessPoints = target.GetComponentsInChildren<BuildingAccessPoints>()[0];
            destination = accessPoints.Entrance.position;
        }
    
        else
        {
            destination = target.transform.position;
        }
        //すでに到着済みの場合
        if (Vector3.Distance(transform.position,destination) <= ArriveRadius)
        {
            Debug.Log("すでに目的地に到着済み");
            if (state == GoState.GoJobBuilding ||
                state == GoState.GoCarry)
            {
                MyRenderOff();
                jobandscript[currentjob]
                    .ArriveAtTarget(target);
            }
            else if (state == GoState.GoObject)
            {
                jobandscript[currentjob]
                    .ArriveAtTarget(target);
            }
            else if (state == GoState.GoLeisure)
            {
                StartCoroutine(
                    StartLeisure(target)
                );
            }
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(destination);

        StartCoroutine(ShowAfterFacingDirection(state));//向きとアニメを調整

        if (arrcheck == null)
        {
            arrcheck = StartCoroutine(ArrCheck(target, state));
        }
    }
    IEnumerator ShowAfterFacingDirection(GoState state)
    {
        // SetDestinationの経路計算を1フレーム待つ
        yield return null;

        yield return new WaitUntil(() => !agent.pathPending);
        if (!agent.hasPath ||
    agent.pathStatus !=
        NavMeshPathStatus.PathComplete)
        {
            yield break;
        }
        // NavMeshの最初の進行方向を向く
        if (agent.path != null && agent.path.corners.Length > 1)
        {
            Vector3 direction =
                agent.path.corners[1] - transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation =
                    Quaternion.LookRotation(direction);
            }
        }

        // 向きを直してから表示
        MyRenderOn();

        if (state == GoState.GoJobBuilding
            || state == GoState.GoLeisure
            || state == GoState.GoObject)
        {
            anim.Play(AnimType.Walk);
        }
        else if (state == GoState.GoCarry)
        {
            anim.Play(AnimType.Carry);
        }
    }
    IEnumerator ArrCheck(GameObject target,GoState state)
    {
        // SetDestinationしたフレームではまだ経路計算が始まっていないことがある
        yield return null;

        // 経路計算終了まで待つ
        yield return new WaitUntil(() => !agent.pathPending);


        if (!agent.hasPath ||
            agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning(
                $"経路が見つかりませんでした: {target.name}"
            );

            agent.isStopped = true;
            agent.ResetPath();
            arrcheck = null;
            // 歩行アニメのまま止まらないようにする
            MyRenderOn();

            SetActivity(ActivityState.Blocked);//スタック中
            anim.Play(
                AnimType.Idle
            );


            // 頭上の警告
            if (warningpartcle != null)
            {
                warningpartcle.Play();
            }


            // Informationへ追加
            uicontroller.MakePathWarning(
                this.gameObject,
                target.name
            );


            arrcheck = null;

            StartBlockedJobChangeWait();

            yield break;
        }
        yield return new WaitUntil(
    () => !agent.pathPending);

        if (blockedJobChangeWait != null)
        {
            StopCoroutine(
                blockedJobChangeWait
            );

            blockedJobChangeWait =
                null;
        }
        // 成功した
        uicontroller.RemovePathWarning(
           this.gameObject
        );
        yield return new WaitUntil(() =>agent.remainingDistance <= ArriveRadius &&agent.velocity.sqrMagnitude < 0.01f);

        agent.isStopped = true;
        arrcheck = null;

        anim.Play(AnimType.Idle);
        if (state == GoState.GoJobBuilding ||
            state == GoState.GoCarry)
        {
            MyRenderOff();
            jobandscript[currentjob].ArriveAtTarget(target);
        }
        else if (state == GoState.GoObject)
        {
            jobandscript[currentjob].ArriveAtTarget(target);
        }
        else if (state == GoState.GoLeisure)
        {
            StartCoroutine(StartLeisure(target));
        }
    }
    private void StartBlockedJobChangeWait()
    {
        if (blockedJobChangeWait != null)
            return;

        blockedJobChangeWait =
            StartCoroutine(
                WaitForJobChangeWhileBlocked()
            );
    }


    private IEnumerator WaitForJobChangeWhileBlocked()
    {
        while (true)
        {
            if (jobchangeflag)
            {
                blockedJobChangeWait =
                    null;

                // 経路警告を消す
                uicontroller.RemovePathWarning(
                    this.gameObject
                );

                if (warningpartcle != null)
                {
                    warningpartcle.Stop();
                }

                // 古い経路を完全に捨てる
                if (agent != null &&
                    agent.isActiveAndEnabled)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }

                JobChangeExecute();

                yield break;
            }

            yield return null;
        }
    }
    IEnumerator StartLeisure(GameObject leisure)
    {
        // 同じ村人でLeisureが二重起動するのを防ぐ
        if (leisureActive)
        {
            Debug.LogWarning(
                $"Leisure二重起動: {gameObject.name}"
            );

            yield break;
        }

        leisureActive = true;

        BuildingAccessPoints accessPoints =
            leisure.GetComponentInChildren<BuildingAccessPoints>();

        if (accessPoints == null)
        {
            leisureActive = false;
            yield break;
        }


        // =====================================================
        // 本当にEntranceまで到着しているか確認
        // =====================================================

        Transform entrance =
            accessPoints.Entrance;

        if (entrance == null)
        {
            leisureActive = false;
            yield break;
        }


        Vector3 entranceCheckPos =
            entrance.position;

        entranceCheckPos.y =
            transform.position.y;


        float distanceFromEntrance =
            Vector3.Distance(
                transform.position,
                entranceCheckPos
            );


        if (distanceFromEntrance >
            ArriveRadius + 0.5f)
        {
            Debug.LogWarning(
                $"Entrance到着前にStartLeisureが呼ばれました " +
                $"distance={distanceFromEntrance}"
            );

            leisureActive = false;
            yield break;
        }


        // =====================================================
        // ここからMarket内部
        // =====================================================

        agent.enabled = false;

        // 新しいMarket滞在なので前回値を捨てる
        currentLeisurePoint = null;


        List<Transform> hubpoints =
            new List<Transform>();

        foreach (
            Transform point
            in accessPoints.LeisurePoints)
        {
            if (point != null)
            {
                hubpoints.Add(point);
            }
        }


        // =====================================================
        // Market内を滞在
        // =====================================================

        bool continueLeisure = true;

        while (continueLeisure)
        {
            if (hubpoints.Count == 0)
            {
                SetActivity(
                    ActivityState.Resting
                );

                anim.Play(
                    AnimType.LookPoint
                );

                yield return new WaitForSeconds(2f);
            }

            else
            {
                // =============================================
                // 前回とは違うHubPointを候補にする
                // =============================================

                List<Transform> candidates =
                    new List<Transform>();


                foreach (Transform point in hubpoints)
                {
                    if (point != currentLeisurePoint)
                    {
                        candidates.Add(point);
                    }
                }


                // HubPointが1個しかない場合
                if (candidates.Count == 0)
                {
                    candidates.AddRange(
                        hubpoints
                    );
                }


                Transform pos =
                    candidates[
                        Random.Range(
                            0,
                            candidates.Count
                        )
                    ];


                currentLeisurePoint =
                    pos;


                Vector3 targetPosition =
                    pos.position;

                targetPosition.y =
                    transform.position.y;


                float distance =
                    Vector3.Distance(
                        transform.position,
                        targetPosition
                    );


                // =============================================
                // 本当に移動が必要なときだけ歩く
                // =============================================

                if (distance > 0.15f)
                {
                    SetActivity(
                        ActivityState.Moving
                    );


                    Vector3 dir =
                        targetPosition -
                        transform.position;

                    dir.y = 0f;


                    if (dir.sqrMagnitude >
                        0.001f)
                    {
                        transform.rotation =
                            Quaternion.LookRotation(
                                dir
                            );
                    }


                    anim.Play(
                        AnimType.Walk
                    );


                    // WalkへのCrossFadeを少し待つ
                    yield return new WaitForSeconds(
                        0.15f
                    );


                    Tween moveTween =
                        transform.DOMove(
                            targetPosition,
                            3f
                        );


                    // 移動が終わるまで次へ進まない
                    yield return
                        moveTween.WaitForCompletion();
                }


                // =============================================
                // HubPointで滞在
                // =============================================

                SetActivity(
                    ActivityState.Resting
                );

                anim.Play(
                    AnimType.LookPoint
                );


                yield return new WaitForSeconds(
                    2f
                );
            }


            // 50%でMarketを出る
            continueLeisure =
                Random.Range(0f, 1f) <= 0.5f;
        }


        // =====================================================
        // Marketを出る
        // =====================================================

        BuildingData data =
            leisure.GetComponentInChildren<BuildingData>();


        if (data != null)
        {
            data.OccupantVillagers.Remove(
                gameObject
            );
        }


        SetActivity(
            ActivityState.Moving
        );


        Vector3 exitPosition =
            entrance.position;

        exitPosition.y =
            transform.position.y;


        Vector3 exitDir =
            exitPosition -
            transform.position;

        exitDir.y = 0f;


        if (exitDir.sqrMagnitude >
            0.001f)
        {
            transform.rotation =
                Quaternion.LookRotation(
                    exitDir
                );
        }


        anim.Play(
            AnimType.Walk
        );


        yield return new WaitForSeconds(
            0.15f
        );


        Tween exitTween =
            transform.DOMove(
                exitPosition,
                3f
            );


        // Entranceまで出終わるまで待つ
        yield return
            exitTween.WaitForCompletion();


        currentLeisurePoint = null;

        agent.enabled = true;

        leisureActive = false;


        VH.HungerLevelUpdate(
            50f
        );


        jobandscript[currentjob]
            .StartMyJob();
    }
    //進捗UIの操作
    public void CallChangeProgressUI(GameObject building,int progress)
    {
        WSUIcontroller.ChangeProgressBar(building, progress);
    }
    public void NoMaterialAndWait(MaterialType material,string placestring)
    {
        SetActivity(ActivityState.Idle);
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
        if (other.CompareTag("Road"))
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
            BuildingData data = building.GetComponentsInChildren<BuildingData>()[0];
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
