using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstSetLine : MonoBehaviour
{
    public List<GameObject> GideLines = new List<GameObject>();

    private List<LineRenderer> linerenders = new List<LineRenderer>();

    private Vector3 RenderSize;
    private float halfZ;
    private float halfX;
    private float yoffset = 0.1f;
    private void Start()
    {
        foreach (var line in GideLines) { 
             LineRenderer linerenderer = line.GetComponent<LineRenderer>();
            if (linerenderer == null) {
                DebugController.Log("GideLineÇ©ÇÁRendererÇéÊìæÇ≈Ç´Ç‹ÇπÇÒÇ≈ÇµÇΩÅB");
                break;
            }
            linerenders.Add(linerenderer);
        }
    }
    public void OnConstGide(Vector3 size)
    {
        foreach (var line in GideLines)
        {
            line.SetActive(true);
        }
       
            RenderSize = size;
            halfZ = size.z * 0.5f;
            halfX = size.x * 0.5f;
        
    }
    public void OffConstGide()
    {
        foreach (var line in GideLines)
        {
            line.SetActive(false);
        }
    }
    public void UpdateConstGideLines(Vector3 snapPos)
    {
        int lineindex = 0;
        foreach (var line in GideLines) {

            if (lineindex == 0)
            {
                line.transform.position = new Vector3(snapPos.x + halfX, snapPos.y +yoffset,snapPos.z);
            }
            else if (lineindex == 1) {
                line.transform.position = new Vector3(snapPos.x - halfX, snapPos.y+ yoffset, snapPos.z);
            }
            else if (lineindex == 2)
            {
                line.transform.position = new Vector3(snapPos.x, snapPos.y + yoffset, snapPos.z + halfZ);
            }
            else if (lineindex == 3)
            {
                line.transform.position = new Vector3(snapPos.x, snapPos.y + yoffset, snapPos.z - halfZ);
            }
            lineindex++;

        }
    }


}
