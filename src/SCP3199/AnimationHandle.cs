using System.Collections.Generic;
using UnityEngine;

namespace SCP3199.SCP3199;

public partial class SCP3199AI : ModEnemyAI<SCP3199AI>
{
    private List<Transform> usedNodes = new List<Transform>();
    
    public Transform GetNode(Vector3 currentPosition)
    {
        var nodes = RoundManager.Instance.outsideAINodes;
        
        if (nodes.Length < 3)
        {
            Debug.LogError("Not enough nodes to choose from.");
            return null;
        }

        // List to hold nodes and their distances
        List<(Transform node, float distance)> nodesWithDistances = new List<(Transform, float)>();

        // Calculate distances from the current position to each node
        foreach (var node in nodes)
        {
            if (!usedNodes.Contains(node.transform))
            {
                float distance = Vector3.Distance(currentPosition, node.transform.position);
                nodesWithDistances.Add((node.transform, distance));
            }
        }

        // Sort nodes based on distance
        nodesWithDistances.Sort((a, b) => a.distance.CompareTo(b.distance));

        if (nodesWithDistances.Count < 3)
        {
            Debug.LogError("Not enough unused nodes to choose from.");
            return null;
        }

        // Select a node that is not the closest nor the farthest
        int middleIndex = nodesWithDistances.Count / 2;
        Transform selectedNode = nodesWithDistances[middleIndex].node;

        // Add the selected node to the usedNodes list
        usedNodes.Add(selectedNode);

        return selectedNode;
    }
}