using Grpc.Core;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class MazeGenerator : MonoBehaviour
{
    public int width = 10;
    public int height = 10;
    public float cellSize = 3f;

    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject targetPrefab;
    public GameObject agentPrefab;


    List<GameObject> cells = new List<GameObject>();

    private bool[,] visited;
    private Dictionary<Vector2Int, GameObject[]> wallsDict = new Dictionary<Vector2Int, GameObject[]>();
    private GameObject targetInstance;

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

    void Start()
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



        GenerateGrid();
        StartCoroutine(GenerateMaze(Vector2Int.zero));



    }

    public void SaveMazeAsPrefab(GameObject mazeRoot, string prefabName)
    {
        string localPath = "Assets/Prefab/GeneratedMazes/" + prefabName + ".prefab";
        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

        PrefabUtility.SaveAsPrefabAsset(mazeRoot, localPath);
        Debug.Log("Maze prefab salvato in: " + localPath);
    }


    // Funzione per assegnare una posizione casuale sul pavimento (e assegnare visita = 1 alla prima cella)
    private void RandomFloorPosition()
    {
        // Scegli una cella casuale
        GameObject randomCell = cells[Random.Range(0, cells.Count)];
        // Restituisci le coordinate del centro della cella
        Vector3 cellPosition = randomCell.transform.position;
        //cellPosition.y = transform.position.y; // Mantieni l'altezza dell'agente
        Quaternion rot = Quaternion.Euler(Vector3.up * Random.Range(0f, 360f));
        Instantiate(agentPrefab, cellPosition, rot, transform);

        CubeAgent2 scriptInstance = agentPrefab.GetComponent<CubeAgent2>();
        // Assegna valori ai campi dello script
        scriptInstance.floor = floorParent;
    }



    void GenerateGrid()
    {
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

        // Calcolo posizione per centrare il labirinto
        float mazeWidth = width * cellSize;
        float mazeHeight = height * cellSize;
        Vector3 centerOffset = new Vector3(-mazeWidth / 2f + cellSize / 2f, 0, -mazeHeight / 2f + cellSize / 2f);
        transform.position = centerOffset;
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

        //RandomFloorPosition();

        // Salva il labirinto come prefab
        SaveMazeAsPrefab(mazeParent, "Maze_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));

        // Rimuovi script di generazione dal prefab
        Destroy(mazeParent.GetComponent<MazeGenerator>());
    }


    void PlaceExit(Vector2Int last)
    {
        Vector3 exitPos = Vector3.zero;
        GameObject wallToRemove = null;

        // Determina quale lato è sul bordo
        if (last.y == height - 1)  // bordo nord
        {
            wallToRemove = wallsDict[last][0];
            exitPos = wallToRemove.transform.position;
        }
        else if (last.x == width - 1) // bordo est
        {
            wallToRemove = wallsDict[last][1];
            exitPos = wallToRemove.transform.position;
        }
        else if (last.y == 0) // bordo sud
        {
            wallToRemove = wallsDict[last][2];
            exitPos = wallToRemove.transform.position;
        }
        else if (last.x == 0) // bordo ovest
        {
            wallToRemove = wallsDict[last][3];
            exitPos = wallToRemove.transform.position;
        }
        else
        {
            // Se non è sul bordo, scegli un lato a caso da aprire
            int border = Random.Range(0, 4);
            wallToRemove = wallsDict[last][border];
            exitPos = wallToRemove.transform.position;
        }

        if (wallToRemove != null)
        {
            Destroy(wallToRemove);
            targetInstance = Instantiate(targetPrefab, exitPos, wallToRemove.transform.rotation, mazeParent.transform);

        }
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