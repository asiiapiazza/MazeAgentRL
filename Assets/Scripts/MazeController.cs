using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.Sentis;
using Unity.VisualScripting;
using UnityEngine;


public class MazeController : MonoBehaviour

{
    public GameObject maze;
    public GameObject agent;
    private CubeAgent script;
    public TMPro.TextMeshProUGUI text;


    private void Start()
    {
        script = agent.GetComponent<CubeAgent>();

    }

    public void TargetReached(int totalVisitedCells)
    {

        // Cambio testo del target con il numero di celle totali visitate
        text.text = "Total Visited Cells: " + totalVisitedCells.ToString();

        //metti agente in idle animation usando animator
        Animator animator = agent.GetComponent<Animator>();
        animator.SetBool("isWalking", false);


    }

    public void ResetText()
    {
        //resetto testo
        text.text = " ";

    }

    public static void UploadModelFile(ModelAsset pathFile)
    {


        //agent.GetComponent<Agent>().SetModel("CubeAgent", model);

    }
}
