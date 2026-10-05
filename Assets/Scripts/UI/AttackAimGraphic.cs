using UnityEngine;
using UnityEngine.UI;
public class AttackAimGraphic : Graphic
{
    public Vector2 start, end;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Vector2 direction = end - start;
        if (direction.sqrMagnitude < 1f) return;
        Vector2 unit = direction.normalized;
        Vector2 perpendicular = new Vector2(-unit.y, unit.x);
        Vector2 neck = end - unit * Mathf.Min(24f, direction.magnitude * .4f);
        Vector2[] points = { start - perpendicular * 3, start + perpendicular * 3,
            neck + perpendicular * 3, neck - perpendicular * 3,
            neck - perpendicular * 12, neck + perpendicular * 12, end };
        foreach (Vector2 point in points) vh.AddVert(point, color, Vector2.zero);
        vh.AddTriangle(0, 1, 2); vh.AddTriangle(0, 2, 3); vh.AddTriangle(4, 5, 6);
    }
    public void SetEndpoints(Vector2 a, Vector2 b) { start = a; end = b; SetVerticesDirty(); }
}
