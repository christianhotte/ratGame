using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HotteStuff;

public class CardVisualizer : MonoBehaviour
{
    //Created: 10-9-21
    //Purpose: Generating card objects with prefabricated elements and displaying/moving/hiding them as needed according to the InputManager and GameDirector

    //Classes, Enums & Structs:
    [System.Serializable]
    public class CardAvatarData
    {
        //Created: 10-11-21
        //Purpose: Contains information for visualizing and positioning a generated card

        //Object References:
        public InputManager.TouchData touchData = null; //Touch object influencing this card's position (NULL when card is deployed)
        public GameDirector.Card cardData;              //Programmatic card object associated with this data
        public Transform transform;                     //Card object transform associated with this data

        //Status Vars:
        public Zone originZone;              //Zone where this card originated
        public Zone targetZone = Zone.None;  //Zone where card will snap to if floating and not influenced by touch
        public bool held = false;            //True if card is currently being held
        public bool faceUp = false;          //True if this card is currently face-up (cards always generate face-down)
        public bool floating = false;        //Whether or not this card is lerping toward a designated location
        public bool movedOnPile = false;     //Whether or not this card is on the pile and is out of position
    }

    //Objects & Components:
    public static CardVisualizer visualizer;
    public CardSpriteSet spriteSet; //The card sprite set to use when generating cards
    public GameObject cardPrefab;   //The card prefab to instantiate and build off of

    //Settings:
    public Vector2 iconMaxSpread;  //Max units from center an icon can be
    public float snapToTouchSpeed; //How fast a floating card will snap to player finger location when touched (with deltaTime applied)
    public float snapToZoneSpeed;  //How fast a floating card will move to designated zone when released (with deltaTime applied)
    public float snapToZoneRadius; //How close to a zone a card must get before it snaps into the zone and stops being rendered
    public float cardHoldYOffset;  //Allows the point where cards are held to be adjusted along the Y axis (for visibility)
    [Space()]
    public Vector2 pileDragMinRange;
    public Vector2 pileDragMaxRange;

    //Conditions & Memory Vars:
    private List<CardAvatarData> activeCards = new List<CardAvatarData>(); //List of all currently-generated cards
    private List<CardAvatarData> pileCards = new List<CardAvatarData>();   //Cards in pile
    private List<CardAvatarData> hand1Cards = new List<CardAvatarData>();  //Cards in Player1 hand
    private List<CardAvatarData> hand2Cards = new List<CardAvatarData>();  //Cards in Player2 hand
    internal CardAvatarData heldCard1 = null; //Card LAST held by Player1
    internal CardAvatarData heldCard2 = null; //Card LAST held by Player2
    private bool holdingCard1 = false; //Whether or not Player1 is holding a card (I think this is redundant but I was having problems)
    private bool holdingCard2 = false; //Whether or not Player2 is holding a card

    private void Awake()
    {
        //Initialize as sole card visualizer:
        if (visualizer == null) visualizer = this;
        else Destroy(this);
    }

    private void Update()
    {
        MoveFloatingCards();
        MovePileCards();
    }

