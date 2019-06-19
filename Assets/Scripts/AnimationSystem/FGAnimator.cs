using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public enum FGAnimationStageType
{
    STARTUP, ACTIVE, RECOVERY, NUM_TYPES
}

public enum FGAnimationType
{
    LOOP, ONCE, BOUNCE, REVERSE, NUM_TYPES
}



[CustomPropertyDrawer(typeof(FGAnimationKeyFrame))]
public class FrameDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Calculate rects
        var imageRect = new Rect(position.x, position.y, 30, position.height);

        // Draw fields - passs GUIContent.none to each so they are drawn without labels
        EditorGUI.PropertyField(imageRect, property.FindPropertyRelative("sprite"), GUIContent.none);

        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }
}

[System.Serializable]
public class FGAnimationKeyFrame
{
    public FGAnimationKeyFrame()
    {
        frameCount = 0;
        sprite = null;
        type = FGAnimationStageType.NUM_TYPES;
    }
    public FGAnimationKeyFrame(Sprite s)
    {
        frameCount = 1;
        sprite = s;
        type = FGAnimationStageType.NUM_TYPES;
    }
    public static float FrameTime = 1.0f / 60.0f;
    //! Sprite that shows during this state of animation
    [SerializeField]
    public Sprite sprite;
    //!
    [SerializeField]
    public int frameCount;
    //!
    [SerializeField]
    public FGAnimationStageType type;
    //!
    [SerializeField]
    public Rect hitBox;
    //! defines the amount the character should move on this frame
    [SerializeField]
    public Vector2 deltaPosition;
}

public class FGAnimHelper
{
    public static void AddFrame(ref FGAnimationKeyFrame[] frameList)
    {
        if (frameList == null)
        {
            frameList = new FGAnimationKeyFrame[1];
        }
        else
        {
            FGAnimationKeyFrame[] cpy = new FGAnimationKeyFrame[frameList.Length + 1];
            for (int i = 0; i < frameList.Length; i++)
            {
                cpy[i] = frameList[i];
            }
            frameList = cpy;
        }
        frameList[frameList.Length - 1] = new FGAnimationKeyFrame();
    }

    public static void RemoveFrame(ref FGAnimationKeyFrame[] frameList, int index)
    {
        if (frameList != null && index < frameList.Length)
        {
            FGAnimationKeyFrame[] cpy = new FGAnimationKeyFrame[frameList.Length - 1];
            for (int i = 0; i < frameList.Length; i++)
            {
                if(i < index)
                    cpy[i] = frameList[i];
                else if (i > index)
                    cpy[i - 1] = frameList[i];
            }
            frameList = cpy;
        }
    }
}

public class AnimationPlayer
{
    public static int GetCurrentFrame(FGAnimation animation, float time)
    {
        return animation.GetCurrentFrame(time / animation.duration);
    }
}

public class EditorAnimator
{
    //!
    FGAnimationKeyFrame _currentKeyFrame;
    double animatorTimer = 0;

    int currentFrame;

    public EditorAnimator()
    {
        animatorTimer = 0;
        _currentKeyFrame = null;
    }

    public void Reset()
    {
        animatorTimer = 0;
        currentFrame = 0;
    }

    private void ApplyFrame(FGAnimationKeyFrame frame)
    {
        _currentKeyFrame = frame;
    }

    public int GetCurrentFrame()
    {
        return currentFrame;
    }

    public void NextFrame(FGAnimation animation)
    {
        if (animation.totalFrameCount == (currentFrame + 1))
            currentFrame = 0;
        else
            currentFrame++;
        FGAnimationKeyFrame keyFrame = animation.GetKeyFrame(currentFrame);
        ApplyFrame(keyFrame);
    }

    public void LastFrame(FGAnimation animation)
    {
        if ((currentFrame - 1) < 0)
            currentFrame = animation.totalFrameCount - 1;
        else
            currentFrame--;
        FGAnimationKeyFrame keyFrame = animation.GetKeyFrame(currentFrame);
        ApplyFrame(keyFrame);
    }

    public FGAnimationKeyFrame GetCurrent()
    {
        return _currentKeyFrame;
    }

    public FGAnimationKeyFrame LoopAnimationPlay(FGAnimation animation, double dt)
    {
        bool reset = animatorTimer >= animation.duration;
        if (reset)
        {
            animatorTimer -= animation.duration;
        }

        float adjustedTime = (float)animatorTimer;
        if (animation.type == FGAnimationType.REVERSE)
            adjustedTime = animation.duration - adjustedTime;

        currentFrame = AnimationPlayer.GetCurrentFrame(animation, adjustedTime);
        FGAnimationKeyFrame keyFrame = animation.GetKeyFrame(currentFrame);

        animatorTimer += dt;

        if (keyFrame != null)
            ApplyFrame(keyFrame);


        if(_currentKeyFrame != null)
            return _currentKeyFrame;
        return animation.frames[0];
    }
}

