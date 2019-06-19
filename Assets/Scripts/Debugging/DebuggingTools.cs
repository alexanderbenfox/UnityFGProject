using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebuggingTools
{
    public static string PrintPlayerState(PlayerFrameState state)
    {
        string str = "";
        if ((state & PlayerFrameState.AERIAL) != 0)
        {
            str += "Aerial ";
        }
        if ((state & PlayerFrameState.HITSTUN) != 0)
        {
            str += "HitStun ";
        }
        if ((state & PlayerFrameState.ATTACK_NONCANCELLABLE) != 0)
        {
            str += "AttackNC ";
        }
        if ((state & PlayerFrameState.ATTACK_CANCELLABLE) != 0)
        {
            str += "AttackC ";
        }
        if ((state & PlayerFrameState.MOVING_HORIZ) != 0)
        {
            str += "MovingHorizontal ";
        }
        if ((state & PlayerFrameState.MOVING_VERT) != 0)
        {
            str += "MovingVertical ";
        }
        if ((state & PlayerFrameState.BLOCKSTUN) != 0)
        {
            str += "BLOCKSTUN ";
        }
        if ((state & PlayerFrameState.CROUCHING) != 0)
        {
            str += "CROUCHING ";
        }

        return str;
    }
}
