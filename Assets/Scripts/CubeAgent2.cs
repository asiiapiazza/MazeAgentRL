using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using static UnityEngine.GraphicsBuffer;

public class CubeAgent2 : Agent
{
    [SerializeField] private Transform target;
    [SerializeField] private float moveSpeed = 4f;
    private Rigidbody rb;
    private Vector3 startPosition;
    // Dimensione delle celle della griglia
    private float cellSize = 2.0f; // Modifica a piacere (es. 2x2x2 o 5x5x5)

    // HashSet per tenere traccia delle celle visitate
    private HashSet<Vector3Int> visitedCells = new HashSet<Vector3Int>();

    Vector3Int GetCellIndex(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;

    }

    public override void OnEpisodeBegin()
    {
        //target.localPosition = new Vector3(Random.Range(-30f, 30f), 0.5f, Random.Range(-24f, 24f));
        //this.transform.localPosition = new Vector3(Random.Range(-30f, 30f), 0.5f, Random.Range(-24f, 24f));
        this.transform.position = startPosition;
        this.rb.linearVelocity = Vector3.zero;
        this.rb.angularVelocity = Vector3.zero;
        this.transform.rotation = Quaternion.Euler(Vector3.up * Random.Range(0f, 360f));
        AssignTargetWall();

    }

    private void AssignTargetWall()
    {
        // Trova i muri con i nomi "Target_R", "Target_L", "Target_A" e "Target_B"
        GameObject targetR = GameObject.Find("Target_R");
        GameObject targetL = GameObject.Find("Target_L");
        //GameObject targetA = GameObject.Find("Target_A");
        //GameObject targetB = GameObject.Find("Target_B");
        //GameObject targetC = GameObject.Find("Target_C");
        //GameObject targetD = GameObject.Find("Target_D");
        //GameObject targetE = GameObject.Find("Target_E");

        // Crea una lista dei muri
        //List<GameObject> walls = new List<GameObject> { targetR, targetL, targetA, targetB, targetD, targetC, targetE };
        List<GameObject> walls = new List<GameObject> { targetR, targetL };

        // Rimuovi i muri nulli dalla lista
        walls.RemoveAll(wall => wall == null);

        // Se ci sono muri nella lista
        if (walls.Count > 0)
        {
            // Scegli un muro a caso come target
            int targetIndex = Random.Range(0, walls.Count);
            GameObject targetWall = walls[targetIndex];

            // Assegna il tag "Target" al muro scelto e il tag "Wall" agli altri
            foreach (GameObject wall in walls)
            {
                if (wall == targetWall)
                {
                    wall.tag = "Target";
                    wall.GetComponent<Renderer>().material.color = Color.red;
                    wall.GetComponent<Collider>().isTrigger = true;
                }
                else
                {
                    wall.tag = "Wall";
                    wall.GetComponent<Renderer>().material.color = Color.yellow;
                    wall.GetComponent<Collider>().isTrigger = false;
                }
            }
        }
    }


    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);


    }

    public override void OnActionReceived(ActionBuffers actions)
    {

        // CONTINUOS ACTIONS
        //float moveRotate = actions.ContinuousActions[0];
        //float moveForward = actions.ContinuousActions[1];

        //rb.MovePosition(transform.position + transform.forward * moveSpeed * moveForward * Time.deltaTime);
        //transform.Rotate(0f, moveRotate * moveSpeed, 0f, Space.Self);   


        //DISCRETE ACTIONS - MEGLIO PER LSM

        // 2 azioni
        int moveForward = actions.DiscreteActions[0];
        int moveRotate = actions.DiscreteActions[1];

        Vector3 move = transform.forward * moveSpeed * Time.deltaTime;

        // primo branch con 2 casi: avanti o indietro
        switch (moveForward)
        {
           
            case 1:
                rb.MovePosition(transform.position + move);
                break;
            case 2:
                rb.MovePosition(transform.position - move);
                break;
        }

        // secondo branch con 2 casi: ruoto destra o sinistra

        switch (moveRotate)
        {
            case 1:
                transform.Rotate(0f, moveSpeed, 0f, Space.Self);
                break;
            case 2:
                transform.Rotate(0f, -moveSpeed, 0f, Space.Self);
                break;
        }


        //// Converte la posizione dell'agente nella cella della griglia
        //Vector3Int currentCell = GetCellIndex(transform.position);

        //// Se la cella non è mai stata visitata, assegna il reward e segna come visitata
        //if (!visitedCells.Contains(currentCell))
        //{
        //    visitedCells.Add(currentCell);
        //    AddReward(1.0f); // Reward per nuova cella esplorata
        //}

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        int vertical = Mathf.RoundToInt(Input.GetAxisRaw("Vertical"));
        int horizontal = Mathf.RoundToInt(Input.GetAxisRaw("Horizontal"));
        bool jump = Input.GetKey(KeyCode.Space);
        ActionSegment<int> actions = actionsOut.DiscreteActions;
        actions[0] = vertical >= 0 ? vertical : 2;
        actions[1] = horizontal >= 0 ? horizontal : 2;
        //actions[2] = jump ? 1 : 0;

        //ActionSegment<float> continuosActions = actionsOut.ContinuousActions;
        //continuosActions[0] = Input.GetAxis("Horizontal");
        //continuosActions[1] = Input.GetAxis("Vertical");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target"))
        {
            SetReward(5.0f);
            EndEpisode();
        }
    }

    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (collision.gameObject.CompareTag("Wall"))
    //    {
    //        SetReward(-2.0f);
    //        //EndEpisode();

    //    }
    //}
}
