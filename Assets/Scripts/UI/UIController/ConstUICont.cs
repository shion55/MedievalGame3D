using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConstUICont : MonoBehaviour
{
    UIController UC;

    [Header("建築UIの起動ボタンとパネル")]
    public GameObject ConstMenuOpenButton;
    public GameObject ConstMenuUI;

    [Header("タブのボタン")]
    public Button HouseTabButton;
    public Button BuildingTabButton;
    public Button RoadTabButton;
    public Button PlantTabButton;
    public Button AmuseTabButton;

    [Header("建築物一覧を生成するContent")]
    public Transform ConstContentTransform;

    [Header("必要金額が足りないときのテキスト")]
    public TextMeshProUGUI NotEnoughMoneyText;

    [Header("建築物毎のプレハブとマテリアル毎のプレハブ")]
    public GameObject ConstDetailPrefab;
    public GameObject NeedmaterialPrefab;

    [Header("マスターデータ")]
    public ConstBuildingMasterDataSO constbuildingmaster;

    [Header("道建設ハンドラー")]
    public ConstRoadHandler constroadhandler;

    [Header("畑ハンドラー")]
    public ConstFarmBlockHandler constfarmblockhandler;


    void Start()
    {
        UC = GetComponent<UIController>();

        // タブボタン
        HouseTabButton.onClick.AddListener(
            () => ShowCategory(ConstCategory.House)
        );

        BuildingTabButton.onClick.AddListener(
            () => ShowCategory(ConstCategory.JobBuilding)
        );

        RoadTabButton.onClick.AddListener(
            () => ShowCategory(ConstCategory.Road)
        );

        PlantTabButton.onClick.AddListener(
            () => ShowCategory(ConstCategory.Plant)
        );

        AmuseTabButton.onClick.AddListener(
            () => ShowCategory(ConstCategory.AmuseBuilding)
        );
    }


    public void OpenConstMenuUI()
    {
        ConstMenuUI.SetActive(true);
        ConstMenuOpenButton.SetActive(false);

        if (!UC.temporaryUI.Contains(ConstMenuUI))
        {
            UC.temporaryUI.Add(ConstMenuUI);
        }

        // 開いたときは家タブを表示
        ShowCategory(ConstCategory.House);
    }


    // =========================================================
    // タブ切り替え
    // =========================================================

    private void ShowCategory(ConstCategory category)
    {
        ClearContent();

        foreach (ConstBuildingType type in UC.constmanager.ConstableBuildingType)
        {
            var data = constbuildingmaster.GetData(type);

            if (data == null)
                continue;

            // 違うカテゴリなら表示しない
            if (data.constcategory != category)
                continue;

            MakeConstDetailUIPanel(type);
        }
    }


    // =========================================================
    // Content内を削除
    // =========================================================

    private void ClearContent()
    {
        foreach (Transform child in ConstContentTransform)
        {
            // Destroyはフレーム末なので先に非表示
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }


    // =========================================================
    // 建築物1個分のUIを生成
    // =========================================================

    private void MakeConstDetailUIPanel(ConstBuildingType constType)
    {
        var data = constbuildingmaster.GetData(constType);

        if (data == null)
            return;

        GameObject constDetail =
            Instantiate(ConstDetailPrefab, ConstContentTransform);

        ConstDetailUIPrefab detailUIPrefab =
            constDetail.GetComponent<ConstDetailUIPrefab>();


        // -------------------------
        // 建物画像
        // -------------------------

        detailUIPrefab.ConstBuildingImage.sprite =
            data.conbuildingSprite;


        // -------------------------
        // 必要素材
        // -------------------------

        Transform needMaterialTransform =
            constDetail.transform.Find("NeedMaterialPanel");

        var matdict = data.GetMaterialDict();

        foreach (var pair in matdict)
        {
            MaterialType matType = pair.Key;
            int amount = pair.Value;

            GameObject needMaterial =
                Instantiate(
                    NeedmaterialPrefab,
                    needMaterialTransform
                );

            ConstNeedMatUIPrefab needPrefab =
                needMaterial.GetComponent<ConstNeedMatUIPrefab>();

            needPrefab.MaterialImage.sprite =
                UC.MaterialspriteData.GetSprite(matType);

            needPrefab.NeedMatText.text =
                amount.ToString();
        }


        // -------------------------
        // 建築ボタン
        // -------------------------

        Button constButton =
            detailUIPrefab.ConstSetButton;

        constButton.onClick.RemoveAllListeners();

        if (data.constcategory == ConstCategory.Road)
        {
            constButton.onClick.AddListener(
                () => RoadGenModeStart(constType)
            );
        }
        else if (data.constcategory == ConstCategory.Plant)
        {
            constButton.onClick.AddListener(
                () => FarmGenModeStart()
            );
        }
        else
        {
            constButton.onClick.AddListener(
                () => SiteGenModeStart(constType)
            );
            Debug.Log(constType + "addListner");
        }
    }


    // =========================================================
    // 建築場所決めモード
    // =========================================================

    public void SiteGenModeStart(ConstBuildingType type)
    {
        int needMoney =
            constbuildingmaster
                .GetData(type)
                .GetMaterialDict()[MaterialType.Money];

        if (UC.moneyManager.HavingMoney >= needMoney)
        {

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


    // =========================================================
    // 道敷設
    // =========================================================

    private void RoadGenModeStart(ConstBuildingType type)
    {
        ConstMenuUI.SetActive(false);
        ConstMenuOpenButton.SetActive(false);

        UC.cameracont.CameraContActive = true;

        constroadhandler.RoadGenModeOn(type);
    }


    // =========================================================
    // 畑
    // =========================================================

    private void FarmGenModeStart()
    {
        ConstMenuUI.SetActive(false);
        ConstMenuOpenButton.SetActive(false);

        UC.cameracont.CameraContActive = true;

        constfarmblockhandler.FarmGenModeOn();
    }


    // =========================================================
    // お金不足表示
    // =========================================================

    IEnumerator NotEnoughMoneyDisplay()
    {
        NotEnoughMoneyText.text = "NotEnoughMoney";

        yield return new WaitForSeconds(3);

        NotEnoughMoneyText.text = "";
    }


    // =========================================================
    // 建築メニューを閉じる
    // =========================================================

    public void BuildMenuClose()
    {
        UC.UIOPEN = false;

        UC.constSiteSetHandler.SiteGenModeOff();

        ConstMenuUI.SetActive(false);
        ConstMenuOpenButton.SetActive(true);

        UC.cameracont.CameraContActive = true;
    }
}