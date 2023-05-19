using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopUpComputerCloseListButton : LongRangeButton
{
    public PopUpComputerCosmetics masterScript;

    public override void ActivateButton(bool onOff)
    {
        if (PopUpComputer.instance.computerGroup.blocksRaycasts == true)
        {
            base.ActivateButton(onOff);
            masterScript.CloseListMenu();
        }
    }
}
