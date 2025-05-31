using Assets.Scripts;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class OptionController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public MazeGenerator mazeGenerator;
    public GameObject minimap;
    public TMPro.TextMeshProUGUI text;

    private void DeleteMaze()
    {
        // Trova l'oggetto genitore del labirinto
        GameObject mazeParent = GameObject.Find("MazeParent");
        if (mazeParent != null)
        {
            Destroy(mazeParent);
        }
    }

    public void GenerateMaze_5x5()
    {
        Reset();
        mazeGenerator.GenerateMaze();     
    }

    public void GenerateMaze_7x7()
    {
        Reset();
        mazeGenerator.GenerateMaze();
    }

    public void GenerateMaze_10x10()
    {
        Reset();
        mazeGenerator.GenerateMaze();
    }

    public void GenerateMaze_15x15()
    {
        Reset();
        mazeGenerator.GenerateMaze();
    }


    private void Reset()
    {
        text.text = " "; //resetto testo
        AgentState.bottoneCliccato = false; //resetto bottone cliccato
        minimap.SetActive(false);
        DeleteMaze();
    }
}
