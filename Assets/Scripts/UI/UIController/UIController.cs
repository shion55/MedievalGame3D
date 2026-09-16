using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;


public class UIController : MonoBehaviour
{
    [HideInInspector] public static UIController UC { get; private set; }
    [Header("各スクリプト")]
    public HouseAndVillager houseandvillager;
    public VIllagerStatusManager statusManager;
    public ConstSiteSetHandler constSiteSetHandler;
    
    public ConstructioinManager constmanager;
    public BuildingManager buildingManager;
    public MoneyManager moneyManager;
　　public Cameracont cameracont;  //UIを開いている間はカメラ移動を止める
    public ObjectOutlineManager outlinemanager;//villagerにスポットを当てる用
    public ObjectTapHandler tapHandler;

    [Header("JobAndBuildingマスターデータ")]
    public JobBuildingMasterData jobbuildingmaster;

    [Header ("建物・城共通マテリアルとストレージのプレハブ")]
    public GameObject　MaterialStoragePrefab;
    public MaterialAndSpriteSO MaterialspriteData;

    [Header("建築UIを開くボタン")]
    public GameObject ConstMenuOpenButton;   //ボタン    
    
    [Header("警告UI")]
    public GameObject InformationButton;
    public GameObject InformationUI;
    public Transform InformatioinTextParrnt;
    public GameObject InformationTextPrefab;
    
    [Header("村人一覧UI")]
    public GameObject OpenVillagerOverViewUIButton;
    public GameObject VillagerOverViewUI;     //村人と職業の全体UI
    public GameObject VillOvewViewVillagerPrefab; //村人と職業の一人分のprefab
    public Transform VillagerOverViewContetPa;//↑のprefabが入っている親
    private List<string> NotEnoughWarnings = new List<string>();


    [Header("お金")]
    //お金表示
    public TextMeshProUGUI MoneyText;
    private int currentMoney;

    [Header("BackUI")]
    public GameObject BackButton;

    [Header("ローディングUI")]
    public GameObject loadingPanel;

    [HideInInspector]
    public bool UIOPEN = false;

    //警告ボタンを表示するか
    [HideInInspector]
    public bool HasInformation = false;

    private int tempjobnum;

    //一時UIを入れるリスト(CloseAllで呼ぶ)
    public List<GameObject> temporaryUI = new List<GameObject>();
    private List<GameObject> permanentUI;

    //Spot用コールチン
    private Coroutine outlineRoutine;
    private GameObject currentSpotted;  // 今アウトラインを当てているオブジェクト


    [Header("各UIスクリプト")]
    public HouseUICont houseUIcont;
    public BuildingUICont buildingUIcont;
    public BuildingHireVillagerUICont buildinghirevillagerUIcont;
    public CastleUICont castleUIcont;
    public ConstUICont constUIcont;
    public SettingUICont settingUICont;

    void Start()
    {
        //PermanentUIに恒久UIを入れる
        permanentUI = new List<GameObject> { 
          ConstMenuOpenButton,
          OpenVillagerOverViewUIButton,
          settingUICont.SettingButton
        };
    }
   
