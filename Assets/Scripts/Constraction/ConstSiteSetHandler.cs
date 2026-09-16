using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class ConstSiteSetHandler : MonoBehaviour
{
    public UIController uiController;
    public ConstructioinManager constManager;
    public ConstBuildingMasterDataSO constbuildingmaster;
    public ConstSetLine constsetline;

    public GameObject Plane;
    public GameObject Site;
    public Transform PreviewObjParent;

    [HideInInspector]
    public bool SiteGenMode = false;
    public float checkRadius = 5f;//建物の周りに建てられない範囲

    [Header("釣り人小屋の設置範囲用Collider")]
    public GameObject PondCollider;
    [SerializeField] private LayerMask hitLayers;

    public Material previewMaterial;

    public Button SiteSetButton;
    public Button SiteSetStopButton;
    private bool CanSetSite = false;

    private Vector3 snapPos;

    private Dictionary<ConstBuildingType, GameObject> PreviewObjects = new Dictionary<ConstBuildingType, GameObject>();
    private ConstBuildingType buildingNowType;
    private GameObject NowPreviewPrefab;
    private List<MeshRenderer> NowRenderers = new List<MeshRenderer>();
    private Vector3 startpos;

    private void Start()
    {
        foreach(ConstBuildingType constbuilding in Enum.GetValues(typeof(ConstBuildingType)))
        {
           GameObject prefab = constbuildingmaster.GetData(constbuilding).conbuildingPrefab;
            GameObject building = Instantiate(prefab, new Vector3(20, 20, 20), prefab.transform.rotation, PreviewObjParent);
            //普通の建物のprefabをpreview用に加工していく
            foreach (var renderer in building.GetComponentsInChildren<Renderer>())
            {
                var mats = renderer.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = previewMaterial;
                }
                renderer.materials = mats; // ← これでちゃんと反映される！

                renderer.enabled = false;
            }
            // 不要なスクリプトを無効にする
            foreach (var comp in building.GetComponentsInChildren<MonoBehaviour>())
            {
                comp.enabled = false;
            }
            foreach (var col in building.GetComponentsInChildren<Collider>())
            {
                col.isTrigger = true;
            }
            foreach (var nav in building.GetComponentsInChildren<NavMeshObstacle>())
            {
                nav.enabled = false;
            }
            PreviewObjects.Add(constbuilding, building);//建物の種類とプレビューオブジェクトの辞書
        }
        
        SiteSetButton.gameObject.SetActive(false);
        SiteSetStopButton.gameObject.SetActive(false);
        SiteSetButton.onClick.AddListener(() => SetSiteExecute());
        SiteSetStopButton.onClick.AddListener(() => SiteGenModeOff());
    }
    void Update()
    {
        if (SiteGenMode)
        {
            SetSite();
        }
    }

    public void SiteGenModeOn(ConstBuildingType type)//UIControllerから呼ばれる
    {
        buildingNowType = type;
        CanSetSite = false;
        NowPreviewPrefab = PreviewObjects[type];//プレビューオブジェクト

        NowRenderers.Clear();
        NowRenderers.AddRange(NowPreviewPrefab.GetComponentsInChildren<MeshRenderer>());

        NowRenderers.ForEach(rend => rend.enabled = true);
        startpos = NowPreviewPrefab.transform.position;//線
        
        Collider collider = NowPreviewPrefab.GetComponent<Collider>();
        collider.enabled = true;
        Vector3 collidersize =  collider.bounds.size;
        checkRadius = Mathf.Max(collidersize.x, collidersize.z)/2;
        collider.enabled = false;
        constsetline.OnConstGide(collidersize);//線をオンに


        SiteGenMode = true;

        SiteSetStopButton.gameObject.SetActive(true);
        DebugController.Log("建設地決定モードオン");
    }
    public void SiteGenModeOff()
    {
        SiteGenMode = false;
        uiController.CloseUI();
        NowRenderers.ForEach(rend => rend.enabled = false);
        NowPreviewPrefab.transform.position = startpos;
        
        constsetline.OffConstGide();
        SiteSetButton.gameObject.SetActive(false);
        SiteSetStopButton.gameObject.SetActive(false);
        DebugController.Log("建設地決定モードオフ");
    }
    private void SetSite()
    {
        Ray ray = Camera.main.ScreenPointToRay(GetInputPosition());
        RaycastHit hit;
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        bool isPlane = false;
        bool isPond = false;
        foreach (var item in hits)
        {
            if(item.collider.gameObject == Plane)
            {
                isPlane = true;
            }
            else if(item.collider.gameObject == PondCollider)
            {
                isPond = true;
            }
        }
        if (Physics.Raycast(ray, out hit))
        {
            snapPos = GetSnappedPosition(hit.point);
                        
            NowPreviewPrefab.transform.position = snapPos;
            Vector3 offset = Vector3.zero;
            Vector3 lineposition= snapPos;
            //各プレハブごとの微妙な調整
            if (buildingNowType == ConstBuildingType.sawmill || buildingNowType == ConstBuildingType.mine 
                )
            {
                offset.y += 1.15f;
            }
            else if(buildingNowType == ConstBuildingType.huntercabin)
            {
               offset.y += 1.4f;
            }
            else if(buildingNowType == ConstBuildingType.House)
            {
                offset.x += 1.2f;
                offset.z -= 0.5f;
            }
            else if(buildingNowType == ConstBuildingType.market)
            {
                offset.y -= 5.3f;
                lineposition = new Vector3(lineposition.x + 1.3f, lineposition.y, lineposition.z + 1.3f);
            }
            NowPreviewPrefab.transform.position += offset;
           
           
            //建築可能
            //釣り人小屋は湖colliderのフチのみに設置可能
            if (buildingNowType == ConstBuildingType.fishmancabin)
            {
                BoxCollider col = PondCollider.GetComponent<BoxCollider> ();
                Vector3 snappedpondpos = col.ClosestPoint(snapPos);//湖colliderのフチに移動
                Vector3 pondcenter = col.bounds.center;
                Vector3 extents = Vector3.Scale(col.size * 0.5f, col.transform.lossyScale);
                
                if (isPond)//カーソル等がが湖内部にいる場合
                {
                    Vector3 dir = (snapPos - pondcenter).normalized;
                    if(dir.sqrMagnitude > 0f)
                    {
                        // 方向ベクトルの絶対値を取って、各軸のエクステントに乗算
                        Vector3 absDir = new Vector3(Mathf.Abs(dir.x), Mathf.Abs(dir.y), Mathf.Abs(dir.z));
                        // ボックスの境界までの距離は extents と absDir のドット積
                        float distance = Vector3.Dot(extents, absDir);

                        Vector3 boundaryPos = pondcenter + dir * distance;
                        snappedpondpos = boundaryPos;
                    }
                }

                snappedpondpos.y+= 2f;
                NowPreviewPrefab.transform.position = snappedpondpos;//移動させて
                lineposition = snappedpondpos;
                Vector3 lookTarget = new Vector3(pondcenter.x, snappedpondpos.y, pondcenter.z);
                NowPreviewPrefab.transform.LookAt(lookTarget);//湖の中心に向かせる
              
                    NowRenderers.ForEach(rend => rend.material.color = Color.grey);
                    if (Input.GetMouseButtonDown(0))
                    {
                        GameObject SiteObj = Instantiate(Site, hit.point, Site.transform.rotation);
                        constManager.waitingSite.Add(SiteObj);
                        constManager.ConstractionSetting(SiteObj, buildingNowType, NowPreviewPrefab.transform.position);
                        SiteGenMode = false;
                        SiteGenModeOff();
                    }
             
            }
            //通常の建物
            else if (isPlane && !Physics.CheckSphere(snapPos, checkRadius, hitLayers))
            {
                
                SiteSetButton.gameObject.SetActive(true);
                CanSetSite = true;
                if (NowRenderers[0].material.color != Color.grey)
                {
                    NowRenderers.ForEach(rend => rend.material.color = Color.grey);
                }
                if (Input.GetMouseButtonDown(0))
                {
                    GameObject SiteObj = Instantiate(Site, hit.point, Site.transform.rotation);
                    constManager.waitingSite.Add(SiteObj);
                    constManager.ConstractionSetting(SiteObj, buildingNowType, NowPreviewPrefab.transform.position);
                    SiteGenMode = false;
                    SiteGenModeOff();
                }
            }
            else//建築不可
            {
                CanSetSite = false;
                SiteSetButton.gameObject.SetActive(false);
                //赤くする
                NowRenderers.ForEach(rend => rend.material.color = Color.red);
            }

            constsetline.UpdateConstGideLines(lineposition);
        }

       
    }
    void SetSiteExecute()
    {
        if (CanSetSite) {

            DebugController.Log("建設地生成");
            GameObject SiteObj = Instantiate(Site,snapPos, Site.transform.rotation);
            constManager.waitingSite.Add(SiteObj);
            constManager.ConstractionSetting(SiteObj, buildingNowType, snapPos);
            SiteGenMode = false;
            SiteGenModeOff();
        }
    }

    Vector3 GetSnappedPosition(Vector3 rawPos)
    {
        float gridSize = 1f; // 好きなサイズに
        float x = Mathf.Round(rawPos.x / gridSize) * gridSize;
        float z = Mathf.Round(rawPos.z / gridSize) * gridSize;
        return new Vector3(x, Plane.transform.position.y +1.8f, z);
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(snapPos, checkRadius);
    }
    Vector2 GetInputPosition()
    {
        // タッチ対応デバイスかつタッチされていればその位置
        if (Input.touchSupported && Input.touchCount > 0)
            return Input.GetTouch(0).position;
        // それ以外はマウス位置
        return Input.mousePosition;
    }
}
