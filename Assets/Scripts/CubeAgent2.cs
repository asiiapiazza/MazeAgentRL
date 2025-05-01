using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class CubeAgent2 : Agent
{
    [SerializeField] private float moveSpeed = 4f;

    private Rigidbody rb;

    private Vector3 startPosition; 
    public GameObject floor;
    public GameObject wall; // Prefab del muro da generare

    private int nonStraightMoveCount = 0; // Contatore dei movimenti non dritti
    private Transform[] floorCells;
    private List<Transform> cells = new List<Transform>();

    public GameObject targetL;
    public GameObject targetR;
    public GameObject targetF;
    public GameObject targetN;


    private GameObject lastVisitedCell = null; // Memorizza l'ultima cella visitata
    private GameObject currentCell = null; // Memorizza la cella corrente;
    private Dictionary<Transform, int> cellVisitCount = new Dictionary<Transform, int>(); // Contatore delle visite per cella


    public override void Initialize()
    {
        
        Time.timeScale = 1f;

        rb = GetComponent<Rigidbody>();
        startPosition = transform.localPosition;

        // Inizializza lo stato delle celle
        InitializeCells();

        //Prendi le celle del pavimento
        floorCells = floor.GetComponentsInChildren<Transform>();

        // Filtra i figli per escludere l'oggetto floor stesso
        InitiateFloor();
    }



    public override void OnEpisodeBegin()
    {
        transform.localPosition = startPosition;
        // Resetta lo stato delle celle esplorate
        ResetCells();

        // Rimetti le celle del paviento
        //cells.Clear();
        //InitiateFloor();
        nonStraightMoveCount = 0; // Resetta il contatore dei movimenti non dritti


        //rimuovi una cella del pavimento per creare il buco
        //RemoveRandomFloorCell();

        // spawna agente casualmente
        //this.transform.position = RandomFloorPosition();

        currentCell = GetCurrentCell();

        this.rb.linearVelocity = Vector3.zero;
        this.rb.angularVelocity = Vector3.zero;
        this.transform.rotation = Quaternion.Euler(Vector3.up * Random.Range(0f, 360f));

        //AssignTargetWall();
        PickWallAsTarget();
    }

    #region CellMethods
    private void InitiateFloor()
    {
        foreach (Transform cell in floorCells)
        {
            if (cell != floor.transform)
            {
                cell.gameObject.SetActive(true);
                cells.Add(cell);
            }
        }
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

    // Funzione per determinare la cella corrente
    private GameObject GetCurrentCell()
    {
        // Esegui un raycast per determinare in quale cella si trova l'agente
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 1f)) // Assumendo che le celle siano sotto l'agente
        {
            return hit.collider.gameObject;  // Restituisce la cella in cui l'agente è attualmente
        }
        return null;
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
    #endregion

    #region "Randomiziation Position"

    private void AssignTargetWall()
    {
        List<GameObject> targets = new List<GameObject>();

        if (targetR != null) targets.Add(targetR);
        if (targetL != null) targets.Add(targetL);
        if (targetF != null) targets.Add(targetF);
        if (targetN != null) targets.Add(targetN);

        if (targets.Count == 0) return;

        int targetIndex = Random.Range(0, targets.Count);

        for (int i = 0; i < targets.Count; i++)
        {
            AssignWallProperties(targets[i], i == targetIndex);
        }
    }

    private void AssignWallProperties(GameObject wall, bool isTarget)
    {
        wall.tag = isTarget ? "Target" : "Wall";
        //assegna colore #ECA12F al rendere
        ColorUtility.TryParseHtmlString("#ECA12F", out Color color);
        wall.GetComponent<Renderer>().material.color = isTarget ? Color.magenta : color;
        wall.GetComponent<Collider>().isTrigger = isTarget;
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

    //metodo per rimuovere casualmente una cella del pavimento
    private void RemoveRandomFloorCell()
    {
        if (cells.Count > 0)
        {
            // Scegli una cella casuale
            Transform randomCell = cells[Random.Range(0, cells.Count)];

            // Rimuovi la cella dalla lista
            cells.Remove(randomCell);

            randomCell.gameObject.SetActive(false);
        }
    }

    private void PickWallAsTarget()
    {

        //prendi figli di wall 
        Transform[] walls = wall.GetComponentsInChildren<Transform>();
        //RIMUOVI PRIMO ELEENTO
        List<Transform> wallsList = new List<Transform>(walls);
        wallsList.RemoveAt(0);

        // Resetta proprietà di tutti i muri e target

        foreach (Transform obj in wallsList)
        {
            //se l'oggetto ha il componente render
            if (obj.gameObject.GetComponent<Renderer>() != null)
            {
                AssignWallProperties(obj.gameObject, false);
            }
        }

        // Seleziona un muro casuale e assegna come target
        if (walls.Length > 0)
        {
            Transform randomWall = wallsList[Random.Range(0, wallsList.Count)];
            AssignWallProperties(randomWall.gameObject, true);
        }

    }
    #endregion 

    #region "Observations"
    public enum VisitStatus
    {
        NotVisited = 0,
        Visited = 1,
        Invalid = 2
    }

    const int NUM_VISIT_STATES = 3;
    private const float MAX_VISITS = 10f; // Or whatever upper bound makes sense

    public override void CollectObservations(VectorSensor sensor)
    {

        // Aggiungi osservazioni per la cella corrente SE CAMBIA
        AddCellObservation(currentCell.transform, sensor);

        // Aggiungi osservazioni per le celle vicine (front, back, left, right)
        AddNeighborObservations(currentCell.transform, Vector3.forward, sensor);
        AddNeighborObservations(currentCell.transform, Vector3.back, sensor);
        AddNeighborObservations(currentCell.transform, Vector3.left, sensor);
        AddNeighborObservations(currentCell.transform, Vector3.right, sensor);

    }
    
    private void AddCellObservation(Transform cell, VectorSensor sensor)
    {

        VisitStatus status;
        int visits = 0;
        if (cell == null)
        {
            status = VisitStatus.Invalid;
        }
        else
        {
            visits = cellVisitCount.ContainsKey(cell) ? cellVisitCount[cell] : 0;
            status = visits > 0 ? VisitStatus.Visited : VisitStatus.NotVisited;
        }

        sensor.AddOneHotObservation((int)status, NUM_VISIT_STATES);

        float normalizedVisits = Mathf.Clamp01(visits / MAX_VISITS);
        sensor.AddObservation(normalizedVisits);

    }

    private void AddNeighborObservations(Transform cell, Vector3 direction, VectorSensor sensor)
    {
        VisitStatus status;
        int visits = 0;
        if (cell != null && Physics.Raycast(cell.position, direction, out RaycastHit hit, 2f) && hit.collider.CompareTag("Floor"))
        {
            Transform neighborCell = hit.transform;
            visits = cellVisitCount.ContainsKey(neighborCell) ? cellVisitCount[neighborCell] : 0;
            status = visits > 0 ? VisitStatus.Visited : VisitStatus.NotVisited;
        }
        else
        {
            status = VisitStatus.Invalid;
        }

        // One-hot encode the status (NotVisited = 0, Visited = 1, Invalid = 2)
        sensor.AddOneHotObservation((int)status, NUM_VISIT_STATES);

        float normalizedVisits = Mathf.Clamp01(visits / MAX_VISITS);
        sensor.AddObservation(normalizedVisits);
    }
    #endregion


    private void ObsticlesRewardSystem()
    {

        if (transform.position.y < 0)
        {
            SetReward(-10f);
            EndEpisode();
        }

        // Controllo se ho qualcosa sotto con raycast
        bool hits = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 5f);

        //deve stare in aria e sotto non deve esserci niente
        if (!IsGrounded() && (!hits || (hit.collider.tag == "Obstacle")))
        {
            AddReward(0.2f);
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

         if (jump == 1 && IsGrounded())
         {
            AddReward(-0.001f);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 5f, 0f);
         }

        //penalizza per movimenti non dritti
        if (nonStraightMoveCount > StepCount * 0.5f)
            AddReward(-0.005f);

        UpdateCellStatus();

        if (currentCell.tag == "Floor")
            CheckExploredCell();

        ObsticlesRewardSystem();


        // Penalizza l'agente per ogni passo
        AddReward(-0.001f);
    }

    void Update()
    {
        if (Input.GetKey("r"))
        {
            Restart();
        }
    }

    void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void CheckExploredCell()
    {
      
            if (cellVisitCount[currentCell.transform] == 0)
            {
                // Premia l'esplorazione
                AddReward(0.1f);

                cellVisitCount[currentCell.transform]++;

                // Cambia il colore della cella per indicare che è stata esplorata
                var renderer = currentCell.transform.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material.color = Color.cyan;


            }
            // La cella è stata esplorata, ma la visitiamo di nuovo
            else if(lastVisitedCell != currentCell && lastVisitedCell != null && IsGrounded())
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

                if (visits >= 3)
                    AddReward(-0.001f * visits);

            }

        UpdateCellStatus();  // Aggiorna la cella corrente e la cella precedente

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        int vertical = Mathf.RoundToInt(Input.GetAxisRaw("Vertical"));
        int horizontal = Mathf.RoundToInt(Input.GetAxisRaw("Horizontal"));
        bool jump = Input.GetKey(KeyCode.Space);
        ActionSegment<int> actions = actionsOut.DiscreteActions;
        actions[0] = vertical >= 0 ? vertical : 2;
        actions[1] = horizontal >= 0 ? horizontal : 2;
        actions[2] = jump ? 1 : 0;
    }


    #region UtilityMethods
    void OnDrawGizmos()
    {
        foreach (Transform cell in cellVisitCount.Keys)
        {
            Bounds bounds = cell.GetComponent<Collider>().bounds;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
    #endregion

   
    #region BoolMethodsCheck
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target"))
        {
            SetReward(10f);
            EndEpisode();

        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Obstacle"))
        {
            SetReward(-10f);
            EndEpisode();
        }
    }

    private bool IsGrounded()
    {
        bool r = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1f);
        if(r && hit.collider.tag == "Obstacle")
        {
            SetReward(-10f);
            EndEpisode();
        }
        return r;
    }
    #endregion
}
