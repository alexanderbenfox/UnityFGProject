using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputState : MonoBehaviour
{
    public void Init()
    {
        input = PlayerInput.NONE;
    }

    public KeyCode LightAttackKey;
    public KeyCode MediumAttackKey;
    public KeyCode HeavyAttackKey;

    public float x;
    public float y;

    PlayerInput input;

    enum PlayerInput
    {
        NONE = 0,
        UP = 1 << 0,
        DOWN = 1 << 1,
        RIGHT = 1 << 2,
        LEFT = 1 << 3,

        LDOWN = 1 << 4,
        LHOLD = 1 << 5,
        LRELEASE = 1 << 6,

        MDOWN = 1 << 7,
        MHOLD = 1 << 8,
        MRELEASE = 1 << 9,

        HDOWN = 1 << 10,
        HHOLD = 1 << 11,
        HRELEASE = 1 << 12,
    }

    private bool CheckKeyDown(KeyCode key, PlayerInput lastFrameInput, PlayerInput downAndHold)
    {
        return (Input.GetKeyDown(key) || (Input.GetKey(key) && ((lastFrameInput & downAndHold) == 0)));
    }

    private void CheckKeyState(PlayerInput lastFrameInput, KeyCode key, PlayerInput down, PlayerInput hold, PlayerInput release)
    {
        if (CheckKeyDown(key, lastFrameInput, down | hold))
        {
            input |= down;
        }

        if (Input.GetKeyDown(key) && ((lastFrameInput & down) != 0))
        {
            input |= hold;
        }

        if (Input.GetKeyUp(key))
        {
            input |= release;
        }
    }

    public void GetInputState()
    {
        PlayerInput prev = input;
        input = PlayerInput.NONE;

        x = Mathf.Round(Input.GetAxis("Horizontal"));
        y = Mathf.Round(Input.GetAxis("Vertical"));

        if (x > 0)
            input |= PlayerInput.RIGHT;
        else if (x < 0)
            input |= PlayerInput.LEFT;

        if (y > 0)
            input |= PlayerInput.UP;
        else if (y < 0)
            input |= PlayerInput.DOWN;

        /*if (CheckKeyDown(LightAttackKey, prev, PlayerInput.LDOWN | PlayerInput.LHOLD))
        {
            input |= PlayerInput.LDOWN;
        }

        if (Input.GetKeyDown(LightAttackKey) && ((prev & PlayerInput.LDOWN) != 0))
        {
            input |= PlayerInput.LHOLD;
        }

        if (Input.GetKeyUp(LightAttackKey))
        {
            input |= PlayerInput.LRELEASE;
        }*/

        CheckKeyState(prev, LightAttackKey, PlayerInput.LDOWN, PlayerInput.LHOLD, PlayerInput.LRELEASE);
        CheckKeyState(prev, MediumAttackKey, PlayerInput.MDOWN, PlayerInput.MHOLD, PlayerInput.MRELEASE);
        CheckKeyState(prev, HeavyAttackKey, PlayerInput.HDOWN, PlayerInput.HHOLD, PlayerInput.HRELEASE);
    }

    public bool Right()
    {
        return (input & PlayerInput.RIGHT) != 0;
    }

    public bool Left()
    {
        return (input & PlayerInput.LEFT) != 0;
    }

    public bool Down()
    {
        return (input & PlayerInput.DOWN) != 0;
    }

    public bool Up()
    {
        return (input & PlayerInput.UP) != 0;
    }

    public bool Light()
    {
        return (input & PlayerInput.LDOWN) != 0;
    }

    public bool Medium()
    {
        return (input & PlayerInput.MDOWN) != 0;
    }

    public bool Heavy()
    {
        return (input & PlayerInput.HDOWN) != 0;
    }
}
