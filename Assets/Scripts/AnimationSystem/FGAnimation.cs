using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Animation", menuName = "Custom/Animation", order = 1)]
[System.Serializable]
public class FGAnimation : ScriptableObject
{
    public FGAnimation()
    {
        frames = new FGAnimationKeyFrame[0];
        //positionOffset = Vector2.zero;
    }
    //!
    [SerializeField]
    public string animName;
    //!
    [SerializeField]
    public FGAnimationKeyFrame[] frames;
    //!
    public void AddFrame(FGAnimationKeyFrame frame)
    {
        FGAnimHelper.AddFrame(ref frames);
        frames[frames.Length - 1] = frame;
        for (int i = 0; i < frame.frameCount; i++)
        {
            _frameToKeyFrame.Add(frames.Length - 1);
        }
        totalFrameCount += frame.frameCount;
    }
    //!
    public void RemoveFrame(int indx)
    {
        if (indx < frames.Length)
        {
            totalFrameCount -= frames[indx].frameCount;
            int startIndex = 0;
            for (int i = 0; i < _frameToKeyFrame.Count; i++)
            {
                if (_frameToKeyFrame[i] == indx)
                {
                    startIndex = i;
                    break;
                }
            }
            _frameToKeyFrame.RemoveRange(startIndex, frames[indx].frameCount);
            FGAnimHelper.RemoveFrame(ref frames, indx);
        }
    }
    //!
    public void ModifyFrameAtIndex(int indx, Sprite s, int fc, FGAnimationStageType t)
    {
        if (indx < frames.Length)
        {
            if (s != null)
                frames[indx].sprite = s;
            if (fc >= 0)
            {
                totalFrameCount += (fc - frames[indx].frameCount);
                frames[indx].frameCount = fc;
                RecalculateFrameToKeyFrame();
            }
            if (t != FGAnimationStageType.NUM_TYPES)
                frames[indx].type = t;
        }
    }
    //!
    public FGAnimationKeyFrame GetKeyFrame(int frameNumber)
    {
        if(totalFrameCount != _frameToKeyFrame.Count)
        {
            RecalculateFrameToKeyFrame();
        }
        if (frameNumber < totalFrameCount)
        {

            return frames[_frameToKeyFrame[frameNumber]];

        }
        return null;
    }

    private void RecalculateFrameToKeyFrame()
    {
        _frameToKeyFrame = new List<int>();
        for (int i = 0; i < frames.Length; i++)
        {
            for (int j = 0; j < frames[i].frameCount; j++)
            {
                _frameToKeyFrame.Add(i);
            }
        }
    }
    //!
    [SerializeField]
    public FGAnimationType type;
    //!
    public int totalFrameCount;
    //!
    public float duration
    {
        get
        {
            return FGAnimationKeyFrame.FrameTime * totalFrameCount;
        }
    }

    private List<int> _frameToKeyFrame;
    public int GetCurrentFrame(float percentAnimation)
    {
        return Mathf.FloorToInt(percentAnimation * totalFrameCount);
    }

    //[SerializeField]
    //public Vector2 positionOffset;

    [SerializeField]
    public FGAttackData attackData;

    [SerializeField]
    public Vector2 imageOffset;

    [SerializeField]
    public Vector2 imageSize;

    //animation to go into after this one is done
    [SerializeField]
    public string nextAnimation = "";
}