    public void OpenUI(int uitoopen)
    {
        cameracont.CameraContActive = false;
        UIOPEN = true;
        if(uitoopen == 0)//村人とジョブ対応UIを開く
        {
            VillagerOverViewUIOpen();
        }
        else if(uitoopen == 1){ //建築モード

            constUIcont.OpenConstMenuUI();
        }
        else if(uitoopen == 2)//警告モード
        {
            OpenInformationUI();
        }
        else if(uitoopen == 3)//設定モード
        {
            settingUICont.OpenSettingUI();
        }
        ClosepermanentUI();//恒久UIを閉じる
    }
    public void OpenUIFromTap(GameObject building,UIENUM ui)
    {
        UIOPEN = true;
        cameracont.CameraContActive = false;
        if(ui == UIENUM.House)
        {
            DebugController.Log("家のUIを開いた");
            houseUIcont.OpenHouseUI(building);
            temporaryUI.Add(houseUIcont.HouseUI);
        }
        else if(ui == UIENUM.Building)
        {
            DebugController.Log("建物のUIを開いた");
            buildingUIcont.OpenBuildingUI(building);
            temporaryUI.Add(buildingUIcont.BuildingUI);
        }
        else if(ui == UIENUM.Castle)
        {
            DebugController.Log("城のUIを開いた");
            castleUIcont.OpenCastleUI();
            temporaryUI.Add(castleUIcont.CastleUI);
        }
        ClosepermanentUI();
    }
    public void CloseUI()
    {
        DebugController.Log("UIを閉じた");
        UIOPEN = false;
        foreach (GameObject ui in temporaryUI)
        {
            ui.SetActive(false);
        }
        temporaryUI.Clear();
        ReActivepermanentUI();//恒久UIを開ける
        cameracont.CameraContActive = true;
    }
    //BackButtonをActiveにする
    void BackButtonActive(System.Action OnBack)
    {
        BackButton.SetActive(true);
        Button backbutton = BackButton.GetComponent<Button>();
        backbutton.onClick.RemoveAllListeners();
        backbutton.onClick.AddListener(() => OnBack?.Invoke());
        backbutton.onClick.AddListener(() => BackButton.SetActive(false));

    }
    public void ClosetemporaryUI()
    {
        foreach (GameObject ui in temporaryUI)
        {
            ui.SetActive(false);
        }
    }
    void ClosepermanentUI()
    {
        foreach (GameObject ui in permanentUI)
        {
            ui.SetActive(false);
        }
    }
    void ReActivepermanentUI()
    {
        foreach (GameObject ui in permanentUI)
        {
            ui.SetActive(true);
        }
    }

    
    
    #region 村人一覧UI
    private void VillagerOverViewUIOpen()
    {
        foreach (Transform child in VillagerOverViewContetPa)
        {
            Destroy(child.gameObject);
        }
        VillagerOverViewUI.SetActive(true);  //開く
        temporaryUI.Add(VillagerOverViewUI);
        foreach (var vAndj in statusManager.villagersjob)
        {
            GameObject villager = vAndj.Key;
            Job job = vAndj.Value;
            Sprite jobimage = jobbuildingmaster.GetDataByJob(job).jobSprite;
            GameObject VandJUI = Instantiate(VillOvewViewVillagerPrefab, VillagerOverViewContetPa);
            VillagerJobUIPrefab VandJprefab = VandJUI.GetComponent<VillagerJobUIPrefab>();
            VandJprefab.SetData(jobimage,() => SpotToVillager(villager,2));
        }
    }
    #endregion
    #region 村人追従
    public void SpotToVillager(GameObject villager,int UItypeindex)//SpotButtonが押され、村人がスポットされる
    {
        
        ClosetemporaryUI();　　　　　　　　　　//現在開いているUIを閉じる
        cameracont.CameraContActive = true;　　//カメラは動かせる　→拡大・縮小だけ
        cameracont.EnterFollowMode(villager); //spot用にカメラが動く

        // 既存コルーチンがあれば止める
        if (outlineRoutine != null) StopCoroutine(outlineRoutine);
        outlineRoutine = StartCoroutine(OutlineMonitor(villager));

         BackButtonActive(() => {
                                StopOutlineMonitor();
                                ReturnSpotToUI(villager,UItypeindex);//どこから呼ばれたかの通し番号
                }); 
        
    }

    private IEnumerator OutlineMonitor(GameObject villager)
    {
        // “村人” と “建物” の参照をあらかじめ取っておく
        var villagerMesh = villager.GetComponentInChildren<SkinnedMeshRenderer>();

        while (true)
        {
            // 毎フレーム「見えているか」をチェック
            GameObject nextSpotted;
            if (villagerMesh.enabled)
            {
                nextSpotted = villager;
            }
            else
            {
                // 最新の MyJobBuilding を取得（nullなら家にフォールバック）
                var jobBuilding = villager.GetComponent<VillagerBase>().MyJobBuilding;
                nextSpotted = jobBuilding != null
                    ? jobBuilding
                    : houseandvillager.villagerhouse[villager];
            }

            // 前回と違うオブジェクトなら切り替え
            if (nextSpotted != currentSpotted)
            {
                if (currentSpotted != null)
                    outlinemanager.OutLineOff(currentSpotted);

                outlinemanager.OutLineOn(nextSpotted);
                currentSpotted = nextSpotted;
            }
            yield return null;
        }
    }
    private void StopOutlineMonitor()
    {
        if (outlineRoutine != null)
        {
            StopCoroutine(outlineRoutine);
            outlineRoutine = null;
        }
        
        outlinemanager.OutLineOff(currentSpotted);
    }
    void ReturnSpotToUI(GameObject villager, int index)
    {
        cameracont.ExitFollowMode();
        cameracont.CameraContActive = false;
        if (index == 0)//家UIから呼ばれている
        {
            GameObject house = houseandvillager.villagerhouse[villager];
            houseUIcont.OpenHouseUI(house);
        }
        else if (index == 1)
        {
            VillagerBase VB = villager.GetComponent<VillagerBase>();
            GameObject building = VB.MyJobBuilding;
            buildingUIcont.OpenBuildingUI(building);
        }
        else if (index == 2) {//村人一覧から呼ばれている
            VillagerOverViewUIOpen();
        }
    }
    #endregion

