using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class CubeAgent2 : Agent
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private bool canAgentJump = false;
    [SerializeField] private int timeScale = 1;

    [SerializeField] private int numberOfObstacles = 1;

    private Rigidbody rb;

    private Vector3 startPosition;
    public GameObject floor;
    public GameObject wall; // Prefab del muro da generare
    private bool isGrounded = false; // Variabile per controllare se l'agente è a terra
    private int nonStraightMoveCount = 0; // Contatore dei movimenti non dritti
    private Transform[] floorCells;
    private List<Transform> cells = new List<Transform>();

    [SerializeField] private GameObject obstaclePrefab;



    private GameObject lastVisitedCell = null; // Memorizza l'ultima cella visitata
    private GameObject currentCell = null; // Memorizza la cella corrente  
    private Dictionary<Transform, int> cellVisitCount = new Dictionary<Transform, int>(); // Contatore delle visite per cella


    public override void Initialize()
    {
        Time.timeScale = timeScale;
        rb = GetComponent<Rigidbody>();
        startPosition = transform.localPosition;
    }



    public override void OnEpisodeBegin()
    {
        transform.localPosition = startPosition;
        nonStraightMoveCount = 0; // Resetta il contatore dei movimenti non dritti


        // Resetta lo stato delle celle esplorate
        InitiateFloor();

        //rimuovi una cella del pavimento per creare il buco
        if(canAgentJump)
            PlaceObsticles(numberOfObstacles);

        // spawna agente casualmente
        this.transform.position = RandomFloorPosition();

        this.rb.linearVelocity = Vector3.zero;
        this.rb.angularVelocity = Vector3.zero;
        this.transform.rotation = Quaternion.Euler(Vector3.up * Random.Range(0f, 360f));

        PickWallAsTarget();
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

    //metodo per rimuovere casualmente una cella del pavimento
    private void PlaceObsticles(int countObstacles)
    {

        // distruggo ostacoli precedenti
        //cercalo solo nei fratelli di this

       GameObject obstacles = null;
       Transform randomCell;


      Transform parent2 = this.transform.parent;
       foreach (Transform sibling in parent2)
            {
                if (sibling.name == "Obstacles")
                {
                    obstacles = sibling.gameObject;
                    DestroyAll(obstacles);
                }
            }
        

        //itera per couuntoObstacles
        for (int i = 0; i < countObstacles; i++)
        {

            //controlla che due osctaoli non siano vicini 
            //fai 50 tentaitivi poi esci
            int attempts = 0;
            do
            {
                randomCell = cells[Random.Range(0, cells.Count)];
                attempts++;
                if (attempts > 50)
                    break;
            }
            while (!GetNeighbors(randomCell));

            var pos = randomCell.position;

            //rimuovi cella dalla logica
            randomCell.gameObject.SetActive(false);
            cells.Remove(randomCell);
            cellVisitCount.Remove(randomCell);

            //GameObject obstacleInstance = Instantiate(obstaclePrefab, pos, Quaternion.identity, obstacles.transform);

            int random = Random.Range(0, 2);
            if (random == 1)
            {
                GameObject obstacleInstance = Instantiate(obstaclePrefab, pos, Quaternion.identity, obstacles.transform);
            }
        }

    }

    private bool GetNeighbors(Transform cell)
    {
        // Ottieni la cella davanti e dietro usando raycast dalla cella data  
        _ = Physics.Raycast(cell.position, cell.forward, out RaycastHit frontHit, 2f);
        _ = Physics.Raycast(cell.position, -cell.forward, out RaycastHit backHit, 2f);
        _ = Physics.Raycast(cell.position, -cell.right, out RaycastHit leftHit, 2f);
        _ = Physics.Raycast(cell.position, cell.right, out RaycastHit rightHit, 2f);

        //se uno di questi ha il collider che hitta un oggetto taggato obstacles oppure niente, allora returna false
        if (frontHit.collider == null || backHit.collider == null || leftHit.collider == null || rightHit.collider == null)
            return false;
        else if(frontHit.collider.CompareTag("Obstacle") || backHit.collider.CompareTag("Obstacle") || leftHit.collider.CompareTag("Obstacle") || rightHit.collider.CompareTag("Obstacle"))
            return false;     
        else
            return true;
    }

    #region CellMethods
    private void InitiateFloor()
    {
        currentCell = null;
        lastVisitedCell = null;
        cells.Clear();
        cellVisitCount.Clear();

        floorCells = floor.GetComponentsInChildren<Transform>(true);
        foreach (Transform cell in floorCells)
        {
            if (cell != floor.transform)
            {
                cell.gameObject.SetActive(true);
                cells.Add(cell);
                cellVisitCount[cell] = 0; // Inizialmente tutte le celle hanno 0 visite
                cell.GetComponent<Renderer>().material.color = Color.white; // Resetta il colore delle celle
            }
        }
    }


    #endregion

    #region "Randomiziation Position"

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



    private void AssignWallProperties(GameObject wall, bool isTarget)
    {
        wall.tag = isTarget ? "Target" : "Wall";
        //assegna colore #ECA12F al rendere
        ColorUtility.TryParseHtmlString("#ECA12F", out Color color);
        wall.GetComponent<Renderer>().material.color = isTarget ? Color.magenta : color;
        wall.GetComponent<Collider>().isTrigger = isTarget;
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
        Invalid = 2 //buchi, muri, ostacoli
    }

    const int NUM_VISIT_STATES = 3;
    private const float MAX_VISITS = 10f; // Or whatever upper bound makes sense


    public override void CollectObservations(VectorSensor sensor)
    {
        // Aggiungi osservazioni per la cella corrente
        GameObject currentGroundObject = null;
        (bool r, RaycastHit hit) = GetDownRayCast(5f);
        if (r)
            currentGroundObject = hit.collider.gameObject;

        // Aggiungi osservazioni per la cella corrente
        AddCellObservation(currentGroundObject, sensor);// Aggiungi osservazioni per le celle vicine (front, back, left, right)
        AddNeighborObservations(currentGroundObject, Vector3.forward, sensor);
        AddNeighborObservations(currentGroundObject, Vector3.back, sensor);
        AddNeighborObservations(currentGroundObject, Vector3.left, sensor);
        AddNeighborObservations(currentGroundObject, Vector3.right, sensor);
    }

    private void AddCellObservation(GameObject cellObject, VectorSensor sensor)
    {
        VisitStatus status = VisitStatus.Invalid;
        float normalizedVisits = 0f;

        if (cellObject != null)
        {
            Transform cellTransform = cellObject.transform;

            if (cellTransform.CompareTag("Obstacle"))
            {
                status = VisitStatus.Invalid;
            }
            else
            {
                int visitCount = cellVisitCount.TryGetValue(cellTransform, out int visits) ? visits : 0;
                status = visitCount > 0 ? VisitStatus.Visited : VisitStatus.NotVisited;
                normalizedVisits = Mathf.Clamp01((float)visitCount / MAX_VISITS);
            }
        }
        // se non becca niiente (quindi il vuoto) da il valore di deafult

        sensor.AddOneHotObservation((int)status, NUM_VISIT_STATES);
        sensor.AddObservation(normalizedVisits);
    }


    private void AddNeighborObservations(GameObject currentCell, Vector3 direction, VectorSensor sensor)
    {
        VisitStatus status = VisitStatus.Invalid;
        float normalizedVisits = 0f;

        // Funzione per valutare la cella vicina  
        void EvaluateNeighborCell(RaycastHit hit)
        {
            Transform neighborCell = hit.transform;
            int visits = cellVisitCount.TryGetValue(neighborCell, out int visitCount) ? visitCount : 0;
            status = visits > 0 ? VisitStatus.Visited : VisitStatus.NotVisited;
            normalizedVisits = Mathf.Clamp01((float)visits / MAX_VISITS);
        }

        // Determina il punto di partenza del raycast  
        Vector3 rayOrigin = currentCell != null ? currentCell.transform.position : new Vector3(transform.position.x, 0, transform.position.z);

        // Esegui il raycast nella direzione specificata  
        if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, 2f))
        {
            if (hit.collider != null)
            {
                string tag = hit.collider.gameObject.tag;
                if (tag == "Floor")
                {
                    EvaluateNeighborCell(hit);
                }
                else if (tag == "Obstacle" || tag == "Wall")
                {
                    status = VisitStatus.Invalid;
                }
            }
        }
        // se non becca niiente (quindi il vuoto) da il valore di deafult

        // Aggiungi le osservazioni al sensore  
        sensor.AddOneHotObservation((int)status, NUM_VISIT_STATES);
        sensor.AddObservation(normalizedVisits);
    }
    #endregion


    private GameObject cellBeforeJump;
    private bool jumpedOverObstacle = false;
    private void ObsticlesRewardSystem()
    {
        if (transform.position.y < 0)
        {
            SetReward(-10f);
            EndEpisode();
        }

        // Controllo se ho qualcosa sotto con raycast
        //(bool r, RaycastHit hits) = GetDownRayCast(5f);

        if (isGrounded && currentCell != null)
        {
            cellBeforeJump = currentCell;

            // Lancia un raycast in avanti per vedere se c'è un ostacolo davanti
            _ = Physics.Raycast(cellBeforeJump.transform.position, this.transform.forward, out RaycastHit hit, 3f);

                if (hit.collider == null || hit.collider.CompareTag("Obstacle"))
                    jumpedOverObstacle = true;  // Possibile salto sopra ostacolo
                else
                    // Nessun hit = buco
                    jumpedOverObstacle = false;            
                
        }

        //deve stare in aria e sotto non deve esserci niente, oppure un ostacolo
        //if (!isGrounded && (!r || (hits.collider.CompareTag("Obstacle"))))
        //{
        //    AddReward(0.1f);
        //}
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

        if (jump == 1 && isGrounded && canAgentJump)
        {
            isGrounded = false;
            AddReward(-0.05f);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 5f, 0f);
        }

        //penalizza per movimenti non dritti
        if (nonStraightMoveCount > StepCount * 0.5f)
            AddReward(-0.005f);

        UpdateCellStatus();
        if (currentCell != lastVisitedCell && currentCell.tag == "Floor")
            CheckExploredCell();        

        if(canAgentJump)
            ObsticlesRewardSystem();

        // Penalizza l'agente per ogni passo
        AddReward(-0.001f);
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
        //queste due celle saranno sempre diverse, deve valere una sola volta
        else if (lastVisitedCell != currentCell && lastVisitedCell != null && isGrounded)
        {

            // Incrementa il contatore delle visite
            if (!cellVisitCount.TryGetValue(currentCell.transform, out int visits))
                visits = 0;

            visits++;
            cellVisitCount[currentCell.transform] = visits;

            var renderer = currentCell.transform.GetComponent<Renderer>();
            if (renderer != null)
            {
                float intensity = Mathf.Clamp01(cellVisitCount[currentCell.transform] / 10f); 
                                                                                              
                renderer.material.color = Color.Lerp(Color.cyan, Color.blue, intensity);
            }

            if (visits > 2)
                AddReward(-0.001f * visits);
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

        if(canAgentJump)
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
        else if (collision.gameObject.CompareTag("Floor"))
        {
            isGrounded = true;
            GameObject landedCell = collision.collider.gameObject;

            // Se ha saltato e atterrato in un'altra cella
            if (jumpedOverObstacle && cellBeforeJump != null && landedCell != cellBeforeJump)
                AddReward(0.1f);  // Premio perché ha davvero superato un ostacolo

            // Reset
            jumpedOverObstacle = false;

        }
    }


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

    private (bool, RaycastHit) GetDownRayCast(float distance)
    {
        bool r = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, distance);
        return (r, hit);
    }

}