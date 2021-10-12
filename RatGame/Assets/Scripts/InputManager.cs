using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    //Created: 10-3-21
    //Purpose: Detecting and translating commands from player input and triggering events in GameDirector

    //Classes, Enums & Structs:
    public enum InputMode {Swipe, Drop}
    [System.Serializable] public class TouchData
    {
        //Purpose: Object containing data relevant for a touch being actively tracked by the program
        //         This is used to create an organized layer of information separate from the touch list itself, in order to simplify the program and prevent errors

        //Data:
        public Vector2 position; //The current point of contact for the touch (in screen pixel space)
        public Vector2 delta;    //How much this touch has moved since last update
        public Vector2 origin;   //The initial point of contact for the touch
        public int fingerID;     //The ID number linking this object to an existing touch
        public Player player = Player.None; //The player who's finger this is (as assumed by the program)

        //Meta:
        public bool markedForDisposal = false; //Set true once this object's associated touch has ended
        public bool markedComplete = false;    //Set true once this touch creates an input event (does not destroy object but prevents it from triggering more events)
    }

    //Objects & Components:
    public static InputManager inputManager;
        //NOTE: These objects determine the positions of the three major areas of play (don't move in hierarchy)
    private Transform pile;
    private Transform hand1;
    private Transform hand2;
        //NOTE: Zones are used to determine whether a touch will initiate a card pick-up animation
    private Collider2D pileZone;
    private Collider2D hand1Zone;
    private Collider2D hand2Zone;
        //NOTE: Places are used to determine where cards and decks will be rendered
    internal Transform pilePlace;
    internal Transform hand1Place;
    internal Transform hand2Place;

    //Settings:
    [Header("Settings (player):")]
    public InputMode inputMode; //Determines how inputs are registered during gameplay.  Swipe Mode: Cards are played when they cross a given threshold; Place Mode: Cards are played when the player lifts their finger
    [Header("Settings (editor):")]
    public bool drawDebugLines;             //Switch for whether or not to display debug lines
    [Range(0, 1)] public float playLine;    //Position of line (as percentage of screen space from each player side) players must drag cards past (from hand) in order to play them (when in swipe mode)
    [Range(0, 1)] public float collectLine; //Position of line (as percentage of screen space from each player side) players must drag cards past (from pile) in order to collect the pile (when in swipe mode)

    //Conditions & Memory Vars:
    private Touch[] touches; //Array of active touch inputs
    internal List<TouchData> touchDataList = new List<TouchData>(); //Companion list to touches array for tracking origin positions (replacement for touch.rawPosition)

    private void Awake()
    {
        //Initialize as sole input manager:
        if (inputManager == null) inputManager = this;
        else Destroy(this);

        //Get positional components:
        pile = transform.GetChild(0);
        hand1 = transform.GetChild(1);
        hand2 = transform.GetChild(2);
        pileZone = pile.GetComponent<Collider2D>();
        hand1Zone = hand1.GetComponent<Collider2D>();
        hand2Zone = hand2.GetComponent<Collider2D>();
        pilePlace = pile.GetChild(0);
        hand1Place = hand1.GetChild(0);
        hand2Place = hand2.GetChild(0);
    }

    private void Update()
    {
        //Debug Stuff:
        if (drawDebugLines)
        {
            Debug.DrawRay(ActualScreenToWorldPoint(new Vector3(0, GetPositionOfLine(playLine, Player.Player1))), Vector3.right * 6, Color.red);
            Debug.DrawRay(ActualScreenToWorldPoint(new Vector3(0, GetPositionOfLine(playLine, Player.Player2))), Vector3.right * 6, Color.red);
            Debug.DrawRay(ActualScreenToWorldPoint(new Vector3(0, GetPositionOfLine(collectLine, Player.Player1))), Vector3.right * 6, Color.yellow);
            Debug.DrawRay(ActualScreenToWorldPoint(new Vector3(0, GetPositionOfLine(collectLine, Player.Player2))), Vector3.right * 6, Color.yellow);
        }

        //Detect Input (parse touch arrays):
        touches = Input.touches; //Get most recent array of touches
        if (touches.Length > 0) //TOUCH INPUT:
        {
            foreach (Touch touch in touches) //Parse through list of touches
            {
                TouchData touchData = GetTouchDataByID(touch.fingerId); //Try and retrieve existing touch data
                if (touchData == null) //Create new touch data object
                {
                    touchData = new TouchData();
                    touchData.position = touch.position;
                    touchData.origin = touch.position;
                    touchData.fingerID = touch.fingerId;
                    if (ReadPositionAsZone(touchData.position) == Zone.Hand1) touchData.player = Player.Player1;
                    else if (ReadPositionAsZone(touchData.position) == Zone.Hand2) touchData.player = Player.Player2;
                    touchDataList.Add(touchData); //Add new data to list
                    TouchStarted(touchData); //Indicate to program that this touch has begun
                }
            }
        }
        if (touchDataList.Count > 0) //TOUCH INPUT DATA:
        {
            foreach (TouchData data in touchDataList) //Parse through list of touchData
            {
                if (data.markedForDisposal) continue; //Pass items marked for disposal
                bool foundTouch = false;
                foreach (Touch touch in touches) //Look for matching touch
                {
                    if (touch.fingerId == data.fingerID) //Data still has a matching touch
                    {
                        foundTouch = true;
                        if (data.position != touch.position) //Touch has moved since last update
                        {
                            data.delta = touch.position - data.position; //Calculate delta between before and after positions
                            data.position = touch.position; //Update position data
                            TouchMoved(data);
                        }
                        break;
                    }
                }
                if (!foundTouch) //Touch is no longer in array and has ended
                {
                    TouchEnded(data); //Indicate to program that this touch has ended
                    data.markedForDisposal = true;
                }
            }
            for (int i = 0; i < touchDataList.Count;) //Very smart cool nifty totally efficient way to clean up list without throwing indexoutofrange errors
            {
                TouchData data = touchDataList[i]; //Get first item from touchData list
                if (data.markedForDisposal) touchDataList.RemoveAt(i); //Remove item if marked for deletion
                else i++; //Move on to next item if not marked for deletion
            }
        }
    }

    //INPUT EVENTS:
    private void TouchStarted(TouchData data)
    {
        //Process Card Visualization:
        if (ReadPositionAsZone(data.position) == Zone.Hand1 ||
            ReadPositionAsZone(data.position) == Zone.Hand2)
        {
            //NOTE: The "held" status of a card is purely visual, and is/should be overridden by GameDirector-driven mechanics
            CardVisualizer.visualizer.HoldCard(data); //Indicate that a card has been picked up (but not played)
        }
    }
    private void TouchMoved(TouchData data)
    {
        //Process Input Event:
        if (inputMode == InputMode.Swipe && //Only process inputs in this phase if swipes are enabled
            !data.markedComplete)           //Only consider this input if it hasn't already been used for a swipe
        {
            switch (ReadPositionAsZone(data.origin)) //Check which zone the swipe started in
            {
                case Zone.Pile:
                    if (data.position.y < GetPositionOfLine(collectLine, Player.Player1)) //Player1 swipes past his/her collect line
                    {
                        GameDirector.director.CollectPile(Player.Player1); //Trigger input event
                        data.markedComplete = true; //Mark touch as inert now that it has triggered an event
                    }
                    else if (data.position.y > GetPositionOfLine(collectLine, Player.Player2)) //Player2 swipes past his/her collect line
                    {
                        GameDirector.director.CollectPile(Player.Player2); //Trigger input event
                        data.markedComplete = true; //Mark touch as inert now that it has triggered an event
                    }
                    break;
                case Zone.Hand1:
                    if (data.position.y > GetPositionOfLine(playLine, Player.Player1)) //Player1 swipes past his/her play line
                    {
                        GameDirector.director.PlayCard(Player.Player1); //Trigger input event
                        CardVisualizer.visualizer.ReleaseCard(Player.Player1, Zone.Pile); //Trigger visual event
                        data.markedComplete = true; //Mark touch as inert now that it has triggered an event
                    }
                    break;
                case Zone.Hand2:
                    if (data.position.y < GetPositionOfLine(playLine, Player.Player2)) //Player2 swipes past his/her play line
                    {
                        GameDirector.director.PlayCard(Player.Player2); //Trigger input event
                        CardVisualizer.visualizer.ReleaseCard(Player.Player2, Zone.Pile); //Trigger visual event
                        data.markedComplete = true; //Mark touch as inert now that it has triggered an event
                    }
                    break;
                default:
                    break;
            }
        }
    }
    private void TouchEnded(TouchData data)
    {
        //Process Input Event:
        Zone endZone = ReadPositionAsZone(data.position);
        if (inputMode == InputMode.Drop) //Only process inputs in this phase if drag&drop is enabled
        {
            switch (ReadPositionAsZone(data.origin)) //Check which zone the drag started in
            {
                case Zone.Pile:
                    if (endZone == Zone.Hand1) //Player1 drags from the pile to his/her hand
                    {
                        GameDirector.director.CollectPile(Player.Player1); //Trigger input event
                    }
                    else if (endZone == Zone.Hand2) //Player2 drags from the pile to his/her hand
                    {
                        GameDirector.director.CollectPile(Player.Player2); //Trigger input event
                    }
                    break;
                case Zone.Hand1:
                    if (endZone == Zone.Pile) //Player1 drags from his/her hand to the pile
                    {
                        GameDirector.director.PlayCard(Player.Player1); //Trigger input event
                    }
                    break;
                case Zone.Hand2:
                    if (endZone == Zone.Pile) //Player2 drags from his/her hand to the pile
                    {
                        GameDirector.director.PlayCard(Player.Player2); //Trigger input event
                    }
                    break;
                default:
                    break;
            }
        }
        //Process Visual Event:
        CardVisualizer.visualizer.ReleaseCard(data.player, endZone); //Trigger visual event
    }

    //UTILITY METHODS:
    private Zone ReadPositionAsZone(Vector2 position)
    {
        //Function: Determines which zone, if any, given position is in (and returns result as zone)

        Vector3 realPosition = ActualScreenToWorldPoint(position);
        if (pileZone.bounds.Contains(realPosition)) return Zone.Pile;
        else if (hand1Zone.bounds.Contains(realPosition)) return Zone.Hand1;
        else if (hand2Zone.bounds.Contains(realPosition)) return Zone.Hand2;
        else return Zone.None;
    }
    public Vector2 GetPositionFromZone(Zone zone)
    {
        //Function: Returns position of "place" transform for given zone

        switch (zone)
        {
            case Zone.Pile:
                return pilePlace.position;
            case Zone.Hand1:
                return hand1Place.position;
            case Zone.Hand2:
                return hand2Place.position;
            default:
                Debug.LogError("Tried to get position of null zone");
                return Vector2.zero;
        }
    }
    private float GetPositionOfLine(float lineSetting, Player player)
    {
        //Function: Determines where in screen space the given boundary line (playLine or collectLine) is
        //          Automatically accounts for screen size between devices, and flips line depending on player

        float linePos = lineSetting * Camera.main.scaledPixelHeight; //Get position (in screen space pixels) of given line
        if (player == Player.Player2) linePos = Camera.main.scaledPixelHeight - linePos; //Flip position (along center of screen) for player 2
        return linePos;
    }
    public Vector3 ActualScreenToWorldPoint(Vector2 screenPosition)
    {
        //Function: Does what ScreenToWorldPoint should do ):

        Vector3 worldPosition = screenPosition; //Make it a V3
        worldPosition.z = -Camera.main.transform.position.z; //Offset given position by Z position of camera
        worldPosition = Camera.main.ScreenToWorldPoint(worldPosition); //Then do the thing
        return worldPosition;
    }
    private TouchData GetTouchDataByID(int ID)
    {
        //Function: Returns item from touch data array with ID matching given number (or null if none exists)

        if (touchDataList.Count == 0) return null; //Return null if there are no items to return
        foreach (TouchData item in touchDataList) if (item.fingerID == ID) return item; //Parse through list and return matching item if found
        return null; //If matching item is never found, return null
    }
}
