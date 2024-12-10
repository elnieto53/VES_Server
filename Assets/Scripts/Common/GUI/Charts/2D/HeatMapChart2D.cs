namespace VESCharts
{
    using UnityEngine;

    public class HeatMapChart2D : IChart2D
    {
        private Vector3[] vertices;
        private Vector2[] uv;
        int[] triangles;

        /*Dimensions*/
        public Rect perimeter { get; private set; }
        public int columns { get; private set; }
        public int rows { get; private set; }
        public static readonly int MaxHeightValue = 100;

        /*Resolution*/
        public float resolution { get; private set; }
        public float minResolution { get => 2 * Mathf.Sqrt(perimeter.width * perimeter.height / MaxVertices) * 1.1f; } //10% above the min value
        public float maxResolution { get => Mathf.Max(perimeter.width, perimeter.height) / 10; } //Minimum chart dimensions of 1x10 (or 10x1)

        /*Unity components*/
        private GameObject go;
        private Mesh mesh;
        private MeshRenderer meshRenderer;

        /*Chart axes reference*/
        private ChartAxes2D axes;

        /*Internal constants*/
        private static readonly float uvStep = 1f / (MaxHeightValue + 2);
        private static readonly int MaxVertices = 65536;

        /*Auxiliary variables to check if resize is needed*/
        public Vector2 bottomLeftCorner;
        public Vector2 upperRightCorner;

        public HeatMapChart2D(ChartAxes2D axes, Rect perimeter, int depth)
        {
            this.axes = axes;
            this.perimeter = perimeter;
            resolution = (maxResolution - minResolution) / 2;

            columns = Mathf.CeilToInt(perimeter.width / resolution);
            rows = Mathf.CeilToInt(perimeter.height / resolution);

            //if (rows * columns * 4 >= MaxVertices)
            //    throw new System.ArgumentException("Vertex buff overflow - the heatmap is too large");

            go = new GameObject("HeatMap GUI");
            meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.material = (Material)Resources.Load(ResourcesPath.Material.palette2);
            mesh = go.AddComponent<MeshFilter>().mesh;
            go.layer = LayerMask.NameToLayer("UI");
            RectTransform hostRT = go.AddComponent<RectTransform>();
            RectTransform referenceRT = axes.gameObject.GetComponent<RectTransform>();
            hostRT.SetParent(referenceRT, false);

            hostRT.pivot = Vector3.zero;
            hostRT.anchorMin = new Vector2(0, 0);
            hostRT.anchorMax = new Vector2(1, 1);
            hostRT.sizeDelta = referenceRT.sizeDelta;
            hostRT.position = referenceRT.position;

            this.axes.AddChart(this, depth);

            bottomLeftCorner = this.axes.GetCoordinates(new Vector2(perimeter.xMin, perimeter.yMin));
            upperRightCorner = this.axes.GetCoordinates(new Vector2(perimeter.xMax, perimeter.yMax));

            ResetMesh();
            SetHeight(0);
        }

        public void SetResolution(float resolution)
        {
            int _columns = Mathf.CeilToInt(perimeter.width / resolution);
            int _rows = Mathf.CeilToInt(perimeter.height / resolution);

            if (_columns * _rows * 4 >= MaxVertices)
                return;

            this.resolution = resolution;
            columns = _columns;
            rows = _rows;
            upperRightCorner = axes.GetCoordinates(new Vector2(columns * resolution, rows * resolution));

            ResetMesh();
            UpdateChartSize();
            SetHeight(0);
        }

        private int GetQuadIndex(int column, int row) => column * rows + row;

        private void ResetMesh()
        {
            vertices = new Vector3[columns * rows * 4];
            uv = new Vector2[columns * rows * 4];
            triangles = new int[columns * rows * 6];

            mesh.Clear();

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
        }

        public void UpdateChartSize()
        {
            Vector3 position;
            float cellHeight = resolution;
            float cellWidth = resolution;

            int nQuad;
            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    nQuad = GetQuadIndex(i, j);

                    position = new Vector3(perimeter.xMin, perimeter.yMin) + new Vector3(i * cellWidth, j * cellHeight);

                    vertices[nQuad * 4] = axes.GetCoordinates(position);
                    vertices[nQuad * 4 + 1] = axes.GetCoordinates(position + new Vector3(0, cellHeight));
                    vertices[nQuad * 4 + 2] = axes.GetCoordinates(position + new Vector3(cellWidth, cellHeight));
                    vertices[nQuad * 4 + 3] = axes.GetCoordinates(position + new Vector3(cellWidth, 0));

                    triangles[nQuad * 6] = nQuad * 4;
                    triangles[nQuad * 6 + 1] = nQuad * 4 + 1;
                    triangles[nQuad * 6 + 2] = nQuad * 4 + 2;

                    triangles[nQuad * 6 + 3] = nQuad * 4;
                    triangles[nQuad * 6 + 4] = nQuad * 4 + 2;
                    triangles[nQuad * 6 + 5] = nQuad * 4 + 3;
                }

                mesh.vertices = vertices;
                mesh.triangles = triangles;
            }
        }

        public bool SizeChanged()
        {
            Vector2 newBottomLeftCorner = axes.GetCoordinates(new Vector2(perimeter.xMin, perimeter.yMin));
            Vector2 newUpperRightCorner = axes.GetCoordinates(new Vector2(perimeter.xMax, perimeter.yMax));

            if (bottomLeftCorner == newBottomLeftCorner && upperRightCorner == newUpperRightCorner)
                return false;

            bottomLeftCorner = newBottomLeftCorner;
            upperRightCorner = newUpperRightCorner;
            return true;
        }

        public void Draw()
        {
            if (SizeChanged())
                UpdateChartSize();
            UpdateColor();
        }

        private void UpdateColor() => mesh.uv = uv;

        public bool TryGetCoordinates(Vector3 position, out int column, out int row)
        {
            column = 0;
            row = 0;
            if (!perimeter.Contains(position))
                return false;

            column = Mathf.FloorToInt((position.x - perimeter.xMin) / resolution);
            row = Mathf.FloorToInt((position.z - perimeter.yMin) / resolution);

            return true;
        }

        public void SetHeight(int column, int row, int colorIndex)
        {
            if (colorIndex < 0)
                return;
            if (colorIndex >= MaxHeightValue)
                colorIndex = MaxHeightValue;
            if (column < 0 || column >= columns || row < 0 || row >= rows)
                return;

            float lowerBound = uvStep * (colorIndex + 1);
            float upperBound = uvStep * (colorIndex + 2);

            int nQuad = GetQuadIndex(column, row);

            uv[nQuad * 4] = new Vector2(lowerBound, 0);
            uv[nQuad * 4 + 1] = new Vector2(lowerBound, 1);
            uv[nQuad * 4 + 2] = new Vector2(upperBound, 1);
            uv[nQuad * 4 + 3] = new Vector2(upperBound, 0);
        }

        public void SetHeight(int height)
        {
            if (height < 0 || height >= MaxHeightValue)
                return;

            float lowerBound = uvStep * (height + 1);
            float upperBound = uvStep * (height + 2);

            for (int column = 0; column < columns; column++)
            {
                for (int row = 0; row < rows; row++)
                {
                    int nQuad = GetQuadIndex(column, row);

                    uv[nQuad * 4] = new Vector2(lowerBound, 0);
                    uv[nQuad * 4 + 1] = new Vector2(lowerBound, 1);
                    uv[nQuad * 4 + 2] = new Vector2(upperBound, 1);
                    uv[nQuad * 4 + 3] = new Vector2(upperBound, 0);
                }
            }
        }

        public bool isEmpty() => columns == 0 || rows == 0;

        public Chart2DRange GetRange() => new Chart2DRange(perimeter.xMin, perimeter.xMin + columns * resolution, perimeter.yMin, perimeter.yMin + rows * resolution);

        public void SetMaterial(Material mat) => meshRenderer.material = mat;

        public void SetZOffset(float offset) => ((RectTransform)go.transform).localPosition = new Vector3(0, 0, offset);
    }
}