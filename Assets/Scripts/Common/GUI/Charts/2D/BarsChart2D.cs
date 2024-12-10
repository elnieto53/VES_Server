namespace VESCharts
{

    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public class BarsChart2D : IChart2D
    {
        private Vector3[] vertices;
        private Vector2[] uv;
        private int[] triangles;

        private ChartAxes2D axes;

        private GameObject go;
        private Mesh mesh;
        private MeshRenderer meshRenderer;

        public List<Vector2> points;

        public BarsChart2D(ChartAxes2D axes, int depth)
        {
            this.axes = axes;

            points = new List<Vector2>();

            go = new GameObject();
            meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.material = (Material)Resources.Load(ResourcesPath.Material.blue);
            mesh = go.AddComponent<MeshFilter>().mesh;
            go.layer = LayerMask.NameToLayer("UI");
            RectTransform hostRT = go.AddComponent<RectTransform>();
            RectTransform referenceRT = axes.gameObject.GetComponent<RectTransform>();
            hostRT.SetParent(referenceRT, false);

            hostRT.pivot = Vector3.zero;
            hostRT.anchorMin = Vector2.zero;
            hostRT.anchorMax = Vector2.one;
            hostRT.sizeDelta = referenceRT.sizeDelta;
            hostRT.position = referenceRT.position;

            this.axes.AddChart(this, depth);
        }

        public void Draw()
        {
            int segments = points.Count - 1;
            if (segments <= 0)
                return;

            vertices = new Vector3[segments * 4];
            uv = new Vector2[segments * 4];
            triangles = new int[segments * 6];

            for (int i = 0; i < segments; i++)
            {
                vertices[i * 4] = axes.GetCoordinates(new Vector2(points[i].x, 0));
                vertices[i * 4 + 1] = axes.GetCoordinates(points[i]);
                vertices[i * 4 + 2] = axes.GetCoordinates(new Vector2(points[i + 1].x, points[i].y));
                vertices[i * 4 + 3] = axes.GetCoordinates(new Vector2(points[i + 1].x, 0));

                uv[i * 4] = new Vector2(0, 0);
                uv[i * 4 + 1] = new Vector2(0, 1);
                uv[i * 4 + 2] = new Vector2(1, 1);
                uv[i * 4 + 3] = new Vector2(1, 0);

                Vector3 vec1 = vertices[i * 4 + 1] - vertices[i * 4];
                Vector3 vec2 = vertices[i * 4 + 2] - vertices[i * 4];
                Vector3 vec3 = vertices[i * 4 + 3] - vertices[i * 4];

                bool invertOrientation = Vector3.Cross(vec1, vec2).z > 0;
                triangles[i * 6] = i * 4;
                triangles[i * 6 + 1] = invertOrientation ? i * 4 + 2 : i * 4 + 1;
                triangles[i * 6 + 2] = invertOrientation ? i * 4 + 1 : i * 4 + 2;

                invertOrientation = Vector3.Cross(vec2, vec3).z > 0;
                triangles[i * 6 + 3] = i * 4;
                triangles[i * 6 + 4] = invertOrientation ? i * 4 + 3 : i * 4 + 2;
                triangles[i * 6 + 5] = invertOrientation ? i * 4 + 2 : i * 4 + 3;
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
        }

        public bool isEmpty() => points.Count == 0;

        public Chart2DRange GetRange()
        {
            if (points.Count == 0)
                return null;
            return new Chart2DRange(points.Min(p => p.x), points.Max(p => p.x), Mathf.Min(points.Min(p => p.y), 0), points.Max(p => p.y));
        }

        public void SetMaterial(Material mat) => meshRenderer.material = mat;

        public void SetZOffset(float offset)
        {
            ((RectTransform)go.transform).localPosition = new Vector3(0, 0, offset);
        }
    }
}

