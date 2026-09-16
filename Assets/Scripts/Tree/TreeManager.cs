using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeManager : MonoBehaviour
{
    public Transform treeparent; // 親オブジェクトを設定
    public List<GameObject> Trees = new List<GameObject>();
    [HideInInspector]
    public List<GameObject> currentrees = new List<GameObject>();
    // Start is called before the first frame update
    void Start()
    {
        foreach (Transform child in treeparent)
        {
            Trees.Add(child.gameObject);
            currentrees.Add(child.gameObject);
        }
    }
    public void delitetree(GameObject tree)
    {
        Destroy(tree);
    }
}
