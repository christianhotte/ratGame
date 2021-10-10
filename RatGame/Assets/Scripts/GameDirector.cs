using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameDirector : MonoBehaviour
{
    //Created: 10-2-21
    //Purpose: Governing core (mechanical) gameplay functions in Play scene

    //Classes, Enums & Structs:
    [System.Serializable] public class Card
    {
        //Created: 10-2-21
        //Purpose: Storing data for differentiating individual cards

        //Core (Static) Properties:
        public Suit suit { get; set; }
        public int number { get; set; }

        //Meta Properties:
        public Player owner;
        public bool isFaceCard;
        public bool isBurned;
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
    internal bool slappable = false;   //Whether or not the Center Pile is currently slappable
    internal bool collectible = false; //Whether or not the Center Pile is currently collectible (by the player who's turn it is)
    internal bool gameOver = false;    //Whether or not the game/round has ended
    internal Player turn;              //The player who's turn it is to play a card
    internal int faceTriesLeft = 0;    //How many tries the current player has left to beat the current Face Card Contest (0 if NA)

    //Debug Controls:
    [Space()]
    public bool enableLogs;

    private void Awake()
    {
        //Initialize as sole director:
        if (director == null) director = this;
        else Destroy(this);

        //Set up cards:
        GenerateHands();
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
        Suit currentSuit = 0;
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

    public void PlayCard(Player player)
    {
        //Function: Plays the top card from the given player's hand to the pile, then processes the results and sets game behavior based on certain conditions

        //Get card and move it from player hand:
        Card playedCard;
        if (player == Player.Player1) //Remove card from top of hand
        {
            if (hand1.Count == 0) return; //Exit if Player1 has no cards to play
            playedCard = hand1[0];
            hand1.RemoveAt(0);
        }
        else //Remove card from top of hand
        {
            if (hand2.Count == 0) return; //Exit if Player2 has no cards to play
            playedCard = hand2[0];
            hand2.RemoveAt(0);
        }

        //Check where to place card in pile (in case it has been placed out of turn):
        if (player == turn && !collectible) //Card is played by correct player
        {
            pile.Insert(0, playedCard); //Add card to top of pile
            if (enableLogs) Debug.Log(player + " played " + playedCard.number + " of " + playedCard.suit + "s");
        }
        else //Card is played by wrong player (or while pile is collectible)
        {
            playedCard.isBurned = true;
            pile.Add(playedCard); //Add card to bottom of pile
            if (enableLogs) Debug.Log(player + " burned " + playedCard.number + " of " + playedCard.suit + "s");
            return;
        }

        //Determine which player goes next:
        Player lastTurn = turn;
        ToggleTurn(); //Initially assume that turn order will continue as normal
        if (playedCard.isFaceCard) //Begin new Face Card Contest
        {
            if (playedCard.number == 1) faceTriesLeft = 4;
            else faceTriesLeft = playedCard.number - 10;
            if (enableLogs) Debug.Log("Faceoff initiated, " + turn + " has " + faceTriesLeft + " tries to play a face card");
        }
        else //Continue Face Card Contest or normal play
        {
            if (faceTriesLeft > 0) //Continue ongoing Face Card Contest
            {
                faceTriesLeft--; //Indicate that a non-face card has been placed
                if (faceTriesLeft == 0) //End Face Card Contest
                {
                    collectible = true;
                    if (enableLogs) Debug.Log(turn + " has won the faceoff, and may now collect the pile");
                }
                else ToggleTurn(); //The same player continues playing until contest is resolved
            }
        }
        if (hand1.Count == 0) turn = Player.Player2;      //If Player1 is out of cards, Player2 must go next
        else if (hand2.Count == 0) turn = Player.Player1; //If Player2 is out of cards, Player1 must go next

        //Check slappability:
        slappable = false;
        Card c1 = playedCard;
        if (redTens) //Special check for if red tens rule is enabled
        {
            if (c1.suit == Suit.Diamond && c1.number == 10 ||
                c1.suit == Suit.Heart && c1.number == 10)
            {
                slappable = true;
                if (enableLogs) Debug.Log("Slap opportunity: Red Ten");
            }
        }
        if (pile.Count > 1 && !slappable) //Pile is large enough to check for doubles
        {
            Card c2 = pile[1];
            if (c1.number == c2.number) //Check for double
            {
                slappable = true;
                if (enableLogs) Debug.Log("Slap opportunity: Double");
            }
            else if (pile.Count > 2) //Pile is large enough to check for sandwiches and runs
            {
                Card c3 = pile[2];
                if (c1.number == c3.number) //Check for sandwich
                {
                    slappable = true;
                    if (enableLogs) Debug.Log("Slap opportunity: Sandwich");
                }
                else if (c1.number == c2.number - 1 || //Check for forward run
                         c1.number == 1 && c2.number == 13) //Account for king -> ace 
                {
                    if (c2.number == c3.number - 1 ||
                        c2.number == 1 && c3.number == 13)
                    {
                        slappable = true;
                        if (enableLogs) Debug.Log("Slap opportunity: Run");
                    }
                }
                else if (c1.number == c2.number + 1 || //Check for backward run
                         c1.number == 13 && c2.number == 1) //Account for ace -> king
                {
                    if (c2.number == c3.number + 1 ||
                        c2.number == 13 && c3.number == 1)
                    {
                        slappable = true;
                        if (enableLogs) Debug.Log("Slap opportunity: Reverse Run");
                    }
                }
            }
        }
    }
    public void CollectPile(Player player)
    {
        //Function: Attempts to claim the pile for the given player, combining the pile with their deck if successful and checking for a win condition

        //Check if collection is invalid:
        if (!slappable && !collectible || !slappable && player != turn) //Pile is not slappable or collectible by this player
        {
            //Burn a card:
            Card burnedCard;
            if (player == Player.Player1) //Remove card from top of hand
            {
                if (hand1.Count == 0) return; //Exit if Player1 has no cards to burn
                burnedCard = hand1[0];
                hand1.RemoveAt(0);
            }
            else //Remove card from top of hand
            {
                if (hand2.Count == 0) return; //Exit if Player2 has no cards to burn
                burnedCard = hand2[0];
                hand2.RemoveAt(0);
            }
            burnedCard.isBurned = true;
            pile.Add(burnedCard); //Add burned card to bottom of pile
            if (enableLogs) Debug.Log(player + " burned " + burnedCard.number + " of " + burnedCard.suit + "s");
            return;
        }

        //Collection cleanup:
        turn = player; //Ensure the player who collects the pile goes first
        faceTriesLeft = 0; //Break out of Face Card Contest mode if applicable
        slappable = false;
        collectible = false;

        //Add cards to collecting player's hand:
        for (int n = 0; n < pile.Count;)
        {
            Card collectedCard = pile[pile.Count - 1]; //Remove card from bottom of pile
            collectedCard.isBurned = false;
            collectedCard.owner = turn;
            if (player == Player.Player1) hand1.Add(collectedCard); //Add card to bottom of player hand
            else hand2.Add(collectedCard);                          //Add card to bottom of player hand
            pile.Remove(collectedCard);
        }
        if (enableLogs) Debug.Log(player + " collected the pile");

        //Check for win condition:
        if (hand1.Count == 0) //Player 1 is out of cards, Player 2 wins
        {
            gameOver = true;
            if (enableLogs) Debug.Log("Player2 won");
        }
        else if (hand2.Count == 0) //Player 2 is out of cards, Player 1 wins
        {
            gameOver = true;
            if (enableLogs) Debug.Log("Player1 won");
        }
    }
    private void ToggleTurn()
    {
        //Function: Switches which player is currently going next

        if (turn == Player.Player1) turn = Player.Player2;
        else turn = Player.Player1;
    }
}
