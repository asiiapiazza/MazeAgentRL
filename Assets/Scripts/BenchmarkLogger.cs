using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;
using System;
using System.Linq;

public class BenchmarkLogger : MonoBehaviour
{
    private string filePath;
    private bool headerWritten = false;

    void Start()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        filePath = Application.dataPath + "/Benchmarks/Benchmark_" + timestamp + ".csv";
    }

    public void LogEpisode(bool success, int steps, int visitedCells, int totalRivisted, int totalCell, float cumulativeReward, int numberOfJumps, float RatioRivisitedCells)
    {
        // Se il file non esiste o non ha header, aggiungilo
        if (!headerWritten)
        {
            File.WriteAllText(filePath, "Success;Steps;CellsVisitedUnique;RivisitedCells;TotalCells;CumulativeReward;numberOfJumps;RatioRivisitedCells\n");
            headerWritten = true;
        }

        string line = $"{(success ? 1 : 0)};{steps};{visitedCells};{totalRivisted};{totalCell};{cumulativeReward};{numberOfJumps};{RatioRivisitedCells}";
        File.AppendAllText(filePath, line + "\n");
    }

    void OnApplicationQuit()
    {
        //FinalizeLogging();
    }


    private bool finalized = false;

    public void FinalizeLogging()
    {
        if (finalized) return; // evita duplicazioni
        finalized = true;

        if (File.Exists(filePath))
        {
            List<string> lines = File.ReadAllLines(filePath).ToList();
            CalculateAndSaveStatistics(filePath, lines);
        }
        else
        {
            Debug.LogWarning("File non trovato per il calcolo delle statistiche.");
        }
    }


    private void CalculateAndSaveStatistics(string filePath, List<string> episodeLines)
    {
        int successCount = 0;
        int totalEpisodes = 0;
        List<float> allReward = new List<float>();
        List<int> allSteps = new List<int>();
        List<int> allCells = new List<int>();
        List<int> stepsOnSuccess = new List<int>();
        List<int> cellsOnSuccess = new List<int>();

        foreach (string line in episodeLines.Skip(1)) // salta intestazione
        {
            string[] parts = line.Split(';');
            if (parts.Length < 5) continue;

            int success = int.Parse(parts[0]);
            int steps = int.Parse(parts[1]);
            int cellsUnique = int.Parse(parts[2]);
            int totalRivisted = int.Parse(parts[3]);
            int totalCell = int.Parse(parts[4]);
            float cumulativeReward = float.Parse(parts[5]);

            totalEpisodes++;
            allSteps.Add(steps);
            allCells.Add(totalCell);
            allReward.Add(cumulativeReward);

            if (success == 1)
            {
                successCount++;
                stepsOnSuccess.Add(steps);
                cellsOnSuccess.Add(cellsUnique);
            }
        }

        double successRate = (double)successCount / totalEpisodes;
        double avgSteps = allSteps.Average();
        float avgCells = (float)allCells.Average(); 
        double avgStepsSuccess = stepsOnSuccess.Any() ? stepsOnSuccess.Average() : 0;
        double avgCellsSuccess = cellsOnSuccess.Any() ? cellsOnSuccess.Average() : 0;
        double stdSteps = CalculateStdDev(allSteps);
        double stdCellsSuccess = CalculateStdDev(cellsOnSuccess);
        double avgReward = allReward.Average();

        var stats = new List<string>
        {
            "",
            "Statistiche Calcolate:",
            $"Success RATE;{successRate.ToString("0.########")}",
            $"Passi medi;{avgSteps.ToString("0.########")}",
            $"Celle medie visitate;{avgCells.ToString("0.########")}",
            $"Deviazione standard passi;{stdSteps.ToString("0.########")}",
            $"Deviazione standard celle;{stdCellsSuccess.ToString("0.########")}",
            $"Reward medio;{avgReward.ToString("0.########")}"
        };

        File.AppendAllLines(filePath, stats);
    }

    private double CalculateStdDev(List<int> values)
    {
        if (values.Count == 0) return 0;
        double avg = values.Average();
        double sumSq = values.Sum(v => (v - avg) * (v - avg));
        return Math.Sqrt(sumSq / values.Count);
    }
}
