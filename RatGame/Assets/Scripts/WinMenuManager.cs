using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinMenuManager : MonoBehaviour
{
    //Objects & Components:
    public static WinMenuManager winMenu;
    private Canvas canvas;

    //Conditions & Memory Vars:
    internal bool activated;

    private void Awake()
    {
        if (winMenu == null) winMenu = this;
        else Destroy(this);

        canvas = GetComponent<Canvas>();
    }

    public void TriggerWinScreen(Player winningPlayer)
    {
        //Function: Enables win screen

        if (winningPlayer == Player.Player2) canvas.transform.Rotate(new Vector3(0, 0, 180)); //Orient win screen to face Player2 (if he/she won)
        canvas.enabled = true;
        activated = true;
    }
    public void ProcessInput(InputManager.TouchData touch)
    {
        //Function: Receives touch input and maybe presses a button

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
