using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    //Function: Controls UI which runs during the game

    //Objects & Components:
    public static GameUIManager UIManager;
    private Text cardCount1;
    private Text cardCount1S;
    private Text cardCount2;
    private Text cardCount2S;
    private Text burnCount1;
    private Text burnCount1S;
    private Text burnCount2;
    private Text burnCount2S;
    private Text pileCount;
    private Text pileCountS;
    private GameObject turnArrow1;
    private GameObject turnArrow1S;
    private GameObject turnArrow2;
    private GameObject turnArrow2S;

    private void Awake()
    {
        if (UIManager == null) UIManager = this;
        else Destroy(this);

        //Get text components (the bad way):
        cardCount1S = transform.GetChild(0).GetComponent<Text>();
        cardCount1 = transform.GetChild(1).GetComponent<Text>();
        cardCount2S = transform.GetChild(2).GetComponent<Text>();
        cardCount2 = transform.GetChild(3).GetComponent<Text>();
        burnCount1S = transform.GetChild(4).GetComponent<Text>();
        burnCount1 = transform.GetChild(5).GetComponent<Text>();
        burnCount2S = transform.GetChild(6).GetComponent<Text>();
        burnCount2 = transform.GetChild(7).GetComponent<Text>();
        pileCount = transform.GetChild(8).GetComponent<Text>();
        pileCountS = transform.GetChild(9).GetComponent<Text>();
        turnArrow1S = transform.GetChild(12).gameObject;
        turnArrow1 = transform.GetChild(13).gameObject;
        turnArrow2S = transform.GetChild(14).gameObject;
        turnArrow2 = transform.GetChild(15).gameObject;
    }

    public void UpdateUI()
    {
        //Function: Updates all on-screen UI based on GameDirector values

        //Card counters:
        cardCount1.text = GameDirector.director.hand1.Count.ToString();
        cardCount1S.text = cardCount1.text;
        cardCount2.text = GameDirector.director.hand2.Count.ToString();
        cardCount2S.text = cardCount2.text;
        burnCount1.text = "-" + GameDirector.director.cardsToBurn1.ToString();
        burnCount1S.text = burnCount1.text;
        burnCount2.text = "-" + GameDirector.director.cardsToBurn2.ToString();
        burnCount2S.text = burnCount2.text;
        pileCount.text = GameDirector.director.pile.Count.ToString();
        pileCountS.text = pileCount.text;
        //Turn indicator:
        if (GameDirector.director.turn == Player.Player1)
        {
            turnArrow1.SetActive(true);
            turnArrow1S.SetActive(true);
            turnArrow2.SetActive(false);
            turnArrow2S.SetActive(false);
        }
        else
        {
            turnArrow1.SetActive(false);
            turnArrow1S.SetActive(false);
            turnArrow2.SetActive(true);
            turnArrow2S.SetActive(true);
        }
    }
}
