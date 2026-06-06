using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HJBPathBenchmarkResult
{
    public HJBAlgorithmBenchmarkResult hjb = new HJBAlgorithmBenchmarkResult("HJB");
    public HJBAlgorithmBenchmarkResult aStar = new HJBAlgorithmBenchmarkResult("A*");
}

[Serializable]
public class HJBAlgorithmBenchmarkResult
{
    public string algorithm;
    public double calculationMilliseconds = -1d;
    public string calculationTimeDisplay = "--:--.---";
    public float travelSeconds = -1f;
    public string travelTimeDisplay = "--:--.---";
    public float plannedPathDistance = -1f;
    public float actualTravelDistance = -1f;
    public float staminaUsed = -1f;
    public HJBBenchmarkCompletionStatus status = HJBBenchmarkCompletionStatus.NotRun;
    public int waypointCount;

    public HJBAlgorithmBenchmarkResult()
    {
    }

    public HJBAlgorithmBenchmarkResult(string algorithm)
    {
        this.algorithm = algorithm;
    }
}

public enum HJBBenchmarkCompletionStatus
{
    NotRun,
    Completed,
    NoPath,
    Stuck,
    Timeout
}

public class HJBBenchmarkPathCalculationResult
{
    public List<Vector3> HjbPath;
    public List<Vector3> AStarPath;
    public double HjbSolveMilliseconds = -1d;
    public double HjbBacktrackMilliseconds = -1d;
    public double HjbTotalMilliseconds = -1d;
    public double AStarMilliseconds = -1d;
}
