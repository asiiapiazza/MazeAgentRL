
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

[System.Serializable]
public class RewardSettings
{
    public float fallingInVoid = -10f;
    public float inAirNotOnGround = 0.01f;
    public float oneJump = -0.001f;
    public float notStraightMovement = -0.005f;
    public float eachStep = -0.001f;
    public float exploreNotVisitedCell = 0.1f;
    public float exploreVisitedCell = -0.001f;
    public float hitExit = 10f;
    public float hitWall = -5f;
    public float jumpedOverObstacle = 0.1f;
    public float hitObstacle = -10f;

    public void ResetToDefaults()
    {
        fallingInVoid = -10f;
        inAirNotOnGround = 0.1f;
        oneJump = -0.001f;
        notStraightMovement = -0.005f;
        eachStep = -0.001f;
        exploreNotVisitedCell = 0.1f;
        exploreVisitedCell = -0.001f;
        hitExit = 10f;
        hitWall = -5f;
        jumpedOverObstacle = 0.1f;
        hitObstacle = -10f;
    }
}

public class CubeTraining : Agent
{
    [Header("Agent Settings")]
    [SerializeField] public float moveSpeed = 4f;
    [SerializeField] public bool canAgentJump = false;
    [SerializeField] public int timeScale = 1;
    [SerializeField] public int numberOfObstacles = 1;
    [SerializeField] public GameObject obstaclePrefab;
    [SerializeField] public GameObject floor;
    [SerializeField] public GameObject wall;
    [SerializeField] public RewardSettings rewardSettings;

    private Rigidbody rb;
    private Vector3 startPosition;
    private bool isGrounded = false;
    private int nonStraightMoveCount = 0;

    private List<Transform> cells = new List<Transform>();
    private Transform[] floorCells;
    private Dictionary<Transform, int> cellVisitCount = new();
    private GameObject currentCell = null;
    private GameObject lastVisitedCell = null;
    private GameObject cellBeforeJump;
    private bool jumpedOverObstacle = false;


    public override void Initialize()
    {
        Time.timeScale = timeScale;
        rb = GetComponent<Rigidbody>();
        startPosition = transform.localPosition;
    }


    public override void OnEpisodeBegin()
    {
        Debug.Log("Episodio numero: " + Academy.Instance.EnvironmentParameters.GetWithDefault("episode_number", 0));


        transform.localPosition = startPosition;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.rotation = Quaternion.Euler(Vector3.up * Random.Range(0f, 360f));

        ResetFloor();
        if (canAgentJump && numberOfObstacles > 0)
            PlaceObstacles(numberOfObstacles);

        transform.position = RandomFloorPosition();
        currentCell = GetCurrentCell();
        PickWallAsTarget();
    }

    private void ResetFloor()
    {
        cells.Clear();
        cellVisitCount.Clear();
        floorCells = floor.GetComponentsInChildren<Transform>(true);

        foreach (Transform cell in floorCells)
        {
            if (cell != floor.transform)
            {
                cell.gameObject.SetActive(true);
                cells.Add(cell);
                cellVisitCount[cell] = 0;
                cell.GetComponent<Renderer>().material.color = Color.white;
            }
        }
        currentCell = null;
        lastVisitedCell = null;
    }

    private Vector3 RandomFloorPosition()
    {
        if (cells.Count == 0) return startPosition;
        Transform randomCell = cells[Random.Range(0, cells.Count)];
        Vector3 pos = randomCell.position;
        pos.y = transform.position.y;
        return pos;
    }