public class FGAnimator : MonoBehaviour
{
    public enum AnimatorState
    {
        FREE, IN_ATTACK
    }
    //!
    public FGAnimation[] editorAnimationList;
    //!
    public string fallBackAnimationName;
    //! Animation diction that the editor list is transferred to
    private Dictionary<string, FGAnimation> _animations;

    //! Current animation
    [SerializeField]
    private string _currentAnimation;
    //! Default animation
    private string _defaultAnimation;
    //! Time kept by animator
    private float _animatorTime = 0;
    //!
    public Image[] _layerRenderers;
    //!
    FGAnimationKeyFrame _currentKeyFrame;
    //!
    Vector2 _currentOffset;
    //!
    Vector2 _originalOffset;

    private AnimatorState _currentState;

    private FGAnimationStageType _currentStage;

    private Image _mainLayer;
    private Image _secondaryLayer;

    private Player _player;

    //private Rect _activeHitBox;
    //public bool hitBoxActive;

    private bool _reversed;

    public bool InAttackAnimation()
    {
        return _currentState == AnimatorState.IN_ATTACK;
    }

    //public Rect GetCurrentHitBox()
    //{
    //    return _activeHitBox;
    //}

    // Start is called before the first frame update
    public void Init()
    {
        //initialize all components necessary
        //_spriteController = this.GetComponent<Image>();
        _player = this.GetComponent<Player>();

        _defaultAnimation = fallBackAnimationName;
        _currentAnimation = _defaultAnimation;
        _currentOffset = Vector2.zero;

        _mainLayer = _layerRenderers[0];

        _originalOffset = _mainLayer.rectTransform.anchoredPosition;

        _animations = new Dictionary<string, FGAnimation>();
        for(int i = 0; i < editorAnimationList.Length; i++)
        {
            string textureSetName = editorAnimationList[i].frames[0].sprite.texture.name;
            World.instance.LoadSprites(textureSetName);
            FGAnimation loadedAnim = Resources.Load<FGAnimation>("Animations\\" + editorAnimationList[i].name);
            if (loadedAnim == null)
                loadedAnim = Resources.Load<FGAnimation>("Animations\\AttackAnimations\\" + editorAnimationList[i].name);
            _animations.Add(editorAnimationList[i].animName, loadedAnim);
        }
    }

    public Vector2 GetFrameMovement(float dt)
    {
        return _currentKeyFrame.deltaPosition / dt;
    }

    public Hitbox GetFrameHitbox(FGRectCollider playerCollider)
    {
        if (_currentKeyFrame.hitBox != null)
        {
            float originalSizeToWorldSizeX = _layerRenderers[0].GetComponent<RectTransform>().rect.size.x / _currentKeyFrame.sprite.rect.size.x;
            float originalSizeToWorldSizeY = _layerRenderers[0].GetComponent<RectTransform>().rect.size.y / _currentKeyFrame.sprite.rect.size.y;

            Vector2 editorToWorld = new Vector2(originalSizeToWorldSizeX, originalSizeToWorldSizeY);

            Rect hb = new Rect(_currentKeyFrame.hitBox.position * editorToWorld, _currentKeyFrame.hitBox.size * editorToWorld);

            

            Hitbox worldSpaceHitbox;
            if (_reversed)
            {
                Vector2 currentSpriteBottomRight = new Vector2(playerCollider.GetRect().position.x + playerCollider.GetRect().size.x, playerCollider.GetRect().position.y);
                currentSpriteBottomRight = new Vector2(currentSpriteBottomRight.x - _originalOffset.x, currentSpriteBottomRight.y);
                //Debug.DrawLine(currentSpriteBottomRight, currentSpriteBottomRight + -Vector2.one * 1000, Color.blue, 1000000);

                //update the position because the point must remain on the top left while being on opposite side
                hb.position = new Vector2(-hb.position.x - hb.size.x, hb.position.y);

                worldSpaceHitbox = new Hitbox(currentSpriteBottomRight + hb.position, hb.size, _animations[_currentAnimation].attackData, _player);
            }
            else
            {

                Vector2 currentSpriteBottomLeft = new Vector2(playerCollider.GetRect().position.x, playerCollider.GetRect().position.y);
                currentSpriteBottomLeft = new Vector2(currentSpriteBottomLeft.x + _originalOffset.x, currentSpriteBottomLeft.y);
                //Debug.DrawLine(currentSpriteBottomLeft, currentSpriteBottomLeft + Vector2.one * 1000, Color.blue, 1000000);
                worldSpaceHitbox = new Hitbox(currentSpriteBottomLeft + hb.position, hb.size, _animations[_currentAnimation].attackData, _player);
            }

            
            return worldSpaceHitbox;
        }
        return null;
        //Hitbox hitbox = new Hitbox(hb.position, hb, _animations[_currentAnimation].attackData);
    }

