using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameDirector : MonoBehaviour
{
    //Created: 10-2-21
    //Purpose: Governing core (mechanical) gameplay functions in Play scene

    //Classes, Enums & Structs:
    public enum Suit { Ace, Spade, Diamond, Heart }
    public enum PileStatus { Neutral, Slappable, Collectible1, Collectible2 }
    public enum Player { Player1, Player2 }
    [System.Serializable] public class Card
    {
        //Created: 10-2-21
        //Purpose: Storing data for differentiating individual cards

        //Card Properties:
        public Suit suit { get; set; }
        public int number { get; set; }
        public bool isFaceCard;
        public Player owner;
    }

    //Objects & Components:
    public static GameDirector director;
    public List<Card> pile;
    public List<Card> hand1;
    public List<Card> hand2;
    private readonly System.Random rnd = new System.Random();

    //Settings:
    [Header("Game Settings:")]
    public bool singlePlayer;                //Whether or not the game is in singleplayer mode
    public bool redTens;                     //Whether or not the optional rule of red 10s always being slappable is activated
    [Range(1, 51)] public int handicap = 26; //The number of cards that will be dealt to P1 next time hands are generated

    //Conditions & Memory Vars:
    [Header("[Temp Exposed]")]
    public PileStatus pileStatus;            //Determines active behavior when players try to play/slap cards
    public Player nextToPlay;                //The player who is next up to play a card
    [ShowOnly] public int faceTriesLeft = 0; //How many tries the current player has left to beat the current Face Card Contest (0 if NA)

    //Debug Controls:
    [Space()]
    public bool playCard1 = false;
    public bool playCard2 = false;
    public bool take1 = false;
    public bool take2 = false;

    private void Awake()
    {
        //Initialize as sole director:
        if (director == null)
        {
            DontDestroyOnLoad(gameObject);
            director = this;
        }
        else
        {
            Destroy(gameObject);
        }

        //Set up cards:
        GenerateHands();
    }
    private void Update()
    {
        
    }

    private void GenerateHands()
    {
        //Function: Creates a deck of 52 cards in random order, then deals to player hands.  Also clears all existing cards from game

        //Initialize & clear existing cards:
        List<Card> deck = new List<Card>();
        pile = new List<Card>();
        hand1 = new List<Card>();
        hand2 = new List<Card>();

        //Generate instance for each card:
        Suit currentSuit = Suit.Ace;
        for (int s = 0; s < 4; s++)
        {
            for (int n = 1; n <= 13; n++)
            {
                Card newCard = new Card() { suit = currentSuit, number = n };
                if (n == 1 || n > 10) newCard.isFaceCard = true; //Indicate that card is an Ace (1), a Jack (11), a Queen (12), or a King (13)
                deck.Add(newCard);
            }
            currentSuit++;
        }

        //Shuffle and deal:
        deck = ShuffleCards(deck);
        for (int n = 0; n < deck.Count; n++)
        {
            Card currentCard = deck[n];
            if (n < handicap)
            {
                currentCard.owner = Player.Player1;
                hand1.Add(currentCard); //Determine hand size based on handicap setting, default is 26
            }
            else
            {
                currentCard.owner = Player.Player2;
                hand2.Add(currentCard);
            }
        }
    }
    private List<Card> ShuffleCards(List<Card> cards)
    {
        //Function: Randomly reorders given list of cards

        //Initialization & safety check:
        int quantity = cards.Count;
        if (quantity == 0) return cards;
        List<Card> shuffledCards = cards;

        //Rearrange deck (using Fisher-Yates shuffle):
        Card currentCard;
        for (int index = 0; index < quantity; index++)
        {
            int randomSpot = index + (int)(rnd.NextDouble() * (quantity - index - 1));
            currentCard = shuffledCards[randomSpot];
            shuffledCards[randomSpot] = shuffledCards[index];
            shuffledCards[index] = currentCard;
        }
        return shuffledCards;
    }
    private void PlayCard(Player player)
    {
        //Function: Plays the top card from the given player's hand to the pile, then processes the results and sets game behavior

        Card playedCard;
        if (player == Player.Player1)
        {
            playedCard = hand1[0];
        }
        else
        {
            playedCard = hand2[0];
        }
    }
}
