using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using static UnityEngine.GraphicsBuffer;
using static UnityEditor.Searcher.SearcherWindow.Alignment;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;




public class CubeAgent2 : Agent
{
    [SerializeField] private float moveSpeed = 4f;
    private Rigidbody rb;
    private Vector3 startPosition;
    public MeshCollider meshCollider;


    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();

        //startPosition = transform.position;

    }

    public override void OnEpisodeBegin()
    {
        // crea metodo per spostare il target in una posizione casuale all'interno del labirinto
        this.transform.position = GetTriangle();
        this.rb.linearVelocity = Vector3.zero;
        this.rb.angularVelocity = Vector3.zero;
        this.transform.rotation = Quaternion.Euler(Vector3.up * Random.Range(0f, 360f));


        //// Trova tutti gli oggetti con il tag "Maze"
        //GameObject[] mazes = GameObject.FindGameObjectsWithTag("Maze");

        //foreach (GameObject maze in mazes)
        //{
        //    AssignTargetWall(maze);
        //}


    }

    private Vector3 GetTriangle()
    {
        Mesh mesh = meshCollider.sharedMesh;
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        int randomIndex = Random.Range(0, triangles.Length / 3) * 3; // Scegli un triangolo casuale

        Vector3 v1 = vertices[triangles[randomIndex]];
        Vector3 v2 = vertices[triangles[randomIndex + 1]];
        Vector3 v3 = vertices[triangles[randomIndex + 2]];

        return GetRandomPointInTriangle(v1, v2, v3);
    }

    private Vector3 GetRandomPointInTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        float a = Random.value;
        float b = Random.value;

        // Ensure the point is inside the triangle
        if (a + b > 1)
        {
            a = 1 - a;
            b = 1 - b;
        }

        float c = 1 - a - b;

        var localPoint =  a * v1 + b * v2 + c * v3;
        return meshCollider.transform.TransformPoint(localPoint);
    }

    private void AssignTargetWall(GameObject maze)
    {
        Transform targetR = maze.transform.Find("Target_R");
        Transform targetL = maze.transform.Find("Target_L");

        if (targetR != null && targetL != null)
        {
            if (Random.value > 0.5f)
            {
                targetR.tag = "Wall";
                targetL.tag = "Target";
                targetR.GetComponent<Renderer>().material.color = Color.yellow;
                targetR.GetComponent<Collider>().isTrigger = false;
                targetL.GetComponent<Renderer>().material.color = Color.magenta;
                targetL.GetComponent<Collider>().isTrigger = true;
            }
            else
            {
                targetR.tag = "Target";
                targetL.tag = "Wall";
                targetL.GetComponent<Renderer>().material.color = Color.yellow;
                targetL.GetComponent<Collider>().isTrigger = false;
                targetR.GetComponent<Renderer>().material.color = Color.magenta;
                targetR.GetComponent<Collider>().isTrigger = true;
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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            SetReward(-2.0f);
            //EndEpisode();

        }
    }

}