    private GameObject GetCurrentCell()
    {
        return Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1f) ? hit.collider.gameObject : null;
    }

    private void PickWallAsTarget()
    {
        Transform[] wallChildren = wall.GetComponentsInChildren<Transform>();
        List<Transform> walls = new List<Transform>(wallChildren);
        if (walls.Count > 0) walls.RemoveAt(0);

        foreach (Transform w in walls)
        {
            if (w.TryGetComponent(out Renderer renderer))
            {
                ColorUtility.TryParseHtmlString("#ECA12F", out Color color);
                renderer.material.color = color;
                w.tag = "Wall";
                w.GetComponent<Collider>().isTrigger = false;
            }
        }

        if (walls.Count > 0)
        {
            Transform target = walls[Random.Range(0, walls.Count)];
            target.tag = "Target";
            if (target.TryGetComponent(out Renderer r))
                r.material.color = Color.magenta;
            target.GetComponent<Collider>().isTrigger = true;
        }
    }

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


    public override void OnActionReceived(ActionBuffers actions)
    {


        Vector3 move = transform.forward * moveSpeed * Time.deltaTime;
        float rotationSpeed = moveSpeed * 0.6f;

        switch (actions.DiscreteActions[0])
        {
            case 1: rb.MovePosition(transform.position + move); break;
            case 2: rb.MovePosition(transform.position - move); break;
        }

        switch (actions.DiscreteActions[1])
        {
            case 1: transform.Rotate(0, rotationSpeed, 0); nonStraightMoveCount++; break;
            case 2: transform.Rotate(0, -rotationSpeed, 0); nonStraightMoveCount++; break;
        }

        if (canAgentJump && actions.DiscreteActions[2] == 1 && IsGrounded())
        {
            ApplyReward(rewardSettings.oneJump, "Jump");
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 5f, 0f);
        }

        //if (nonStraightMoveCount > StepCount * 0.5f)
        //    ApplyReward(rewardSettings.notStraightMovement, "NotStraight");

        UpdateCellStatus();
        if (currentCell.CompareTag("Floor"))
            EvaluateExplorationReward();

        EvaluateJumpRewards();
        AddReward(rewardSettings.eachStep);
    }

    private void UpdateCellStatus()
    {
        GameObject newCell = GetCurrentCell();
        if (newCell != null)
        {
            lastVisitedCell = currentCell;
            currentCell = newCell;
        }
    }

    private void EvaluateExplorationReward()
    {
        if (cellVisitCount[currentCell.transform] == 0)
        {
            // Premia l'esplorazione
            ApplyReward(rewardSettings.exploreNotVisitedCell, "NewCell");
            var renderer = currentCell.transform.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = Color.cyan;

            cellVisitCount[currentCell.transform]++;

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
                renderer.material.color = Color.Lerp(Color.cyan, Color.blue, intensity);
            }

            if (visits > 1)
                ApplyReward(rewardSettings.exploreVisitedCell * visits, "RevisitCell x" + visits);


        }
        UpdateCellStatus();  // Aggiorna la cella corrente e la cella precedente
    }


    private void EvaluateJumpRewards()
    {
        if (transform.position.y < 0)
        {
            SetReward(rewardSettings.fallingInVoid);
            EndEpisode();
            return;
        }

        bool hasGround = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 5f);

        if (!IsGrounded() && (!hasGround || hit.collider.CompareTag("Obstacle")))
            ApplyReward(rewardSettings.inAirNotOnGround, "AirJump");

        if (isGrounded && currentCell != null)
        {
            cellBeforeJump = currentCell;
            bool forwardHit = Physics.Raycast(cellBeforeJump.transform.position, transform.forward, out RaycastHit forward, 3f);
            jumpedOverObstacle = !forwardHit || forward.collider.CompareTag("Obstacle");
        }
    }

    private void ApplyReward(float value, string reason)
    {
      
        AddReward(value);
        //Debug.Log($"[Reward] {value} for {reason}");
    }

    private bool IsGrounded()
    {
        bool grounded = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1f);
        if (grounded && hit.collider.CompareTag("Obstacle"))
        {
            SetReward(rewardSettings.hitObstacle);
            EndEpisode();
        }
        return grounded;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Target"))
        {
            SetReward(rewardSettings.hitExit);
            EndEpisode();
            Debug.Log($"[Reward] {rewardSettings.hitExit} for hitting the exit wall.");

        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Obstacle"))
        {
            SetReward(rewardSettings.hitWall);
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Floor"))
        {
            isGrounded = true;
            if (jumpedOverObstacle && cellBeforeJump != null && collision.gameObject != cellBeforeJump)
            {
                ApplyReward(rewardSettings.jumpedOverObstacle, "JumpedObstacle");
            }
            jumpedOverObstacle = false;
        }

    }

    //void update per vedere se cadon nel vuoto
    private void Update()
    {

        if (transform.position.y < 0)
        {
            SetReward(rewardSettings.fallingInVoid);
            EndEpisode();
            return;
        }


    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.DiscreteActions;
        actions[0] = Input.GetAxisRaw("Vertical") >= 0 ? Mathf.RoundToInt(Input.GetAxisRaw("Vertical")) : 2;
        actions[1] = Input.GetAxisRaw("Horizontal") >= 0 ? Mathf.RoundToInt(Input.GetAxisRaw("Horizontal")) : 2;
        actions[2] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    private void PlaceObstacles(int countObstacles)
    {
        GameObject obstacles = null;

        // Trova oggetto "Obstacles" tra i fratelli
        Transform parent = transform.parent;
        foreach (Transform sibling in parent)
        {
            if (sibling.name == "Obstacles")
            {
                obstacles = sibling.gameObject;
                foreach (Transform child in obstacles.transform)
                    Destroy(child.gameObject);
                break;
            }
        }

        for (int i = 0; i < countObstacles; i++)
        {
            int attempts = 0;
            Transform randomCell;
            do
            {
                randomCell = cells[Random.Range(0, cells.Count)];
                attempts++;
                if (attempts > 10000) break;
            }
            while (!AreNeighborsAccessible(randomCell) || IsCellDeadEnd(randomCell.gameObject));

            Vector3 pos = randomCell.position;

            randomCell.gameObject.SetActive(false);
            cells.Remove(randomCell);
            cellVisitCount.Remove(randomCell);

            if (Random.Range(0, 2) == 1 && obstaclePrefab != null && obstacles != null)
                Instantiate(obstaclePrefab, pos, Quaternion.identity, obstacles.transform);
        }
    }

    private bool AreNeighborsAccessible(Transform cell)
    {
        Vector3[] directions = { cell.forward, -cell.forward, cell.right, -cell.right };
        foreach (Vector3 dir in directions)
        {
            if (!Physics.Raycast(cell.position, dir, out RaycastHit hit, 2f) ||
                (hit.collider != null && hit.collider.CompareTag("Obstacle")))
                return false;
        }
        return true;
    }

    private bool IsCellDeadEnd(GameObject cell)
    {
        Vector3[] directions = { cell.transform.forward, -cell.transform.forward, cell.transform.right, -cell.transform.right };
        int wallCount = 0;
        foreach (Vector3 dir in directions)
        {
            if (Physics.Raycast(cell.transform.position, dir, out RaycastHit hit, 2f) && hit.collider.CompareTag("Wall"))
                wallCount++;
        }
        return wallCount >= 3;
    }

    void OnDrawGizmos()
    {
        foreach (Transform cell in cellVisitCount.Keys)
        {
            Bounds bounds = cell.GetComponent<Collider>().bounds;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }


}