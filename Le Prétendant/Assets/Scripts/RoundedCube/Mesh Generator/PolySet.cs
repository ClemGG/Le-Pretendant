using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolySet : HashSet<Polygon>
{
    //Given a set of Polys, calculate the set of Edges
    //that surround them.
    public EdgeSet CreateEdgeSet()
    {
        EdgeSet edgeSet = new EdgeSet();
        foreach (Polygon poly in this)
        {
            foreach (Polygon neighbor in poly.m_Neighbors)
            {
                if (this.Contains(neighbor))
                    continue;

                // If our neighbor isn't in our PolySet, then
                // the edge between us and our neighbor is one
                // of the edges of this PolySet.
                Edge edge = new Edge(poly, neighbor);
                edgeSet.Add(edge);
            }
        }
        return edgeSet;
    }
    // GetUniqueVertices calculates a list of the vertex indices 
    // used by these Polygons with no duplicates.
    public List<int> GetUniqueVertices()
    {
        List<int> verts = new List<int>();
        foreach (Polygon poly in this)
        {
            foreach (int vert in poly.m_Vertices)
            {
                if (!verts.Contains(vert))
                    verts.Add(vert);
            }
        }
        return verts;
    }
}