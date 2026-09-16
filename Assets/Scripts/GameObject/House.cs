using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class House : MonoBehaviour
{
    public GameObject Coin;
    MoneyManager moneyManager;
    private int paytaxduration;
    void Start()
    {
         moneyManager = GameObject.Find("MONEYMANAGER").GetComponent<MoneyManager>();
        paytaxduration = moneyManager.CollectTaxDuration;
        StartCoroutine(PayTax());

    }
    IEnumerator PayTax()
    {
        yield return new WaitForSeconds(paytaxduration);
        moneyManager.CollectTax(this.gameObject);
        StartCoroutine(PayTax());
    }
}