    #region 警告
    public void OpenInformationUI()
    {
        UIOPEN = true;
        foreach(Transform child in InformatioinTextParrnt)
        {
            Destroy(child.gameObject);
        }
        InformationUI.SetActive(true);
        InformationButton.SetActive(false);

        temporaryUI.Add(InformationUI);

        foreach(string warning in NotEnoughWarnings)
        {
            GameObject infoObj = Instantiate(InformationTextPrefab, InformatioinTextParrnt);
            TextMeshProUGUI warntext = infoObj.GetComponentInChildren<TextMeshProUGUI>();
            warntext.text = warning;
            //Button Spotbutton = infoObj.GetComponentInChildren<Button>();
        }

    }
    public void InformationCloseButton()
    {
        UIOPEN = false;
        InformationUI.SetActive(false);
        if (NotEnoughWarnings.Count > 0)
        {
            InformationButton.SetActive(true);
        }
    }
    public void MakeNotEnoughString(MaterialType material,string NotEnoughPlace)
    {      
        string message = $"Not enough {material} in {NotEnoughPlace} ";
        if (!NotEnoughWarnings.Contains(message))
        {
            NotEnoughWarnings.Add(message);
        }
        if (InformationButton.activeSelf == false)
        {
            InformationButton.SetActive(true);
        }
    }
    public void RemoveNotEnoughString(MaterialType material,string Mysite)
    {
        DebugController.Log("不十分テキスト削除");
        string message = $"Not enough {material} in {Mysite} ";
        if (NotEnoughWarnings.Contains(message))
        {
            NotEnoughWarnings.Remove(message);
        }
        if(NotEnoughWarnings.Count == 0)
        {
            InformationButton.SetActive(false);
        }
    }
    private void SpotToBuilding()
    {

    }
    #endregion
    #region 所持金
    public void UpdateWallet(int money)
    {

        int newmoney = money + currentMoney;

        DOTween.To(() => currentMoney, x => {
            currentMoney = x;
            MoneyText.text = currentMoney.ToString();
        }, newmoney, 0.5f);
    }
    #endregion
    #region 建物の雇用
    
    public void StartHireMode(GameObject building)
    {
        buildingUIcont.BuildingUI.SetActive(false);
        tapHandler.JobChangeHouseTap = true;
        BuildingData data = building.GetComponent<BuildingData>();
        Job job = data.buildingjob;
        cameracont.CameraContActive = true;  //HireMode中はカメラを動かせるようにする
        buildinghirevillagerUIcont.HireJob = job;
        buildinghirevillagerUIcont.HireBuilding = building;
        BackButtonActive(() => FinishHireModeAndBackBuildingUI(building));
    }
    public void FinishHireModeAndBackBuildingUI(GameObject building)
    {
        buildingUIcont.OpenBuildingUI(building);
        cameracont.CameraContActive = false;
        foreach (GameObject housepan in buildinghirevillagerUIcont.HouseAndHirePopDic.Values)
        {
            housepan.SetActive(false);
        }
        tapHandler.JobChangeHouseTap = false;
    }
    #endregion


    #region ローディング
    public void OpenLoadingUI()
    {
        loadingPanel.SetActive(true);
    }
    public void CloseLoadingUI()
    {
        loadingPanel.SetActive(false);
    }
    #endregion
}