    //CARD MANIPULATION/MOVEMENT:
    private void MoveFloatingCards()
    {
        //Function: Runs through all active cards and lerps floating cards toward where they need to go

        if (activeCards.Count > 0)
        {
            //Update Card Visuals:
            foreach (CardAvatarData cardAvatar in activeCards) //Parse through all active cards in scene
            {
                //Move Floating Cards:
                if (cardAvatar.floating) //Process movement for cards which are floating between zones
                {
                    //Setup Vars:
                    Vector2 currentPosition = cardAvatar.transform.position;
                    Vector2 targetPosition;
                    float lerpSpeed;
                    //Get Target Position:
                    if (cardAvatar.held) //Card avatar has associated touch which it is moving toward
                    {
                        targetPosition = InputManager.inputManager.ActualScreenToWorldPoint(cardAvatar.touchData.position); //Get position of touch in world space
                        if (cardAvatar.touchData.player == Player.Player1) targetPosition.y -= cardHoldYOffset; //Offset held card by given amount
                        else targetPosition.y += cardHoldYOffset; //Offset held card by given amount (reversed for Player2)
                        lerpSpeed = snapToTouchSpeed; //Set lerp speed
                    }
                    else //Card has been released and is moving toward its target zone
                    {
                        targetPosition = InputManager.inputManager.GetPositionFromZone(cardAvatar.targetZone); //Get position of target zone in world space
                        lerpSpeed = snapToZoneSpeed; //Set lerp speed
                    }
                    //Lerp Card:
                    Vector2 newPosition = Vector2.Lerp(currentPosition, targetPosition, lerpSpeed * Time.deltaTime);
                    cardAvatar.transform.position = newPosition;
                    //Check for Zone Snap:
                    if (!cardAvatar.held && Vector2.Distance(newPosition, targetPosition) < snapToZoneRadius) //Card is close enough to target zone
                    {
                        cardAvatar.transform.position = targetPosition; //Set card position to exact target
                        cardAvatar.floating = false; //Indicate that card is no longer floating
                    }
                }
            }
        }
    }
    private void MovePileCards()
    {
        //Function: Allows players to pull pile aside to see top three cards (by moving cards depending on player input)

        //Safety/redundancy check:
        if (pileCards.Count < 2) return; //Don't bother if there are fewer than 2 cards (all cards are visible or there are no cards)
        if (pileCards[0].floating) return; //Don't try and mess with floating cards

        //Decide target location for top card of pile:
        InputManager.TouchData[] pileTouches = InputManager.inputManager.GetTouchesInZone(Zone.Pile); //Get touches on pile
        Vector2 pilePosition = InputManager.inputManager.pilePlace.position; //Get position of pile in scene
        Vector2 avgTouchPos = pilePosition; //Initialize touch position variable at location of pile (so that touches offset it)
        foreach (InputManager.TouchData touch in pileTouches) //Parse through array of current touches
        {
            avgTouchPos += (Vector2)InputManager.inputManager.ActualScreenToWorldPoint(touch.position); //Add position to targetXPosition
        }
        if (pileTouches.Length != 0) avgTouchPos /= pileTouches.Length; //Get average of touch position targets (also avoid dividing by zero)
        if (Mathf.Abs(avgTouchPos.x) < pileDragMinRange.x) avgTouchPos.x = pilePosition.x; //Negate value if too low
        else avgTouchPos.x = Mathf.Sign(avgTouchPos.x) * HotteMath.Map(Mathf.Abs(avgTouchPos.x), pileDragMinRange.x, pileDragMaxRange.x, 0, pileDragMaxRange.x);
        if (Mathf.Abs(avgTouchPos.y) < pileDragMinRange.y) avgTouchPos.y = pilePosition.y; //Negate value if too low
        else avgTouchPos.y = Mathf.Sign(avgTouchPos.y) * HotteMath.Map(Mathf.Abs(avgTouchPos.y), pileDragMinRange.y, pileDragMaxRange.y, 0, pileDragMaxRange.y);
        Vector2 targetPosition = avgTouchPos; //Default target position to location of pile

        //Move top card toward target:
        CardAvatarData topCard = pileCards[0]; //Get top card on pile
        Vector2 currentPosition = topCard.transform.position;
        //Vector2 targetPosition = new Vector2(targetXPos, InputManager.inputManager.pilePlace.position.y); //Set top card target position
        Vector2 newPosition = Vector2.Lerp(currentPosition, targetPosition, snapToTouchSpeed * Time.deltaTime); //Move toward target position
        topCard.transform.position = newPosition; //Set new card position
        topCard.movedOnPile = true; //Indicate that this card is on pile but is out of position

        //Move card directly under top halfway toward target (if applicable):
        if (pileCards.Count < 3) return; //Don't bother if there are only two cards in pile
        CardAvatarData secondCard = pileCards[1]; //Get second card to top on pile
        currentPosition = secondCard.transform.position;
        targetPosition = Vector2.Lerp(InputManager.inputManager.pilePlace.position, targetPosition, 0.5f); //Get position halfway between topCard target and pile
        newPosition = Vector2.Lerp(currentPosition, targetPosition, snapToTouchSpeed * Time.deltaTime); //Move toward target position
        secondCard.transform.position = newPosition; //Set new card position
        secondCard.movedOnPile = true; //Indicate that this card is on pile but is out of position
        
        //Make sure all other cards in pile are in their normal position:
        for (int i = 2; i < pileCards.Count; i++) //Iterate through cards in pile
        {
            CardAvatarData avatar = pileCards[i]; //Get card
            if (avatar.movedOnPile) //If avatar is more than 2 cards deep in pile but is still out of position
            {
                currentPosition = avatar.transform.position; //Get position of card
                targetPosition = InputManager.inputManager.pilePlace.position; //Get position of pile
                newPosition = Vector2.Lerp(currentPosition, targetPosition, snapToZoneSpeed * Time.deltaTime); //Move card back toward pile
                if (Vector2.Distance(newPosition, targetPosition) < snapToZoneRadius) //Card is now fully on pile
                {
                    avatar.movedOnPile = false; //Indicate that card is now static on pile again
                    newPosition = targetPosition; //Ensure card is placed exactly on pile
                }
                avatar.transform.position = newPosition; //Set new card position
            }
        }
    }

