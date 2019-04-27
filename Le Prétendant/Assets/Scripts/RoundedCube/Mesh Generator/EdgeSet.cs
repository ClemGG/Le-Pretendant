using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EdgeSet : HashSet<Edge>
{
    // Split - Given a list of original vertex indices and a list of
    // replacements, update m_InnerVerts to use the new replacement
    // vertices.
    public void Split(List<int> oldVertices, List<int> newVertices)
    {
        foreach (Edge edge in this)
        {
            for (int i = 0; i < 2; i++)
            {
                edge.m_InnerVerts[i] = newVertices[oldVertices.IndexOf(
                                       edge.m_OuterVerts[i])];
            }
        }
    }
    // GetUniqueVertices - Get a list of all the vertices referenced
    // in this edge loop, with no duplicates.
    public List<int> GetUniqueVertices()
    {
        List<int> vertices = new List<int>();
        foreach (Edge edge in this)
        {
            foreach (int vert in edge.m_OuterVerts)
            {
                if (!vertices.Contains(vert))
                    vertices.Add(vert);
            }
        }
        return vertices;
    }
}