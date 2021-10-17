using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    //Objects & Components:
    public static PauseManager pauseManager;
    private Canvas canvas;

    //Conditions & Memory Vars:
    internal bool paused = false; //Whether or not the game is currently paused

    private void Awake()
    {
        if (pauseManager == null) pauseManager = this;
        else Destroy(this);

        canvas = GetComponent<Canvas>();
    }
    public void TogglePause(bool pause)
    {
        //Function: Pauses or unpauses the game

        if (paused == pause) return; //Do nothing if game is already in given mode
        paused = pause; //Set new pause mode
        canvas.enabled = paused; //Enable or disable canvas
    }
    public void ProcessInput(InputManager.TouchData touch)
    {
        //Function: Receives touch input and maybe presses a button

        if (!paused) return; //Safety check
        Vector2 touchPosition = InputManager.inputManager.ActualScreenToWorldPoint(touch.position);

        //Push button (or unpause):
        TogglePause(false);
    }
}