    //CARD PLACEMENT:
    public void HoldCard(InputManager.TouchData touch)
    {
        //Function: Called whenever player touches inside a card zone and picks up a card

        //Move card into held position:
        CardAvatarData cardAvatar;
        if (touch.player == Player.Player1)
        {
            //Redundancy Check:
            if (holdingCard1) //Player1 is already holding a card
            {
                heldCard1.touchData = touch;
                return;
            }
            if (hand1Cards.Count == 0) //Player1 is out of cards
            {
                Debug.Log("Player1 is out of cards");
                return;
            }
            //Hold Card:
            cardAvatar = hand1Cards[0]; //Get avatar from top of player hand
            heldCard1 = cardAvatar; //Set held card to be avatar
            cardAvatar.targetZone = Zone.Hand1; //Set hand-specific zone target
            holdingCard1 = true; //Indicate that Player1 is holding a card
        }
        else
        {
            //Redundancy Check:
            if (holdingCard2)
            {
                heldCard2.touchData = touch;
                return;
            }
            if (hand2Cards.Count == 0) //Player2 is out of cards
            {
                Debug.Log("Player2 is out of cards");
                return;
            }
            //Hold Card:
            cardAvatar = hand2Cards[0]; //Get avatar from top of player hand
            heldCard2 = cardAvatar; //Set held card to be avatar
            cardAvatar.targetZone = Zone.Hand2; //Set hand-specific zone target
            holdingCard2 = true; //Indicate that Player2 is holding a card
        }

        //Indicate that card is held:
        cardAvatar.touchData = touch;
        cardAvatar.floating = true;
        cardAvatar.held = true;
        RenderCardOnTopOfAll(cardAvatar); //Ensure card is rendered above the rest of the cards in the scene
    }
    public void PlayCard(Player player)
    {
        //Function: Called by GameDirector to play given player's held card to the pile

        //Safety check:
        if (player == Player.None) return;
        if (player == Player.Player1 && !holdingCard1) return;
        if (player == Player.Player2 && !holdingCard2) return;

        //Release card to pile:
        CardAvatarData avatar = ReleaseCard(player); //Release player's held card
        MoveCardToZone(avatar.originZone, Zone.Pile, true, true); //Add card to pile
        FlipCard(avatar, true); //Flip card face up
    }
    public void BurnCard(Player player)
    {
        //Function: Visualizes a card being burnt

        //Get origin zone from player:
        Zone originZone = Zone.Hand1;
        if (player == Player.Player2) originZone = Zone.Hand2;

        MoveCardToZone(originZone, Zone.Pile, true, false); //Move card to bottom of pile
        FlipCard(pileCards[pileCards.Count - 1], true);
    }
    public void CollectPile(Player player)
    {
        //Function: Visualizes pile collection for given player

        //Get target zone from player:
        Zone targetZone = Zone.Hand1;
        if (player == Player.Player2) targetZone = Zone.Hand2;

        //Move cards:
        while (pileCards.Count > 0) //Go through all cards in pile
        {
            CardAvatarData avatar = pileCards[pileCards.Count - 1];
            MoveCardToZone(Zone.Pile, targetZone, false, false); //Take cards from bottom of pile and add to bottom of hand
            FlipCard(avatar, false); //Flip cards face-down
            avatar.movedOnPile = false; //Just making sure
        }
    }
    public CardAvatarData ReleaseCard(Player player)
    {
        //Function: Un-holds given card and allows it to snap to target zone

        CardAvatarData avatar;
        if (player == Player.Player1) //Player1's held card
        {
            if (heldCard1 == null) return null; //Safety check
            avatar = heldCard1;
            heldCard1 = null;
            holdingCard1 = false;
        }
        else //Player2's held card
        {
            if (heldCard2 == null) return null; //Safety check
            avatar = heldCard2;
            heldCard2 = null;
            holdingCard2 = false;
        }
        avatar.held = false;
        avatar.touchData = null;
        avatar.floating = true;
        return avatar;
    }
    private void MoveCardToZone(Zone originZone, Zone targetZone, bool takeFromTop, bool addToTop)
    {
        //Function: Processes a card moving from the top or bottom of one zone to the top or bottom of another zone
        //NOTE: Should happen immediately after cards are played or collected (or burned) in the GameDirector

        //Get card from origin list:
        List<CardAvatarData> originList; //Initialize container for list to take card from
        switch (originZone) //Check origin zone
        {
            case Zone.Hand1:
                originList = hand1Cards;
                break;
            case Zone.Hand2:
                originList = hand2Cards;
                break;
            case Zone.Pile:
                originList = pileCards;
                break;
            default:
                Debug.LogError("Tried to move card from null zone");
                return;
        }
        if (originList.Count == 0) //Safety check for empty zone
        {
            Debug.LogError("Can't move card from zone, zone is empty");
            return;
        }
        int takeFromIndex = 0; //Initialize index of list to take card from as 0 (top)
        if (!takeFromTop) takeFromIndex = originList.Count - 1; //Take from last index of list (bottom)
        CardAvatarData avatar = originList[takeFromIndex]; //Get card from origin zone
        
        //Put card in target list:
        List<CardAvatarData> targetList; //Initialize container for list to add card to
        switch (targetZone) //Check target zone
        {
            case Zone.Hand1:
                targetList = hand1Cards;
                break;
            case Zone.Hand2:
                targetList = hand2Cards;
                break;
            case Zone.Pile:
                targetList = pileCards;
                break;
            default:
                Debug.LogError("Tried to move card to null zone");
                return;
        }
        if (addToTop) //Insert card on top of list
        {
            targetList.Insert(0, avatar); //Add card to first index of list
            QuickRenderCardOnTop(avatar, targetList); //RENDER STEP: Render new card on top of new list
        }
        else //Add card to end of list
        {
            targetList.Add(avatar); //Add card to end of list
            RenderListByOrder(targetList); //RENDER STEP: Re-adjust layer order of list to fit new card
        }

        //Cleanup:
        originList.Remove(avatar); //Remove card from origin list
        avatar.originZone = targetZone; //Establish new card position
        avatar.targetZone = targetZone; //Just making sure
        avatar.floating = true; //Make sure card can move physically
    }

