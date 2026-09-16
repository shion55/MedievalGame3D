using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public UIController uicontroller;
    public ConstructioinManager constmanager;

  
    public Transform Castle;
    public int InitialMoney = 100;

    public int CollectTaxDuration = 60;

    public int HavingMoney;
    [Header("ƒRƒCƒ“‚Ì‹““®")]
    public Transform CoinsParent;
    public GameObject CoinPrefab;
    public float coinfloatHeight = 1.0f;     // Å‰‚Ì‚Ó‚í‚Á‚Æ‚‚³
    public float coinfloatDuration = 0.3f;   // ‚Ó‚í‚Á‚Æã‚ª‚éŠÔ
    public float coinwaitTime = 0.2f;        // ‚Ó‚í‚Á‚ÆŒã‚Ì‘Ò‹@ŠÔ
    public float coinflightTime = 1.0f;
    public float coinarcHeight = 2.0f;
    [Header("Å—¦“™")]
    public int taxamount = 50;
    public float taxrate;
    
    private void Start()
    {
        uicontroller.UpdateWallet(InitialMoney);
        HavingMoney = InitialMoney;
    }
    public void GenerateCoin(GameObject house)
    {
        House HOUSE = house.GetComponent<House>();
        Vector3 pos = house.transform.position;
        pos.x -= 0.5f;
        Vector3 genpos = pos;
        HOUSE.Coin = Instantiate(CoinPrefab, house.transform.position, Quaternion.Euler(90, 0, 0), CoinsParent);
    }
    public void CollectTax(GameObject house)//‚¨‹à‚ÌˆÚ“®ˆ—
    {
        House HOUSE = house.GetComponent<House>();
        GameObject coin = HOUSE.Coin;
        MeshRenderer coinRender = coin.GetComponent<MeshRenderer>();

        if( coinRender.enabled == false)
        {
            coinRender.enabled = true;
        }
        Vector3 pos = house.transform.position;
        Quaternion lotate = house.transform.rotation;
        pos.x -= 0.5f;
        Vector3 startPos = pos;

        float rotatetime = coinflightTime / 5;

        Sequence seq = DOTween.Sequence();

        Vector3 upPos = startPos + Vector3.up * coinfloatHeight; // ­‚µã‚É•‚‚©‚¹‚é

        Vector3 midPoint = (upPos + Castle.position) / 2;
        midPoint.y += coinarcHeight;
  
        // ‚Ü‚¸•‚‚©‚¹‚Ä­‚µ~‚ß‚é
        seq.Append(coin.transform.DOMove(upPos, coinfloatDuration).SetEase(Ease.OutQuad));
        seq.AppendInterval(coinwaitTime);
        seq.Append(coin.transform.DOPath(new Vector3[] { upPos, midPoint, Castle.position }, coinflightTime, PathType.CatmullRom).SetEase(Ease.InOutQuad)).
            Join(coin.transform.DORotate(new Vector3(0, 0, 360f),rotatetime,RotateMode.FastBeyond360).SetLoops(5,LoopType.Restart).SetEase(Ease.Linear));

        // ’…‚¢‚½‚çŒ³‚ÌˆÊ’u‚É–ß‚µ‚Ä”ñ•\¦
        seq.OnComplete(() =>
        {
            //DebugController.Log("ƒRƒCƒ“ˆÚ“®Š®—¹");
            coin.transform.DOKill(); // ‰ñ“]’â~
            coin.transform.rotation = lotate;
            coin.transform.position = startPos;
            coinRender.enabled = false;
            HavingMoneyUpdate(taxamount);
         
        });

        
    }

    public void HavingMoneyUpdate(int money)
    {
        //‚¨‹à‚ğŠi”[
        HavingMoney += money;
        uicontroller.UpdateWallet(money);
    }
}
