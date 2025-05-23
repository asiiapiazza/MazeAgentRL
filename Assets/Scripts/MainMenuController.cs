using Unity.MLAgents;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public GameObject sizeOptions;
    public GameObject minimap;


    public void StartGame() => SceneManager.LoadScene("Main");

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void ChooseMaze()
    {
        sizeOptions.SetActive(!sizeOptions.activeSelf); // attiva/disattiva
    }

 
    public void GotoMain()
    {
        //CARICA SCENA MAINmenu
        SceneManager.LoadScene("Menu");
    }

    public void StartAgent()
    {
        //una volta che ho il labirinto, attivo l'agente
        GameObject maze = GameObject.Find("MazeParent");

        var childrens = maze.GetComponentsInChildren<Transform>(true);

        //prendi figlio da boh
        GameObject agent = null;
        foreach (var item in childrens)
        {
            if (item.name == "BearFinal(Clone)")
            {
                agent = item.gameObject;
                break;
            }
        }

        if (agent != null)
        {
            CubeAgent2 script = agent.GetComponent<CubeAgent2>();
            script.enabled = true;
            agent.SetActive(true);

            
        }

        //attivo minimappa
         minimap.SetActive(true);
        
    }

}

