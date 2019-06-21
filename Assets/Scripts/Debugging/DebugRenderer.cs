using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugRenderer : MonoBehaviour
{
    public Material DebugLineMaterial;
    public Color hurtboxColor;
    public Color hitboxColor;
    public Color staticColor;

    private Dictionary<CollidableType, Color> _colors;

    private int _lineCount = 100;

    public int debugHitboxBorderWidth;

    public struct DrawCircleRequestData
    {
        public Vector3 center;
        public float radius;
    }

    public struct DrawRectRequest
    {
        public Vector3 topLeft, topRight, bottomLeft, bottomRight;
        public Color color;
    }
    private List<DrawRectRequest> _rectStack;

    public void Init()
    {
        _rectStack = new List<DrawRectRequest>();

        _colors = new Dictionary<CollidableType, Color>();
        _colors.Add(CollidableType.HITBOX, hitboxColor);
        _colors.Add(CollidableType.HURTBOX, hurtboxColor);
        _colors.Add(CollidableType.STATIC, staticColor);

        StartCoroutine(DrawLoop());
    }

    public void AddDebugCollider(FGRectCollider collider)
    {
        Rect r = collider.GetRect();

        Vector3 topLeft = new Vector2(r.position.x, r.position.y + r.size.y);
        Vector3 bottomLeft = new Vector2(r.position.x, r.position.y);
        Vector3 topRight = new Vector2(r.position.x + r.size.x, r.position.y + r.size.y);
        Vector3 bottomRight = new Vector2(r.position.x + r.size.x, r.position.y);

        DrawRectRequest rect = new DrawRectRequest();
        rect.topLeft = topLeft;
        rect.topRight = topRight;
        rect.bottomLeft = bottomLeft;
        rect.bottomRight = bottomRight;
        rect.color = _colors[collider.type];

        _rectStack.Add(rect);
    }

    static void CreateLineMaterial(ref Material lineMaterial)
    {
        if (!lineMaterial)
        {
            // Unity has a built-in shader that is useful for drawing simple colored things.
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;

            // Turn on alpha blending
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

            // Turn backface culling off
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

            // Turn off depth writes
            lineMaterial.SetInt("_ZWrite", 0);
        }
    }

    private void DrawSolidRectangle(Vector3 leftBottomCorner, Vector3 rightTopCorner, Color color)
    {
        for (int y = Mathf.FloorToInt(leftBottomCorner.y); y < Mathf.CeilToInt(rightTopCorner.y); y++)
        {
            DrawLine(new Vector3(leftBottomCorner.x, y, leftBottomCorner.z), new Vector3(rightTopCorner.x, y, rightTopCorner.z), color);
        }
    }

    private void DrawFilledRect(DrawRectRequest rect)
    {
        DrawSolidRectangle(rect.topLeft + new Vector3(0, -debugHitboxBorderWidth, 0), rect.topRight, rect.color);
        DrawSolidRectangle(rect.bottomLeft, rect.bottomRight + new Vector3(0, debugHitboxBorderWidth, 0), rect.color);

        Color innerColor = rect.color;
        innerColor.a = .6f;

        for(int y = (int)rect.bottomRight.y + debugHitboxBorderWidth; y < rect.topRight.y - debugHitboxBorderWidth; y++)
        {
            DrawLine(new Vector3(rect.topLeft.x + debugHitboxBorderWidth, y, rect.topLeft.z), new Vector3(rect.topRight.x - debugHitboxBorderWidth, y, rect.topRight.z), innerColor);
        }
    }

    private void DrawLine(Vector3 a, Vector3 b, Color color)
    {
        GL.Color(color);
        GL.Vertex3(a.x, a.y, a.z);
        GL.Vertex3(b.x, b.y, b.z);
    }

    private void DrawCircle(Vector3 center, float radius, Color color)
    {
        GL.Color(color);
        for (int i = 0; i < _lineCount; ++i)
        {
            float a = i / (float)_lineCount;
            float angle = a * Mathf.PI * 2;

            // One vertex at transform position
            GL.Vertex3(0, 0, 0);
            // Another vertex at edge of circle
            GL.Vertex3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        }
    }

    // Will be called after all regular rendering is done
    private IEnumerator DrawLoop()
    {
        CreateLineMaterial(ref DebugLineMaterial);

        while (true)
        {
            yield return new WaitForEndOfFrame();

            GL.PushMatrix();
            // Apply the line material
            DebugLineMaterial.SetPass(0);
            //set up an ortho perspective transform
            GL.LoadPixelMatrix();

            // Draw lines
            GL.Begin(GL.LINES);

            while (_rectStack.Count > 0)
            {
                DrawRectRequest rect = _rectStack[0];
                DrawFilledRect(rect);
                _rectStack.RemoveAt(0);
            }

            GL.End();

            GL.PopMatrix();
        }
        yield return null;
    }
}
