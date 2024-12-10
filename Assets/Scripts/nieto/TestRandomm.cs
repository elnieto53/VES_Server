using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRandomm : MonoBehaviour
{
    public Vector3 globalRotRef = new Vector3(0, 0, 1);
    public Vector3 localRotRef = new Vector3(0, 1, 0);
    private Quaternion offset2;
    public Quaternion initialRot;

    private Quaternion offset1 = new Quaternion(0, (float)-0.71, (float)0.71, 0);

    // Start is called before the first frame update
    void Start()
    {
        //offset1 = Quaternion.ide
    }

    // Update is called once per frame
    void Update()
    {
        Quaternion imuRotation = go.transform.rotation;
        this.transform.rotation = offset1 * imuRotation * offset2;
        //this.transform.rotation = imuRotation;

        if (button)
        {

            Vector3 vecFrom = TransformDirection(localRotRef, imuRotation);
            vecFrom.y = 0;
            //	vecFrom.z = 1;

            //Debug.Log("La vecFrom es: " + vecFrom);

            vecFrom = vecFrom.normalized;

            Vector3 vecTo = globalRotRef;
            vecTo = vecTo.normalized;

            imuRotation = GetQuaternionFromVectorToVector(vecFrom, vecTo) * imuRotation;
            offset2 = Quaternion.Inverse(imuRotation) * initialRot;

            offset2 = offset2.normalized;

            button = false;
        }
    }

    Quaternion GetQuaternionFromVectorToVector(Vector3 A, Vector3 B)
    {
        // Normalizar ambos vectores para asegurar que sean unitarios
        Vector3 aNormalized = A.normalized;
        Vector3 bNormalized = B.normalized;

        // Calcular el producto cruzado para obtener el eje de rotación
        Vector3 crossProduct = Vector3.Cross(aNormalized, bNormalized);

        // Calcular el producto punto para obtener el coseno del ángulo
        float dotProduct = Vector3.Dot(aNormalized, bNormalized);

        // Calcular el ángulo entre los dos vectores en radianes
        float angle = Mathf.Acos(dotProduct);

        // Si los vectores son paralelos, la rotación es 0 o 180 grados
        if (Mathf.Approximately(angle, 0f))
        {
            return Quaternion.identity; // No hay rotación
        }
        else if (Mathf.Approximately(angle, Mathf.PI))
        {
            // Si están en direcciones opuestas, hay que encontrar un eje perpendicular
            Vector3 perpendicular = Vector3.Cross(aNormalized, Vector3.right);
            if (perpendicular.magnitude < 0.001f) // Si son paralelos a Vector3.right, usar otro eje
            {
                perpendicular = Vector3.Cross(aNormalized, Vector3.up);
            }
            return Quaternion.AngleAxis(180f, perpendicular.normalized);
        }

        // Crear el cuaternión a partir del ángulo y el eje
        return Quaternion.AngleAxis(angle * Mathf.Rad2Deg, crossProduct.normalized);
    }

    Vector3 TransformDirection(Vector3 dir, Quaternion rotation)
    {
        // Crear un cuaternión a partir del vector dir, con w = 0
        Quaternion dirQuat = new Quaternion(dir.x, dir.y, dir.z, 0);

        // Multiplicar el cuaternión de rotación por el cuaternión del vector
        Quaternion retvalQuat = rotation * dirQuat * Quaternion.Inverse(rotation);

        // Retornar el vector (x, y, z) del cuaternión resultante
        return new Vector3(retvalQuat.x, retvalQuat.y, retvalQuat.z);
    }

    public GameObject go;
    public bool button;
}