    private void ApplyFrame(FGAnimationKeyFrame frame)
    {
        _mainLayer.sprite = frame.sprite;
        _currentStage = frame.type;
        _currentKeyFrame = frame;
        /*if (frame.type == FGAnimationStageType.ACTIVE)
        {
            hitBoxActive = true;
            _activeHitBox = new Rect(frame.hitBox.position + _currentOffset, frame.hitBox.size);
        }
        else
        {
            hitBoxActive = false;
        }*/
    }

    public FGAnimation GetCurrent()
    {
        return _animations[_currentAnimation];
    }

    public void Reverse(bool facingRight)
    {
        _reversed = !facingRight;
        for (int i = 0; i < _layerRenderers.Length; i++)
        {
            if (_reversed)
            {
                _layerRenderers[i].rectTransform.localScale = new Vector3(-1, 1, 1);
                _layerRenderers[i].rectTransform.anchoredPosition = _animations[_currentAnimation].imageOffset * _layerRenderers[i].rectTransform.localScale;
            }
            else
            {
                _layerRenderers[i].rectTransform.localScale = new Vector3(1, 1, 1);
                _layerRenderers[i].rectTransform.anchoredPosition = _animations[_currentAnimation].imageOffset;
            }
        }
    }

    //!
    public void Play(string animationName)
    {
        if(_animations.ContainsKey(animationName) && animationName != _currentAnimation)
        {
            if (_animations[animationName].type == FGAnimationType.ONCE)
            {
                _currentState = AnimatorState.IN_ATTACK;
                _currentStage = FGAnimationStageType.STARTUP;
            }
            else
                _currentState = AnimatorState.FREE;

            for (int i = 0; i < _layerRenderers.Length; i++)
            {
                Vector2 rendererPosition = _layerRenderers[i].rectTransform.anchoredPosition;
                //_layerRenderers[i].rectTransform.anchoredPosition = rendererPosition + (_animations[animationName].positionOffset - _currentOffset);
                if (_reversed)
                {
                    _layerRenderers[i].rectTransform.localScale = new Vector3(-1, 1, 1);
                    _layerRenderers[i].rectTransform.anchoredPosition = _animations[animationName].imageOffset * _layerRenderers[i].rectTransform.localScale;
                }
                else
                {
                    _layerRenderers[i].rectTransform.localScale = new Vector3(1, 1, 1);
                    _layerRenderers[i].rectTransform.anchoredPosition = _animations[animationName].imageOffset;
                }

                _layerRenderers[i].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _animations[animationName].imageSize.x);
                _layerRenderers[i].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _animations[animationName].imageSize.y);
            }
            _currentOffset = _animations[animationName].imageOffset - _originalOffset;
            _currentKeyFrame = null;
            _animatorTime = 0;
            _currentAnimation = animationName;
        }
    }

    //!
    public void UpdateAnimator(float dt)
    {
        bool reset = _animatorTime >= _animations[_currentAnimation].duration;
        bool bounceAnimationReverse = false;
        //check what type of animation we're dealing with
        switch (_animations[_currentAnimation].type)
        {
            case FGAnimationType.BOUNCE:
            case FGAnimationType.LOOP:
                if (reset)
                {
                    _animatorTime -= _animations[_currentAnimation].duration;
                }
                break;
            case FGAnimationType.REVERSE:
            case FGAnimationType.ONCE:
                if(reset)
                {
                    if (_animations[_currentAnimation].nextAnimation != "")
                        Play(_animations[_currentAnimation].nextAnimation);
                    else
                        Play(_defaultAnimation);
                }
                break;
        }

        FGAnimation current = _animations[_currentAnimation];
        float adjustedTime = _animatorTime;
        if (current.type == FGAnimationType.REVERSE)
            adjustedTime = current.duration - adjustedTime;

        int currFrame = AnimationPlayer.GetCurrentFrame(_animations[_currentAnimation], adjustedTime);
        FGAnimationKeyFrame keyFrame = _animations[_currentAnimation].GetKeyFrame(currFrame);
        if (_currentKeyFrame != keyFrame)
            ApplyFrame(keyFrame);

        //at end of update, update animator's time
        _animatorTime += dt;
    }
}
