using UnityEngine;
using System.Collections.Generic;
using Pathfinding;

/// <summary>
/// Dijkstra path (no heuristic) implemented as a Path subclass.
/// Use: var p = TestPath.Construct(startPos, endPos, callback); AstarPath.StartPath(p);
/// Reuses PathHandler, PathNode and heap from the A* package.
/// </summary>
namespace Pathfinding
{
    public class TestPath : Path
    {
        // Public fields similar to ABPath for convenience
        public GraphNode startNode;
        public GraphNode endNode;

        public Vector3 originalStartPoint;
        public Vector3 originalEndPoint;

        public Vector3 startPoint;
        public Vector3 endPoint;

        public Int3 startIntPoint;

        // If true, return partial path if target unreachable
        public bool calculatePartial = false;
        protected PathNode partialBestTarget;

        /// <summary>Construct a TestPath (uses pooling)</summary>
        public static TestPath Construct(Vector3 start, Vector3 end, OnPathDelegate callback = null)
        {
            var p = PathPool.GetPath<TestPath>();
            p.Setup(start, end, callback);
            return p;
        }

        protected void Setup(Vector3 start, Vector3 end, OnPathDelegate callbackDelegate)
        {
            callback = callbackDelegate;
            UpdateStartEnd(start, end);
        }

        protected void UpdateStartEnd(Vector3 start, Vector3 end)
        {
            originalStartPoint = start;
            originalEndPoint = end;

            startPoint = start;
            endPoint = end;

            startIntPoint = (Int3)start;
            hTarget = (Int3)end;
        }

        protected override void Reset()
        {
            base.Reset();

            startNode = null;
            endNode = null;
            originalStartPoint = Vector3.zero;
            originalEndPoint = Vector3.zero;
            startPoint = Vector3.zero;
            endPoint = Vector3.zero;
            calculatePartial = false;
            partialBestTarget = null;
            startIntPoint = new Int3();
            hTarget = new Int3();
            hTargetNode = null;
        }

        /// <summary>Find nearest nodes and basic validation</summary>
        protected override void Prepare()
        {
            // Initialize NNConstraint
            nnConstraint.tags = enabledTags;

            var startNNInfo = AstarPath.active.GetNearest(startPoint, nnConstraint);
            startPoint = startNNInfo.position;
            startIntPoint = (Int3)startPoint;
            startNode = startNNInfo.node;

            if (startNode == null)
            {
                FailWithError("Couldn't find a node close to the start point");
                return;
            }

            if (!CanTraverse(startNode))
            {
                FailWithError("The node closest to the start point could not be traversed");
                return;
            }

            var endNNInfo = AstarPath.active.GetNearest(endPoint, nnConstraint);
            endPoint = endNNInfo.position;
            endNode = endNNInfo.node;

            if (endNode == null)
            {
                FailWithError("Couldn't find a node close to the end point");
                return;
            }

            if (!CanTraverse(endNode))
            {
                FailWithError("The node closest to the end point could not be traversed");
                return;
            }

            // For compatibility with other systems
            hTarget = (Int3)endPoint;
            hTargetNode = endNode;

            // Mark the target node (some graphs use flag1 to detect targets)
            pathHandler.GetPathNode(endNode).flag1 = true;
        }

        /// <summary>Initialize open list and start node</summary>
        protected override void Initialize()
        {
            // Mark nodes to enable special connection costs for start and end nodes
            if (startNode != null) pathHandler.GetPathNode(startNode).flag2 = true;
            if (endNode != null) pathHandler.GetPathNode(endNode).flag2 = true;

            // Zero out the properties on the start node
            PathNode startRNode = pathHandler.GetPathNode(startNode);
            startRNode.node = startNode;
            startRNode.pathID = pathHandler.PathID;
            startRNode.parent = null;
            startRNode.cost = 0;
            // Dijkstra: no heuristic (H = 0)
            startRNode.G = GetTraversalCost(startNode);
            startRNode.H = 0;

            // If start is the target, complete immediately
            if (pathHandler.GetPathNode(startNode).flag1)
            {
                CompleteWith(startNode);
                Trace(pathHandler.GetPathNode(startNode));
                return;
            }

            // Open start node (this will push its neighbours onto the heap)
            startNode.Open(this, startRNode, pathHandler);
            searchedNodes++;

            partialBestTarget = startRNode;

            if (pathHandler.heap.isEmpty)
            {
                if (calculatePartial)
                {
                    CompletePartial(partialBestTarget);
                }
                else
                {
                    FailWithError("The start node either had no neighbours, or no neighbours that the path could traverse");
                }
                return;
            }

            // Pop first node off the open list
            currentR = pathHandler.heap.Remove();
        }

        protected void CompletePartial(PathNode node)
        {
            CompleteState = PathCompleteState.Partial;
            endNode = node.node;
            endPoint = endNode.ClosestPointOnNode(originalEndPoint);
            Trace(node);
        }

        void CompleteWith(GraphNode node)
        {
            // No special grid handling here to keep implementation simple
            CompleteState = PathCompleteState.Complete;
        }

        /// <summary>Core Dijkstra loop (no heuristic)</summary>
        protected override void CalculateStep(long targetTick)
        {
            int counter = 0;

            while (CompleteState == PathCompleteState.NotCalculated)
            {
                searchedNodes++;

                // If current node is the target, finish
                if (currentR.node == endNode || currentR.flag1)
                {
                    CompleteWith(currentR.node);
                    break;
                }

                // Track partial best by lowest G (cost)
                if (partialBestTarget == null || currentR.G < partialBestTarget.G)
                {
                    partialBestTarget = currentR;
                }

                // Expand neighbors (node.Open will compute G and push to heap)
                currentR.node.Open(this, currentR, pathHandler);

                // Any nodes left to search?
                if (pathHandler.heap.isEmpty)
                {
                    if (calculatePartial && partialBestTarget != null)
                    {
                        CompletePartial(partialBestTarget);
                    }
                    else
                    {
                        FailWithError("Searched all reachable nodes, but could not find target.");
                    }
                    return;
                }

                // Select next node with lowest F (here F == G since H == 0)
                currentR = pathHandler.heap.Remove();

                // Periodically check time budget
                if (counter > 500)
                {
                    if (System.DateTime.UtcNow.Ticks >= targetTick)
                    {
                        // Time's up for this step; continue next tick
                        return;
                    }
                    counter = 0;

                    if (searchedNodes > 1000000)
                    {
                        throw new System.Exception("Probable infinite loop. Over 1,000,000 nodes searched");
                    }
                }
                counter++;
            }

            // If path completed, reconstruct it
            if (CompleteState == PathCompleteState.Complete)
            {
                Trace(currentR);
            }
            else if (calculatePartial && partialBestTarget != null)
            {
                CompletePartial(partialBestTarget);
            }
        }

        protected override void Cleanup()
        {
            // Unset flags on start/end nodes to avoid leaking state to other path calculations
            if (startNode != null)
            {
                var pn = pathHandler.GetPathNode(startNode);
                pn.flag1 = false;
                pn.flag2 = false;
            }
            if (endNode != null)
            {
                var pn = pathHandler.GetPathNode(endNode);
                pn.flag1 = false;
                pn.flag2 = false;
            }
        }
    }
}