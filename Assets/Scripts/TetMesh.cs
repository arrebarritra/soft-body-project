using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TetMesh
{
    public int nVertices { get; private set; }
    public int nTets { get; private set; }
    public int nEdges { get; private set; }
    public int nTriangles { get; private set; }
    public float[] vertices { get; private set; }
    public int[] tetIndices { get; private set; }
    public int[] edgeIndices { get; private set; }
    public int[] surfaceTriangleIndices { get; private set; }

    public void LoadFromFile(string file)
    {
        using(TextReader reader = File.OpenText(file))
        {
            nVertices = int.Parse(reader.ReadLine());
            nTets = int.Parse(reader.ReadLine());
            nEdges = int.Parse(reader.ReadLine());
            nTriangles = int.Parse(reader.ReadLine());

            vertices = new float[nVertices * 3];
            for(int  i = 0; i < nVertices * 3; i++)
            {
                vertices[i] = float.Parse(reader.ReadLine());
            }

            tetIndices = new int[nTets * 4];
            for (int i = 0; i < nTets * 4; i++)
            {
                tetIndices[i] = int.Parse(reader.ReadLine());
            }

            edgeIndices = new int[nEdges * 2];
            for (int i = 0; i < nEdges * 2; i++)
            {
                edgeIndices[i] = int.Parse(reader.ReadLine());
            }

            surfaceTriangleIndices = new int[nTriangles * 3];
            for (int i = 0; i < nTriangles * 3; i++)
            {
                surfaceTriangleIndices[i] = int.Parse(reader.ReadLine());
            }
        }
    }

    public void LoadTestModel()
    {
        LoadFromFile("Assets/TetMeshes/Bunny.txt");
    }
}
