using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobManager : MonoBehaviour
{
    public DeerSpawner deerSpawner;


    public Transform DeersParent;
   
    public List<GameObject> CountDeers()
    {
        List<GameObject> list = new List<GameObject>();
        foreach (Transform t in DeersParent.transform)
        {
            list.Add(t.gameObject);
        }
        return list;
    }
}
