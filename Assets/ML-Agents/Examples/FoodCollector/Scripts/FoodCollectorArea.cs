using UnityEngine;
using Unity.MLAgentsExamples;

public class FoodCollectorArea : Area
{
    public GameObject food;
    public GameObject badFood;

    public GameObject bucket;

    public int numFood;
    public int numBadFood;
    public bool respawnFood;
    public float range;

    void CreateFood(int num, GameObject type)
    {

        float x_range = 2 * range;
        // float z_range = range;

        for (int i = 0; i < num; i++)
        {
            GameObject f = Instantiate(type, new Vector3(Random.Range(-x_range, x_range), 1f,
                Random.Range(-range, range)) + transform.position,
                Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 90f)));
            f.GetComponent<FoodLogic>().respawn = respawnFood;
            f.GetComponent<FoodLogic>().myArea = this;
        }
    }

    void CreateBucket(int num, GameObject type)
    {
        float x_range = 2 * range;
        // float z_range = range;

        for (int i = 0; i < num; i++)
        {
            GameObject f = Instantiate(type, new Vector3(Random.Range(-x_range, x_range), 1f,
                Random.Range(-range, range)) + transform.position,
                Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 0f)));
            // f.GetComponent<FoodLogic>().respawn = respawnFood;
            // f.GetComponent<FoodLogic>().myArea = this;
        }
    }

    public void ResetFoodArea(GameObject[] agents)
    {
        float x_range = 2 * range;
        // float z_range = range;

        foreach (GameObject agent in agents)
        {
            if (agent.transform.parent == gameObject.transform)
            {
                agent.transform.position = new Vector3(Random.Range(-x_range, x_range), 2f,
                    Random.Range(-range, range))
                    + transform.position;
                agent.transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
            }
        }

        CreateFood(numFood, food);
        CreateFood(numBadFood, badFood);

        int numbuckets = 5;

        CreateBucket(numbuckets, bucket);

    }

    public override void ResetArea()
    {
    }
}
