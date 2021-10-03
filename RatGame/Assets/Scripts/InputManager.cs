using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    //Created: 10-3-21
    //Purpose: Detecting and translating commands from player input and triggering events in GameDirector

    //Classes, Enums & Structs:
    [System.Serializable] public class ActiveInput
    {
        //Created: 10-3-21
        //Purpose: Storing data for touch motions related to playing and drawing cards

        public int originZone;   //Where the touch started (0 = Pile, 1 = Hand1, 2 = Hand2, 3 = NA)
        public Vector2 position; //Current position of the touch
        internal GameObject debugIndicator; //temporary
    }

    //Objects & Components:
    private Transform pile;
    private Transform hand1;
    private Transform hand2;
    private Collider2D pileZone;
    private Collider2D hand1Zone;
    private Collider2D hand2Zone;

    public GameObject debugIndicatorPrefab;

    //Conditions & Memory Vars:
    public List<ActiveInput> activeInputs = new List<ActiveInput>(); //Companion list to Input.touches array

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
        if (Input.touchCount > 0) //Check if there is any touch input
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
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
        }

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
    private Vector3 ActualScreenToWorldPoint(Vector2 screenPosition)
    {
        //Function: Does what ScreenToWorldPoint should do ):

        Vector3 worldPosition = screenPosition; //Make it a V3
        worldPosition.z = -Camera.main.transform.position.z; //Offset given position by Z position of camera
        worldPosition = Camera.main.ScreenToWorldPoint(worldPosition); //Then do the thing
        return worldPosition;
    }
}
