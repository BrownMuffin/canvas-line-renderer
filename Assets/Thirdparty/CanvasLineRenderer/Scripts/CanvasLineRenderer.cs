using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class CanvasLineRenderer : Graphic
{
    private enum Rotate { UP, DOWN, LEFT, RIGHT };

    [SerializeField][Range(0f, 1f)]
    private float fillAmount = 1f;

    [SerializeField] private List<Point> points;

    [SerializeField] private bool drawLines = true;
    [SerializeField] private float lineThickness;
    [SerializeField] private bool drawEndcaps = true;
    [SerializeField] private float endcapSize;
    [SerializeField] private bool drawCheckpoints = true;
    [SerializeField] private float checkpointSize;
    [SerializeField] private bool drawProgress = true;
    [SerializeField] private float progressSize;

    [SerializeField] private Rotate rotateIcons;

    // Split the texture up in four uvs
    private Vector2[] uvsLine       = { new Vector2(0.5f, 1.0f), new Vector2(0.5f, 0.5f), new Vector2(0.0f, 0.5f), new Vector2(0.0f, 1.0f) };
    private Vector2[] uvsEndcap     = { new Vector2(1.0f, 1.0f), new Vector2(1.0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1.0f) };
    private Vector2[] uvsCheckpoint = { new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.0f), new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.5f) };
    private Vector2[] uvsProgress   = { new Vector2(1.0f, 0.5f), new Vector2(1.0f, 0.0f), new Vector2(0.5f, 0.0f), new Vector2(0.5f, 0.5f) };

    public override Texture mainTexture
    {
        get
        {
            return material?.mainTexture == null ? s_WhiteTexture : material.mainTexture;
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        // Don't do anything if there aren't or less than 2 points defined
        if (points == null || points.Count < 2)
        {
            return;
        }

        // Scale everything based on the rect transform size
        Vector2 scale = new Vector2(rectTransform.rect.width, rectTransform.rect.height);
        float scaledLineThickness = lineThickness * scale.y;
        float scaledEndcapSize = endcapSize * scale.y;
        float scaledCheckpointSize = checkpointSize * scale.y;
        float scaledProgressSize = progressSize * scale.y;

        List<Vector2> scaledPoints = new List<Vector2>();
        foreach (var point in points)
        {
            scaledPoints.Add(new Vector2(point.Position.x * scale.x, point.Position.y * scale.y));
        }

        // Create the lines
        if (drawLines)
        {
            CalculateLine(vh, scaledPoints, scaledLineThickness);
        }

        // Create the endcaps
        if (drawEndcaps)
        {
            CalculateEndcaps(vh, scaledPoints, scaledEndcapSize);
        }

        // Create the checkpoints
        if (drawCheckpoints)
        {
            CalculateCheckpoints(vh, scaledPoints, points, scaledCheckpointSize);
        }

        // Create the progress icon
        if (drawProgress)
        {
            CalculateProgress(vh, scaledPoints, scaledProgressSize);
        }

        // Create the triangles
        for (int i = 0; i < vh.currentVertCount; i += 4)
        {
            vh.AddTriangle(i, i + 1, i + 3);
            vh.AddTriangle(i + 2, i + 3, i + 1);
        }
    }

    private void CalculateLine(VertexHelper vh, List<Vector2> points, float scale)
    {
        Vector2[] vps = new Vector2[] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
        float angle;

        for (int i = 0; i < points.Count - 1; i++)
        {
            angle = CalculateAngle(points[i], points[i + 1]);
            var localFill = CalculateFillAmount(fillAmount, i, points.Count);
            var midPoint = Vector2.Lerp(points[i], points[i + 1], localFill);

            vps[0] = points[i] + new Vector2(scale * -0.5f, 0f);
            vps[1] = points[i] + new Vector2(scale * 0.5f, 0f);
            vps[2] = midPoint + new Vector2(scale * 0.5f, 0f);
            vps[3] = midPoint + new Vector2(scale * -0.5f, 0f);

            vps[0] = RotatePointAroundPivot(vps[0], points[i], new Vector3(0f, 0f, angle));
            vps[1] = RotatePointAroundPivot(vps[1], points[i], new Vector3(0f, 0f, angle));
            vps[2] = RotatePointAroundPivot(vps[2], midPoint, new Vector3(0f, 0f, angle));
            vps[3] = RotatePointAroundPivot(vps[3], midPoint, new Vector3(0f, 0f, angle));
            
            if (localFill <= 0f)
            {
                continue;
            }
            else if (localFill < 1f)
            {
                Vector2[] uvs = new Vector2[4];
                uvsLine.CopyTo(uvs, 0);

                float uvScale = (localFill * (uvs[2].x - uvs[0].x)) + uvs[0].x;
                uvs[2].x = uvScale;
                uvs[3].x = uvScale;

                DrawVerticies(vps, uvs, vh);
            }
            else
            {
                DrawVerticies(vps, uvsLine, vh);
            }
        }
    }

    private void CalculateEndcaps(VertexHelper vh, List<Vector2> points, float scale)
    {
        Vector2[] vps = new Vector2[] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
        float angle = 0;

        for (int i = 0; i < points.Count; i++)
        {
            if (CalculateFillAmount(fillAmount, i, points.Count) == 0f)
            {
                break;
            }

            // First point
            if (i == 0)
            {
                angle = CalculateAngle(points[i], points[i + 1]);
            }
            // Last point
            else if (i == points.Count - 1)
            {
                angle = CalculateAngle(points[i], points[i - 1]) + 180f;
            }
            else
            {
                angle = CalculateAngle(points[i - 1], points[i + 1]);
            }

            vps[0] = points[i] + new Vector2(scale * -0.5f, scale * 0.5f);
            vps[1] = points[i] + new Vector2(scale * 0.5f, scale * 0.5f);
            vps[2] = points[i] + new Vector2(scale * 0.5f, scale * -0.5f);
            vps[3] = points[i] + new Vector2(scale * -0.5f, scale * -0.5f);

            vps[0] = RotatePointAroundPivot(vps[0], points[i], new Vector3(0f, 0f, angle));
            vps[1] = RotatePointAroundPivot(vps[1], points[i], new Vector3(0f, 0f, angle));
            vps[2] = RotatePointAroundPivot(vps[2], points[i], new Vector3(0f, 0f, angle));
            vps[3] = RotatePointAroundPivot(vps[3], points[i], new Vector3(0f, 0f, angle));

            DrawVerticies(vps, uvsEndcap, vh);
        }
    }

    private void CalculateCheckpoints(VertexHelper vh, List<Vector2> scaledPoints, List<Point> points, float scale)
    {
        Vector2[] vps = new Vector2[] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
        float angle = 0;

        for (int i = 0; i < scaledPoints.Count; i++)
        {
            if (CalculateFillAmount(fillAmount, i, scaledPoints.Count) == 0f)
            {
                break;
            }

            if (!points[i].CheckPoint)
            {
                continue;
            }

            switch (rotateIcons)
            {
                case Rotate.UP:
                    angle = 270f;
                    break;
                case Rotate.DOWN:
                    angle = 90f;
                    break;
                case Rotate.LEFT:
                    angle = 0f;
                    break;
                case Rotate.RIGHT:
                    angle = 180f;
                    break;
            }

            vps[0] = scaledPoints[i] + new Vector2(scale * -0.5f, scale * 0.5f);
            vps[1] = scaledPoints[i] + new Vector2(scale * 0.5f, scale * 0.5f);
            vps[2] = scaledPoints[i] + new Vector2(scale * 0.5f, scale * -0.5f);
            vps[3] = scaledPoints[i] + new Vector2(scale * -0.5f, scale * -0.5f);

            vps[0] = RotatePointAroundPivot(vps[0], scaledPoints[i], new Vector3(0f, 0f, angle));
            vps[1] = RotatePointAroundPivot(vps[1], scaledPoints[i], new Vector3(0f, 0f, angle));
            vps[2] = RotatePointAroundPivot(vps[2], scaledPoints[i], new Vector3(0f, 0f, angle));
            vps[3] = RotatePointAroundPivot(vps[3], scaledPoints[i], new Vector3(0f, 0f, angle));

            DrawVerticies(vps, uvsCheckpoint, vh);
        }
    }

    private void CalculateProgress(VertexHelper vh, List<Vector2> points, float scale)
    {
        Vector2[] vps = new Vector2[] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
        float angle = 0;
        Vector2 point = Vector2.zero;

        if (fillAmount == 0)
        {
            point = points[0];
        }
        else if (fillAmount == 1)
        {
            point = points[points.Count - 1];
        }
        else
        {
            int index = Mathf.FloorToInt(fillAmount * (points.Count - 1));
            var localFill = CalculateFillAmount(fillAmount, index, points.Count);
            point = Vector2.Lerp(points[index], points[index + 1], localFill);
        }

        switch (rotateIcons)
        {
            case Rotate.UP:
                angle = 270f;
                break;
            case Rotate.DOWN:
                angle = 90f;
                break;
            case Rotate.LEFT:
                angle = 0f;
                break;
            case Rotate.RIGHT:
                angle = 180f;
                break;
        }

        vps[0] = point + new Vector2(scale * -0.5f, scale * 0.5f);
        vps[1] = point + new Vector2(scale * 0.5f, scale * 0.5f);
        vps[2] = point + new Vector2(scale * 0.5f, scale * -0.5f);
        vps[3] = point + new Vector2(scale * -0.5f, scale * -0.5f);

        vps[0] = RotatePointAroundPivot(vps[0], point, new Vector3(0f, 0f, angle));
        vps[1] = RotatePointAroundPivot(vps[1], point, new Vector3(0f, 0f, angle));
        vps[2] = RotatePointAroundPivot(vps[2], point, new Vector3(0f, 0f, angle));
        vps[3] = RotatePointAroundPivot(vps[3], point, new Vector3(0f, 0f, angle));

        DrawVerticies(vps, uvsProgress, vh);
    }

    private void DrawVerticies(Vector2[] vps, Vector2[] uvs, VertexHelper vh)
    {
        for (int i = 0; i < vps.Length; i++)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = vps[i];
            vertex.uv0 = uvs[i];
            vh.AddVert(vertex);
        }
    }

    #region Helpers
    private float CalculateAngle(Vector2 start, Vector2 end)
    {
        return (float)(Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg) + 90f;
    }

    private Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        Vector3 dir = point - pivot;
        dir = Quaternion.Euler(angles) * dir;
        point = dir + pivot;
        return point;
    }

    private float CalculateFillAmount(float fillAmount, int index, int range)
    {
        if (fillAmount <= 0f)
        {
            return 0f;
        }
        else if (fillAmount >= 1f)
        {
            return 1f;
        }

        return Mathf.Clamp01((fillAmount * (range - 1)) - (index));
    }
    #endregion

    [System.Serializable]
    public struct Point
    {
        public Vector2 Position;
        public bool CheckPoint;
    }
}
