using System.Collections.Generic;
using UnityEngine;
using PathCreation.Utility;
using UnityEditor;

namespace PathCreation.Examples
{
    public class RoundedCubeMeshCreator : PathSceneTool
    {

        #region Variables

        Transform t;


        [Space(10)]
        [Header("Rounded Cube settings")]
        [Space(10)]

        public int résolution;
        public float diamètre;
        public float roundness;
        public bool flattenSurface, useOffset = true;

        [Space(10)]
        [Header("Material settings")]
        [Space(10)]

        public Material[] mats;



        private MeshCollider col;
        private MeshFilter filter;
        private MeshRenderer rend;
        private Vector3[] vertices;
        private Vector3[] normals;
        private Vector3[,] localXYZ;


        [Space(10)]
        [Header("Gizmos : ")]
        [Space(10)]

        [SerializeField] bool showIDs;
        [SerializeField] bool showVertices;
        [SerializeField] bool showNormals;
        [SerializeField] bool showLocalXYZ;
        [SerializeField] float dotSize = 0.15f, localSize = 1f, normalSize = 1f;



        [Space(10)]
        [Header("Polygons : ")]
        [Space(10)]

        static List<Polygon> polygons;

        #endregion


        private void OnValidate()
        {
            /* 
             * Comme j'ai pas pu mettre les côtés à 1 ou la roundness à 0 
             * sans rendre le cube noir ou avoir des messages d'erreur, on va les laisser comme ça
             */

            diamètre = Mathf.Max(diamètre, 1f);

            if (roundness > diamètre / 2f)
                roundness = diamètre / 2f;

            roundness = Mathf.Max(roundness, 0.0001f);
            résolution = Mathf.Max(résolution, 2);
            localSize = diamètre + diamètre / 2f;


            //if (roundness > Mathf.Max(diamètre, 0.0001f))
            //    roundness = Mathf.Max(diamètre, 0.0001f);


            //print(path.NumVertices);
        }



        protected override void PathUpdated()
        {
            if (pathCreator != null)
            {
                t = pathCreator.transform;

                AssignMeshComponents();
                AssignMaterials();
                filter.mesh = CreateRoadMesh();
                filter.sharedMesh.name = "Procedural Path";
                UpdateCollider();
            }
        }



        #region Création du mesh



