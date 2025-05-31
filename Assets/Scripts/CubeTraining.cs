using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class CubeTraining : Agent
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private bool canAgentJump = false;
    [SerializeField] private int timeScale = 1;
    [SerializeField] private int numberOfObstacles = 1;
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] GameObject floor;
    [SerializeField] GameObject wall; // Prefab del muro da generare

    private bool isGrounded = false; // Variabile per controllare se l'agente è a terra
    private int nonStraightMoveCount = 0; // Contatore dei movimenti non dritti
    private Transform[] floorCells;
    private List<Transform> cells = new List<Transform>();
    private Rigidbody rb;
    private Vector3 startPosition;
    private GameObject lastVisitedCell = null; // Memorizza l'ultima cella visitata
    private GameObject currentCell = null; // Memorizza la cella corrente  
    private Dictionary<Transform, int> cellVisitCount = new Dictionary<Transform, int>(); // Contatore delle visite per cella


    // variabili del reward
    const float falling_in_void = -10f;
    const float inAir_and_notOnGround = 0.1f;
    const float one_jump = -0.001f;
    const float not_straight_movement = -0.005f;
    const float each_step = -0.001f;
    const float explore_notVisited_cell = 0.1f;
    const float explore_visited_cell = -0.001f; //moltiplicato per il numero di visite nel metodo
    const float hit_exit = 10f;
    const float hit_wall = -5f;
    const float jumped_over_obstacle = 0.1f;
    const float hit_obstacle = -10f;

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
        if (canAgentJump && numberOfObstacles > 0)
            PlaceObsticles(numberOfObstacles);

        // spawna agente casualmente
        this.transform.position = RandomFloorPosition();

        currentCell = GetCurrentCell();

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

    private bool isCellDeadEnd(GameObject cell)
    {

        //check se cella corrente è circondata da almeno 3 muri
        _ = Physics.Raycast(cell.transform.position, cell.transform.forward, out RaycastHit frontHit, 2f);
        _ = Physics.Raycast(cell.transform.position, -cell.transform.forward, out RaycastHit backHit, 2f);
        _ = Physics.Raycast(cell.transform.position, -cell.transform.right, out RaycastHit leftHit, 2f);
        _ = Physics.Raycast(cell.transform.position, cell.transform.right, out RaycastHit rightHit, 2f);

        //devono essere almeno 3
        int wallCount = 0;
        if (frontHit.collider != null && frontHit.collider.CompareTag("Wall")) wallCount++;
        if (backHit.collider != null && backHit.collider.CompareTag("Wall")) wallCount++;
        if (rightHit.collider != null && rightHit.collider.CompareTag("Wall")) wallCount++;
        if (leftHit.collider != null && leftHit.collider.CompareTag("Wall")) wallCount++;

        if (wallCount >= 3)
            return true;
        else
            return false;
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
            while (!GetNeighbors(randomCell) || isCellDeadEnd(randomCell.gameObject));



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
        else if (frontHit.collider.CompareTag("Obstacle") || backHit.collider.CompareTag("Obstacle") || leftHit.collider.CompareTag("Obstacle") || rightHit.collider.CompareTag("Obstacle"))
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
                //cell.GetComponent<Renderer>().material.color = Color.white; // Resetta il colore delle celle
            }
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


    private GameObject cellBeforeJump;
    private bool jumpedOverObstacle = false;
    private void ObsticlesRewardSystem()
    {
        if (transform.position.y < 0)
        {
            SetReward(falling_in_void);
            EndEpisode();
        }

        // Controllo se ho qualcosa sotto con raycast
        bool hits2 = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit2, 5f);

        //deve stare in aria e sotto non deve esserci niente
        if (!IsGrounded() && (!hits2 || (hit2.collider.tag == "Obstacle")))
        {
            AddReward(inAir_and_notOnGround);
        }

        if (isGrounded && currentCell != null)
        {
            cellBeforeJump = currentCell;

            // Lancia un raycast in avanti per vedere se c'è un ostacolo davanti
            _ = Physics.Raycast(cellBeforeJump.transform.position, this.transform.forward, out RaycastHit hits, 3f);

            if (hits.collider == null || hits.collider.CompareTag("Obstacle"))
            {
                jumpedOverObstacle = true;  // Possibile salto sopra ostacolo

            }
            else
            {
                // Nessun hit = buco
                jumpedOverObstacle = false;
            }

        }

        // se sono su floor e se ho di fronte un ostcolo oppure un buco, e questo buco o ostacolo si trovano ad un vicolo cieco, io non ci devo andare
        // quindi penalizzo se sto saltando verso un ostacolo oppure un buco e questo buco o ostacolo si trovano ad un vicolo cieco




        //// Controlla se l'agente è sopra un ostacolo o un vuoto
        //(bool isHit, RaycastHit hit) = GetDownRayCast(5f);
        //if (!isGrounded && (!isHit || (hit.collider != null && hit.collider.CompareTag("Obstacle"))))
        //{
        //    int floorCellCount = 0;
        //    var pos = transform.transform.position;
        //    pos.y = 0;

        //    // Controlla le celle vicine all'ostacolo o al vuoto
        //    Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        //    foreach (Vector3 direction in directions)
        //    {
        //        if (Physics.Raycast(pos, direction, out RaycastHit neighborHit, 5f) && neighborHit.collider.CompareTag("Floor") || neighborHit.collider.CompareTag("Target"))
        //        {
        //            floorCellCount++;
        //        }
        //    }

        //    // Se ci sono almeno due celle floor vicine, assegna una ricompensa
        //    if (floorCellCount >= 2)
        //    {
        //        AddReward(0.05f);
        //    }
        //    // Se ci sono meno di due celle floor (quindi vicolo cieco) e i muri non sono target, penalizza
        //    else
        //    {
        //        AddReward(-0.05f);

        //    }

        //}

    }

    private (bool, RaycastHit) GetDownRayCast(float distance)
    {
        bool r = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, distance);
        return (r, hit);
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

        if (canAgentJump && jump == 1 && IsGrounded())
        {
            AddReward(one_jump);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 5f, 0f);
        }

        //penalizza per movimenti non dritti
        if (nonStraightMoveCount > StepCount * 0.5f)
            AddReward(not_straight_movement);

        UpdateCellStatus();

        if (currentCell.tag == "Floor")
            CheckExploredCell();

        ObsticlesRewardSystem();


        // Penalizza l'agente per ogni passo
        AddReward(each_step);
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
            AddReward(explore_notVisited_cell);

            cellVisitCount[currentCell.transform]++;

            // Cambia il colore della cella per indicare che è stata esplorata
            var renderer = currentCell.transform.GetComponent<Renderer>();
            //if (renderer != null)
            //    renderer.material.color = Color.cyan;


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
                                                                                              //renderer.material.color = Color.Lerp(Color.cyan, Color.blue, intensity);
            }

            if (visits > 1)
                AddReward(explore_visited_cell * visits);

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
    //void OnDrawGizmos()
    //{
    //    foreach (Transform cell in cellVisitCount.Keys)
    //    {
    //        Bounds bounds = cell.GetComponent<Collider>().bounds;
    //        Gizmos.color = Color.green;
    //        Gizmos.DrawWireCube(bounds.center, bounds.size);
    //    }
    //}


    #endregion


    #region BoolMethodsCheck
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target"))
        {
            SetReward(hit_exit);
            EndEpisode();

        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Obstacle"))
        {
            SetReward(hit_wall);
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Floor"))
        {
            isGrounded = true;
            GameObject landedCell = collision.collider.gameObject;


            // controllo se la landed cell ha un ostacolo vicino a lei
            _ = Physics.Raycast(landedCell.transform.position, Vector3.back, out RaycastHit hit, 2f);


            // Se ha saltato e atterrato in un'altra cella
            if (jumpedOverObstacle && cellBeforeJump != null && landedCell != cellBeforeJump && (hit.collider == null || hit.collider.tag == "Obstacle"))
            {
                AddReward(jumped_over_obstacle);  // Premio perché ha davvero superato un ostacolo


            }

            // Reset
            jumpedOverObstacle = false;
        }
    }


    private bool IsGrounded()
    {
        bool r = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1f);
        if (r && hit.collider.tag == "Obstacle")
        {
            SetReward(hit_obstacle);
            EndEpisode();
        }
        return r;
    }
    #endregion
}