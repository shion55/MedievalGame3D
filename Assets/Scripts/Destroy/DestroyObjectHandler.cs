using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyObjectHandler : MonoBehaviour
{
    public GameObject smokePrefab;
    public UIController uiController;
    public bool IsDestroyMode { get; private set; }
    public TreeManager treeManager;
    public BuildingManager BuildingManager;
    public HouseAndVillager houseAndvillager;

    private HashSet<GameObject> destroyingObjects =new HashSet<GameObject>();
    public void DestroyModeOn()
    {
        IsDestroyMode = true;
        uiController.OpenDestroyModeUI();
        Debug.Log("îjâÛÉÇÅ[Éhon");
    }
    public void DestroyModeOff()
    {
        IsDestroyMode = false;
        uiController.CloseDestroyModeUI();
        Debug.Log("îjâÛÉÇÅ[Éhoff");
    }

    private void Update()
    {
        if (!IsDestroyMode)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        Ray ray =
            Camera.main.ScreenPointToRay(
                Input.mousePosition
            );

        if (Physics.Raycast(
                ray,
                out RaycastHit hit))
        {
            //ñÿ
            if (hit.collider.transform.IsChildOf(treeManager.treeparent))
            {
                Transform treeTransform = hit.collider.transform;

                while (treeTransform.parent != treeManager.treeparent)
                {
                    treeTransform = treeTransform.parent;
                }

                GameObject tree = treeTransform.gameObject;
                if (!treeManager.currentrees.Contains(tree))
                {
                    return;
                }

                // îjâÛèàóùíÜÇ∆ÇµÇƒó\ñÒ
                treeManager.currentrees.Remove(tree);

                StartCoroutine(DestroyTree(tree));

                return;
            }

            // êEã∆åöï®Ç»Ç«
            BuildingData buildingData =hit.collider.GetComponentInParent<BuildingData>();

            if (buildingData != null)
            {
                Debug.Log("åöï®: " + buildingData.building.name);
                return;
            }

            // â∆
            House house =
                hit.collider.GetComponentInParent<House>();

            if (house != null)
            {
                
                
                GameObject houseobj = house.myobj;
                if (destroyingObjects.Contains(houseobj))
                {
                    return;
                }

                destroyingObjects.Add(houseobj);
                StartCoroutine(DestroyHouse(houseobj));
                return;
            }

        }
    }
    private IEnumerator DestroyTree(GameObject tree)
    {
        StartCoroutine(PlayDestroySmoke(tree.transform.position));

        yield return new WaitForSeconds(1.5f);
        treeManager.Trees.Remove(tree);

        Destroy(tree);
    }
    private IEnumerator DestroyHouse(GameObject house)
    {
        StartCoroutine(PlayDestroySmoke(house.transform.position));

        yield return new WaitForSeconds(1.5f);
        destroyingObjects.Remove(house);
        houseAndvillager.RemoveHouse(house);
    }
    private IEnumerator PlayDestroySmoke(Vector3 position)
    {
        GameObject smokeObject =
            Instantiate(
                smokePrefab,
                position,
                Quaternion.identity
            );

        ParticleSystem smoke =
            smokeObject.GetComponent<ParticleSystem>();

        smoke.Play();

        yield return new WaitForSeconds(1.5f);

        smoke.Stop();

        Destroy(smokeObject);
    }

}