    //CARD RENDERING:
    private void SetCardRenderLayer(CardAvatarData cardAvatar, int layer)
    {
        //Function: Sets the layers sprites on given card are rendered at (in order to ensure cards don't overlap each other
        //NOTE: Cards should be placed 3 indexes apart (yea it's not ideal but it's fine)

        cardAvatar.transform.Find("Front").GetComponent<SpriteRenderer>().sortingOrder = layer;
        cardAvatar.transform.Find("Numeral").GetComponent<SpriteRenderer>().sortingOrder = layer + 1;
        cardAvatar.transform.Find("Suit").GetComponent<SpriteRenderer>().sortingOrder = layer + 1;
        for (int i = 4; i < cardAvatar.transform.childCount; i++) //Iterate through all the icons on the card
        {
            cardAvatar.transform.GetChild(i).GetComponent<SpriteRenderer>().sortingOrder = layer + 1;
        }
        cardAvatar.transform.Find("Back").GetComponent<SpriteRenderer>().sortingOrder = layer + 2;
    }
    private void RenderListByOrder(List<CardAvatarData> list)
    {
        //Function: Renders cards in list in order from top (index 0) to bottom (last index), keeps piles of cards looking nice and clean
        //NOTE: Each card contains 3 render layers, and thus cards need to be spaced 3 layers apart to work right

        int layer = 0; //Initialize tracker to store where in layer order to put next card
        for (int i = list.Count - 1; i >= 0; i--) //Iterate backward through list
        {
            SetCardRenderLayer(list[i], layer); //Set card to render on current layer
            layer += 3; //Increment layer tracker by 3
        }
    }
    private void QuickRenderCardOnTop(CardAvatarData avatar, List<CardAvatarData> list)
    {
        //Function: Quick and simple way to make sure a card going on top of a zone is rendered on top of that zone

        if (list.Count == 0) SetCardRenderLayer(avatar, 0); //Render card on bottom layer if given list is empty
        int highestLayer = list[0].transform.Find("Front").GetComponent<SpriteRenderer>().sortingOrder; //Get layer number of top card in deck
        SetCardRenderLayer(avatar, highestLayer + 3); //Render card above highest layer in given list
    }
    private void RenderCardOnTopOfAll(CardAvatarData avatar)
    {
        //Function: Forces given card to be rendered above all other active cards in scene

        int highestLayer = 0;
        foreach (CardAvatarData otherAvatar in activeCards) //Iterate through all card avatars in scene
        {
            highestLayer = Mathf.Max(highestLayer, otherAvatar.transform.Find("Front").GetComponent<SpriteRenderer>().sortingOrder); //Compare with current highest number
        }
        SetCardRenderLayer(avatar, highestLayer + 3); //Render card above highest layer among all other cards
    }
    private void FlipCard(CardAvatarData cardAvatar, bool faceUp)
    {
        //Function: Sets given card to either face up or face down (and applies effects accordingly)

        if (cardAvatar.faceUp == faceUp) return; //Cancel if card is already oriented correctly
        cardAvatar.faceUp = faceUp;
        cardAvatar.transform.Find("Back").gameObject.SetActive(!faceUp); //Turn card back on or off depending on new orientation
    }

