using UnityEngine;

public class MeshGenerator : MonoBehaviour
{

    public static Mesh FieldOfViewMesh(float height, float angleX, float angleY)
    {
        float radiusX = height * Mathf.Tan(angleX * Mathf.PI / 360);
        float radiusY = height * Mathf.Tan(angleY * Mathf.PI / 360);

        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[5];
        Vector2[] uv = new Vector2[5];
        int[] triangles = new int[18];

        vertices[0] = Vector3.zero;
        vertices[1] = new Vector3(radiusX, -height, radiusY);
        vertices[2] = new Vector3(-radiusX, -height, radiusY);
        vertices[3] = new Vector3(-radiusX, -height, -radiusY);
        vertices[4] = new Vector3(radiusX, -height, -radiusY);

        uv[0] = new Vector2(0.5f, 1f);
        uv[1] = new Vector2(0f, 0f);
        uv[2] = new Vector2(0.3f, 0f);
        uv[3] = new Vector2(0.5f, 0f);
        uv[4] = new Vector2(0.8f, 0f);

        // construct bottom
        triangles[0] = 1;   //Triangle 1
        triangles[1] = 2;
        triangles[2] = 3;
        triangles[3] = 3;   //Triangle 2
        triangles[4] = 4;
        triangles[5] = 1;

        // construct sides
        triangles[6] = 2;   //Triangle 3
        triangles[7] = 1;
        triangles[8] = 0;
        triangles[9] = 3;   //Triangle 4
        triangles[10] = 2;
        triangles[11] = 0;
        triangles[12] = 4;  //Triangle 5
        triangles[13] = 3;
        triangles[14] = 0;
        triangles[15] = 1;  //Triangle 6
        triangles[16] = 4;
        triangles[17] = 0;

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }
}
