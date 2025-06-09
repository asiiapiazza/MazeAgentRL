using Assets.Scripts;
using Unity.MLAgents;
using Unity.Sentis;
using UnityEditor;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public GameObject sizeOptions;
    public GameObject minimap;
    public TMPro.TextMeshProUGUI startButtonText; // Se usi UI.Text


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

    public void OnStartButtonClicked()
    {


        GameObject maze = GameObject.Find("MazeParent");

        if (maze != null)
        {
            var childrens = maze.GetComponentsInChildren<Transform>(true);
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
                CubeAgent script = agent.GetComponent<CubeAgent>();

                //episodio gia iniziato? posso resettare
                if (AgentState.bottoneCliccato == false)
                {
                    // Primo avvio
                    script.enabled = true;
                    agent.SetActive(true);
                    AgentState.bottoneCliccato = true;
                    startButtonText.text = "Start";

                }
                else //episodio non ancora iniziato
                {
                    // Restart episodio
                    script.RestartEpisode();

                }
            }

            // Attiva la minimappa
            minimap.SetActive(true);
        }

    }

}

