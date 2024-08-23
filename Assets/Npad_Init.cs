using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using nn.hid;

public class Npad_Init : MonoBehaviour
{
    NpadId npadid = NpadId.No1;
    NpadStyle npadstyle = NpadStyle.Invalid;
    NpadState npastate = new NpadState();
    void Start()
    {
        Npad.Initialize();
        Npad.SetSupportedIdType(new NpadId[] { NpadId.Handheld, NpadId.No1 });
        Npad.SetSupportedStyleSet(NpadStyle.FullKey | NpadStyle.Handheld | NpadStyle.JoyDual);
    }
}
