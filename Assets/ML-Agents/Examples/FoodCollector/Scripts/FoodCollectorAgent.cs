using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;
using UnityEngine.UI;

public class FoodCollectorAgent : Agent
{
    FoodCollectorSettings m_FoodCollecterSettings;
    public GameObject area;
    FoodCollectorArea m_MyArea;

    bool m_go_to_bucket;
    public MQTTConnection DTcon;
    public Toggle toggleHW;

    float last_bucket_distance;

    float localAngularVelocity;

    // bool m_Frozen;
    bool m_Poisoned;
    bool m_Satiated;

    // bool m_Shoot;
    // float m_FrozenTime;

    float m_EffectTime;
    Rigidbody m_AgentRb;

    // float m_LaserLength;
    // Speed of agent rotation.

    int n_foods_left;

    public float turnSpeed = 150;

    // Speed of agent movement.
    public float moveSpeed = 1;
    public Material normalMaterial;
    public Material badMaterial;
    public Material goodMaterial;
    public Material frozenMaterial;
    // public GameObject myLaser;
    public bool contribute;
    public bool useVectorObs;
    [Tooltip("Use only the frozen flag in vector observations. If \"Use Vector Obs\" " +
             "is checked, this option has no effect. This option is necessary for the " +
             "VisualFoodCollector scene.")]
    public bool useVectorFrozenFlag;

    EnvironmentParameters m_ResetParams;


    public override void Initialize()
    {
        m_AgentRb = GetComponent<Rigidbody>();
        m_MyArea = area.GetComponent<FoodCollectorArea>();
        m_FoodCollecterSettings = FindObjectOfType<FoodCollectorSettings>();
        m_ResetParams = Academy.Instance.EnvironmentParameters;

        n_foods_left = GameObject.FindGameObjectsWithTag("food").Length;
        m_go_to_bucket = false;
        last_bucket_distance = Mathf.Infinity;
        localAngularVelocity = 0f;

        SetResetParameters();
    }

    public override void CollectObservations(VectorSensor sensor)
    {

        if (useVectorObs)
        {
            GameObject[] buckets = GameObject.FindGameObjectsWithTag("bucket");

            float bucket_distance = Mathf.Infinity;

            foreach (GameObject bucket in buckets)
            {
                float distance = Vector3.Distance(gameObject.transform.position, bucket.transform.position);

                if (distance < bucket_distance)
                {
                    bucket_distance = distance;
                }

            }

            bucket_distance *= m_go_to_bucket ? 1 : 0;

            // Debug.Log(bucket_distance);

            var localLinearVelocity = transform.InverseTransformDirection(m_AgentRb.linearVelocity);

            // Debug.Log("Rotating: " + m_AgentRb.linearVelocity);

            sensor.AddObservation(localLinearVelocity.x);
            sensor.AddObservation(localLinearVelocity.z);
            sensor.AddObservation(localAngularVelocity);
            sensor.AddObservation(bucket_distance);
            // sensor.AddObservation(m_go_to_bucket);
            // sensor.AddObservation(m_Frozen);
            // sensor.AddObservation(m_Shoot);

        }
        // else if (useVectorFrozenFlag)
        // {
        //     // sensor.AddObservation(m_Frozen);
        // }
    }

    public Color32 ToColor(int hexVal)
    {
        var r = (byte)((hexVal >> 16) & 0xFF);
        var g = (byte)((hexVal >> 8) & 0xFF);
        var b = (byte)(hexVal & 0xFF);
        return new Color32(r, g, b, 255);
    }

