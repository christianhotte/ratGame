using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    //Created: 10-3-21
    //Purpose: Detecting and translating commands from player input and triggering events in GameDirector

    //Classes, Enums & Structs:
    public enum InputMode {Swipe, Place}
    public enum Zone {Pile, Hand1, Hand2, None}
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
    private Collider2D pileZone;
    private Collider2D hand1Zone;
    private Collider2D hand2Zone;

    //Settings:
    [Header("Settings (player):")]
    public InputMode inputMode; //Determines how inputs are registered during gameplay.  Swipe Mode: Cards are played when they cross a given threshold; Place Mode: Cards are played when the player lifts their finger
    [Header("Settings (editor):")]
    [Range(0, 1)] public float playLine;    //Position of line (as percentage of screen space from each player side) players must drag cards past (from hand) in order to play them (when in swipe mode)
    [Range(0, 1)] public float collectLine; //Position of line (as percentage of screen space from each player side) players must drag cards past (from pile) in order to collect the pile (when in swipe mode)

    //Conditions & Memory Vars:
    private Touch[] touches; //Array of active touch inputs
    internal List<TouchData> touchDataList = new List<TouchData>(); //Companion list to touches array for tracking origin positions (replacement for touch.rawPosition)

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
        
    }
    private void TouchMoved(TouchData data)
    {
        if (inputMode == InputMode.Swipe)
        {
            
        }
    }
    private void TouchEnded(TouchData data)
    {
        
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
