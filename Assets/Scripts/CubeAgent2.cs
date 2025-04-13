using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

public class CubeAgent2 : Agent
{
    [SerializeField] private float moveSpeed = 4f;

    private Rigidbody rb;

    private Vector3 startPosition;
    public GameObject floor;

    private int nonStraightMoveCount = 0; // Contatore dei movimenti non dritti
    private Transform[] floorCells;
    private List<Transform> cells = new List<Transform>();

    public GameObject targetL;
    public GameObject targetR;
    public GameObject targetF;


    private GameObject lastVisitedCell = null; // Memorizza l'ultima cella visitata
    private GameObject currentCell = null; // Memorizza la cella corrente;
    private Dictionary<Transform, int> cellVisitCount = new Dictionary<Transform, int>(); // Contatore delle visite per cella


    public override void Initialize()
    {
        //Time.timeScale = 1f;

        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;

        // Inizializza lo stato delle celle
        InitializeCells();

        //Prendi le celle del pavimento
       floorCells = floor.GetComponentsInChildren<Transform>();

        // Filtra i figli per escludere l'oggetto floor stesso
        foreach (Transform cell in floorCells)
        {
            if (cell != floor.transform)
            {
                cells.Add(cell);
            }
        }
    }

    public override void OnEpisodeBegin()
    {

        // Resetta lo stato delle celle esplorate
        ResetCells();
        nonStraightMoveCount = 0; // Resetta il contatore dei movimenti non dritti

        //this.transform.position = startPosition;

        //this.transform.position = RandomFloorPosition();
        //this.transform.localPosition = new Vector3(Random.Range(-23f, 26f), 0.75f, Random.Range(-26f, 23f));
        //targetL.transform.localPosition = new Vector3(Random.Range(-23f, 26f), 1.5f, Random.Range(-26f, 23f));


        // Controlla se targetL non ha niente vicino ai lati destro e sinistro
        //if (!Physics.Raycast(targetL.transform.position, targetL.transform.right, 2f) ||
        //    !Physics.Raycast(targetL.transform.position, -targetL.transform.right, 2f))
        //{
        //    targetL.transform.Rotate(0f, 90f, 0f);
        //}

        this.transform.position = RandomFloorPosition();
        //targetL.transform.position = RandomFloorPosition();

        currentCell = GetCurrentCell();

        this.rb.linearVelocity = Vector3.zero;
        this.rb.angularVelocity = Vector3.zero;
        this.transform.rotation = Quaternion.Euler(Vector3.up * Random.Range(0f, 360f));
        AssignTargetWall();
    }

    // Funzione per inizializzare le celle del pavimento
    private void InitializeCells()
    {
        // Ottieni tutti i figli dell'oggetto floor
        Transform[] floorCells = floor.GetComponentsInChildren<Transform>();

        // Filtra i figli per escludere l'oggetto floor stesso
        foreach (Transform cell in floorCells)
        {
            if (cell != floor.transform)
            {
                cellVisitCount[cell] = 0; // Inizialmente tutte le celle hanno 0 visite
            }
        }
    }

    // Funzione per resettare a fine episodio ogni info delle celle
    private void ResetCells()
    {
        lastVisitedCell = null;
        currentCell = null;

        List<Transform> visitKeys = new List<Transform>(cellVisitCount.Keys);
        foreach (Transform key in visitKeys)
        {
            cellVisitCount[key] = 0;
            key.GetComponent<Renderer>().material.color = Color.white; // Resetta il colore delle celle
            key.tag = "Floor"; // Resetta il tag delle celle
        }
    }

    // Funzione per assegnare una posizione casuale sul pavimento (e assegnare visita = 1 alla prima cella)
    private Vector3 RandomFloorPosition()
    {
        if (cells != null && cells.Count != 0)
        {
            // Scegli una cella casuale
            Transform randomCell = cells[Random.Range(0, cells.Count)];

            // Restituisci le coordinate del centro della cella
            Vector3 cellPosition = randomCell.position;
            cellPosition.y = transform.position.y; // Mantieni l'altezza dell'agente
            return cellPosition;
        }
        return this.startPosition;
    }

    // Funzione per aggiornare la cella corrente e la cella precedente
    private void UpdateCellStatus()
    {
        GameObject currentCheckedCell = GetCurrentCell();
        if (currentCheckedCell != null)
        {
            lastVisitedCell = currentCell;
            currentCell = currentCheckedCell;
        }

    }

