using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;
using UnityEngine.TerrainUtils;
using Unity.AI.Navigation;
public class ConstFarmBlockHandler : MonoBehaviour
{
    [Header("UIController")]
    public UIController UC;
    [Header("お金更新用")]
    public MoneyManager moneyManager;
    [Header("畑管理用")]
    public FarmManager farmManager;
    [Header("親とプレハブ")]
    public GameObject Farm_Brock_Preview_Prefab;
    public Transform Farm_Brock_Preview_Pa;
    public GameObject Farm_Brock;
    public Transform Farm_Brock_Pa;
    //previewゲームオブジェクト
    private GameObject previewfarm_Start;//始点の畑
    private GameObject previewfarm_now;//始点or終点の畑

    private float Farm_Block_Size = 0;        //畑のサイズ

    private List<GameObject> previewfarms = new List<GameObject>();//終点を置いている間にinstanteateするpreviewの畑
    private List<Renderer> previewRenderers = new List<Renderer>();//↑の畑と始点終点の畑
    [Header("畑設定ボタン")]
    public Button FarmSetButton;
    public Button FarmConstStopButton;

    [Header("必要金額表示")]
    public int FarmCostParMas = 100;
    public GameObject NeedMoneyPanel;
    public TextMeshProUGUI NeedMoneyText;
    private int CurrentCost;

    public float checkRadius = 0.2f;//建物の周りに建てられない範囲
    public GameObject Plane;
    public LayerMask mask;
    private bool Clickable = true;
    private Vector3[] FarmStartAndEndPos = new Vector3[2];


    // 追加：既存の確定済み畑マスのグリッド座標を管理する
    private HashSet<Vector2Int> existingFarmCoords = new HashSet<Vector2Int>();


