using Grpc.Core;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

public class MazeGenerator : MonoBehaviour
{
    public int width = 10;
    public int height = 10;
    public float cellSize = 3f;
    public Camera topCamera;

    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject targetPrefab;
    public GameObject agentPrefab;

    [SerializeField] private GameObject woodObstacle;
    [SerializeField] private GameObject puddleObstacle;
    [SerializeField] private TMP_InputField numberOfObstacles;




    List<GameObject> cells = new List<GameObject>();

    private bool[,] visited;
    private Dictionary<Vector2Int, GameObject[]> wallsDict = new Dictionary<Vector2Int, GameObject[]>();

    // Crea oggetto genitore per pavimenti, muri, ostacoli, fiori
    GameObject mazeParent;
    GameObject floorParent;
    GameObject wallParent;
    GameObject flowerParent;
    GameObject obstacleParent;


    private Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),   // North
        new Vector2Int(1, 0),   // East
        new Vector2Int(0, -1),  // South
        new Vector2Int(-1, 0),  // West
    };

    public void GenerateMaze()
    {
        // Crea oggetto vuoto genitore maze
        mazeParent = new GameObject("MazeParent");

        floorParent = new GameObject("FloorParent");
        wallParent = new GameObject("WallParent");
        flowerParent = new GameObject("FlowerParent");
        obstacleParent = new GameObject("ObstacleParent");


        floorParent.transform.parent = mazeParent.transform;
        wallParent.transform.parent = mazeParent.transform;
        flowerParent.transform.parent = mazeParent.transform;
        obstacleParent.transform.parent = mazeParent.transform;

        GenerateGrid(width, height);
        StartCoroutine(GenerateMaze(Vector2Int.zero));


        //posiziona al centro
        Vector3 areaCenter = new Vector3(410f, 0, 493); // o Vector3.zero o qualsiasi altro centro
        float mazeWidth = width * cellSize;
        float mazeHeight = height * cellSize;
        Vector3 mazeOffset = new Vector3(-mazeWidth / 2f + cellSize / 2f, 0, -mazeHeight / 2f + cellSize / 2f);
        mazeParent.transform.position = areaCenter + mazeOffset;

        //setto dimensioni della camera in base alla dimensione

        // Imposta orthographicSize per vedere tutto il labirinto
        topCamera.orthographicSize = width * 2f; // padding opzionale, tipo 1f

    }

    public void SaveMazeAsPrefab(GameObject mazeRoot, string prefabName)
    {
        string localPath = "Assets/Prefab/GeneratedMazes/" + prefabName + ".prefab";
        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

        PrefabUtility.SaveAsPrefabAsset(mazeRoot, localPath);
        Debug.Log("Maze prefab salvato in: " + localPath);
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


    private void PlaceObsticles()
    {

        //prendo i numeri di ostacoli presi dalla inputtextbox, altrimenti sono 0


        string nObs = numberOfObstacles.text;

        //converti ad intero nObs
        int numberOfObstaclesint = 0;
        int.TryParse(nObs, out numberOfObstaclesint);
  


        // distruggo ostacoli precedenti
        //cercalo solo nei fratelli di this

        GameObject obstacles = null;
        Transform randomCell;


        foreach (Transform sibling in mazeParent.transform)
        {
            if (sibling.name == "ObstacleParent")
            {
                obstacles = sibling.gameObject;
                DestroyAll(obstacles);
            }
        }



        //itera per couuntoObstacles
        for (int i = 0; i < numberOfObstaclesint; i++)
        {

            //controlla che due osctaoli non siano vicini 
            //fai 50 tentaitivi poi esci
            int attempts = 0;
            do
            {
                randomCell = cells[Random.Range(0, cells.Count)].transform;
                attempts++;
                if (attempts > 50)
                    break;
            }
            while (!GetNeighbors(randomCell) || isCellDeadEnd(randomCell.gameObject));



            var pos = randomCell.position;

            //rimuovi cella dalla logica
            randomCell.gameObject.SetActive(false);
            cells.Remove(randomCell.gameObject);
            //cellVisitCount.Remove(randomCell);


            int random = Random.Range(0, 2);

            //dammi una rotazione casuale (0, 90, 180, 270)
            //ma deve essere intera

            int randomRotation = Random.Range(0, 4) * 90;
            Quaternion rotation = Quaternion.identity;
            rotation.y = randomRotation;

            if (random == 1)
            {
                pos.y += -0.078f;
                GameObject obstacleInstance = Instantiate(woodObstacle, pos, rotation, obstacles.transform);
            }
            else
            {
                GameObject obstacleInstance = Instantiate(puddleObstacle, pos, rotation, obstacles.transform);

            }
        }

    }


    void GenerateGrid(int width, int height)
    {
        cells.Clear();
        visited = new bool[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 pos = new Vector3(x * cellSize, 0, z * cellSize);
                GameObject cell = Instantiate(floorPrefab, pos, Quaternion.identity, floorParent.transform);

                GameObject north = null, east = null, south = null, west = null;

                // Solo se siamo sull'ultima riga, creiamo il muro nord
                if (z == height - 1)
                    north = Instantiate(wallPrefab, pos + new Vector3(0, 1.5f, cellSize / 2), Quaternion.identity, wallParent.transform);

                // Solo se siamo sull'ultima colonna, creiamo il muro est
                if (x == width - 1)
                    east = Instantiate(wallPrefab, pos + new Vector3(cellSize / 2, 1.5f, 0), Quaternion.Euler(0, 90, 0), wallParent.transform);

                // Sempre crea sud e ovest
                south = Instantiate(wallPrefab, pos + new Vector3(0, 1.5f, -cellSize / 2), Quaternion.identity, wallParent.transform);
                west = Instantiate(wallPrefab, pos + new Vector3(-cellSize / 2, 1.5f, 0), Quaternion.Euler(0, 90, 0), wallParent.transform);

                cells.Add(cell);

                wallsDict[new Vector2Int(x, z)] = new GameObject[] { north, east, south, west };
            }
        }


    }


    IEnumerator GenerateMaze(Vector2Int start)
    {
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(start);
        visited[start.x, start.y] = true;

        Vector2Int last = start;

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Pop();
            List<Vector2Int> neighbors = new List<Vector2Int>();

            foreach (var dir in directions)
            {
                Vector2Int next = current + dir;
                if (IsInside(next) && !visited[next.x, next.y])
                    neighbors.Add(next);
            }

            if (neighbors.Count > 0)
            {
                stack.Push(current);

                Vector2Int chosen = neighbors[Random.Range(0, neighbors.Count)];
                RemoveWallBetween(current, chosen);

                visited[chosen.x, chosen.y] = true;
                stack.Push(chosen);

                last = chosen;

                yield return null; // per animare la generazione
            }
        }

        PlaceExit(last);


        PlaceObsticles();
        SpawnAgent();

        // Salva il labirinto come prefab
        //SaveMazeAsPrefab(mazeParent, "Maze_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));

        // Rimuovi script di generazione dal prefab
        // Destroy(mazeParent.GetComponent<MazeGenerator>());


    }




    void SpawnAgent()
    {
        //prendi una cella casuale
        GameObject randomCell = cells[Random.Range(0, cells.Count)];
        Vector3 pos = randomCell.transform.position;
        pos.y += 0.5f; // alza l'agente sopra il pavimento

        GameObject agentInstance = Instantiate(agentPrefab, pos, Quaternion.identity, mazeParent.transform);
        CubeAgent2 script = agentInstance.GetComponent<CubeAgent2>();

        script.wall = wallParent;
        script.floor = floorParent;
        script.flowers = flowerParent;

        //piazzo l'agente però non lo faccio partire
        script.enabled = false;
        agentInstance.SetActive(false);


    }


    void PlaceExit(Vector2Int last)
    {
        Vector3 exitPos = Vector3.zero;
        GameObject wallToRemove = null;
        GameObject[] wallOptions = wallsDict[last];

        if (last.y == height - 1 && wallOptions[0] != null)  // bordo nord
        {
            wallToRemove = wallOptions[0];
        }
        else if (last.x == width - 1 && wallOptions[1] != null) // bordo est
        {
            wallToRemove = wallOptions[1];
        }
        else if (last.y == 0 && wallOptions[2] != null) // bordo sud
        {
            wallToRemove = wallOptions[2];
        }
        else if (last.x == 0 && wallOptions[3] != null) // bordo ovest
        {
            wallToRemove = wallOptions[3];
        }
        else
        {
            // Se non è sul bordo o il muro è già stato distrutto, scegli uno ancora presente
            List<int> validWalls = new List<int>();
            for (int i = 0; i < wallOptions.Length; i++)
            {
                if (wallOptions[i] != null)
                    validWalls.Add(i);
            }

            if (validWalls.Count > 0)
            {
                int chosenIndex = validWalls[Random.Range(0, validWalls.Count)];
                wallToRemove = wallOptions[chosenIndex];
            }
            else
            {
                Debug.LogWarning("Nessun muro disponibile per creare un'uscita.");
                return;
            }
        }

        exitPos = wallToRemove.transform.position;

        Destroy(wallToRemove);
        Instantiate(targetPrefab, exitPos, wallToRemove.transform.rotation, mazeParent.transform);
    }


    void RemoveWallBetween(Vector2Int a, Vector2Int b)
    {
        Vector2Int dir = b - a;

        // Rimuovi muro da A verso B
        if (dir == new Vector2Int(0, 1))      // B sopra A → rimuovi muro nord di A e sud di B
        {
            Destroy(wallsDict[a][0]);
            Destroy(wallsDict[b][2]);
        }
        else if (dir == new Vector2Int(1, 0)) // B a destra di A
        {
            Destroy(wallsDict[a][1]);
            Destroy(wallsDict[b][3]);
        }
        else if (dir == new Vector2Int(0, -1)) // B sotto A
        {
            Destroy(wallsDict[a][2]);
            Destroy(wallsDict[b][0]);
        }
        else if (dir == new Vector2Int(-1, 0)) // B a sinistra di A
        {
            Destroy(wallsDict[a][3]);
            Destroy(wallsDict[b][1]);
        }
    }

    bool IsInside(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
    }
}