    // Funzione per determinare la cella corrente
    private GameObject GetCurrentCell()
    {
        // Esegui un raycast per determinare in quale cella si trova l'agente
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 2f)) // Assumendo che le celle siano sotto l'agente
        {
            return hit.collider.gameObject;  // Restituisce la cella in cui l'agente è attualmente
        }
        return null;
    }


    public override void CollectObservations(VectorSensor sensor)
    {

        // Aggiungi posizione locale dell'agente
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(transform.forward);


        //// Aggiungi osservazioni per la cella corrente SE CAMBIA
        AddCellObservation(currentCell.transform, sensor);

        // Aggiungi osservazioni per le celle vicine (front, back, left, right)
        AddNeighborObservations(currentCell.transform, Vector3.forward, sensor);
        AddNeighborObservations(currentCell.transform, Vector3.back, sensor);
        AddNeighborObservations(currentCell.transform, Vector3.left, sensor);
        AddNeighborObservations(currentCell.transform, Vector3.right, sensor);

    }

    private void AddCellObservation(Transform cell, VectorSensor sensor)
    {
        if (cell == null)
        {
            sensor.AddObservation(-1f); // fallback per numero visite
            sensor.AddObservation(0f);  // cella non esplorata
            return;
        }

        int visits = cellVisitCount != null && cellVisitCount.ContainsKey(cell) ? cellVisitCount[cell] : 0;
        sensor.AddObservation(visits);
        sensor.AddObservation(visits > 0 ? 1.0f : 0.0f);
    }




    private void AddNeighborObservations(Transform cell, Vector3 direction, VectorSensor sensor)
    {

        if (cell != null && Physics.Raycast(cell.position, direction, out RaycastHit hit, 2f) && hit.collider.CompareTag("Floor"))
        {
            Transform neighborCell = hit.transform;

            int visits = cellVisitCount.ContainsKey(neighborCell) ? cellVisitCount[neighborCell] : 0;
            sensor.AddObservation(visits);

            bool explored = visits > 0;
            sensor.AddObservation(explored ? 1.0f : 0.0f);
        }
        else
        {
            sensor.AddObservation(-1f);
            sensor.AddObservation(0f);

        }

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // 3 azioni
        int moveForward = actions.DiscreteActions[0];
        int moveRotate = actions.DiscreteActions[1];
        int jump = actions.DiscreteActions[2];
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
        float rotationSpeed = moveSpeed * 0.6f; // Riduci la velocità di rotazione

        switch (moveRotate)
        {
            case 1:
                transform.Rotate(0f, rotationSpeed, 0f, Space.Self);
                nonStraightMoveCount++;
                break;
            case 2:
                transform.Rotate(0f, -rotationSpeed, 0f, Space.Self);
                nonStraightMoveCount++;
                break;
        }

        //// terzo branch per il salto
        //if (jump == 1 && IsGrounded())
        //{
        //    rb.AddForce(Vector3.up * 1.5f, ForceMode.VelocityChange);
        //}

        // Incrementa il contatore dei passi

        // penalizza per movimenti non dritti
        if (nonStraightMoveCount > StepCount * 0.3f)
        {
            AddReward(-0.01f);
        }

        // Penalizza l'agente per ogni passo
        float penaltyMultiplier = Mathf.Clamp01(StepCount / 4000); // Ridurre la penalità dopo un certo numero di passi
        AddReward(-0.001f * penaltyMultiplier);


        CheckExploredCell();
        //Debug.Log($"Step n. {StepCount} at time: {Time.time:F2}s");

    }



    private void AssignTargetWall()
    {
        if (targetR == null || targetL == null) return;

        // Lista dei target disponibili
        List<GameObject> targets = new List<GameObject> { targetR, targetL };
        if (targetF != null)
        {
            targets.Add(targetF);
        }

        // Determina casualmente quale muro sarà il target
        int targetIndex = Random.Range(0, targets.Count);

        for (int i = 0; i < targets.Count; i++)
        {
            AssignWallProperties(targets[i], i == targetIndex);
        }
    }

    private void AssignWallProperties(GameObject wall, bool isTarget)
    {
        wall.tag = isTarget ? "Target" : "Wall";
        wall.GetComponent<Renderer>().material.color = isTarget ? Color.red : Color.yellow;
        wall.GetComponent<Collider>().isTrigger = isTarget;
    }


    void CheckExploredCell()
    {
        //incremento di uno il contatore delle visite della cell

        // se non è stata ancora esplorata
        if (cellVisitCount[currentCell.transform] == 0)
        {
            // Premia l'esplorazione
            AddReward(5f);

            cellVisitCount[currentCell.transform]++;

            // Cambia il colore della cella per indicare che è stata esplorata
            var renderer = currentCell.transform.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = Color.cyan;

            currentCell.transform.tag = "VisitedFloor";

        }
        // La cella è stata esplorata, ma la visitiamo di nuovo
        else if (lastVisitedCell != currentCell && lastVisitedCell != null)
        {
 
            // Incrementa il contatore delle visite
            if (!cellVisitCount.TryGetValue(currentCell.transform, out int visits))
                visits = 0;

            visits++;
            cellVisitCount[currentCell.transform] = visits;

            var renderer = currentCell.transform.GetComponent<Renderer>();
            if (renderer != null)
            {
                float intensity = Mathf.Clamp01(cellVisitCount[currentCell.transform] / 10f); // Normalizza l'intensità tra 0 e 1       
                // Colore dal blu chiaro a blu scuro
                renderer.material.color = Color.Lerp(Color.cyan, Color.blue, intensity);
            }


            //// Penalità forte se si va in loop/stallo
            if (visits >= 2 && visits < 5)
                AddReward(-0.005f * visits);  // leggera penalità
            else if (visits >= 5)
                AddReward(-0.01f * visits);   // più severa
            else if (visits >= 10)
                EndEpisode();   // severa penalità

        }
        UpdateCellStatus();  // Aggiorna la cella corrente e la cella precedente
     

    }

    //private void Update()
    //{
    //    Debug.Log($"Reward: {GetCumulativeReward()}");
    //}

    void OnDrawGizmos()
    {
        foreach (Transform cell in cellVisitCount.Keys)
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
        //actions[2] = jump ? 1 : 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target"))
        {
            SetReward(1000f);
            EndEpisode();

        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            SetReward(-1000f);
            EndEpisode();


        }
    }
    private bool IsGrounded()
    {
        bool r = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.1f);
        return r;
    }
}
