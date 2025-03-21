using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class CubeAgent : Agent
{
    //https://www.youtube.com/watch?v=liWdLrv8pY0&list=PLhOLzjLZmaVdpnux85hqEGEBeqzcQsYNt&index=3
    Rigidbody rBody;

    private Vector3 startPosition;
    private SimpleCharacterController characterController;

    public override void Initialize()
    {
        startPosition = transform.position;
        characterController = GetComponent<SimpleCharacterController>();
        rBody = GetComponent<Rigidbody>();
    }

    private void AssignTargetWall()
    {
        // Trova i muri con i nomi "Target_R" e "Target_L"
        GameObject targetR = GameObject.Find("Target_R");
        GameObject targetL = GameObject.Find("Target_L");

        // Controlla se i muri esistono
        if (targetR != null && targetL != null)
        {
            // Assegna il tag "Target" a uno dei due muri a caso
            if (Random.value > 0.5f)
            {
                targetR.tag = "Target";
                targetL.tag = "Wall";
                // Assegna il materiale rosso al muro target e il materiale giallo all'altro muro
                targetR.GetComponent<Renderer>().material.color = Color.red;
                targetL.GetComponent<Renderer>().material.color = Color.yellow;
                targetR.GetComponent<Collider>().isTrigger = true;
                targetL.GetComponent<Collider>().isTrigger = false;
            }
            else
            {
                targetR.tag = "Wall";
                targetL.tag = "Target";
                // Assegna il materiale rosso al muro target e il materiale giallo all'altro muro
                targetR.GetComponent<Renderer>().material.color = Color.yellow;
                targetL.GetComponent<Renderer>().material.color = Color.red;
                targetR.GetComponent<Collider>().isTrigger = false;
                targetL.GetComponent<Collider>().isTrigger = true;
            }
        }
    }


    public Transform Target;
    public override void OnEpisodeBegin()
    {
       
        this.transform.position = startPosition;
        this.rBody.linearVelocity = Vector3.zero;
        this.rBody.angularVelocity = Vector3.zero;
        this.transform.rotation = Quaternion.Euler(Vector3.up * Random.Range(0f, 360f));
        //AssignTargetWall();

    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float vertical = actionBuffers.DiscreteActions[0] <= 1 ? actionBuffers.DiscreteActions[0] : -1;
        float horizontal = actionBuffers.DiscreteActions[1] <= 1 ? actionBuffers.DiscreteActions[1] : -1;

        characterController.ForwardInput = vertical;
        characterController.TurnInput = horizontal;

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target")) // Assicurati che il target abbia il tag "Target"
        {
            SetReward(1.0f);
            EndEpisode();
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            SetReward(-0.2f); // Increase penalty with each collision

        }
    }


    public override void CollectObservations(VectorSensor sensor)
    {
        //TOTALE 8 OSSERVAZIONI

        // posizione locale dell'agente
        sensor.AddObservation(this.transform.localPosition);

        //// velocit� dell'agente
        //sensor.AddObservation(rBody.linearVelocity.x);
        //sensor.AddObservation(rBody.linearVelocity.z);

        //// direzione dell'agente
        //sensor.AddObservation(this.transform.forward);
        //sensor.AddObservation(rBody.angularVelocity.y);

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
    }


}
