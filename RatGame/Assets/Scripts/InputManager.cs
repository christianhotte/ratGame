using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    //Created: 10-3-21
    //Purpose: Detecting and translating commands from player input and triggering events in GameDirector

    //Classes, Enums & Structs:
    [System.Serializable] public class TouchData
    {
        //Purpose: Object containing data relevant for a touch being actively tracked by the program
        //         This is used to create an organized layer of information separate from the touch list itself, in order to simplify the program and prevent errors

        //Data:
        public Vector2 position; //The current point of contact for the touch
        public Vector2 delta;    //How much this touch has moved since last update
        public Vector2 origin;   //The initial point of contact for the touch
        public int fingerID;     //The ID number linking this object to an existing touch

        //Meta:
        public bool markedForDisposal = false; //Set true once this object's associated touch has ended
    }

    //Objects & Components:
        //NOTE: These objects determine the positions of the three major areas of play (don't move in hierarchy)
    private Transform pile;
    private Transform hand1;
    private Transform hand2;
        //NOTE: Zones are used to determine whether a touch will initiate a card pick-up animation
    private Collider2D pileZone;  //Zone ID = 0
    private Collider2D hand1Zone; //Zone ID = 1
    private Collider2D hand2Zone; //Zone ID = 2

    //Settings:
    [Header("Settings:")]
    [Range(0, 1)] public float playLine; //Position of line (as percentage of screen space from each player side) players must drag cards past (from hand) in order to play them
    [Range(0, 1)] public float collectLine; //Position of line (as percentage of screen space from each player side) players must drag cards past (from pile) in order to collect the pile

    //Conditions & Memory Vars:
    private Touch[] touches; //Array of active touch inputs
    public List<TouchData> touchDataList = new List<TouchData>(); //Companion list to touches array for tracking origin positions (replacement for touch.rawPosition)

    private void Awake()
    {
        //Get positional components:
        pile = transform.GetChild(0);
        hand1 = transform.GetChild(1);
        hand2 = transform.GetChild(2);
        pileZone = pile.GetComponent<Collider2D>();
        hand1Zone = hand1.GetComponent<Collider2D>();
        hand2Zone = hand2.GetComponent<Collider2D>();
    }

    private void Update()
    {
        //Parse Touch Arrays:
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
            for (int i = 0; i < touchDataList.Count;) //Clean up data list by removing items marked for deletion
            {
                TouchData data = touchDataList[i]; //Get first item from touchData list
                if (data.markedForDisposal) touchDataList.RemoveAt(i); //Remove item if marked for deletion
                else i++; //Move on to next item if not marked for deletion
            }
        }

        /*if (Input.touchCount > 0) //Check if there is any touch input
        {
            for (int i = 0; i < Input.touchCount; i++) //Parse through all active touches in touch array
            {
                Touch touch = Input.touches[i];

                switch (touch.phase) //Decide what to do based on phase
                {
                    case TouchPhase.Began:
                        ActiveInput newInput = new ActiveInput();
                        newInput.position = touch.position;
                        newInput.originZone = ReadPositionAsZone(touch.position);
                        newInput.debugIndicator = Instantiate(debugIndicatorPrefab);
                        newInput.debugIndicator.transform.position = ActualScreenToWorldPoint(newInput.position);
                        activeInputs.Add(newInput);
                        break;
                    case TouchPhase.Moved:
                        activeInputs[i].position = touch.position;
                        activeInputs[i].debugIndicator.transform.position = ActualScreenToWorldPoint(activeInputs[i].position);
                        break;
                    case TouchPhase.Ended: //Compute potential player actions
                        ActiveInput thisInput = activeInputs[i];
                        if (thisInput.originZone == 0) //Card collection actions
                        {
                            if (ReadPositionAsZone(touch.position) == 1)
                            {
                                GameDirector.director.CollectPile(GameDirector.Player.Player1);
                            }
                            else if (ReadPositionAsZone(touch.position) == 2)
                            {
                                GameDirector.director.CollectPile(GameDirector.Player.Player2);
                            }
                        }
                        else if (thisInput.originZone == 1) //Player1 play action
                        {
                            if (ReadPositionAsZone(touch.position) == 0)
                            {
                                GameDirector.director.PlayCard(GameDirector.Player.Player1);
                            }
                        }
                        else if (thisInput.originZone == 2) //Player2 play action
                        {
                            if (ReadPositionAsZone(touch.position) == 0)
                            {
                                GameDirector.director.PlayCard(GameDirector.Player.Player2);
                            }
                        }
                        Destroy(thisInput.debugIndicator);
                        activeInputs.Remove(thisInput);
                        break;
                    default:
                        break;
                }
            }
        }*/

    }

    private void TouchStarted(TouchData data)
    {
        Debug.Log("Touch started at " + data.origin);
    }
    private void TouchMoved(TouchData data)
    {
        //Debug.Log("Touch moved to " + data.position + " by " + data.delta);
    }
    private void TouchEnded(TouchData data)
    {
        Debug.Log("Touch ended at " + data.position + " (beginning at " + data.origin + ")");
    }

    private int ReadPositionAsZone(Vector2 position)
    {
        //Function: Determines which zone, if any, given position is in (and returns result as zone index)

        Vector3 realPosition = ActualScreenToWorldPoint(position);
        if (pileZone.bounds.Contains(realPosition)) return 0;
        else if (hand1Zone.bounds.Contains(realPosition)) return 1;
        else if (hand2Zone.bounds.Contains(realPosition)) return 2;
        else return 3;
    }
    private float GetPositionOfLine(float lineSetting, Player player)
    {
        //Function: Determines where in screen space the given boundary line (playLine or collectLine) is
        //          Automatically accounts for screen size between devices, and flips line depending on player

        float linePos = lineSetting * Camera.main.scaledPixelHeight; //Get position (in screen space pixels) of given line
        if (player == Player.Player2) linePos = Camera.main.scaledPixelHeight - linePos; //Flip position (along center of screen) for player 2
        return linePos;
    }
    private Vector3 ActualScreenToWorldPoint(Vector2 screenPosition)
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
