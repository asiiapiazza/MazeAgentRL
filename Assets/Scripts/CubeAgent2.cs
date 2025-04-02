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
    public GameObject floor;

    private Dictionary<Transform, bool> exploredCells = new Dictionary<Transform, bool>();
    private int stepCount = 0; // Contatore dei passi

    public GameObject targetL;
    public GameObject targetR;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;

        // Inizializza lo stato delle celle
        InitializeCells();
    }

    

    public override void OnEpisodeBegin()
    {
        //this.transform.position = startPosition;

        this.transform.position = RandomFloorPosition();
        this.rb.linearVelocity = Vector3.zero;
        this.rb.angularVelocity = Vector3.zero;
        this.transform.rotation = Quaternion.Euler(Vector3.up * Random.Range(0f, 360f));
       AssignTargetWall();

        // Resetta lo stato delle celle esplorate
        ResetCells();
        stepCount = 0; // Resetta il contatore dei passi
    }

    private void InitializeCells()
    {
        // Ottieni tutti i figli dell'oggetto floor
        Transform[] floorCells = floor.GetComponentsInChildren<Transform>();

        // Filtra i figli per escludere l'oggetto floor stesso
        foreach (Transform cell in floorCells)
        {
            if (cell != floor.transform)
            {
                exploredCells[cell] = false; // Inizialmente tutte le celle non sono esplorate
            }
        }
    }

    private void ResetCells()
    {
        List<Transform> keys = new List<Transform>(exploredCells.Keys);
        foreach (Transform key in keys)
        {
            exploredCells[key] = false;
            key.GetComponent<Renderer>().material.color = Color.white; // Resetta il colore delle celle
        }
    }

    private Vector3 RandomFloorPosition()
    {
        // Ottieni tutti i figli dell'oggetto floor
        Transform[] floorCells = floor.GetComponentsInChildren<Transform>();

        // Filtra i figli per escludere l'oggetto floor stesso
        List<Transform> cells = new List<Transform>();
        foreach (Transform cell in floorCells)
        {
            if (cell != floor.transform)
            {
                cells.Add(cell);
            }
        }

        // Scegli una cella casuale
        Transform randomCell = cells[Random.Range(0, cells.Count)];

        // Restituisci le coordinate del centro della cella
        return randomCell.position;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
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

        // Incrementa il contatore dei passi
        stepCount++;
        // Penalizza l'agente per ogni passo
        SetReward(-0.01f);

        // Verifica se l'agente ha esplorato una nuova cella
        CheckExploredCell();
    }

    private void AssignTargetWall()
    {
        if (targetR == null || targetL == null) return;

        // Determina casualmente quale muro sarà il target
        bool isRightTarget = Random.value > 0.5f;

        AssignWallProperties(targetR, isRightTarget);
        AssignWallProperties(targetL, !isRightTarget);
    }

    private void AssignWallProperties(GameObject wall, bool isTarget)
    {
        wall.tag = isTarget ? "Target" : "Wall";
        wall.GetComponent<Renderer>().material.color = isTarget ? Color.red : Color.yellow;
        wall.GetComponent<Collider>().isTrigger = isTarget;
    }

    private Transform lastVisitedCell = null; // Memorizza l'ultima cella visitata
    void CheckExploredCell()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 2f)) // Un solo Raycast
        {
            Transform hitCell = hit.collider.transform;

            if (exploredCells.ContainsKey(hitCell))
            {
                if (!exploredCells[hitCell]) // Se la cella è nuova
                {
                    exploredCells[hitCell] = true;
                    SetReward(0.5f);
                    hitCell.GetComponent<Renderer>().material.color = Color.blue;
                    lastVisitedCell = hitCell; // Aggiorna la cella visitata
                }
                //else if (lastVisitedCell != hitCell) // Se è già esplorata ma l'agente l'ha lasciata e ci è tornato
                //{
                //    SetReward(0.1f);
                //    hitCell.GetComponent<Renderer>().material.color = Color.green;
                //    lastVisitedCell = hitCell; // Aggiorna la cella visitata
                //}
            }
        }
    }

    void OnDrawGizmos()
    {
        foreach (Transform cell in exploredCells.Keys)
        {
            Bounds bounds = cell.GetComponent<Collider>().bounds;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
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
        }
    }
}