    public void MoveAgent(ActionBuffers actionBuffers)
    {
        // m_Shoot = false;

        // if (Time.time > m_FrozenTime + 4f && m_Frozen)
        // {
        //     Unfreeze();
        // }

        if (Time.time > m_EffectTime + 0.5f)
        {
            if (m_Poisoned)
            {
                Unpoison();
            }
            if (m_Satiated)
            {
                Unsatiate();
            }
        }

        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var continuousActions = actionBuffers.ContinuousActions;
        // var discreteActions = actionBuffers.DiscreteActions;

        // if (!m_Frozen)
        // {
        var forward = Mathf.Clamp(continuousActions[0], -0.1f, 1f);
        var right = Mathf.Clamp(continuousActions[1], -0.1f, 0.1f);
        var rotate = Mathf.Clamp(continuousActions[2], -1f, 1f);

        dirToGo = transform.forward * forward;
        dirToGo += transform.right * right;
        rotateDir = -transform.up * rotate;

        localAngularVelocity = rotateDir[1] * (Time.fixedDeltaTime * turnSpeed);

        // Debug.Log("Rotating: " + rotateDir[1] * (Time.fixedDeltaTime * turnSpeed));

        // var shootCommand = discreteActions[0] > 0;
        // var shootCommand = false;
        // if (shootCommand)
        // {
        //     // m_Shoot = true;
        //     dirToGo *= 0.5f;
        //     m_AgentRb.linearVelocity *= 0.75f;
        // }
        int motion_forward = 0;
        int motion_pan = 0;
        int motion_rotate = 0;

        if (toggleHW.isOn)
        {
            //Debug.Log("HW in the loop");
           // Debug.Log("HW in the loop Forward: " + continuousActions[0] + " Pan: " + continuousActions[1] + " Rotate: " + continuousActions[2]);
          //  DTcon.PublishJointAndGripperValues(Mathf.Round(continuousActions[0] * 10.0f) * 0.1f, Mathf.Round(continuousActions[1] * 10.0f) * 0.1f, Mathf.Round(continuousActions[2] * 10.0f) * 0.1f, false, false); //Always sending 4 we have to figure out how to convert AI command to ROV move commands 
            
            if (DTcon.ROVx < 9999.999) //If we are getting sane readings move the agent
            {
                transform.position = new Vector3(DTcon.ROVx, DTcon.ROVz, DTcon.ROVy);
                transform.eulerAngles = new Vector3(DTcon.ROVrx, -(DTcon.ROVrz) + 60.0f, DTcon.ROVry);
            }
        }
        else
        {
            Debug.Log("SW in the loop Direction: " + continuousActions[0] + " Side: " + continuousActions[1] + " Rotation: " + continuousActions[2]);
            m_AgentRb.AddForce(dirToGo * moveSpeed, ForceMode.VelocityChange);
            transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);
        }

        if (m_AgentRb.linearVelocity.sqrMagnitude > 25f) // slow it down
        {
            m_AgentRb.linearVelocity *= 0.95f;
        }

        if (m_go_to_bucket)
        {
            GameObject[] buckets = GameObject.FindGameObjectsWithTag("bucket");

            float bucket_distance = Mathf.Infinity;

            foreach (GameObject bucket in buckets)
            {
                float distance = Vector3.Distance(gameObject.transform.position, bucket.transform.position);

                if (distance < bucket_distance)
                {
                    bucket_distance = distance;
                }

            }

            // Debug.Log("Distance between objects: " + bucket_distance);
            float bucket_reward = last_bucket_distance - bucket_distance;

            if (bucket_reward > 0)
            {
                AddReward(Mathf.Log(bucket_reward + 1));
                last_bucket_distance = bucket_distance;
            }
            else
            {
                // AddReward(-Mathf.Log(-bucket_reward + 1));
            }

            // AddReward(bucket_reward);
            // 
            // if (bucket_distance < last_bucket_distance)
            // {
            //     // Debug.Log("Getting closer to bucket");
            //     last_bucket_distance = bucket_distance;
            // }

            // if (bucket_distance < last_bucket_distance)
            // {
            //     float bucket_reward = last_bucket_distance - bucket_distance;
            //     // Debug.Log("Getting closer to bucket");
            //     AddReward(bucket_reward);
            //     last_bucket_distance = bucket_distance;
            // }


        }

