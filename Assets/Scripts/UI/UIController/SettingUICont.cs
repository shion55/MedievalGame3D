using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
public class SettingUICont : MonoBehaviour
{
    UIController UC;

    public GameObject SettingUI; // UIƒpƒlƒ‹
    public GameObject SettingButton;
    public Slider VSPeedSlider;

    public float currentvillagerspeed = 3f;
    // Start is called before the first frame update
    void Start()
    {
        UC = this.GetComponent<UIController>();
        VSPeedSlider.onValueChanged.AddListener(OnSpeedChanged);
    }
    // Update is called once per frame
   void OnSpeedChanged(float newspeed)
    {
        foreach(GameObject villager in UC.houseandvillager.villagers)
        {
            NavMeshAgent agent = villager.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = newspeed;
            }
        }
        currentvillagerspeed = newspeed;
    }

    public void OpenSettingUI()
    {
        SettingUI.SetActive(true);
        UC.temporaryUI.Add(SettingUI);
    }
}
