using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldState2D : MonoBehaviour
{
    private RectTransform _rt;
    private List<FGRectCollider> _colliders;

    public CollidableType[] type;

    [SerializeField]
    private Vector2 _position;
    [SerializeField]
    private Vector2 _prevFramePosition;

    [SerializeField]
    private Vector2 _velocity;
    [SerializeField]
    private Vector2 _prevFrameVelocity;

    private void Awake()
    {
        _colliders = new List<FGRectCollider>();
        _position = new Vector2(0, 0);
        _velocity = new Vector2(0, 0);
        _rt = this.GetComponent<RectTransform>();
        for(int i = 0; i < type.Length; i++)
        {
            FGRectCollider collider = new FGRectCollider(_rt);
            collider.type = type[i];
            _colliders.Add(collider);
        }
    }

    public void AddCollider(FGRectCollider collider)
    {
        _colliders.Add(collider);
    }

    public List<FGRectCollider> GetColliders()
    {
        return _colliders;
    }


    public void Init(Vector2 position)
    {
        _position = position;
    }

    public void UpdateState(PlayerStateData data)
    {
        _velocity = data.velocity;
    }

    public void FinalizeFrame(float dt)
    {
        _prevFramePosition = _position;
        _position += (_velocity * dt);

        _rt.anchoredPosition += (_velocity * dt);
        foreach(FGRectCollider collider in _colliders)
        {
            collider.UpdateRectPosition(_velocity * dt);
        }

        _prevFrameVelocity = _velocity;
    }
}

public enum CollisionState
{
    NONE = 0, RIGHT = 1, LEFT = 2, BOTTOM = 4, TOP = 8
}

public enum CollidableType
{
    NONE = 0, HURTBOX = 1 << 0, STATIC = 1 << 1, HITBOX = 1 << 2
}

[SerializeField]
public class FGRectCollider
{
    [SerializeField]
    protected Rect _rect;

    [SerializeField]
    public CollidableType type;
    [SerializeField]
    public CollidableType collidesWith;

    public FGRectCollider(RectTransform t)
    {
        _rect = new Rect(t.anchoredPosition, t.rect.size);
        //_rect = new Rect(position, rect.size);
    }

    public FGRectCollider(Vector2 position, Vector2 size)
    {
        _rect = new Rect(position, size);
        //_rect = new Rect(position, rect.size);
    }

    public void UpdateRectPosition(Vector2 deltaPosition)
    {
        _rect.position += deltaPosition;
    }

    public Rect GetRect()
    {
        return _rect;
    }

    public FGRectCollider GetNextRect(Vector2 dp)
    {
        //check collisions after dv would be applied
        return new FGRectCollider(_rect.position + dp, _rect.size);
    }

    void ResolveCollision(ref PlayerStateData resolutionState, Vector2 overlap)
    {
        Vector2 pushBack = -overlap / Time.fixedDeltaTime;

        if (overlap.x > 0)
            resolutionState.Collisions |= CollisionState.RIGHT;
        if (overlap.x < 0)
            resolutionState.Collisions |= CollisionState.LEFT;
        if (overlap.y > 0)
            resolutionState.Collisions |= CollisionState.TOP;
        if (overlap.y < 0)
            resolutionState.Collisions |= CollisionState.BOTTOM;

        resolutionState.velocity += pushBack;
    }

    PlayerStateData ResolveStaticCollision(FGRectCollider nextFrame, FGRectCollider other, Vector2 velocity)
    {
        PlayerStateData resolutionState = new PlayerStateData();
        if (Collides(nextFrame, other))
        {
            Vector2 overlap = CheckRectCollision(nextFrame.GetRect(), velocity, other.GetRect());
            ResolveCollision(ref resolutionState, overlap);
        }
        return resolutionState;
    }

    public PlayerState ResolveCollisionState(PlayerState state, FGRectCollider[] colliders, float dt)
    {
        for (int i = 0; i < 2; i++)
        {
            Vector2 vel = new Vector2(0, 0);
            if (i == 0)
                vel = new Vector2(state.stateData.velocity.x, 0);
            else
                vel = new Vector2(0, state.stateData.velocity.y);
            Vector2 dp = vel * dt;

            //check collisions after dv would be applied
            FGRectCollider nextRect = GetNextRect(dp);
            foreach (FGRectCollider other in colliders)
            {
                if (Collides(nextRect, other))
                {
                    if(other.type == CollidableType.STATIC)
                    {
                        state += ResolveStaticCollision(nextRect, other, vel);
                    }
                }
            }
        }
        return state;
    }

    public static bool Collides(FGRectCollider first, FGRectCollider other)
    {
        return first.GetRect().Overlaps(other.GetRect(), true);
    }

    protected static Vector2 CheckRectCollision(Rect first, Vector2 v, Rect other)
    {
        Vector2 overlap = new Vector2(0, 0);
        if (first.Overlaps(other))
        {
            //collides on the right
            if (v.x > 0 && first.xMax > other.xMin)
                overlap.x += (first.xMax - other.xMin);
            //collides on the left
            if (v.x < 0 && first.xMin < other.xMax)
                overlap.x -= (other.xMax - first.xMin);
            //collides on the top
            if (v.y > 0 && first.yMax > other.yMin)
                overlap.y += (first.yMax - other.yMin);
            //collides on the bottom
            if (v.y < 0 && first.yMin < other.yMax)
                overlap.y -= (other.yMax - first.yMin);
        }
        return overlap;
    }

    public void DebugShow(float dt)
    {
        Vector3 topLeft = new Vector2(_rect.position.x, _rect.position.y + _rect.size.y);
        Vector3 bottomLeft = new Vector2(_rect.position.x, _rect.position.y);
        Vector3 topRight = new Vector2(_rect.position.x + _rect.size.x, _rect.position.y + _rect.size.y);
        Vector3 bottomRight = new Vector2(_rect.position.x + _rect.size.x, _rect.position.y);

        Color debugColor = Color.blue;
        if (type == CollidableType.HITBOX)
            debugColor = Color.red;
        else if (type == CollidableType.HURTBOX)
            debugColor = Color.green;

        Debug.DrawLine(bottomLeft, bottomRight, debugColor, dt);
        Debug.DrawLine(bottomLeft, topLeft, debugColor, dt);
        Debug.DrawLine(bottomRight, topRight, debugColor, dt);
        Debug.DrawLine(topLeft, topRight, debugColor, dt);
    }
}

public class Hitbox : FGRectCollider
{
    private FGAttackData _data;
    private Player _owner;

    public Hitbox(RectTransform rt, FGAttackData data, Player owner) : base(rt)
    {
        _data = data;
        _owner = owner;
        type = CollidableType.HITBOX;
    }

    public Hitbox(Vector2 position, Vector2 size, FGAttackData data, Player owner) : base(position, size)
    {
        _data = data;
        _owner = owner;
        type = CollidableType.HITBOX;
    }

    public bool IsOwner(ref Player player)
    {
        return _owner == player;
    }

    public FGAttackData GetData()
    {
        return _data;
    }
}

public class Hurtbox : FGRectCollider
{
    public Hurtbox(RectTransform rt) : base(rt)
    {
        type = CollidableType.HURTBOX;
    }

    public Hurtbox(Vector2 position, Vector2 size) : base(position, size)
    {
        type = CollidableType.HURTBOX;
    }

    public static PlayerState ReceiveHit(PlayerState currentState, Hitbox hit)
    {
        FGAttackData data = hit.GetData();
        currentState += data;
        return currentState;
    }
}