        Mesh CreateRoadMesh()
        {
            int cornerVertices = 8;
            int edgeVertices = (résolution + résolution + path.NumVertices - 3) * 4;
            int faceVertices = (
                (résolution - 1) * (résolution - 1) +
                (résolution - 1) * (path.NumVertices - 1) +
                (résolution - 1) * (path.NumVertices - 1)) * 2;

            vertices = new Vector3[cornerVertices + edgeVertices + faceVertices];
            normals = new Vector3[vertices.Length];

            bool usePathNormals = !(path.space == PathSpace.xyz && flattenSurface);
            int v = 0;


            Vector3 versLeHaut = Vector3.zero, versLaDroite = Vector3.zero;
            localXYZ = new Vector3[path.NumVertices, 3];



            float step = 1f / résolution * diamètre;



            for (int z = 0; z <= path.NumVertices; z++)
            {




                if (z < path.NumVertices)
                {
                    versLeHaut = (usePathNormals) ? Vector3.Cross(path.tangents[z], path.normals[z]) : path.up;
                    versLaDroite = (usePathNormals) ? path.normals[z] : Vector3.Cross(versLeHaut, path.tangents[z]);
                    localXYZ[z, 0] = versLaDroite;
                    localXYZ[z, 1] = versLeHaut;
                    localXYZ[z, 2] = path.tangents[z];
                }

                Vector3 pointDuChemin = path.vertices[(z == path.NumVertices) ? z - 1 : z];


                for (int x = 0; x <= résolution; x++)   //BAS
                {
                    AjouterPoint(v++, z, x * step * versLaDroite + pointDuChemin);
                }
                for (int y = 1; y <= résolution; y++)   //DROITE
                {
                    AjouterPoint(v++, z, versLaDroite * diamètre + y * step * versLeHaut + pointDuChemin);
                }
                for (int x = résolution - 1; x >= 0; x--)   //HAUT
                {
                    AjouterPoint(v++, z, x * step * versLaDroite + versLeHaut * diamètre + pointDuChemin);
                }
                for (int y = résolution - 1; y > 0; y--)    // GAUCHE
                {
                    AjouterPoint(v++, z, y * step * versLeHaut + pointDuChemin);
                }
            }





            versLeHaut = (usePathNormals) ? Vector3.Cross(path.tangents[path.NumVertices - 1], path.normals[path.NumVertices - 1]) : path.up;
            versLaDroite = (usePathNormals) ? path.normals[path.NumVertices - 1] : Vector3.Cross(versLeHaut, path.tangents[path.NumVertices - 1]);
            Vector3 dernierPoint = path.vertices[path.NumVertices - 1];

            for (int y = 1; y < résolution; y++)    //FACE DE DERRIERE
            {
                for (int x = 1; x < résolution; x++)
                {
                    AjouterPoint(v++, path.NumVertices - 1, x * step * versLaDroite + y * step * versLeHaut + dernierPoint);
                }
            }



            versLeHaut = (usePathNormals) ? Vector3.Cross(path.tangents[0], path.normals[0]) : path.up;
            versLaDroite = (usePathNormals) ? path.normals[0] : Vector3.Cross(versLeHaut, path.tangents[0]);
            Vector3 premierPoint = path.vertices[0];

            for (int y = 1; y < résolution; y++)    //FACE DE DEVANT
            {
                for (int x = 1; x < résolution; x++)
                {
                    AjouterPoint(v++, 0, x * step * versLaDroite + y * step * versLeHaut + premierPoint);
                }
            }











            int[] trianglesX = new int[(résolution * path.NumVertices) * 12];
            int[] trianglesY = new int[(résolution * path.NumVertices) * 12];
            int[] trianglesZ = new int[(résolution * résolution) * 12];
            int ring = résolution * 4;
            int tZ = 0, tX = 0, tY = 0, i = 0;

            polygons = new List<Polygon>();


            for (int z = 0; z < path.NumVertices; z++, i++)
            {
                for (int x = 0; x < résolution; x++, i++)
                {
                    tY = SetQuad(trianglesY, tY, i, i + 1, i + ring, i + ring + 1);
                }
                for (int y = 0; y < résolution; y++, i++)
                {
                    tX = SetQuad(trianglesX, tX, i, i + 1, i + ring, i + ring + 1);
                }
                for (int x = 0; x < résolution; x++, i++)
                {
                    tY = SetQuad(trianglesY, tY, i, i + 1, i + ring, i + ring + 1);
                }
                for (int y = 0; y < résolution - 1; y++, i++)
                {
                    tX = SetQuad(trianglesX, tX, i, i + 1, i + ring, i + ring + 1);
                }
                tX = SetQuad(trianglesX, tX, i, i - ring + 1, i + ring, i + 1);
            }

            tZ = CreateFrontFace(trianglesZ, tZ, ring);
            tZ = CreateBackFace(trianglesZ, tZ, ring);


            Mesh mesh = new Mesh
            {
                vertices = vertices,
                normals = normals,
                subMeshCount = 3
            };

            mesh.SetTriangles(trianglesX, 0);
            mesh.SetTriangles(trianglesY, 1);
            mesh.SetTriangles(trianglesZ, 2);


            return mesh;
        }








