using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.UIElements;

public class CubeAgent : Agent
{
    Rigidbody rBody;

    private Vector3 startPosition;
    private SimpleCharacterController characterController;

    public override void Initialize()
    {
        startPosition = transform.position;
        characterController = GetComponent<SimpleCharacterController>();
        rBody = GetComponent<Rigidbody>();
    }

    public Transform Target;
    public override void OnEpisodeBegin()
    {
        this.transform.position = startPosition;
        this.rBody.linearVelocity = Vector3.zero;
        this.rBody.angularVelocity = Vector3.zero;
        this.transform.rotation = Quaternion.Euler(Vector3.up * Random.Range(0f, 360f));
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Convert actions from Discrete (0, 1, 2) to expected input values (-1, 0, +1)
        // of the character controller
        float vertical = actionBuffers.DiscreteActions[0] <= 1 ? actionBuffers.DiscreteActions[0] : -1;
        float horizontal = actionBuffers.DiscreteActions[1] <= 1 ? actionBuffers.DiscreteActions[1] : -1;
        //bool jump = actionBuffers.DiscreteActions[2] > 0;

        characterController.ForwardInput = vertical;
        characterController.TurnInput = horizontal;
        //characterController.JumpInput = jump;

        // Fell off platform
        if (this.transform.position.y < 0)
        {
            SetReward(-0.5f);
            EndEpisode();
        }

        // Punish if jumps and lands at the same height
        //if (jump && Mathf.Approximately(this.transform.position.y, startPosition.y))
        //{
        //    SetReward(-0.2f);
        //}
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target")) // Assicurati che il target abbia il tag "Target"
        {
            SetReward(1.0f);
            EndEpisode();
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
    }


}