        // if (m_Shoot)
        // {
        //     var myTransform = transform;
        //     myLaser.transform.localScale = new Vector3(1f, 1f, m_LaserLength);
        //     var rayDir = 25.0f * myTransform.forward;
        //     Debug.DrawRay(myTransform.position, rayDir, Color.red, 0f, true);
        //     RaycastHit hit;
        //     if (Physics.SphereCast(transform.position, 2f, rayDir, out hit, 25f))
        //     {
        //         if (hit.collider.gameObject.CompareTag("agent"))
        //         {
        //             hit.collider.gameObject.GetComponent<FoodCollectorAgent>().Freeze();
        //         }
        //     }
        // }
        // else
        // {
        //     myLaser.transform.localScale = new Vector3(0f, 0f, 0f);
        // }
    }

    // void Freeze()
    // {
    //     gameObject.tag = "frozenAgent";
    //     m_Frozen = true;
    //     m_FrozenTime = Time.time;
    //     gameObject.GetComponentInChildren<Renderer>().material = frozenMaterial;
    // }

    // void Unfreeze()
    // {
    //     m_Frozen = false;
    //     gameObject.tag = "agent";
    //     gameObject.GetComponentInChildren<Renderer>().material = normalMaterial;
    // }

    void Poison()
    {
        m_Poisoned = true;
        m_EffectTime = Time.time;
        gameObject.GetComponentInChildren<Renderer>().material = badMaterial;
    }

    void Unpoison()
    {
        m_Poisoned = false;
        gameObject.GetComponentInChildren<Renderer>().material = normalMaterial;
    }

    void Satiate()
    {
        m_Satiated = true;
        m_EffectTime = Time.time;
        gameObject.GetComponentInChildren<Renderer>().material = goodMaterial;
    }

    void Unsatiate()
    {
        m_Satiated = false;
        gameObject.GetComponentInChildren<Renderer>().material = normalMaterial;
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)

    {
        MoveAgent(actionBuffers);

        n_foods_left = GameObject.FindGameObjectsWithTag("food").Length;

        // UnityEngine.Debug.Log("foods left: " + n_foods_left);

        if (n_foods_left == 0)
        {
            EndEpisode();
        }

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        if (Input.GetKey(KeyCode.D))
        {
            continuousActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.W))
        {
            continuousActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            continuousActionsOut[2] = -1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            continuousActionsOut[0] = -1;
        }
        // var discreteActionsOut = actionsOut.DiscreteActions;
        // discreteActionsOut[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    public override void OnEpisodeBegin()
    {
        // Unfreeze();
        Unpoison();
        Unsatiate();
        // m_Shoot = false;
        m_AgentRb.linearVelocity = Vector3.zero;
        // myLaser.transform.localScale = new Vector3(0f, 0f, 0f);
        transform.position = new Vector3(Random.Range(-m_MyArea.range, m_MyArea.range),
            2f, Random.Range(-m_MyArea.range, m_MyArea.range))
            + area.transform.position;
        transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));

        SetResetParameters();
    }

    void OnCollisionEnter(Collision collision)
    {
        float reward = 0f;

        switch (collision.gameObject.tag)
        {
            case "food":
                if (m_go_to_bucket)
                {
                    reward = -0.1f;
                }
                else
                {
                    reward = 1f;

                    Satiate();
                    collision.gameObject.GetComponent<FoodLogic>().OnEaten();

                    m_go_to_bucket = true;

                    GameObject[] buckets = GameObject.FindGameObjectsWithTag("bucket");

                    float bucket_distance = Mathf.Infinity;

                    foreach (GameObject bucket in buckets)
                    {
                        float distance = Vector3.Distance(gameObject.transform.position, bucket.transform.position);

                        if (distance < bucket_distance)
                        {
                            bucket_distance = distance;
                        }

                    }

                    last_bucket_distance = bucket_distance;

                }

                break;

            case "badFood":

                if (m_go_to_bucket)
                {
                    reward = -1f;
                }
                else
                {
                    Poison();
                    collision.gameObject.GetComponent<FoodLogic>().OnEaten();
                }

                reward = -1f;

                break;

            case "agent":

                reward = -1f;

                break;

            case "bucket":

                if (m_go_to_bucket)
                {
                    reward = 1f;
                    m_go_to_bucket = false;
                    last_bucket_distance = Mathf.Infinity;

                    UnityEngine.Debug.Log("Dropped food in the bucket");
                }
                else
                {
                    reward = -0.1f;
                }

                break;

            default:
                ;
                break;
        }

        AddReward(reward);

        if (contribute)
        {
            m_FoodCollecterSettings.totalScore += reward;
        }

    }

    // void OnCollisionEnter(Collision collision)
    // {
    //     if (!m_go_to_bucket)
    //     {
    //         if (collision.gameObject.CompareTag("food"))
    //         {
    //             Satiate();
    //             collision.gameObject.GetComponent<FoodLogic>().OnEaten();
    //             AddReward(1f);

    //             m_go_to_bucket = true;

    //             GameObject[] buckets = GameObject.FindGameObjectsWithTag("bucket");

    //             float bucket_distance = Mathf.Infinity;

    //             foreach (GameObject bucket in buckets)
    //             {
    //                 float distance = Vector3.Distance(gameObject.transform.position, bucket.transform.position);

    //                 if (distance < bucket_distance)
    //                 {
    //                     bucket_distance = distance;
    //                 }

    //             }

    //             last_bucket_distance = bucket_distance;

    //             if (contribute)
    //             {
    //                 m_FoodCollecterSettings.totalScore += 1;
    //             }
    //         }
    //         if (collision.gameObject.CompareTag("badFood"))
    //         {
    //             Poison();
    //             collision.gameObject.GetComponent<FoodLogic>().OnEaten();

    //             AddReward(-1f);
    //             if (contribute)
    //             {
    //                 m_FoodCollecterSettings.totalScore -= 1;
    //             }
    //         }
    //         if (collision.gameObject.CompareTag("agent"))
    //         {
    //             AddReward(-1f);
    //             if (contribute)
    //             {
    //                 m_FoodCollecterSettings.totalScore -= 1;
    //             }
    //         }
    //         if (collision.gameObject.CompareTag("bucket"))
    //         {
    //             AddReward(-0.1f);
    //             if (contribute)
    //             {
    //                 m_FoodCollecterSettings.totalScore -= 0.1f;
    //             }
    //         }

    //     }
    //     else
    //     {
    //         if (collision.gameObject.CompareTag("bucket"))
    //         {
    //             AddReward(1f);
    //             m_go_to_bucket = false;
    //             last_bucket_distance = Mathf.Infinity;

    //             UnityEngine.Debug.Log("Dropped food in the bucket");

    //             if (contribute)
    //             {
    //                 m_FoodCollecterSettings.totalScore += 1;
    //             }
    //         }
    //         if (collision.gameObject.CompareTag("food"))
    //         {
    //             AddReward(-0.1f);
    //             if (contribute)
    //             {
    //                 m_FoodCollecterSettings.totalScore -= 0.1f;
    //             }
    //         }
    //         if (collision.gameObject.CompareTag("badFood"))
    //         {
    //             AddReward(-0.1f);
    //             if (contribute)
    //             {
    //                 m_FoodCollecterSettings.totalScore -= 0.1f;
    //             }
    //         }
    //         if (collision.gameObject.CompareTag("agent"))
    //         {
    //             AddReward(-1f);
    //             if (contribute)
    //             {
    //                 m_FoodCollecterSettings.totalScore -= 1f;
    //             }
    //         }
    //     }

    // }
    public void SetEnv()
    {
        m_FoodCollecterSettings.EnvironmentReset();

    }

    // public void SetLaserLengths()
    // {
    //     m_LaserLength = m_ResetParams.GetWithDefault("laser_length", 1.0f);
    // }

    public void SetAgentScale()
    {
        float agentScale = m_ResetParams.GetWithDefault("agent_scale", 1.0f);
        gameObject.transform.localScale = new Vector3(agentScale, agentScale, agentScale);
    }

    public void SetResetParameters()
    {
        // SetLaserLengths();
        SetAgentScale();
        SetEnv();

        m_go_to_bucket = false;
        last_bucket_distance = Mathf.Infinity;
    }
}
