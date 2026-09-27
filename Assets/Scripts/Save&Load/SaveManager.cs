using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

//セーブするデータ達

[Serializable]
public class MaterialSaveData
{
    public MaterialType type;
    public int amount;
}

[Serializable]
public class BuildingSaveData
{
    // 村人から参照するためのID
    public int id;

    // House / JobBuilding / AmuseBuilding
    public ConstCategory category;

    // Houseの場合は None
    public BuildingType buildingType;

    public Vector3 position;
    public Quaternion rotation;

    // 建物が持っている資材
    public List<MaterialSaveData> storage =
        new List<MaterialSaveData>();
}
[Serializable]
public class VillagerSaveData
{
    public int id;

    public Vector3 position;
    public Quaternion rotation;

    public Job job;

    // 所属する家
    public int houseId;

    // 無職など職場が無い場合は -1
    public int jobBuildingId;

    public float hungerLevel;
}
[Serializable]
public class SaveData
{
    public int money;

    public List<BuildingSaveData> buildings =
        new List<BuildingSaveData>();

    public List<VillagerSaveData> villagers =
        new List<VillagerSaveData>();
}
public class SaveManager : MonoBehaviour
{
    [SerializeField]
    private MoneyManager moneyManager;

    [SerializeField]
    private BuildingManager buildingManager;

    [SerializeField]
    private HouseAndVillager houseAndVillager;

    [SerializeField]
    private VIllagerStatusManager statusManager;

    [SerializeField]
    private ConstructioinManager constructionManager;


    //村人をロードするとき使う
    private readonly Dictionary<int, GameObject>　loadedBuildings =new Dictionary<int, GameObject>();
    private readonly List<VillagerBase> loadedVillagers = new List<VillagerBase>();
    public static bool LoadRequested = false;

   

    private string SavePath =>
        Path.Combine(
            Application.persistentDataPath,
            "save.json"
        );

    private IEnumerator Start()
    {
        // MoneyManagerなどのStartが終わるのを1フレーム待つ
        yield return null;

        if (LoadRequested)//loadするなら
        {
            LoadRequested = false;
            LoadGame();
        }
    }
    public void SaveGame()
    {
        SaveData data =
            new SaveData();


        data.money =
            moneyManager.HavingMoney;


        // 建物を保存
        Dictionary<GameObject, int> buildingIds =
            SaveBuildings(data);

        SaveVillagers(data,buildingIds);
        string json =
            JsonUtility.ToJson(
                data,
                true
            );


        File.WriteAllText(
            SavePath,
            json
        );


        Debug.Log(
            $"セーブしました: {SavePath}"
        );
    }


    public void LoadGame()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log(
                "セーブデータがありません"
            );

