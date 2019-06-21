using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class DebugTextbox : MonoBehaviour
{
    private Text _text;
    public MaskableGraphic parent;

    public void Init()
    {
        _text = this.GetComponent<Text>(); 
    }

    public void Show()
    {
        parent.enabled = true;
        _text.enabled = true;
    }

    public void Hide()
    {
        parent.enabled = false;
        _text.enabled = false;
    }

    public void RenderPlayerState(Player player)
    {
        _text.text = DebuggingTools.PrintPlayerState(player.GetFrameState());
    }
}
