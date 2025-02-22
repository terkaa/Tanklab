using UnityEngine;
using Unity.MLAgentsExamples;

public class FoodCollectorArea : Area
{
    public GameObject food;
    public GameObject badFood;
    public int numFood;
    public int numBadFood;
    public bool respawnFood;
    public float rangeX;
    public float rangeZ;

    void CreateFood(int num, GameObject type)
    {
        for (int i = 0; i < num; i++)
        {
            GameObject f = Instantiate(type, new Vector3(Random.Range(-rangeX, rangeX), 0.5f,
                Random.Range(-rangeZ, rangeZ)) + transform.position,
                Quaternion.Euler(new Vector3(0f, 0f, 0f)));
            f.GetComponent<FoodLogic>().respawn = respawnFood;
            f.GetComponent<FoodLogic>().myArea = this;
        }
    }

    public void ResetFoodArea(GameObject[] agents)
    {
        foreach (GameObject agent in agents)
        {
            if (agent.transform.parent == gameObject.transform)
            {
                agent.transform.position = new Vector3(Random.Range(-rangeX, rangeX), 0.4f,
                    Random.Range(-rangeZ, rangeZ))
                    + transform.position;
                agent.transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
            }
        }

        CreateFood(numFood, food);
        CreateFood(numBadFood, badFood);
    }

    public override void ResetArea()
    {
    }
}
