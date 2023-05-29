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
        string[] meshtext = Resources.Load<TextAsset>("TetMeshes/" + file).text.Split(new string[] { "\r\n" }, System.StringSplitOptions.None);
        int currentLine = 0;

        nVertices = int.Parse(meshtext[currentLine++]);
        nTets = int.Parse(meshtext[currentLine++]);
        nEdges = int.Parse(meshtext[currentLine++]);
        nTriangles = int.Parse(meshtext[currentLine++]);

        vertices = new float[nVertices * 3];
        for (int i = 0; i < nVertices * 3; i++)
        {
            vertices[i] = float.Parse(meshtext[currentLine++]);
        }

        tetIndices = new int[nTets * 4];
        for (int i = 0; i < nTets * 4; i++)
        {
            tetIndices[i] = int.Parse(meshtext[currentLine++]);
        }

        edgeIndices = new int[nEdges * 2];
        for (int i = 0; i < nEdges * 2; i++)
        {
            edgeIndices[i] = int.Parse(meshtext[currentLine++]);
        }

        surfaceTriangleIndices = new int[nTriangles * 3];
        for (int i = 0; i < nTriangles * 3; i++)
        {
            surfaceTriangleIndices[i] = int.Parse(meshtext[currentLine++]);
        }

    }

    public void LoadTestModel()
    {
        LoadFromFile("Bunny");
    }
}
