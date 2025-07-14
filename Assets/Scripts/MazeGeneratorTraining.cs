using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MazeGeneratorTraining : MonoBehaviour
{
    public int width = 10;
    public int height = 10;
    public float cellSize = 3f;

    public GameObject floorPrefab;
    //public GameObject targetPrefab;
    public GameObject wallPrefab;
    //public GameObject agentPrefab;

    List<GameObject> cells = new List<GameObject>();
    private bool[,] visited;
    private Dictionary<Vector2Int, GameObject[]> wallsDict = new Dictionary<Vector2Int, GameObject[]>();
    private GameObject targetInstance;

    public GameObject mazeParent;
    GameObject floorParent;
    GameObject wallParent;
    GameObject obstacleParent;

    private Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),   // North
        new Vector2Int(1, 0),   // East
        new Vector2Int(0, -1),  // South
        new Vector2Int(-1, 0),  // West
    };


    public void GenerateMazeInEditor()
    {
        // Rimuove eventuali labirinti esistenti
        if (mazeParent != null)
        {
            DestroyImmediate(mazeParent);
        }

        mazeParent = new GameObject("MazeParent");
        floorParent = new GameObject("FloorParent");
        wallParent = new GameObject("WallParent");
        obstacleParent = new GameObject("ObstacleParent");

        floorParent.transform.parent = mazeParent.transform;
        wallParent.transform.parent = mazeParent.transform;
        obstacleParent.transform.parent = mazeParent.transform;

        cells.Clear();
        wallsDict.Clear();
        visited = new bool[width, height];

        GenerateGrid();

        // Generazione procedurale tramite recursive backtracker (iterativo)
        GenerateMazeRecursive(Vector2Int.zero);

        //Funzione per posizionare l'uscita (disattivata per rendere la posizione casuale)
        //PlaceExitAtLastCell();

        // Centra il labirinto nella scena
        float mazeWidth = width * cellSize;
        float mazeHeight = height * cellSize;
        Vector3 centerOffset = new Vector3(-mazeWidth / 2f + cellSize / 2f, 0, -mazeHeight / 2f + cellSize / 2f);
        mazeParent.transform.position = centerOffset;

        Debug.Log("Maze generated!");
    }

    private void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 pos = new Vector3(x * cellSize, 0, z * cellSize);
                GameObject cell = Instantiate(floorPrefab, pos, Quaternion.identity, floorParent.transform);

                GameObject north = null, east = null, south = null, west = null;

                if (z == height - 1)
                    north = Instantiate(wallPrefab, pos + new Vector3(0, 1.5f, cellSize / 2), Quaternion.identity, wallParent.transform);

                if (x == width - 1)
                    east = Instantiate(wallPrefab, pos + new Vector3(cellSize / 2, 1.5f, 0), Quaternion.Euler(0, 90, 0), wallParent.transform);

                south = Instantiate(wallPrefab, pos + new Vector3(0, 1.5f, -cellSize / 2), Quaternion.identity, wallParent.transform);
                west = Instantiate(wallPrefab, pos + new Vector3(-cellSize / 2, 1.5f, 0), Quaternion.Euler(0, 90, 0), wallParent.transform);

                cells.Add(cell);
                wallsDict[new Vector2Int(x, z)] = new GameObject[] { north, east, south, west };
            }
        }
    }

    private void GenerateMazeRecursive(Vector2Int start)
    {
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(start);
        visited[start.x, start.y] = true;

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Pop();
            List<Vector2Int> neighbors = new List<Vector2Int>();

            foreach (var dir in directions)
            {
                Vector2Int next = current + dir;
                if (IsInside(next) && !visited[next.x, next.y])
                {
                    neighbors.Add(next);
                }
            }

            if (neighbors.Count > 0)
            {
                stack.Push(current);

                Vector2Int chosen = neighbors[Random.Range(0, neighbors.Count)];
                RemoveWallBetween(current, chosen);

                visited[chosen.x, chosen.y] = true;
                stack.Push(chosen);
            }
        }
    }

    private void RemoveWallBetween(Vector2Int a, Vector2Int b)
    {
        Vector2Int dir = b - a;

        if (dir == new Vector2Int(0, 1))
        {
            DestroyImmediate(wallsDict[a][0]);
            DestroyImmediate(wallsDict[b][2]);
        }
        else if (dir == new Vector2Int(1, 0))
        {
            DestroyImmediate(wallsDict[a][1]);
            DestroyImmediate(wallsDict[b][3]);
        }
        else if (dir == new Vector2Int(0, -1))
        {
            DestroyImmediate(wallsDict[a][2]);
            DestroyImmediate(wallsDict[b][0]);
        }
        else if (dir == new Vector2Int(-1, 0))
        {
            DestroyImmediate(wallsDict[a][3]);
            DestroyImmediate(wallsDict[b][1]);
        }
    }

    private void PlaceExitAtLastCell()
    {
        Vector2Int last = new Vector2Int(width - 1, height - 1);

        GameObject wallToRemove = null;
        Vector3 exitPos = Vector3.zero;

        if (last.y == height - 1)
        {
            wallToRemove = wallsDict[last][0];
            exitPos = wallToRemove.transform.position;
        }
        else if (last.x == width - 1)
        {
            wallToRemove = wallsDict[last][1];
            exitPos = wallToRemove.transform.position;
        }
        else if (last.y == 0)
        {
            wallToRemove = wallsDict[last][2];
            exitPos = wallToRemove.transform.position;
        }
        else if (last.x == 0)
        {
            wallToRemove = wallsDict[last][3];
            exitPos = wallToRemove.transform.position;
        }

        if (wallToRemove != null)
        {
            DestroyImmediate(wallToRemove);
            //targetInstance = Instantiate(targetPrefab, exitPos, Quaternion.identity, mazeParent.transform);
        }
    }

    private bool IsInside(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
    }
}
