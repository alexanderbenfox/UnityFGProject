using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum PlayerFrameState
{
    NONE = 0,
    AERIAL = 1 << 0,
    HITSTUN = 1 << 1,
    BLOCKSTUN = 1 << 2,
    ATTACK_NONCANCELLABLE = 1 << 3,
    ATTACK_CANCELLABLE = 1 << 4,
    MOVING_HORIZ = 1 << 5,
    MOVING_VERT = 1 << 6, 
    CROUCHING = 1 << 7,
    HOLDINGAWAY = 1 << 8
}

public enum Direction
{
    LEFT = 0,
    RIGHT = 1
}

public class PlayerStateData
{
    public PlayerStateData()
    {
        _currentCollisions = CollisionState.NONE;
        _currentFrameState = PlayerFrameState.NONE;
        velocity = Vector2.zero;
    }
    //players current velocity
    public Vector2 velocity;
    //
    private PlayerFrameState _currentFrameState;
    //
    public PlayerFrameState FrameState
    {
        get
        {
            return _currentFrameState;
        }

        set
        {
            _currentFrameState = value;
        }
    }
    //
    private CollisionState _currentCollisions;

    public CollisionState Collisions
    {
        get
        {
            return _currentCollisions;
        }
        set
        {
            _currentCollisions = value;
        }
    }

    public static PlayerStateData operator +(PlayerStateData p1, PlayerStateData p2)
    {
        //merge both collision state
        p1.FrameState |= p2.FrameState;
        p1.Collisions = p2.Collisions;
        p1.velocity += p2.velocity;

        return p1;
    }
}

public class PlayerState
{
    
    //player's current health
    public int HP;
    //block stun frames
    public int nextActionDelay;
    //
    public PlayerInputState inputState;
    //
    public PlayerStateData stateData;

    [SerializeField]
    private Direction _currentDirection;

    public PlayerState(Direction direction)
    {
        stateData = new PlayerStateData();
        _currentDirection = direction;
        stateData.velocity = Vector2.zero;
    }

    public PlayerState(PlayerState playerState, PlayerInputState inputs)
    {
        stateData = new PlayerStateData();
        stateData.Collisions = playerState.stateData.Collisions;
        stateData.FrameState = playerState.stateData.FrameState;
        stateData.velocity = playerState.stateData.velocity;

        _currentDirection = playerState.GetDirection();
        HP = playerState.HP;
        nextActionDelay = playerState.nextActionDelay;

        inputState = inputs;
    }

    public void AddState(PlayerFrameState state)
    {
        stateData.FrameState |= state;
    }

    public void RemoveState(PlayerFrameState state)
    {
        stateData.FrameState &= ~state;
    }

    public bool CheckState(PlayerFrameState state)
    {
        return (stateData.FrameState & state) != 0;
    }

    public bool IsAttacking()
    {
        return (stateData.FrameState & (PlayerFrameState.ATTACK_CANCELLABLE | PlayerFrameState.ATTACK_NONCANCELLABLE)) != 0;
    }

    public bool IsFacingRight()
    {
        return (_currentDirection & Direction.RIGHT) != 0;
    }

    public void UpdateDirection(Direction dir)
    {
        _currentDirection = dir;
    }

    public Direction GetDirection()
    {
        return _currentDirection;
    }

    public static PlayerState operator +(PlayerState p1, PlayerState p2)
    {
        //merge both collision state
        PlayerStateData mergedData = p1.stateData + p2.stateData;
        p1.stateData = mergedData;
        return p1;
    }

    public static PlayerState operator +(PlayerState player, PlayerStateData data)
    {
        player.stateData = player.stateData + data;
        return player;
    }

    public static PlayerState operator +(PlayerState player, Vector2 velocity)
    {
        player.stateData.velocity += velocity;
        return player;
    }

    public static PlayerState operator +(PlayerState player, FGAttackData data)
    {
        Vector2 attackDirection = player.GetDirection() == Direction.LEFT ? data.attackVector.normalized : data.attackVector.normalized * new Vector2(-1, 1);

        bool canBlock = !player.CheckState(PlayerFrameState.ATTACK_CANCELLABLE) &&
            !player.CheckState(PlayerFrameState.ATTACK_NONCANCELLABLE) &&
            !player.CheckState(PlayerFrameState.HITSTUN);

        canBlock = canBlock &&
            ((attackDirection.x < 0 && player.IsFacingRight() && player.inputState.Left()) || (attackDirection.x > 0 && !player.IsFacingRight() && player.inputState.Right()));

        bool attackIsOverhead = attackDirection.y < 0;

        bool isCrouching = player.CheckState(PlayerFrameState.CROUCHING);

        if ((data.guard == Guard.HIGH || data.guard == Guard.MID) && canBlock && !isCrouching)
        {
            //blocked
            player.AddState(PlayerFrameState.BLOCKSTUN);
            player.nextActionDelay = data.frameAdvOnBlock;
            player.stateData.velocity = (new Vector2(attackDirection.x * data.damage, 0) * World.instance.BlockedKnockbackModifier);
        }

        if (data.guard == Guard.LOW && canBlock && isCrouching && !attackIsOverhead)
        {
            //blocked
            player.AddState(PlayerFrameState.BLOCKSTUN);
            player.nextActionDelay = data.frameAdvOnBlock;
            player.stateData.velocity = (new Vector2(attackDirection.x * data.damage, 0) * World.instance.BlockedKnockbackModifier);
        }

        if (!canBlock)
        {
            player.AddState(PlayerFrameState.HITSTUN);
            player.nextActionDelay = data.frameAdvOnHit;
            player.HP -= data.damage;
            player.stateData.velocity = new Vector2(attackDirection.x * data.damage, attackDirection.y * data.damage);
        }
        return player;
    }
}