            return;
        }


        string json =
            File.ReadAllText(
                SavePath
            );


        SaveData data =
            JsonUtility.FromJson<SaveData>(
                json
            );


        // 現在との差額だけMoneyManagerに渡す
        int difference =
            data.money -
            moneyManager.HavingMoney;


        moneyManager.HavingMoneyUpdate(
            difference
        );

        LoadBuildings(data);

        LoadVillagers(data);

        FinishLoad();

        Debug.Log(
            $"ロードしました Money = {data.money}"
        );
    }

    private void FinishLoad()
    {
        Physics.SyncTransforms();


        if (constructionManager.surface != null)
        {
            constructionManager.surface.BuildNavMesh();
        }


        foreach (VillagerBase villager
                 in loadedVillagers)
        {
            if (villager == null)
                continue;


            villager.JobChangeExecute();
        }


        Debug.Log(
            "ロード復元完了"
        );
    }

    private Dictionary<GameObject, int> SaveBuildings(
    SaveData data)
    {
        Dictionary<GameObject, int> buildingIds =
            new Dictionary<GameObject, int>();


        int nextId = 0;


        // =====================================================
        // 家
        // =====================================================

        foreach (GameObject house
                 in houseAndVillager.houses)
        {
            if (house == null)
                continue;


            BuildingSaveData save =
                new BuildingSaveData();


            save.id =
                nextId++;

            save.category =
                ConstCategory.House;

            save.buildingType =
                BuildingType.None;

            save.position =
                house.transform.position;

            save.rotation =
                house.transform.rotation;


            data.buildings.Add(
                save
            );


            buildingIds.Add(
                house,
                save.id
            );
        }


        // =====================================================
        // 職業建物
        // =====================================================

        foreach (GameObject building
                 in buildingManager.JobBuildings)
        {
            SaveBuilding(
                building,
                data,
                buildingIds,
                ref nextId
            );
        }


        // =====================================================
        // 娯楽施設
        // =====================================================

        foreach (GameObject building
                 in buildingManager.AmuseBuildings)
        {
            SaveBuilding(
                building,
                data,
                buildingIds,
                ref nextId
            );
        }


        return buildingIds;
    }

    private void SaveBuilding(
    GameObject building,
    SaveData data,
    Dictionary<GameObject, int> buildingIds,
    ref int nextId)
    {
        if (building == null)
            return;


        // 同じ建物を二重保存しない
        if (buildingIds.ContainsKey(building))
            return;


        BuildingData buildingData =
            building.GetComponentsInChildren<BuildingData>()[0];


        if (buildingData == null)
            return;


        BuildingSaveData save =
            new BuildingSaveData();


        save.id =
            nextId++;

        save.category =
            buildingData.buildingcategory;

        save.buildingType =
            buildingData.buildingType;

        save.position =
            building.transform.position;

        save.rotation =
            building.transform.rotation;


        // =====================================================
        // Storage
        // =====================================================

        if (buildingData.storage != null)
        {
            foreach (var pair
                     in buildingData.storage.materials)
            {
                MaterialSaveData material =
                    new MaterialSaveData();


                material.type =
                    pair.Key;

                material.amount =
                    pair.Value;


                save.storage.Add(
                    material
                );
            }
        }


        data.buildings.Add(
            save
        );


        buildingIds.Add(
            building,
            save.id
        );
    }

    private void SaveVillagers(
    SaveData data,
    Dictionary<GameObject, int> buildingIds)
    {
        for (int i = 0;
             i < houseAndVillager.villagers.Count;
             i++)
        {
            GameObject villager =
                houseAndVillager.villagers[i];

            if (villager == null)
                continue;


            VillagerBase villagerBase =
                villager.GetComponent<VillagerBase>();

            VillagerHappiness happiness =
                villager.GetComponent<VillagerHappiness>();


            VillagerSaveData save =
                new VillagerSaveData();


            save.id = i;

            save.position =
                villager.transform.position;

            save.rotation =
                villager.transform.rotation;


            // =========================
            // 職業
            // =========================

            if (statusManager.villagersjob.TryGetValue(
                    villager,
                    out Job job))
            {
                save.job = job;
            }
            else
            {
                save.job =
                    Job.UnEmployer;
            }


            // =========================
            // 家
            // =========================

            save.houseId = -1;


            if (villagerBase.Myhouse != null &&
                buildingIds.TryGetValue(
                    villagerBase.Myhouse,
                    out int houseId))
            {
                save.houseId =
                    houseId;
            }


            // =========================
            // 職場
            // =========================

            save.jobBuildingId = -1;


            if (villagerBase.MyJobBuilding != null &&
                buildingIds.TryGetValue(
                    villagerBase.MyJobBuilding,
                    out int jobBuildingId))
            {
                save.jobBuildingId =
                    jobBuildingId;
            }


            // =========================
            // 空腹度
            // =========================

            if (happiness != null)
            {
                save.hungerLevel =
                    happiness.HungerLevel;
            }


            data.villagers.Add(
                save
            );
        }
    }


    private void LoadBuildings(
    SaveData data)
    {
        loadedBuildings.Clear();


        // Sceneに最初からある家を取っておく
        List<GameObject> sceneHouses =
            new List<GameObject>(
                houseAndVillager.houses
            );


        // ロード後のリストを作り直す
        houseAndVillager.houses.Clear();

        buildingManager.JobBuildings.Clear();
        buildingManager.AmuseBuildings.Clear();


        int houseIndex = 0;
        int woodCabinIndex = 0;


        foreach (BuildingSaveData save
                 in data.buildings)
        {
            GameObject building = null;


            // =====================================================
            // 家
            // =====================================================

            if (save.category ==
                ConstCategory.House)
            {
                // Sceneに元からある家が余っていれば再利用
                if (houseIndex <
                    sceneHouses.Count)
                {
                    building =
                        sceneHouses[
                            houseIndex
                        ];

                    building.transform.position =
                        save.position;

                    building.transform.rotation =
                        save.rotation;
                }
                else
                {
                    GameObject prefab =
                        constructionManager
                            .constbuildingmaster
                            .GetData(
                                ConstBuildingType.House
                            )
                            .conbuildingPrefab;


                    building =
                        Instantiate(
                            prefab,
                            save.position,
                            save.rotation,
                            constructionManager
                                .houseParent
                                .transform
                        );
                }


                houseIndex++;


                houseAndVillager.houses.Add(
                    building
                );


                // 税金用コインがまだ無ければ作る
                House house =
                    building.GetComponent<House>();

                if (house != null &&
                    house.Coin == null)
                {
                    moneyManager.GenerateCoin(
                        building
                    );
                }
            }

            // =====================================================
            // Castle
            // =====================================================

            else if (save.buildingType ==
                     BuildingType.castle)
            {
                building =
                    buildingManager.Castle;


                building.transform.position =
                    save.position;

                building.transform.rotation =
                    save.rotation;


                buildingManager.JobBuildings.Add(
                    building
                );
            }

            // =====================================================
            // 最初からSceneにある木こり小屋
            // =====================================================

            else if (save.buildingType == BuildingType.woodcabin)
            {
                bool reusedExistingCabin = false;

                // Sceneに元からある木こり小屋を再利用
                if (woodCabinIndex <
                    buildingManager.woodmanCabins.Count)
                {
                    building =
                        buildingManager
                            .woodmanCabins[
                                woodCabinIndex
                            ];

                    building.transform.position =
                        save.position;

                    building.transform.rotation =
                        save.rotation;

                    woodCabinIndex++;

                    reusedExistingCabin = true;
                }
                else
                {
                    // 2軒目以降はPrefabから生成
                    var masterData =
                        constructionManager
                            .constbuildingmaster
                            .GetData(
                                ConstBuildingType.woodcabin
                            );

                    building =
                        Instantiate(
                            masterData.conbuildingPrefab,
                            save.position,
                            save.rotation,
                            constructionManager
                                .buildingParent
                                .transform
                        );

                    buildingManager
                        .woodmanCabins
                        .Add(building);
                }


                BuildingData buildingData =
                    building.GetComponentsInChildren<BuildingData>()[0];

                buildingData.DataInitialize(
                    ConstCategory.JobBuilding,
                    BuildingType.woodcabin,
                    Job.WoodCutter,
                    building,
                    constructionManager
                        .jobbuildingmaster,
                    null
                );


                buildingManager.JobBuildings.Add(
                    building
                );


                // 新しく生成した木こり小屋だけUI生成
                if (!reusedExistingCabin)
                {
                    constructionManager
                        .worldSpaceUIController
                        .GenerateWorldSpaceBuildingUI(
                            building.transform.position,
                            building
                        );
                }
            }
            // =====================================================
            // それ以外の建物
            // =====================================================

            else
            {
                if (!System.Enum.TryParse(
                        save.buildingType.ToString(),
                        out ConstBuildingType constType))
                {
                    Debug.LogWarning(
                        $"建物を復元できません: {save.buildingType}"
                    );

                    continue;
                }


                var masterData =
                    constructionManager
                        .constbuildingmaster
                        .GetData(
                            constType
                        );


                if (masterData == null ||
                    masterData.conbuildingPrefab == null)
                {
                    continue;
                }


                building =
                    Instantiate(
                        masterData.conbuildingPrefab,
                        save.position,
                        save.rotation,
                        constructionManager
                            .buildingParent
                            .transform
                    );


                BuildingData buildingData =
                    building.GetComponentsInChildren<BuildingData>()[0];


                // -------------------------
                // 職業建物
                // -------------------------

                if (save.category ==
                    ConstCategory.JobBuilding)
                {
                    Job job =
                        constructionManager
                            .jobbuildingmaster
                            .GetDataByBuilding(
                                save.buildingType
                            )
                            .jobType;


                    buildingData.DataInitialize(
                        ConstCategory.JobBuilding,
                        save.buildingType,
                        job,
                        building,
                        constructionManager
                            .jobbuildingmaster,
                        null
                    );


                    buildingManager.JobBuildings.Add(
                        building
                    );


                    // この職業を利用可能にする
                    if (!statusManager
                            .availableJobs
                            .Contains(job))
                    {
                        statusManager
                            .availableJobs
                            .Add(job);
                    }


                    // 農場ならFarmManagerにも登録
                    if (save.buildingType ==
                        BuildingType.farmbuilding)
                    {
                        constructionManager
                            .farmManager
                            .FarmBuildingAdd(
                                building
                            );
                    }


                    constructionManager
                        .worldSpaceUIController
                        .GenerateWorldSpaceBuildingUI(
                            building.transform.position,
                            building
                        );
                }

                // -------------------------
                // 市場など
                // -------------------------

                else if (save.category ==
                         ConstCategory.AmuseBuilding)
                {
                    List<MaterialType> materials =
                        new List<MaterialType>()
                        {
                        MaterialType.Carrot,
                        MaterialType.Venison,
                        MaterialType.Fish
                        };


                    buildingData.DataInitialize(
                        ConstCategory.AmuseBuilding,
                        save.buildingType,
                        null,
                        building,
                        null,
                        materials
                    );


                    buildingManager.AmuseBuildings.Add(
                        building
                    );
                }
            }


            // =====================================================
            // ID登録
            // =====================================================

            if (building == null)
                continue;


            loadedBuildings[
                save.id
            ] = building;


            // =====================================================
            // Storage復元
            // =====================================================

            BuildingData loadedData =
                building.GetComponentsInChildren<BuildingData>()[0];


            if (loadedData != null &&
                loadedData.storage != null)
            {
                // 一旦全部0
                foreach (
                    MaterialType material
                    in System.Enum.GetValues(
                        typeof(MaterialType)
                    ))
                {
                    loadedData
                        .storage
                        .materials[
                            material
                        ] = 0;
                }


                // 保存値を戻す
                foreach (
                    MaterialSaveData material
                    in save.storage)
                {
                    loadedData
                        .storage
                        .materials[
                            material.type
                        ] =
                        material.amount;
                }
            }
        }


        Debug.Log(
            $"建物ロード完了: {loadedBuildings.Count}"
        );
    }

    private void LoadVillagers(
    SaveData data)
    {
        loadedVillagers.Clear();


        // =====================================================
        // Sceneに最初からいる村人を削除
        // =====================================================

        foreach (GameObject villager
                 in houseAndVillager.villagers)
        {
            if (villager != null)
            {
                Destroy(villager);
            }
        }


        houseAndVillager.villagers.Clear();
        houseAndVillager.villagerhouse.Clear();
        houseAndVillager.housevillagers.Clear();

        statusManager.villagersjob.Clear();


        foreach (var pair
                 in statusManager.jobandvillagers)
        {
            pair.Value.Clear();
        }


        // =====================================================
        // 建物側の所属村人もリセット
        // =====================================================

        foreach (GameObject building
                 in loadedBuildings.Values)
        {
            if (building == null)
                continue;


            BuildingData buildingData =
                building.GetComponentsInChildren<BuildingData>()[0];


            if (buildingData != null)
            {
                buildingData.workers.Clear();
                buildingData.OccupantVillagers.Clear();
            }
        }


        // 各家の住民リストを先に作る
        foreach (GameObject house
                 in houseAndVillager.houses)
        {
            houseAndVillager.housevillagers[
                house
            ] =
                new List<GameObject>();
        }


        // =====================================================
        // 村人生成
        // =====================================================

        foreach (VillagerSaveData save
                 in data.villagers)
        {
            // -----------------------------
            // 家
            // -----------------------------

            if (!loadedBuildings.TryGetValue(
                    save.houseId,
                    out GameObject house))
            {
                Debug.LogWarning(
                    $"村人 {save.id} の家が見つかりません"
                );

                continue;
            }


            // -----------------------------
            // 保存位置の近くのNavMeshへ補正
            // -----------------------------

            Vector3 spawnPosition =
                save.position;


            if (NavMesh.SamplePosition(
                    save.position,
                    out NavMeshHit navHit,
                    5f,
                    NavMesh.AllAreas))
            {
                spawnPosition =
                    navHit.position;
            }
            else
            {
                // 保存位置が使えなかったら家へ戻す
                Transform entrance =
                    house.transform.Find(
                        "Entrance"
                    );


                spawnPosition =
                    entrance != null
                        ? entrance.position
                        : house.transform.position;
            }


            GameObject villager =
                Instantiate(
                    houseAndVillager.villagerprefab,
                    spawnPosition,
                    save.rotation,
                    houseAndVillager
                        .villagerParent
                        .transform
                );


            VillagerBase vb =
                villager.GetComponent<VillagerBase>();


            vb.MyIndex =
                save.id;

            vb.Myhouse =
                house;

            // ロード後は仕事を再開させるので
            // 「家で待機中」扱いにはしない
            vb.IsAtHome =
                false;


            // =================================================
            // HouseAndVillagerへ登録
            // =================================================

            houseAndVillager.villagers.Add(
                villager
            );


            houseAndVillager.villagerhouse[
                villager
            ] =
                house;


            houseAndVillager.housevillagers[
                house
            ].Add(
                villager
            );


            // =================================================
            // 空腹度
            // =================================================

            VillagerHappiness happiness =
                villager.GetComponent<
                    VillagerHappiness
                >();


            if (happiness != null)
            {
                happiness.HungerLevel =
                    save.hungerLevel;
            }


            // =================================================
            // 職場
            // =================================================

            GameObject jobBuilding =
                null;


            if (save.jobBuildingId >= 0)
            {
                loadedBuildings.TryGetValue(
                    save.jobBuildingId,
                    out jobBuilding
                );
            }


            // =================================================
            // StatusManagerへ登録
            // =================================================

            statusManager.villagersjob[
                villager
            ] =
                save.job;


            if (statusManager
                .jobandvillagers
                .ContainsKey(save.job))
            {
                statusManager
                    .jobandvillagers[
                        save.job
                    ]
                    .Add(villager);
            }


            // =================================================
            // 職場側workers
            // =================================================

            if (jobBuilding != null)
            {
                BuildingData buildingData =
                    jobBuilding.GetComponentsInChildren<BuildingData>()[0];


                if (buildingData != null)
                {
                    buildingData.workers.Add(
                        villager
                    );
                }
            }


            // JobChangeExecuteはまだしない
            // NavMeshを作り直してから仕事を開始する
            vb.JobChange(
                save.job,
                jobBuilding
            );


            loadedVillagers.Add(
                vb
            );
        }


        Debug.Log(
            $"村人ロード完了: {loadedVillagers.Count}"
        );
    }
}