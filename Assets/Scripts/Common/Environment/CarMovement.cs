using UnityEngine;

public class CarMovement : MonoBehaviour
{
    public float speed;
    public Vector3 direction;
    public Vector3 initialPosition;
    public float maxDistance;

    // Start is called before the first frame update
    void Start()
    {
        speed = 30;
        direction = Vector3.right;
        initialPosition = new Vector3(-10, 0, 0);
        maxDistance = 40;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = transform.position + transform.TransformDirection(direction) * speed * Time.deltaTime;
        if (Vector3.Distance(initialPosition, transform.position) > maxDistance)
            transform.position = initialPosition;
    }
}