    //CARD GENERATION:
    public void GenerateHandAvatars()
    {
        //Function: Generates card avatars for both hands, positions them correctly, and adds them to the appropriate lists

        //Player1 Hand:
        foreach (GameDirector.Card card in GameDirector.director.hand1) //Iterate for each card in player hand
        {
            CardAvatarData newAvatar = GenerateCard(card); //Generate a new card
            newAvatar.transform.position = InputManager.inputManager.hand1Place.position; //Position card
            hand1Cards.Add(newAvatar); //Add to hand list (in order)
        }
        RenderListByOrder(hand1Cards); //Place cards in hand on correct render layers

        //Player2 Hand:
        foreach (GameDirector.Card card in GameDirector.director.hand2) //Iterate for each card in player hand
        {
            CardAvatarData newAvatar = GenerateCard(card); //Generate a new card
            newAvatar.transform.position = InputManager.inputManager.hand2Place.position; //Position card
            hand2Cards.Add(newAvatar); //Add to hand list (in order)
        }
        RenderListByOrder(hand2Cards); //Place cards in hand on correct render layers
    }
    private CardAvatarData GenerateCard(GameDirector.Card cardData)
    {
        //Function: Procedurally generates a card with given card data's exact properties, using given sprite set and current spacing settings

        //Setup:
        GameObject newCard = Instantiate(cardPrefab, transform); //Create new card avatar object
        Suit suit = cardData.suit;    //Get suit from card data
        int number = cardData.number; //Get numeral from card data
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
        GameObject reverseLabel = Instantiate(newCard.transform.Find("Numeral").gameObject, newCard.transform);
        reverseLabel.transform.Rotate(Vector3.forward, 180);
        reverseLabel = Instantiate(newCard.transform.Find("Suit").gameObject, newCard.transform);
        reverseLabel.transform.Rotate(Vector3.forward, 180);
        //Propagate & Position Icons:
        if (number > 1 && number <= 10) //Only do this step for non-face cards
        {
            List<Transform> icons = new List<Transform>(); //Create list to store/keep track of new icons
            icons.Add(newCard.transform.Find("Icon")); //Add first icon to list
            for (int i = 0; i < number - 1; i++) //Add one icon for each numeral above 1
            {
                Transform newIcon = Instantiate(icons[0].gameObject, newCard.transform).transform; //Instantiate a copy of first icon
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

                        if (number > 6) //Placement for 7th icon on 7 card
                        {
                            icons[6].transform.Translate(0, iconMaxSpread.y / 2, 0);

                            if (number > 7) //Placement for 8th icon on 8 card
                            {
                                icons[7].transform.Translate(0, -iconMaxSpread.y / 2, 0);
                                icons[7].transform.Rotate(0, 0, 180);
                            }
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
        //Generate Data:
        CardAvatarData avatarData = new CardAvatarData();
        avatarData.cardData = cardData;
        avatarData.transform = newCard.transform;
        avatarData.originZone = cardData.location;
        //Cleanup:
        activeCards.Add(avatarData);
        return avatarData;
    }
}

//DEPRECATED:
/*public void GenerateCardFromTouch(InputManager.TouchData touch)
    {
        //Function: Generates a floating card (from player hand) and sets it up to follow given touch
        //NOTE:     If a card already exists for given player, this will use that card instead of generating a new one

        //Setup & Checks:
        GameDirector.Card cardData = new GameDirector.Card(); //Initialize cardData as generic object
        CardAvatarData cardAvatar = null; //Initialize cardAvatar as null
        switch (touch.player)
        {
            case Player.Player1:
                cardAvatar = heldCard1; //Try to get held card from Player1
                cardData = GameDirector.director.hand1[0];
                break;
            case Player.Player2:
                cardAvatar = heldCard2; //Try to get held card from Player2
                cardData = GameDirector.director.hand2[0];
                break;
            case Player.None:
                Debug.LogError("Tried to generate card on touch when touch has no associated player.");
                return;
        }
        //Generate New Card (if necessary):
        if (cardAvatar == null) //No card avatar currently exists for this player
        {
            //Generate & Position Card Avatar:
            cardAvatar = GenerateCard(cardData); //Generate new avatar
            Vector3 spawnPosition;
            if (touch.player == Player.Player1) //Spawn in Player1 hand
            {
                spawnPosition = InputManager.inputManager.hand1Place.position;
                cardAvatar.targetZone = Zone.Hand1;
                heldCard1 = cardAvatar; //Set as held card
            }
            else //Spawn in Player2 hand
            {
                spawnPosition = InputManager.inputManager.hand2Place.position;
                cardAvatar.targetZone = Zone.Hand2;
                heldCard2 = cardAvatar; //Set as held card
            }
            cardAvatar.transform.position = spawnPosition; //Move card to spawn position
        }
        //Cleanup:
        cardAvatar.touchData = touch; //Have avatar follow player's most recent touch
        cardAvatar.floating = true;
        currentCardRenderLayer += 3; //Increment render layer counter by 3
        SetCardRenderLayer(cardAvatar, currentCardRenderLayer); //Set card as first card in render queue
    }*/
/* public void MoveTopDeckCardToHand(InputManager.TouchData touch)
    {
        //Function: Moves topdeck card from given player's deck to given player's hand

        //Generate New Topdeck Card:
        CardAvatarData cardAvatar;
        if (touch.player == Player.Player1)
        {
            if (topDeck1 == null) //Player1 has no topdeck card
            {
                return; //Bounce
            }
            heldCard1 = topDeck1; //Move card from deck to hand
            if (GameDirector.director.hand1.Count <= 1) //Player1 is out of cards
            {
                return; //Bounce
            }
            GenerateCardInZone(GameDirector.director.hand1[1], Zone.Hand1); //Generate new topdeck card for Player1
            cardAvatar = heldCard1; //Set card avatar
        }
        else
        {
            if (topDeck2 == null) //Player2 has no topdeck card
            {
                return; //Bounce
            }
            heldCard2 = topDeck2; //Move card from deck to hand
            if (GameDirector.director.hand2.Count <= 1) //Player2 is out of cards
            {
                return; //Bounce
            }
            GenerateCardInZone(GameDirector.director.hand2[1], Zone.Hand2); //Generate new topdeck card for Player1
            cardAvatar = heldCard2; //Set card avatar
        }
        //Change Settings on Card in Hand:
        cardAvatar.touchData = touch; //Have avatar follow player's most recent touch
        cardAvatar.floating = true;
        currentCardRenderLayer += 3; //Increment render layer counter by 3
        SetCardRenderLayer(cardAvatar, currentCardRenderLayer); //Set card as first card in render queue
    }
 */
/* public void GenerateCardInZone(GameDirector.Card cardData, Zone zone)
    {
        //Function: Generates a static card in given zone (used to generate the top cards in player hands)

        //Generate Card:
        MoveAllCardsUpInRenderOrder(); //Place this card below all other cards
        CardAvatarData newCardAvatar = GenerateCard(cardData); //Generate new avatar
        Vector3 spawnPosition = new Vector3();
        switch (zone)
        {
            case Zone.Hand1:
                spawnPosition = InputManager.inputManager.hand1Place.position;
                topDeck1 = newCardAvatar; //Indicate that new card is on top of deck
                break;
            case Zone.Hand2:
                spawnPosition = InputManager.inputManager.hand2Place.position;
                topDeck2 = newCardAvatar; //Indicate that new card is on top of deck
                break;
            default:
                Debug.LogError("Tried to generate card in null zone");
                return;
        }
        newCardAvatar.transform.position = spawnPosition; //Move card to spawn position
        //Cleanup:
        newCardAvatar.targetZone = zone;
    }*/
/*public void DeployHeldCard(Player player, Zone zone)
    {
        //Function: Sets card avatar of given player's currently-held card to move toward indicated zone (used when playing cards to pile or releasing unplayed cards when still in hand)

        //Get Card Avatar (and do safety check):
        CardAvatarData cardAvatar;
        if (player == Player.Player1) cardAvatar = heldCard1;
        else if (player == Player.Player2) cardAvatar = heldCard2;
        else return; //Cancel if no player is specified
        if (cardAvatar == null) return; //Cancel if no held card was found
        //Change Avatar Settings:
        cardAvatar.touchData = null;  //Indicate that card is no longer being controlled by touch
        cardAvatar.targetZone = zone; //Set target zone
        cardAvatar.floating = true;   //Ensure that card is now floating
        cardAvatar.held = false;      //Indicate that card is not being held
        //Additional Considerations:
        if (zone == Zone.Pile) //Deploying to the pile
        {
            if (player == Player.Player1) heldCard1 = null;
            else heldCard2 = null;
            FlipCard(cardAvatar, true); //Flip card face-up when deploying to pile
        }
    }*/
/*public void DeployHeldCard(Player player)
{
    //Function (OVERRIDE): Deploys held card back to given player's hand automatically (in cases where specifying zone is inconvenient)

    switch (player)
    {
        case Player.Player1:
            DeployHeldCard(Player.Player1, Zone.Hand1);
            break;
        case Player.Player2:
            DeployHeldCard(Player.Player2, Zone.Hand2);
            break;
        default:
            break;
    }
}*/
/*private void MoveCardsUpInRenderOrder(List<CardAvatarData> cardList, int startingIndex)
    {
        //Function: Moves all cards in given list (after given index) up 1 increment in render order

        for (int i = startingIndex; i < cardList.Count; i++)
        {
            CardAvatarData avatar = cardList[i]; //Get card from list
            int minLayer = avatar.transform.Find("Front").GetComponent<SpriteRenderer>().sortingOrder; //Get lowest layer of given card
            SetCardRenderLayer(avatar, minLayer + 3); //Increment all sprite layers in card by 3
        }
    }*/
/*public void ReleaseCard(Player player, Zone endZone)
    {
        //Function: Called whenever player lifts a finger (presumably while holding a card), or when a card is "played" by a swipe

        //Set new target zone:
        if (endZone == Zone.None) //Null zone contingency
        {
            if (player == Player.Player1) endZone = heldCard1.originZone; //Send card back to origin zone
            else endZone = heldCard2.originZone; //Send card back to origin zone
        }
        cardAvatar.targetZone = endZone; //Send card to new target zone
        
        //Check for zone change:
        if (cardAvatar.targetZone == Zone.Pile && cardAvatar.originZone != Zone.Pile) //Card is moving to the pile
        {
            FlipCard(cardAvatar, true); //Make sure card is face-up
            if (!cardAvatar.cardData.isBurned) //Card is being played to pile
            {
                MoveCardToZone(cardAvatar.originZone, Zone.Pile, true, true);
            }
            else //Card is being burned to pile
            {
                MoveCardToZone(cardAvatar.originZone, Zone.Pile, true, false);
            }
        }
    }*/