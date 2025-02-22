using UnityEngine;

public class FoodLogic : MonoBehaviour
{
    public bool respawn;
    public FoodCollectorArea myArea;

    public void OnEaten()
    {
        if (respawn)
        {
            transform.position = new Vector3(Random.Range(-myArea.rangeX, myArea.rangeX),
                3f,
                Random.Range(-myArea.rangeZ, myArea.rangeZ)) + myArea.transform.position;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