[RequireComponent(typeof(FGAnimator), typeof(PlayerInputState))]
public class Player : MonoBehaviour
{
    

    public bool initialized = false;

    //provide state
    public WorldState2D entityPhysics;
    public Hurtbox hurtbox;
    public FGAnimator anim;


    //editor stuff
    public bool controlDisabled;
    public bool alwaysBlocking;

    public Direction startingDirection;

    private PlayerState _currentState;
    private PlayerState _lastState;

    private PlayerInputState _inputState;

    [SerializeField]
    private string _currentAttackAnimation;

    public void Init()
    {
        anim = this.GetComponent<FGAnimator>();
        anim.Init();
        anim.Play("Idle");

        _currentAttackAnimation = "";

        _inputState = this.GetComponent<PlayerInputState>();
        _inputState.Init();

        _currentState = new PlayerState(startingDirection);
        _lastState = new PlayerState(startingDirection);

        entityPhysics = this.GetComponent<WorldState2D>();
        entityPhysics.Init(this.GetComponent<RectTransform>().anchoredPosition);
        hurtbox = new Hurtbox(this.GetComponent<RectTransform>());
        entityPhysics.AddCollider(hurtbox);
        //entityPhysics = new Physics2D(this.GetComponent<RectTransform>().anchoredPosition);
        //collision.SetPhysics(entityPhysics);

        initialized = true;
    }

    public PlayerFrameState GetFrameState()
    {
        return _currentState.stateData.FrameState;
    }


    //do this at the end of frame?
    public void UpdatePlayerDirection(Vector2 lookAtPosition)
    {
        if (hurtbox.GetRect().center.x > lookAtPosition.x)
        {
            _currentState.UpdateDirection(Direction.LEFT);
        }
        if (hurtbox.GetRect().center.x < lookAtPosition.x)
        {
            _currentState.UpdateDirection(Direction.RIGHT);
        }
        anim.Reverse(_currentState.IsFacingRight());
    }

    public void AnimateCurrentState(float dt)
    {
        PlayerState state = _currentState;
        if (state.CheckState(PlayerFrameState.BLOCKSTUN) || state.CheckState(PlayerFrameState.HITSTUN))
        {
            if (state.CheckState(PlayerFrameState.BLOCKSTUN))
                anim.Play("Block");
            else
            {
                anim.Play("WasHit1");
            }
        }
        else if (state.IsAttacking())
        {
            anim.Play(_currentAttackAnimation);
            /*if ((_currentState & PlayerState.AERIAL) == 0)
            {
                entityPhysics.SetVelocity(Vector2.zero);
            }*/
        }
        else
        {
            if (state.CheckState(PlayerFrameState.AERIAL))
            {
                if (state.stateData.velocity.y > 0)
                    anim.Play("NJumpAscent");
                else if (state.stateData.velocity.y < 0)
                    anim.Play("NJumpDescent");
            }
            else
            {
                if(state.CheckState(PlayerFrameState.CROUCHING))
                {
                    if (!_lastState.CheckState(PlayerFrameState.CROUCHING) || anim.GetCurrent().animName == "Crouching")
                        anim.Play("Crouching");
                    else
                        anim.Play("Crouch");
                }
                else if ((state.IsFacingRight() && state.stateData.velocity.x > 0) || (!state.IsFacingRight() && state.stateData.velocity.x < 0))
                    anim.Play("WalkForward");
                else if ((!state.IsFacingRight() && state.stateData.velocity.x > 0) || (state.IsFacingRight() && state.stateData.velocity.x < 0))
                    anim.Play("WalkBack");
                else
                    anim.Play("Idle");
            }
        }

        anim.UpdateAnimator(dt);
        entityPhysics.FinalizeFrame(dt);
    }

