using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/CardSpriteSet", order = 1)]
public class CardSpriteSet : ScriptableObject
{
    //Created: 10-9-21
    //Purpose: Organizing the set of sprites which is used by the CardVisualizer to fabricate cards

    [Header("Card Components:")] //Card components are placed on prefabricated templates to form fully-visualized cards
    public Sprite cardFront;
    public Sprite cardBack;
    [Space()]
    //NOTE: All arrays should be organized in this order: Spades, Clubs, Diamonds, Hearts
    public Sprite[] suitIcons =     new Sprite[4];
    public Sprite[] suitLabels =    new Sprite[4];
    public Sprite[] numeralLabels = new Sprite[13];
    public Sprite[] jackIcons =     new Sprite[4];
    public Sprite[] queenIcons =    new Sprite[4];
    public Sprite[] kingIcons =     new Sprite[4];
    public Sprite[] aceIcons =      new Sprite[4];
}