        private void AjouterPoint(int i, int pathIndex, Vector3 newPos)
        {
            pathIndex = (pathIndex == path.NumVertices) ? pathIndex - 1 : pathIndex;
            Vector3 inner = vertices[i] = newPos;
            Vector3 offset = Vector3.zero;


            if (path.isClosedLoop)
            {
                offset = localXYZ[pathIndex, 0] * diamètre / 2f +
                                 localXYZ[pathIndex, 1] * diamètre / 2f;
            }
            else
            {
                if (pathIndex == 0)
                {
                    offset = localXYZ[pathIndex, 0] * diamètre / 2f +
                                     localXYZ[pathIndex, 1] * diamètre / 2f +
                                     localXYZ[0, 2] * diamètre / 8f;
                }
                else if (pathIndex == path.NumVertices - 1)
                {
                    offset = localXYZ[pathIndex, 0] * diamètre / 2f +
                                     localXYZ[pathIndex, 1] * diamètre / 2f -
                                     localXYZ[path.NumVertices - 1, 2] * diamètre / 8f;
                }
                else
                {
                    offset = localXYZ[pathIndex, 0] * diamètre / 2f +
                                     localXYZ[pathIndex, 1] * diamètre / 2f;
                }
            }

            Matrix4x4 m = Matrix4x4.Translate(path.vertices[pathIndex]);
            inner = m.MultiplyPoint(offset);


            normals[i] = (vertices[i] - inner).normalized;
            vertices[i] = inner + normals[i] * diamètre;

            if (useOffset)
            {
                vertices[i] -= offset;
            }

        }









        private int CreateBackFace(int[] triangles, int t, int ring)
        {
            int v = 1;
            int vMid = vertices.Length - (résolution - 1) * (résolution - 1);
            t = SetQuad(triangles, t, ring - 1, vMid, 0, 1);



            for (int x = 1; x < résolution - 1; x++, v++, vMid++)
            {
                t = SetQuad(triangles, t, vMid, vMid + 1, v, v + 1);
            }
            t = SetQuad(triangles, t, vMid, v + 2, v, v + 1);



            int vMin = ring - 2;
            vMid -= résolution - 2;
            int vMax = v + 2;



            for (int y = 1; y < résolution - 1; y++, vMin--, vMid++, vMax++)
            {
                t = SetQuad(triangles, t, vMin, vMid + résolution - 1, vMin + 1, vMid);
                for (int x = 1; x < résolution - 1; x++, vMid++)
                {
                    t = SetQuad(
                        triangles, t,
                        vMid + résolution - 1, vMid + résolution, vMid, vMid + 1);
                }
                t = SetQuad(triangles, t, vMid + résolution - 1, vMax + 1, vMid, vMax);
            }



            int vTop = vMin - 1;
            t = SetQuad(triangles, t, vTop + 1, vTop, vTop + 2, vMid);
            for (int x = 1; x < résolution - 1; x++, vTop--, vMid++)
            {
                t = SetQuad(triangles, t, vTop, vTop - 1, vMid, vMid + 1);
            }
            t = SetQuad(triangles, t, vTop, vTop - 1, vMid, vTop - 2);


            return t;
        }


        private int CreateFrontFace(int[] triangles, int t, int ring)
        {
            int v = ring * path.NumVertices;
            for (int x = 0; x < résolution - 1; x++, v++)
            {
                t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + ring);
            }
            t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + 2);




            int vMin = ring * (path.NumVertices + 1) - 1;
            int vMid = vMin + 1;
            int vMax = v + 2;



            for (int y = 1; y < résolution - 1; y++, vMin--, vMid++, vMax++)
            {
                t = SetQuad(triangles, t, vMin, vMid, vMin - 1, vMid + résolution - 1);
                for (int x = 1; x < résolution - 1; x++, vMid++)
                {
                    t = SetQuad(
                        triangles, t,
                        vMid, vMid + 1, vMid + résolution - 1, vMid + résolution);
                }
                t = SetQuad(triangles, t, vMid, vMax, vMid + résolution - 1, vMax + 1);
            }



            int vTop = vMin - 2;
            t = SetQuad(triangles, t, vMin, vMid, vTop + 1, vTop);
            for (int x = 1; x < résolution - 1; x++, vTop--, vMid++)
            {
                t = SetQuad(triangles, t, vMid, vMid + 1, vTop, vTop - 1);
            }
            t = SetQuad(triangles, t, vMid, vTop - 2, vTop, vTop - 1);