    //should this be done before the animation?
    public void ResolveState(PlayerState unresolvedState)
    {
        PlayerState lastFrame = _currentState;
        PlayerState thisFrame = unresolvedState;

        bool grounded = (thisFrame.stateData.Collisions & CollisionState.BOTTOM) != 0;
        bool lastFrameGrounded = (lastFrame.stateData.Collisions & CollisionState.BOTTOM) != 0;
        bool justLanded = grounded && lastFrame.CheckState(PlayerFrameState.AERIAL);
        bool beganAerial = !grounded && !lastFrame.CheckState(PlayerFrameState.AERIAL);

        //check animator to see if something has finished
        bool startedAttack = anim.InAttackAnimation() || thisFrame.IsAttacking();
        //check animator to see if something has finished
        bool endedAttack = !anim.InAttackAnimation() && lastFrame.IsAttacking();

        if (endedAttack)
        {
            _currentAttackAnimation = "";
            thisFrame.RemoveState(PlayerFrameState.ATTACK_CANCELLABLE);
            thisFrame.RemoveState(PlayerFrameState.ATTACK_NONCANCELLABLE);
        }


        if (thisFrame.stateData.velocity.x != 0)
            thisFrame.AddState(PlayerFrameState.MOVING_HORIZ);
        else
            thisFrame.RemoveState(PlayerFrameState.MOVING_HORIZ);

        if (thisFrame.stateData.velocity.y != 0)
            thisFrame.AddState(PlayerFrameState.MOVING_VERT);
        else
            thisFrame.RemoveState(PlayerFrameState.MOVING_HORIZ);


        if (!grounded)
        {
            thisFrame.AddState(PlayerFrameState.AERIAL);
        }
        else
        {
            thisFrame.RemoveState(PlayerFrameState.AERIAL);
        }

        if (lastFrame.CheckState(PlayerFrameState.HITSTUN) || lastFrame.CheckState(PlayerFrameState.BLOCKSTUN))
        {
            thisFrame.nextActionDelay--;
            if(thisFrame.nextActionDelay == 0)
            {
                thisFrame.RemoveState(PlayerFrameState.HITSTUN);
                thisFrame.RemoveState(PlayerFrameState.BLOCKSTUN);
            }
        }
        _lastState = _currentState;
        _currentState = thisFrame;
        entityPhysics.UpdateState(_currentState.stateData);
        //Debug.Log(PrintState());
    }

    public PlayerState GetInputState(float dt)
    {
        if (!controlDisabled)
            _inputState.GetInputState();
        PlayerState state = new PlayerState(_currentState, _inputState);
        bool inAttack = state.CheckState(PlayerFrameState.ATTACK_CANCELLABLE) || state.CheckState(PlayerFrameState.ATTACK_NONCANCELLABLE);

        if (inAttack)
        {
            state += anim.GetFrameMovement(dt);
            //produce hit boxes here...
            World.instance.UpdateWorldHitboxes(anim.GetFrameHitbox(hurtbox));
            //state += anim.GetFrameHitboxRelativeToCharacter();
        }
        else
        {
           

            bool inAir = state.CheckState(PlayerFrameState.AERIAL);


            bool inNeutral = !inAttack
                && !state.CheckState(PlayerFrameState.HITSTUN)
                && !state.CheckState(PlayerFrameState.BLOCKSTUN);

            if (!inAir && !inAttack && inNeutral)
            {
                if (_inputState.y < 0)
                {
                    state.AddState(PlayerFrameState.CROUCHING);
                    _currentAttackAnimation = GetCrouchingButtons();
                    if (_currentAttackAnimation != "")
                    {
                        state.AddState(PlayerFrameState.ATTACK_NONCANCELLABLE);
                    }
                    state.stateData.velocity = Vector2.zero;
                }
                else
                {
                    state.RemoveState(PlayerFrameState.CROUCHING);
                    _currentAttackAnimation = GetStandingGroundedButtons();
                    if (_currentAttackAnimation != "")
                    {
                        state.AddState(PlayerFrameState.ATTACK_NONCANCELLABLE);
                        state.stateData.velocity = Vector2.zero;
                    }
                    else
                    {
                        Vector2 velocity = GetGroundMovement(_inputState);
                        state.stateData.velocity = velocity;
                    }
                }
            }
            else
            {
                _currentAttackAnimation = GetAerialButtons();
                if (_currentAttackAnimation != "")
                {
                    state.AddState(PlayerFrameState.ATTACK_NONCANCELLABLE);
                }
            }
        }

        return state;
    }

    private Vector2 GetGroundMovement(PlayerInputState inputs)
    {
        float x = inputs.x;
        float y = inputs.y;

        return new Vector2(x * World.instance.GroundMovementSpeed, y * World.instance.AirMovementSpeed);
    }

    private string GetStandingGroundedButtons()
    {
        if(_inputState.Light())
        {
            return "StandingL";
        }
        if (_inputState.Medium())
        {
            return "StandingM";
        }
        if (_inputState.Heavy())
        {
            return "StandingH";
        }
        return "";
    }

    private string GetAerialButtons()
    {
        return "";
    }

    private string GetCrouchingButtons()
    {
        if (_inputState.Light())
        {
            return "CrouchingL";
        }
        if (_inputState.Medium())
        {
            return "CrouchingM";
        }
        if (_inputState.Heavy())
        {
            return "CrouchingH";
        }
        return "";
    }
}