    //Update管理
    private bool StartFarmPutMode = false;//update用
    private bool EndFarmPutMode = false;//update用
    void Start()
    {
        previewfarm_Start = Instantiate(Farm_Brock_Preview_Prefab, Vector3.zero, Quaternion.identity);
        previewfarm_Start.GetComponent<Renderer>().enabled = false ;
        Farm_Block_Size = Farm_Brock_Preview_Prefab.transform.localScale.x * 2;
    }
    void Update()
    {
        if (StartFarmPutMode || EndFarmPutMode)
        {
            SetMark();
        }
    }
    public void FarmGenModeOn()
    {
     
        EndFarmPutMode = false;
        StartFarmPutMode = true;
        FarmConstStopButton.gameObject.SetActive(true);
        NeedMoneyPanel.SetActive(true);
        previewfarm_now = previewfarm_Start;
        previewfarm_now.GetComponent<Renderer>().enabled = true;
    }
    private void SetMark()
    {
       
        Ray ray = Camera.main.ScreenPointToRay(GetInputPosition());
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            Vector3 snapPos = GetSnappedPosition(hit.point);
            Vector3 offset = snapPos;
            offset.y -= 0.5f;
            previewfarm_now.transform.position = offset;       //オブジェクトを移動
            if (hit.collider.gameObject == Plane && !Physics.CheckSphere(snapPos, checkRadius, mask))
            {
                //建築可能な部分にsnapPosがあったら
                FarmSetButton.gameObject.SetActive(true);
                if (StartFarmPutMode)
                {
                    previewfarm_Start.GetComponent<Renderer>().material.color = Color.gray;
                }
                foreach (Renderer renderer in previewRenderers)//プレビュー用の畑
                {
                    renderer.material.color = Color.gray;
                }
                if (EndFarmPutMode)
                {
                    Clickable = true;
                    UpdatPreviewFarm_Blocks(snapPos);  //ここで畑のプレビュー　　(ここで赤になることも)
                }
                if (Input.GetMouseButtonDown(0) && Clickable)
                {
                    ButtonClicked(snapPos);
                }
            }
            else//建築不可能
            {
                FarmSetButton.gameObject.SetActive(false);

                //赤くする
                if (StartFarmPutMode)
                {
                    previewfarm_Start.GetComponent<Renderer>().material.color = Color.red;
                }
                foreach (Renderer renderer in previewRenderers)//プレビュー用の畑
                {
                    renderer.material.color = Color.red;
                }
            }
        }
    }

    void ButtonClicked(Vector3 snapPos)
    {
        if (StartFarmPutMode)
        {
            FarmStartAndEndPos[0] = snapPos;
            EndFarmModeOn();
        }
        else
        {
            FarmGenModeOff();
            FarmGenExecute();
        }
    }
    private void EndFarmModeOn()
    {
      
        previewfarm_now.GetComponent<Renderer>().enabled = false;
        StartFarmPutMode = false;
        EndFarmPutMode = true;
    }
    private void FarmGenModeOff()
    {
        EndFarmPutMode = false;
        previewfarm_now.GetComponent<Renderer>().enabled = false;
        FarmSetButton.gameObject.SetActive(false);
        FarmConstStopButton.gameObject.SetActive(false);
        NeedMoneyPanel.SetActive(false);
        UC.CloseUI();
    }
    void UpdatPreviewFarm_Blocks(Vector3 SnapPos)
    {
        ClearPreview();

        
        // 長方形範囲の min / max を取得（XZのみ）
        int xMin = Mathf.Min((int)FarmStartAndEndPos[0].x, (int)SnapPos.x);
        int xMax = Mathf.Max((int)FarmStartAndEndPos[0].x, (int)SnapPos.x);
        int zMin = Mathf.Min((int)FarmStartAndEndPos[0].z, (int)SnapPos.z);
        int zMax = Mathf.Max((int)FarmStartAndEndPos[0].z, (int)SnapPos.z);

        bool FarmsAllConstable = true;

        for (int xi = xMin; xi <= xMax; xi ++)
        {
            for (int zi = zMin; zi <= zMax; zi++)
            {
                Vector3 tilePos = new Vector3(xi,SnapPos.y -0.5f,zi);
                GameObject tile = Instantiate(Farm_Brock_Preview_Prefab, tilePos, Quaternion.Euler(0f, 0f, 0f), Farm_Brock_Preview_Pa);
                bool hasBuildingOverlap = Physics.CheckSphere(
             tilePos,
             checkRadius,
             mask);
                if (hasBuildingOverlap) {
                    
                    FarmsAllConstable = false;
                }
                previewfarms.Add(tile);
                previewRenderers.Add(tile.GetComponent<Renderer>());
            }

        }

        if (!FarmsAllConstable) {
            foreach (Renderer renderer in previewRenderers)
            {
                renderer.material.color = Color.red;
            }
            Clickable = false;
        }
        else
        {
            if (previewRenderers[0].material.color != Color.gray)
            {
                foreach (Renderer renderer in previewRenderers)
                {
                    renderer.material.color = Color.gray;
                }
            }
        }
            //必要金額表示
            CurrentCost = previewfarms.Count * FarmCostParMas;
        NeedMoneyText.text = CurrentCost.ToString();
    }
        void ClearPreview()
        {
            previewRenderers.Clear();
            foreach (GameObject preview in previewfarms)
            {
                Destroy(preview);
            }
            previewfarms.Clear();
        }

    void FarmGenExecute()
    {
        List<GameObject> farmBlocks = new List<GameObject>();
        foreach(GameObject preview in previewfarms)
        {
            Vector3 genpos = preview.transform.position;    
            GameObject farm = Instantiate(Farm_Brock,genpos, Quaternion.Euler(0f, 0f, 0f), Farm_Brock_Pa);
            //畑管理用マネジャーの畑リストに追加を書く
             farmBlocks.Add(farm);
            Destroy(preview);
        }
        farmManager.FarmBlockAdd(farmBlocks);
        ClearPreview();
        StartCoroutine(RebuildNavMeshNextFrame());
        //お金を引く
        moneyManager.HavingMoneyUpdate(-CurrentCost);
    }
        Vector3 GetSnappedPosition(Vector3 rawPos)
        {
            float gridSize = Farm_Block_Size; // 好きなサイズに
            float x = Mathf.Floor(rawPos.x / gridSize) * gridSize;
            float z = Mathf.Floor(rawPos.z / gridSize) * gridSize;
        return new Vector3(x, Plane.transform.position.y + 1.5f, z);
        }
        Vector2 GetInputPosition()
        {
            // タッチ対応デバイスかつタッチされていればその位置
            if (Input.touchSupported && Input.touchCount > 0)
                return Input.GetTouch(0).position;
            // それ以外はマウス位置
            return Input.mousePosition;
        }

    IEnumerator RebuildNavMeshNextFrame()
    {
        yield return null;
        FindAnyObjectByType<NavMeshSurface>().BuildNavMesh();
    }
}

