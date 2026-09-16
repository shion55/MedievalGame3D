using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ConstUICont : MonoBehaviour
{
    UIController UC;

    [Header("建築UIの起動ボタンとパネル")]
    public GameObject ConstMenuOpenButton;   //建築メニューを開くボタン    
    public GameObject ConstMenuUI;
    [Header("タブのボタン")]
    public Button HouseTabButton;
    public Button BuildingTabButton;
    public Button RoadTabButton;
    public Button PlantTabButton;
    public Button AmuseTabButton;
    [Header("タブ毎のパネル")]
    public Transform ConstHousePanelTransform;
    public Transform ConstBuildingPanelTransform;
    public Transform ConstRoadPanelTransform;
    public Transform ConstPlantPanelTransform;
    public Transform ConstAmusePanelTransform;

    //タブ毎のパネルとそこに所属する建築物タイプ
    private Dictionary<Transform, List<ConstBuildingType>> ConstMenuPanelsAndConstTypes;
    //タブ内で複数ページにまたがる場合のページとDetail(建物詳細)のオブジェクト
    private Dictionary<Transform, Dictionary<GameObject,int>> BigPanel__DetailObj_PageIndex = new Dictionary<Transform, Dictionary<GameObject,int>>();

    //現在開いているページ(TabChangeメソッドで初期化)
    private int currentpage = 0;
    [Header("必要金額が足りないときのテキスト")]
    public TextMeshProUGUI NotEnoughMoneyText;

    [Header("建築物毎のプレハブとマテリアル毎のプレハブ")]
    public GameObject ConstDetailPrefab;
    public GameObject NeedmaterialPrefab;
    [Header("マスターデータ")]
    public ConstBuildingMasterDataSO constbuildingmaster;

    [Header("ページ送りボタン")]
    public Button GoPageButton;
    public Button ReturnPageButton;
    private int OpenedPage = 0;
    [Header("道建設ハンドラーとボタン")]
    public ConstRoadHandler constroadhandler;
    [Header("畑ハンドラー")]
    public ConstFarmBlockHandler constfarmblockhandler;

    void Start()
    {
        UC = this.GetComponent<UIController>();

      

        //タブボタン登録
        HouseTabButton.onClick.AddListener(() => TabChange(ConstHousePanelTransform));
        BuildingTabButton.onClick.AddListener(() => TabChange(ConstBuildingPanelTransform));
        RoadTabButton.onClick.AddListener(() => TabChange(ConstRoadPanelTransform));
        PlantTabButton.onClick.AddListener(() => TabChange(ConstPlantPanelTransform));
        AmuseTabButton.onClick.AddListener(() => TabChange(ConstAmusePanelTransform));
    }
    public void OpenConstMenuUI()
    {
        if(ConstMenuPanelsAndConstTypes != null)
        {
            foreach (Transform t in ConstMenuPanelsAndConstTypes.Keys)
            {

                foreach (Transform child in t)
                {
                    Destroy(child.gameObject);
                }

            }

            ConstMenuPanelsAndConstTypes.Clear();//パネルと建築可能タイプの辞書一度消す
        }
        MakePanelAndTypeDic();//辞書を作り直す

        ConstMenuUI.SetActive(true);
        ConstMenuOpenButton.SetActive(false);
        UC.temporaryUI.Add(ConstMenuUI);

        GoPageButton.gameObject.SetActive(false);
        ReturnPageButton.gameObject.SetActive(false);
        GoPageButton.onClick.RemoveAllListeners();
        ReturnPageButton.onClick.RemoveAllListeners();

        BigPanel__DetailObj_PageIndex.Clear();
        //建築パネルの全てを生成する
        foreach (var pair in ConstMenuPanelsAndConstTypes)//タブ毎のforeach(四回)
        {
            MakeConstDetailUIPanel(pair.Key,pair.Value);
        }
    }
    //ConstCategory毎に呼ばれる↓
    void MakeConstDetailUIPanel(Transform t, List<ConstBuildingType> typelist)
    {
        BigPanel__DetailObj_PageIndex.Add(t, new Dictionary<GameObject,int>());
        int Panelindex = 0;
        int Pageindex = 0;
        foreach (ConstBuildingType ConstType in typelist)//建築できるもの
        {
            GameObject ConstDetail = Instantiate(ConstDetailPrefab,t );//建物の詳細の親部分
            //複数ページにまたがる場合のページ通し番号を設定
            if (Panelindex > 0 && Panelindex % 4 == 0)
            {
                Pageindex++;
                ConstDetail.SetActive(false);
            }
            BigPanel__DetailObj_PageIndex[t].Add(ConstDetail,Pageindex);
            Panelindex++;
            //生成した各建物の部分の子オブジェクト(必要材料を入れる部分)を取得
            Transform NeedMaterialTransform = ConstDetail.transform.Find("NeedMaterialPanel");
            ConstDetailUIPrefab detailUIPrefab = ConstDetail.GetComponent<ConstDetailUIPrefab>();
            //各部分の画像欄とボタン欄に挿入
            detailUIPrefab.ConstBuildingImage.sprite = constbuildingmaster.GetData(ConstType).conbuildingSprite;//建物の画像
            var matdict = constbuildingmaster.GetData(ConstType).GetMaterialDict();

            foreach (MaterialType mattype in matdict.Keys)//建物が必要なマテリアルとその量を表示
            {
                GameObject needmaterial = Instantiate(NeedmaterialPrefab, NeedMaterialTransform);
                ConstNeedMatUIPrefab uineedprefab = needmaterial.GetComponent<ConstNeedMatUIPrefab>();
                uineedprefab.MaterialImage.sprite = UC.MaterialspriteData.GetSprite(mattype);
                uineedprefab.NeedMatText.text = matdict[mattype].ToString();
            }
            //建設決定ボタンの設定
            Button constbutton = detailUIPrefab.ConstSetButton;
            constbutton.onClick.RemoveAllListeners();
            if (t == ConstRoadPanelTransform)
            {
                constbutton.onClick.AddListener(() => RoadGenModeStart(ConstType));//所持金が足りるかもこの中で見る   
            }
            else if(t == ConstPlantPanelTransform)
            {
                constbutton.onClick.AddListener(() => FarmGenModeStart());
            }
            else
            {
                constbutton.onClick.AddListener(() => SiteGenModeStart(ConstType));
            }
            //家のみ表示しておく
           
        }
        if (t == ConstHousePanelTransform)
        {

            t.gameObject.SetActive(true);

        }
        else
        {
           t.gameObject.SetActive(false);
        }
    }
    void MakePanelAndTypeDic()
    {
        ConstMenuPanelsAndConstTypes = new Dictionary<Transform, List<ConstBuildingType>>()
        {
            {ConstHousePanelTransform,new List<ConstBuildingType>()},
            {ConstBuildingPanelTransform,new List<ConstBuildingType>()},
            {ConstRoadPanelTransform,new List<ConstBuildingType>()},
            {ConstPlantPanelTransform,new List<ConstBuildingType>()},
             {ConstAmusePanelTransform,new List<ConstBuildingType>()}
        };
        foreach (ConstBuildingType type in UC.constmanager.ConstableBuildingType)
        {
            var data = constbuildingmaster?.GetData(type);

            ConstCategory consttype = constbuildingmaster.GetData(type).constcategory;
            if (consttype == ConstCategory.House)
            {
                ConstMenuPanelsAndConstTypes[ConstHousePanelTransform].Add(type);
            }
            else if (consttype == ConstCategory.JobBuilding)
            {
                ConstMenuPanelsAndConstTypes[ConstBuildingPanelTransform].Add(type);
            }
            else if (consttype == ConstCategory.Road)
            {
                ConstMenuPanelsAndConstTypes[ConstRoadPanelTransform].Add(type);

            }
            else if (consttype == ConstCategory.Plant)
            {
                ConstMenuPanelsAndConstTypes[ConstPlantPanelTransform].Add(type);

            }
            else if (consttype == ConstCategory.AmuseBuilding)
            {
                ConstMenuPanelsAndConstTypes[ConstAmusePanelTransform].Add(type);

            }
        }
    }
    //タブを変える
    void TabChange(Transform BigPanel)
    {
        GoPageButton.gameObject.SetActive(false);
        ReturnPageButton.gameObject.SetActive(false);
        GoPageButton.onClick.RemoveAllListeners();
        ReturnPageButton.onClick.RemoveAllListeners();
        currentpage = 0;
        foreach (var t in ConstMenuPanelsAndConstTypes.Keys)
        {
            t.gameObject.SetActive(false);
            if (t == BigPanel)
            {
                t.gameObject.SetActive(true);
                foreach(var pair in BigPanel__DetailObj_PageIndex[t])
                {
                    if (pair.Value == 0)
                    {
                        pair.Key.SetActive(true);
                    }
                    else
                    {
                        pair.Key.SetActive(false);
                    }
                }
                    

                if (t.childCount > 4)
                {
                    //戻る進むボタン設定
                    GoPageButton.gameObject.SetActive(true);                  
                    GoPageButton.onClick.AddListener(() => GoNextPage(BigPanel__DetailObj_PageIndex[t]));
                    ReturnPageButton.onClick.AddListener(() => ReturnPage(BigPanel__DetailObj_PageIndex[t]));
                }
            }     
        }
    }

    //page進む
    void GoNextPage(Dictionary<GameObject,int> DetailObj_PageIndex)
    {
        currentpage += 1;
        int currentmax = 0;
        foreach (var pair in DetailObj_PageIndex)
        {
            if(pair.Value == currentpage)
            {
                pair.Key.SetActive(true);
            }
            else
            {
                pair.Key.SetActive(false);
            }
            if(currentmax < pair.Value)
            {
                currentmax = pair.Value;
            }
        }
        
        if (currentmax == currentpage)
        {
            //現在で最大なのでGoNextButtonを消す
            GoPageButton.gameObject.SetActive(false);
        }

        if (!ReturnPageButton.gameObject.activeSelf)
        {
            ReturnPageButton.gameObject.SetActive(true);
        }
    }
    //page戻る
    void ReturnPage(Dictionary<GameObject,int> PageIndex_DetailObj)
    {
        currentpage -= 1;
        foreach (var pair in PageIndex_DetailObj)
        {
            if (pair.Value == currentpage)
            {
                pair.Key.SetActive(true);
            }
            else
            {
                pair.Key.SetActive(false);
            }       
        }
        if(currentpage == 0)
        {
            ReturnPageButton.gameObject.SetActive(false);
            GoPageButton.gameObject.SetActive(true);
        }
    }
    //建築場所決めモード
    public void SiteGenModeStart(ConstBuildingType type)//所持金
    {
        int NeedMoney = constbuildingmaster.GetData(type).GetMaterialDict()[MaterialType.Money];
        if (UC.moneyManager.HavingMoney >= NeedMoney)
        {
           
            UC.moneyManager.HavingMoneyUpdate(-NeedMoney);
            UC.constSiteSetHandler.SiteGenModeOn(type);
            ConstMenuUI.SetActive(false);
            ConstMenuOpenButton.SetActive(false);
        }
        else
        {
            DebugController.Log("お金足りない");
            StartCoroutine(NotEnoughMoneyDisplay());
        }
    }
    //道敷設モード
    private void RoadGenModeStart(ConstBuildingType type)
    {
        ConstMenuUI.SetActive(false);
        ConstMenuOpenButton.SetActive(false);
        //カメラコントロールオン
        UC.cameracont.CameraContActive = true;
        constroadhandler.RoadGenModeOn(type);
    }
    private void FarmGenModeStart()
    {
        ConstMenuUI.SetActive(false);
        ConstMenuOpenButton.SetActive(false);
        //カメラコントロールオン
        UC.cameracont.CameraContActive = true;
        constfarmblockhandler.FarmGenModeOn();
    }
    IEnumerator NotEnoughMoneyDisplay()
    {
        NotEnoughMoneyText.text = "NotEnoughMoney";
        yield return new WaitForSeconds(3);
        NotEnoughMoneyText.text = "";
    }
    public void BuildMenuClose()
    {
        UC.UIOPEN = false;
        UC.constSiteSetHandler.SiteGenModeOff();
        ConstMenuUI.SetActive(false);
        ConstMenuOpenButton.SetActive(true);
        UC.cameracont.CameraContActive = true;
    }
}
