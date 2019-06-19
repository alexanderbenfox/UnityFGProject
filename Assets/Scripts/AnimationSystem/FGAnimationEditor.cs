using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FGAnimation))]
public class FGAnimationEditor : Editor
{
    //bool toggle = false;
    //int startIndex = 0;
    //int endIndex = 0;
    //int numRows = 0;
    //int numCols = 0;

    double _time;
    EditorAnimator anim;
    bool play;
    FGAnimationKeyFrame spriteToShow;

    Texture2D spriteSheet;
    int indxBeg = 0;
    int indxEnd = 0;

    private void Awake()
    {
        if (anim == null)
            anim = new EditorAnimator();
        FGAnimation data = (FGAnimation)target;
        play = false;
        _time = EditorApplication.timeSinceStartup;
        if (data.frames.Length > 0)
        {
            spriteToShow = data.frames[0];
        }
    }

    void OnEnable()
    {
        EditorAnimator anim = new EditorAnimator();
        FGAnimation data = (FGAnimation)target;
        play = false;
        _time = EditorApplication.timeSinceStartup;
        if (data.frames.Length > 0)
        {
            spriteToShow = data.frames[0];
        }
        EditorApplication.update += Update;
    }
    void OnDisable() { EditorApplication.update -= Update; }

    void Update()
    {
        if (play)
        {
            double currTime = EditorApplication.timeSinceStartup;
            if ((currTime - _time) >= FGAnimationKeyFrame.FrameTime * 2)
            {
                if (anim == null)
                    anim = new EditorAnimator();
                spriteToShow = anim.LoopAnimationPlay((FGAnimation)target, (currTime - _time));
                //call animator update here
                _time = currTime;
                Repaint();
            }
            
        }
    }

    Sprite[] GenerateFromSpriteSheet(Texture2D tex, int numRows, int numCol, int startIndex, int endIndex)
    {
        Sprite[] sprites = new Sprite[endIndex - startIndex + 1];
        int sWidth = tex.width / numCol;
        int sHeight = tex.height / numRows;
        bool nextRow = false;
        for(int y = startIndex / numRows; y < numRows; y++)
        {
            for(int x = nextRow ? 0 : startIndex % numRows; x < numCol; x++)
            {
                int index = x + y * numRows;
                sprites[index - startIndex] = Sprite.Create(tex, new Rect(sWidth * x, sHeight * y, sWidth, sHeight), new Vector2(0, 0));
                if (index >= endIndex)
                    return sprites;
            }
            nextRow = true;
        }
        return sprites;
    }

    Texture2D GenerateTextureFromSprite(Sprite aSprite)
    {
        var rect = aSprite.rect;
        var tex = new Texture2D((int)rect.width, (int)rect.height);
        var data = aSprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
        tex.SetPixels(data);
        tex.Apply(true);
        return tex;
    }

    void AddRectToTexture(ref Texture2D tex, Rect rect, Color rectColor)
    {
        if (rect.xMax >= tex.width || rect.yMax >= tex.height || rect.yMin < 0 || rect.xMin < 0)
            return;
        Color[] pixels = tex.GetPixels(0, 0, tex.width, tex.height);

        int topRow = tex.width * (int)rect.yMin;
        int botRow = tex.width * (int)rect.yMax;

        int startColumn = (int)rect.xMin;
        int endColumn = (int)rect.xMax;

        /*for(int y = 0; y < rect.height; y++)
        {
            for(int x = 0; x < rect.width; x++)
            {
                pixels[(startColumn + x) + ((int)rect.yMin + y) * tex.width] = rectColor;
            }
        }*/

        for (int x = 0; x < rect.width; x++)
        {
            pixels[(startColumn + x) + topRow] = rectColor;
            pixels[(startColumn + x) + botRow] = rectColor;
        }

        for (int y = 0; y < rect.height; y++)
        {
            int currY = ((int)rect.yMin + y) * (int)tex.width;
            pixels[ startColumn + currY ] = rectColor;
            pixels[ endColumn + currY] = rectColor;
        }

        tex.SetPixels(pixels);
        tex.Apply(true);
    }

    /*void RenderGenerateToggle(ref FGAnimation animation)
    {
        toggle = (bool)EditorGUILayout.BeginToggleGroup("Generate From Sheet", toggle);
        sheet = (Texture2D)EditorGUILayout.ObjectField(sheet, typeof(Texture2D), true);
        numRows = (int)EditorGUILayout.IntField("Number of Rows", numRows);
        numCols = (int)EditorGUILayout.IntField("Number of Columns", numCols);
        startIndex = (int)EditorGUILayout.IntField("Start Index", startIndex);
        endIndex = (int)EditorGUILayout.IntField("End Index", endIndex);

        if (GUILayout.Button("Generate"))
        {
            if (numRows > 0 && numCols > 0 && endIndex >= startIndex)
            {
                Sprite[] createdSprites = GenerateFromSpriteSheet(sheet, numRows, numCols, startIndex, endIndex);
                for (int i = 0; i < createdSprites.Length; i++)
                {
                    animation.AddFrame(new FGAnimationKeyFrame(createdSprites[i]));
                }
            }
        }
        EditorGUILayout.EndToggleGroup();
    }*/

    void BuildAnimation(string spriteSheetName, int indexBegin, int indexEnd)
    {
        if (indexEnd < indexBegin || indexEnd < 0 || indexBegin < 0)
            return;

        Sprite[] allSprites = Resources.LoadAll<Sprite>(spriteSheetName);

        if (indexEnd >= allSprites.Length || indexBegin >= allSprites.Length)
            return;

        FGAnimation data = (FGAnimation)target;
        for(int i = indexBegin; i <= indexEnd; i++)
        {
            data.AddFrame(new FGAnimationKeyFrame(allSprites[i]));
        }
    }

