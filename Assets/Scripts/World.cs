using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    private static World _instance;

    public static World instance
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<World>();
            return _instance;
        }
    }

    [SerializeField]
    public float Gravity;
    [SerializeField]
    public float GroundMovementSpeed;
    [SerializeField]
    public float AirMovementSpeed;
    [SerializeField]
    public float BlockedKnockbackModifier;

    public RectTransform Anchor;

    public Player[] players;

    public WorldState2D[] staticBounds;

    private List<Hitbox> _hitBoxBounds;

    private List<Hurtbox> _hurtboxBounds;

    //!
    private Dictionary<string, Sprite[]> _loadedSprites;
    //!
    bool initialized = false;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        _hitBoxBounds = new List<Hitbox>();
        _hurtboxBounds = new List<Hurtbox>();

        _loadedSprites = new Dictionary<string, Sprite[]>();
        foreach(Player player in players)
        {
            player.Init();
        }
        initialized = true;
    }


    public void LoadSprites(string resourceName)
    {
        if(!_loadedSprites.ContainsKey(resourceName))
        {
            _loadedSprites.Add(resourceName, Resources.LoadAll<Sprite>(resourceName));
        }
    }


    Vector2 GetWorldAction()
    {
       return (new Vector2(0, -Gravity));
    }

    PlayerState ResolveHitBoxes(Player player, PlayerState potentialState)
    {
        PlayerState state = potentialState;
        foreach(Hitbox hitbox in _hitBoxBounds)
        {
            if(!hitbox.IsOwner(ref player) && FGRectCollider.Collides(hitbox, player.hurtbox))
            {
                state = Hurtbox.ReceiveHit(state, hitbox);
            }
        }
        return state;
    }

    public void UpdateWorldHitboxes(Hitbox hitbox)
    {
        _hitBoxBounds.Add(hitbox);
    }

    //resolves hit box and attack state then does static bounds
    PlayerState ResolveCollisions(Player player, PlayerState potentialState, float dt)
    {
        potentialState = ResolveHitBoxes(player, potentialState);
        foreach (WorldState2D bounds in staticBounds)
        {
            potentialState = player.hurtbox.ResolveCollisionState(potentialState, bounds.GetColliders().ToArray(), dt);
        }
        return potentialState;
    }

    void Loop(float dt)
    {
        List<PlayerState> currentStates = new List<PlayerState>();

        foreach (Player player in players)
        {
            currentStates.Add(player.GetInputState(dt));
        }


        for(int i = 0; i < currentStates.Count; i++)
        {
            //add gravity value to player state
            currentStates[i] += GetWorldAction();
        }

        for (int i = 0; i < currentStates.Count; i++)
        {
            //reset
            currentStates[i].stateData.Collisions = CollisionState.NONE;
            //using the player state and world state, modify the state by checking static collisions and hitbox collisions
            currentStates[i] = ResolveCollisions(players[i], currentStates[i], dt);
        }
        //move is also the finalizing action for the player
        for (int i = 0; i < currentStates.Count; i++)
        {
            players[i].ResolveState(currentStates[i]);
            //update the direction relative to other player
            if (i == 0)
                players[i].UpdatePlayerDirection(players[i + 1].hurtbox.GetRect().center);
            if (i == 1)
                players[i].UpdatePlayerDirection(players[i - 1].hurtbox.GetRect().center);
            //finally complete the player state - animate and move. Animate should only complete the animation state action
            players[i].AnimateCurrentState(dt);
        }

        //clean up
        foreach (Hitbox hb in _hitBoxBounds)
            hb.DebugShow(dt);
        players[0].hurtbox.DebugShow(dt);
        players[1].hurtbox.DebugShow(dt);
        _hitBoxBounds.Clear();
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        foreach(Player player in players)
        {
            if (!player.initialized)
                return;
        }
        float dt = Time.fixedDeltaTime;
        Loop(dt);
    }
}
