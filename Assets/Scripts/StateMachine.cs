using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Guard
{
    LOW, MID, HIGH
}

[System.Serializable]
public class FGAttackData
{
    public FGAttackData()
    {
        damage = 0;
        guard = Guard.LOW;
        frameAdvOnBlock = 0;
        frameAdvOnHit = 0;
    }
    [SerializeField]
    public int damage;
    [SerializeField]
    public Guard guard;
    [SerializeField]
    public int frameAdvOnBlock;
    [SerializeField]
    public int frameAdvOnHit;
    //! defines the vector of pushback when another character is hit by this
    [SerializeField]
    public Vector2 attackVector;
    //can be cancelled after it hits the opponent
    bool regularCancellable;
    // can be interupted during the start up frames
    bool karaCancellable;
}
