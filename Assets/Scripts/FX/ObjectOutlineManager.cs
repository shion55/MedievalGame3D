using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityFx.Outline;

public class ObjectOutlineManager : MonoBehaviour
{
    public int OutLineWidth = 9;
    public float OutLineTime = 5f;
    public void OutLineOn(GameObject obj)
    {
        OutlineBehaviour objOutliner = obj.GetComponent<OutlineBehaviour>();
        objOutliner.OutlineWidth = OutLineWidth;
    }
    IEnumerator OutLineOffCourtine(GameObject obj)
    {
        yield return new WaitForSeconds(OutLineTime);
        OutlineBehaviour objOutliner = obj.GetComponent<OutlineBehaviour>();
        objOutliner.OutlineWidth = 1;
    }
    public void OutLineOff(GameObject obj)
    {
        OutlineBehaviour objOutliner = obj.GetComponent<OutlineBehaviour>();
        objOutliner.OutlineWidth = 1;
    }
}
