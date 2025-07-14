using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class CubeTesting : Agent
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private bool canAgentJump = false;
    [SerializeField] private int timeScale = 1;
    [SerializeField] private GameObject flowerPrefab;

    // campi publici perche devono essere accessibili da mazegenerator
    public GameObject floor;
    public GameObject wall; // Prefab del muro da generare
    public GameObject flowers; // Prefab del muro da generare

    private Rigidbody rb;
    private Vector3 startPosition;
    private Transform[] floorCells;

    public Animator animator;
    private GameObject lastVisitedCell = null; // Memorizza l'ultima cella visitata
    private GameObject currentCell = null; // Memorizza la cella corrente  
    private Dictionary<Transform, int> cellVisitCount = new Dictionary<Transform, int>(); // Contatore delle visite per cella
    private MazeController mazeController;
    private int totalVisitedCells = 0; // Contatore totale delle celle visitate
    public override void Initialize()
    {
        Time.timeScale = timeScale;
        rb = GetComponent<Rigidbody>();
        startPosition = transform.localPosition;
        mazeController = this.GetComponentInParent<MazeController>();
    }



    public override void OnEpisodeBegin()   
    {       
            totalVisitedCells = 0;
            transform.localPosition = startPosition;

            // Resetta lo stato delle celle esplorate
            InitiateFloor();

            // spawna agente casualmente
            this.transform.position = RandomFloorPosition();

            currentCell = GetCurrentCell();
            this.rb.linearVelocity = Vector3.zero;
            this.rb.angularVelocity = Vector3.zero;
            this.transform.rotation = Quaternion.Euler(Vector3.up * Random.Range(0f, 360f));
        
    }


    private void DestroyAll(GameObject parent)
    {
        if (parent != null)
        {
            foreach (Transform child in parent.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

   
    #region CellMethods
    private void InitiateFloor()
    {
        currentCell = null;
        lastVisitedCell = null;
        cellVisitCount.Clear();
        
        floorCells = floor.GetComponentsInChildren<Transform>(true);

        foreach (Transform cell in floorCells)
        {
            if (cell != floor.transform)
            {
                cellVisitCount[cell] = 0; // Inizialmente tutte le celle hanno 0 visite
            }
        }

        DestroyAll(flowers); // Distruggi i fiori esistenti
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

    // Funzione per assegnare una posizione casuale sul pavimento (e assegnare visita = 1 alla prima cella)
    private Vector3 RandomFloorPosition()
    {
        //ottengo oggetto padre floor


        if (floorCells != null && floorCells.Length != 0)
        {
            // Scegli una cella casuale
            Transform randomCell = floorCells[Random.Range(0, floorCells.Length)];

            // Restituisci le coordinate del centro della cella
            Vector3 cellPosition = randomCell.position;
            cellPosition.y = transform.position.y; // Mantieni l'altezza dell'agente
            return cellPosition;
        }
        return this.startPosition;
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
        // Controlla che currentCell sia valido e non sia stato distrutto
        if (currentCell == null || currentCell.Equals(null))
        {
            // Osservazione di fallback se la cella corrente non esiste più
            sensor.AddOneHotObservation((int)VisitStatus.Invalid, NUM_VISIT_STATES);
            sensor.AddObservation(0f);
            // Puoi anche aggiungere osservazioni di fallback per i vicini, se necessario
            for (int i = 0; i < 4; i++)
            {
                sensor.AddOneHotObservation((int)VisitStatus.Invalid, NUM_VISIT_STATES);
                sensor.AddObservation(0f);
            }
            return;
        }

        AddCellObservation(currentCell.transform, sensor);
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
                break;
            case 2:
                transform.Rotate(0f, -rotationSpeed, 0f, Space.Self);
                break;
        }

        if (canAgentJump && jump == 1 && IsGrounded())
        {            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 5f, 0f);
        }


        UpdateCellStatus();

        if (currentCell != null && currentCell.tag == "Floor")
            CheckExploredCell();

    }

    void Update()
    {
        if (Input.GetKey("r"))
        {
            Restart();
        }

        //Calcola quanto si è spostato
        float distanceMoved = (transform.position - startPosition).magnitude;

        // Considera camminata solo se il movimento è maggiore di una soglia
        bool isWalking = distanceMoved > 0.01f;

        // Aggiorna il parametro nell'Animator
        animator.SetBool("isWalking", isWalking);

        // Aggiorna la posizione per il prossimo frame
        startPosition = transform.position;

    }

    void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator SpawnFlowerWithDelay(Vector3 flowerPosition)
    {
        // Istanzia fiore nell'oggetto parent "flowers" con un delay di mezzo secondo
        yield return new WaitForSeconds(0.1f);
        GameObject flowerInstance = Instantiate(flowerPrefab, flowerPosition, Quaternion.identity, flowers.transform);
    }

    void CheckExploredCell()
    {

        if (cellVisitCount[currentCell.transform] == 0)
        {
            // Premia l'esplorazione
            cellVisitCount[currentCell.transform]++;
            totalVisitedCells++;
            Vector3 flowerPosition = currentCell.transform.position;
            flowerPosition.y += 0.1f; // Alza il fiore sopra la cella

            // Istanzia fiore nell'obejct parent Flower con un delay di mezzo secondo
            StartCoroutine(SpawnFlowerWithDelay(flowerPosition));


        }
        // La cella è stata esplorata, ma la visitiamo di nuovo
        else if (lastVisitedCell != currentCell && lastVisitedCell != null && IsGrounded())
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
            }
        }

        // Aggiorna la cella corrente e la cella precedente
        UpdateCellStatus();
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



    #region BoolMethodsCheck
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Obstacle"))
        {
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Floor"))
        {
            GameObject landedCell = collision.collider.gameObject;

            // controllo se la landed cell ha un ostacolo vicino a lei
            _ = Physics.Raycast(landedCell.transform.position, Vector3.back, out RaycastHit hit, 2f);
        }
        else if(collision.gameObject.CompareTag("Target"))
        {
            mazeController.TargetReached(totalVisitedCells);
            this.enabled = false;  // Disattiva l'agente

        }
    }

    private bool IsGrounded()
    {
        bool r = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1f);
        if (r && hit.collider.tag == "Obstacle")
        {
            EndEpisode();
        }
        return r;
    }
    #endregion

    public void RestartEpisode()
    {
        this.enabled = true; // Riattiva l'agente
        mazeController.ResetText();
        EndEpisode(); 
    }

}

