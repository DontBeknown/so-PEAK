using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Renders statistical time-series data as a line graph.
/// SRP: Only responsible for graph rendering.
/// </summary>
public class StatGraphRenderer : MonoBehaviour
{
    [Header("Graph Settings")]
    [SerializeField] private RectTransform graphContainer;
    [SerializeField] private Color lineColor = Color.green;
    [SerializeField] private float lineWidth = 2f;
    [SerializeField] private int pointsToShow = 50;
    
    [Header("Graph Styling")]
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    [SerializeField] private Color gridColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private int gridLinesX = 5;
    [SerializeField] private int gridLinesY = 5;
    
    private RawImage backgroundImage;
    private List<GameObject> graphPoints;
    private List<GameObject> gridLines;
    
    private void Awake()
    {
        if (graphContainer == null)
        {
            graphContainer = GetComponent<RectTransform>();
        }
        
        graphPoints = new List<GameObject>();
        gridLines = new List<GameObject>();
        
        CreateBackground();
        CreateGrid();
    }
    
    private void CreateBackground()
    {
        GameObject bgObj = new GameObject("GraphBackground");
        bgObj.transform.SetParent(graphContainer, false);
        
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        backgroundImage = bgObj.AddComponent<RawImage>();
        backgroundImage.color = backgroundColor;
    }
    
    private void CreateGrid()
    {
        // Create vertical grid lines
        for (int i = 0; i <= gridLinesX; i++)
        {
            GameObject line = CreateLine($"GridLineX_{i}", gridColor, 1f);
            gridLines.Add(line);
        }
        
        // Create horizontal grid lines
        for (int i = 0; i <= gridLinesY; i++)
        {
            GameObject line = CreateLine($"GridLineY_{i}", gridColor, 1f);
            gridLines.Add(line);
        }
        
        UpdateGridPositions();
    }
    
    private void UpdateGridPositions()
    {
        if (graphContainer == null) return;
        
        float width = graphContainer.rect.width;
        float height = graphContainer.rect.height;
        
        // Update vertical lines
        for (int i = 0; i <= gridLinesX; i++)
        {
            float x = (width / gridLinesX) * i;
            RectTransform lineRect = gridLines[i].GetComponent<RectTransform>();
            lineRect.anchoredPosition = new Vector2(x, height / 2f);
            lineRect.sizeDelta = new Vector2(1f, height);
        }
        
        // Update horizontal lines
        for (int i = 0; i <= gridLinesY; i++)
        {
            float y = (height / gridLinesY) * i;
            RectTransform lineRect = gridLines[gridLinesX + 1 + i].GetComponent<RectTransform>();
            lineRect.anchoredPosition = new Vector2(width / 2f, y);
            lineRect.sizeDelta = new Vector2(width, 1f);
        }
    }
    
    /// <summary>
    /// Renders a graph from time-series data.
    /// </summary>
    public void RenderGraph(List<TimeSeriesDataPoint> data, StatMetricType metricType)
    {
        ClearGraph();
        
        if (data == null || data.Count < 2)
        {
            return;
        }
        
        // Set color based on metric type
        SetColorForMetric(metricType);
        
        // Get the most recent data points
        int startIndex = Mathf.Max(0, data.Count - pointsToShow);
        List<TimeSeriesDataPoint> visibleData = data.GetRange(startIndex, data.Count - startIndex);
        
        // Find min/max for scaling
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;
        
        foreach (var point in visibleData)
        {
            if (point.Value < minValue) minValue = point.Value;
            if (point.Value > maxValue) maxValue = point.Value;
        }
        
        // Add padding to the range
        float valueRange = maxValue - minValue;
        if (valueRange < 0.01f) valueRange = 1f; // Avoid division by zero
        
        minValue -= valueRange * 0.1f;
        maxValue += valueRange * 0.1f;
        valueRange = maxValue - minValue;
        
        // Draw line graph
        DrawLineGraph(visibleData, minValue, maxValue, valueRange);
    }
    
    private void DrawLineGraph(List<TimeSeriesDataPoint> data, float minValue, float maxValue, float valueRange)
    {
        if (graphContainer == null) return;
        
        float width = graphContainer.rect.width;
        float height = graphContainer.rect.height;
        
        for (int i = 0; i < data.Count - 1; i++)
        {
            TimeSeriesDataPoint current = data[i];
            TimeSeriesDataPoint next = data[i + 1];
            
            // Normalize positions
            float xPos = (i / (float)(data.Count - 1)) * width;
            float yPos = ((current.Value - minValue) / valueRange) * height;
            
            float xPosNext = ((i + 1) / (float)(data.Count - 1)) * width;
            float yPosNext = ((next.Value - minValue) / valueRange) * height;
            
            // Create line segment
            GameObject lineSegment = CreateLineSegment(
                new Vector2(xPos, yPos),
                new Vector2(xPosNext, yPosNext),
                lineColor,
                lineWidth
            );
            
            graphPoints.Add(lineSegment);
        }
    }
    
    private GameObject CreateLineSegment(Vector2 start, Vector2 end, Color color, float width)
    {
        GameObject line = new GameObject("LineSegment");
        line.transform.SetParent(graphContainer, false);
        
        RectTransform rect = line.AddComponent<RectTransform>();
        Image img = line.AddComponent<Image>();
        img.color = color;
        
        Vector2 direction = end - start;
        float distance = direction.magnitude;
        
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.sizeDelta = new Vector2(distance, width);
        rect.anchoredPosition = start;
        rect.pivot = new Vector2(0, 0.5f);
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rect.rotation = Quaternion.Euler(0, 0, angle);
        
        return line;
    }
    
    private GameObject CreateLine(string name, Color color, float width)
    {
        GameObject line = new GameObject(name);
        line.transform.SetParent(graphContainer, false);
        
        RectTransform rect = line.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        
        Image img = line.AddComponent<Image>();
        img.color = color;
        
        return line;
    }
    
    /// <summary>
    /// Clears all graph elements.
    /// </summary>
    public void ClearGraph()
    {
        foreach (var point in graphPoints)
        {
            if (point != null)
                Destroy(point);
        }
        graphPoints.Clear();
    }
    
    private void SetColorForMetric(StatMetricType metricType)
    {
        lineColor = metricType switch
        {
            StatMetricType.Distance => new Color(0.3f, 0.8f, 1f), // Cyan
            StatMetricType.Stamina => new Color(1f, 0.8f, 0.2f), // Yellow
            StatMetricType.Fatigue => new Color(1f, 0.5f, 0.2f), // Orange
            StatMetricType.Health => new Color(1f, 0.3f, 0.3f), // Red
            StatMetricType.Consumables => new Color(0.5f, 1f, 0.5f), // Green
            _ => Color.white
        };
    }
    
    private void OnDestroy()
    {
        ClearGraph();
    }
}