            return t;
        }



        private static int
        SetQuad(int[] triangles, int i, int v00, int v10, int v01, int v11)
        {
            triangles[i] = v11;
            triangles[i + 1] = triangles[i + 4] = v01;
            triangles[i + 2] = triangles[i + 3] = v10;
            triangles[i + 5] = v00;

            polygons.Add(new Polygon(v11, v01, v10, v00));

            return i + 6;
        }

        #endregion










        private void OnDrawGizmos()
        {
            if (vertices == null || !t)
            {
                return;
            }


            //Matrix4x4 newGizmosMatrix = t.localToWorldMatrix;
            //Matrix4x4 oldGizmosMatrix = Gizmos.matrix;
            //Gizmos.matrix = newGizmosMatrix;

            for (int i = 0; i < vertices.Length; i++)
            {

                if (showVertices)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawCube(vertices[i], Vector3.one * dotSize);
                    Gizmos.color = Color.blue;
                    Gizmos.DrawCube(vertices[0], Vector3.one * dotSize);
                    Gizmos.color = Color.green;
                    Gizmos.DrawCube(vertices[vertices.Length - 1], Vector3.one * dotSize);
                }
                if (showIDs)
                {

                    Matrix4x4 newHandleMatrix = t.localToWorldMatrix;
                    Matrix4x4 oldHandleMatrix = Handles.matrix;
                    Handles.matrix = newHandleMatrix;


                    //To draw text as Gizmo. Unfortunately, I can't change the color.
                    Handles.color = Color.black;
                    Handles.Label(vertices[i], i.ToString());


                    Handles.matrix = oldHandleMatrix;
                }
                if (showNormals)
                {
                    Gizmos.color = new Color(1f, 1f, 1f, .3f);
                    Gizmos.DrawRay(vertices[i], normals[i] * normalSize);
                }
            }
            if (showLocalXYZ && localXYZ != null)
            {
                for (int i = 0; i < path.NumVertices; i++)
                {

                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(path.vertices[i], localXYZ[i, 0] * localSize);
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(path.vertices[i], localXYZ[i, 1] * localSize);
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(path.vertices[i], localXYZ[i, 2] * localSize);
                }
            }
            
            //Gizmos.matrix = oldGizmosMatrix;

        }


        #region Components


        // Add MeshRenderer and MeshFilter components to this gameobject if not already attached
        void AssignMeshComponents()
        {
            // Find/creator mesh holder object in children
            string meshHolderName = "Procedural Path";
            Transform meshHolder = transform.Find(meshHolderName);
            if (meshHolder == null)
            {
                meshHolder = new GameObject(meshHolderName).transform;
                meshHolder.transform.parent = t;
                meshHolder.transform.localPosition = Vector3.zero;
            }

            meshHolder.transform.rotation = Quaternion.identity;

            // Ensure mesh renderer and filter components are assigned
            if (!meshHolder.gameObject.GetComponent<MeshFilter>())
            {
                meshHolder.gameObject.AddComponent<MeshFilter>();
            }
            if (!meshHolder.GetComponent<MeshRenderer>())
            {
                meshHolder.gameObject.AddComponent<MeshRenderer>();
            }
            if (!meshHolder.GetComponent<MeshCollider>())
            {
                meshHolder.gameObject.AddComponent<MeshCollider>();
            }

            filter = meshHolder.GetComponent<MeshFilter>();
            rend = meshHolder.GetComponent<MeshRenderer>();
            col = meshHolder.GetComponent<MeshCollider>();
        }

        void AssignMaterials()
        {
            if (mats != null)
                rend.materials = mats;
        }


        private void UpdateCollider()
        {
            if (col != null)
                col.sharedMesh = filter.sharedMesh;
        }


        #endregion




        #region Gestion des polygones


        public void CalculateNeighbors()
        {
            foreach (Polygon poly in polygons)
            {
                foreach (Polygon other_poly in polygons)
                {
                    if (poly == other_poly)
                        continue;

                    if (poly.IsNeighborOf(other_poly))
                        poly.m_Neighbors.Add(other_poly);
                }
            }
        }


        #endregion

    }

}