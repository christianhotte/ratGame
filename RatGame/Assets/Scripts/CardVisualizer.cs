using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardVisualizer : MonoBehaviour
{
    //Created: 10-9-21
    //Purpose: Generating card objects with prefabricated elements and displaying/moving/hiding them as needed according to the InputManager and GameDirector

    //Classes, Enums & Structs:
    public class CardAvatar
    {
        //Created: 10-9-21
        //Purpose: Storing real-time data for a physical version of a played card

        public GameObject cardObject;
    }

    //Objects & Components:
    public CardSpriteSet spriteSet; //The card sprite set to use when generating cards
    public GameObject cardPrefab;   //The card prefab to instantiate and build off of

    //Settings:
    public Vector2 iconMaxSpread; //Max units from center an icon can be
    [Space()] //Temp debug stuff:
    public bool generateDebugCard;
    public int debugCardNumber;
    public Suit debugCardSuit;

    //Conditions & Memory Vars:
    private List<CardAvatar> cardAvatars = new List<CardAvatar>(); //List of all currently-rendered cards in scene

    private void Update()
    {
        if (generateDebugCard)
        {
            GenerateCard(debugCardSuit, debugCardNumber);
            generateDebugCard = false;
        }
    }

    public void GenerateCard(Suit suit, int number)
    {
        //Setup Object:
        GameObject newCard = Instantiate<GameObject>(cardPrefab, transform); //Create new card avatar object\
        //Assign Card Elements:
        Sprite icon = null; //Initialize variable to store card icon
        switch (number) //Get primary icon for card (may be face icon or just regular)
        {
            case 1:  //Ace
                icon = spriteSet.aceIcons[(int)suit]; //Get ace icon based on suit
                break;
            case 11: //Jack
                icon = spriteSet.jackIcons[(int)suit]; //Get jack icon based on suit
                break;
            case 12: //Queen
                icon = spriteSet.queenIcons[(int)suit]; //Get queen icon based on suit
                break;
            case 13: //King
                icon = spriteSet.kingIcons[(int)suit]; //Get king icon based on suit
                break;
            default:
                icon = spriteSet.suitIcons[(int)suit]; //Get non-face icon based on suit
                break;
        }
        newCard.transform.Find("Icon").GetComponent<SpriteRenderer>().sprite = icon; //Assign icon sprite
        newCard.transform.Find("Numeral").GetComponent<SpriteRenderer>().sprite = spriteSet.numeralLabels[number - 1]; //Assign numeral label sprite
        newCard.transform.Find("Suit").GetComponent<SpriteRenderer>().sprite = spriteSet.suitLabels[(int)suit]; //Assign suit label sprite
        if ((int)suit > 1) newCard.transform.Find("Numeral").GetComponent<SpriteRenderer>().color = Color.red; //Change numeral color to red if suit is red
        //Mirror Suit & Numeral Labels:
        GameObject reverseLabel = Instantiate<GameObject>(newCard.transform.Find("Numeral").gameObject, newCard.transform);
        reverseLabel.transform.Rotate(Vector3.forward, 180);
        reverseLabel = Instantiate<GameObject>(newCard.transform.Find("Suit").gameObject, newCard.transform);
        reverseLabel.transform.Rotate(Vector3.forward, 180);
        //Propagate & Position Icons:
        if (number > 1 && number <= 10) //Only do this step for non-face cards
        {
            List<Transform> icons = new List<Transform>(); //Create list to store/keep track of new icons
            icons.Add(newCard.transform.Find("Icon")); //Add first icon to list
            for (int i = 0; i < number - 1; i++) //Add one icon for each numeral above 1
            {
                Transform newIcon = Instantiate<GameObject>(icons[0].gameObject, newCard.transform).transform; //Instantiate a copy of first icon
                icons.Add(newIcon); //Add newly-made icon to list
            }
            if (number < 4) //Placement for first 2 icons on cards 2 and 3
            {
                icons[0].transform.Translate(0, iconMaxSpread.y, 0);
                icons[1].transform.Translate(0, -iconMaxSpread.y, 0);
                icons[1].transform.Rotate(0, 0, 180);
            }
            else //Placement for first 4 icons on all other numbered cards
            {
                icons[0].transform.Translate(iconMaxSpread.x, iconMaxSpread.y, 0);
                icons[1].transform.Translate(iconMaxSpread.x, -iconMaxSpread.y, 0);
                icons[2].transform.Translate(-iconMaxSpread.x, iconMaxSpread.y, 0);
                icons[3].transform.Translate(-iconMaxSpread.x, -iconMaxSpread.y, 0);
                icons[1].transform.Rotate(0, 0, 180);
                icons[3].transform.Rotate(0, 0, 180);

                if (number > 5) //Placement for 5th and 6th icons on other cards
                {
                    if (number < 9) //Placement for 5th and 6th icons on other cards under 9
                    {
                        icons[4].transform.Translate(iconMaxSpread.x, 0, 0);
                        icons[5].transform.Translate(-iconMaxSpread.x, 0, 0);

                        if (number > 7) //Placement for 7th and 8th icons on 8 card
                        {
                            icons[6].transform.Translate(0, iconMaxSpread.y / 2, 0);
                            icons[7].transform.Translate(0, -iconMaxSpread.y / 2, 0);
                            icons[7].transform.Rotate(0, 0, 180);
                        }
                    }
                    else //Placement for icons 5-8 on cards above 8
                    {
                        icons[4].transform.Translate(iconMaxSpread.x, iconMaxSpread.y / 3, 0);
                        icons[5].transform.Translate(iconMaxSpread.x, -iconMaxSpread.y / 3, 0);
                        icons[6].transform.Translate(-iconMaxSpread.x, iconMaxSpread.y / 3, 0);
                        icons[7].transform.Translate(-iconMaxSpread.x, -iconMaxSpread.y / 3, 0);
                        icons[5].transform.Rotate(0, 0, 180);
                        icons[7].transform.Rotate(0, 0, 180);

                        if (number == 10) //Placement for icons 9 and 10 on 10 card
                        {
                            icons[8].transform.Translate(0, iconMaxSpread.y / 1.5f, 0);
                            icons[9].transform.Translate(0, -iconMaxSpread.y / 1.5f, 0);
                            icons[9].transform.Rotate(0, 0, 180);
                        }
                    }
                }
            }
        }
    }
}