    void ShowAnimationPreview()
    {
        FGAnimation data = (FGAnimation)target;
        if (data.frames.Length > 0)
        {
            if (spriteToShow != null)
            {
                if(spriteToShow.type == FGAnimationStageType.ACTIVE)
                {
                    Texture2D sprite = GenerateTextureFromSprite(spriteToShow.sprite);
                    AddRectToTexture(ref sprite, spriteToShow.hitBox, Color.red);
                    GUILayout.Label(sprite);
                }
                else
                {
                    GUILayout.Label(GenerateTextureFromSprite(spriteToShow.sprite));
                }
                
            }
        }
        if (play)
        {
            if (GUILayout.Button("Stop"))
            {
                play = false;
                if (anim != null)
                    anim.Reset();
            }
        }
        else
        {
            if (GUILayout.Button("Play"))
            {
                play = true;
                if (anim != null)
                    anim.Reset();
            }

            if (GUILayout.Button("Move To Next Frame"))
            {
                anim.NextFrame((FGAnimation)target);
                spriteToShow = anim.GetCurrent();
            }
            if (GUILayout.Button("Back To Last Frame"))
            {
                anim.LastFrame((FGAnimation)target);
                spriteToShow = anim.GetCurrent();
            }
        }

        if(anim != null)
            GUILayout.Label(new GUIContent("Frame number: " + anim.GetCurrentFrame().ToString()));
    }

    public override void OnInspectorGUI()
    {
        //base.DrawDefaultInspector();
        GUILayout.BeginVertical();
        FGAnimation data = (FGAnimation)target;

        data.animName = EditorGUILayout.TextField("Animation name: ", data.animName) as string;
        data.type = (FGAnimationType)EditorGUILayout.Popup("Animation play type: ", (int)data.type, new string[]{ "Loop", "Once", "Bounce", "Reverse"});

        //data.positionOffset = (Vector2)EditorGUILayout.Vector2Field("Offset: ", data.positionOffset);

        data.imageOffset = (Vector2)EditorGUILayout.Vector2Field("Image Offset Position: ", data.imageOffset);
        data.imageSize = (Vector2)EditorGUILayout.Vector2Field("Image Size: ", data.imageSize);

        if (data.type == FGAnimationType.ONCE)
        {
            data.nextAnimation = EditorGUILayout.TextField("Next animation to go to after this one completes: ", data.nextAnimation) as string;

            if (data.attackData == null)
            {
                data.attackData = new FGAttackData();
            }
            data.attackData.guard = (Guard)EditorGUILayout.Popup("Guard Type: ", (int)data.attackData.guard, new string[] { "LOW", "MID", "HIGH"});
            data.attackData.frameAdvOnBlock = (int)EditorGUILayout.IntField("Frame adv on block: ", (int)data.attackData.frameAdvOnBlock);
            data.attackData.frameAdvOnHit = (int)EditorGUILayout.IntField("Frame adv on hit: ", (int)data.attackData.frameAdvOnHit);
            data.attackData.damage = (int)EditorGUILayout.IntField("Damage: ", (int)data.attackData.damage);
            data.attackData.attackVector = (Vector2)EditorGUILayout.Vector2Field("Direction vector for attack pushback", data.attackData.attackVector);
        }

        //RenderGenerateToggle(ref data);

        //List<int> removeFrames = new List<int>();
        if (data.frames.Length <= 0)
        {
            GUILayout.Label("Create animation from sprite sheet?");
            spriteSheet = (Texture2D)EditorGUILayout.ObjectField(spriteSheet, typeof(Texture2D), true);

            indxBeg = (int)EditorGUILayout.IntField("Starting Index: ", indxBeg);
            indxEnd = (int)EditorGUILayout.IntField("Ending Index: ", indxEnd);

            if (GUILayout.Button("Generate"))
            {
                if(spriteSheet != null)
                {
                    BuildAnimation(spriteSheet.name, indxBeg, indxEnd);
                }
            }
        }
        else
        {

            for (int i = 0; i < data.frames.Length; i++)
            {
                GUILayout.BeginVertical();
                FGAnimationStageType stage = (FGAnimationStageType)EditorGUILayout.Popup("Stage in animation: ", (int)data.frames[i].type, new string[] { "Startup", "Active", "Recovery" });
                int frameCount = (int)EditorGUILayout.IntField("Frame count: ", data.frames[i].frameCount);
                Sprite sprite = (Sprite)EditorGUILayout.ObjectField(data.frames[i].sprite, typeof(Sprite), true);

                data.ModifyFrameAtIndex(i, sprite, frameCount, stage);

                if (stage == FGAnimationStageType.ACTIVE)
                {
                    if (data.frames[i].hitBox == null)
                    {
                        data.frames[i].hitBox = new Rect();
                    }

                    data.frames[i].hitBox = (Rect)EditorGUILayout.RectField(data.frames[i].hitBox);
                }


                GUILayout.EndVertical();
                /*GUILayout.BeginHorizontal();
                for (int j = 0; j < data.frames[i].frameCount; j++)
                {
                    if (sprite.texture)
                    {

                    }   
                }
                GUILayout.EndHorizontal();*/
                if (GUILayout.Button("Remove Key"))
                {
                    data.RemoveFrame(i);
                    break;
                }
            }
        }

        if (GUILayout.Button("Add Key Frame"))
        {
            data.AddFrame(new FGAnimationKeyFrame());
        }

        ShowAnimationPreview();
        GUILayout.EndVertical();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }
    }
}
