using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using QuikGraph;
using QuikGraph.Algorithms.VertexColoring;

public struct Particle
{
    public Vector3 x, xm1;
    public float invm;
}

struct LengthConstraint
{
    public int p1, p2;
    public float l0;
}

struct VolumeConstraint
{
    public int p1, p2, p3, p4;
    public float V0;
}


public class SoftBody : MonoBehaviour
{
    public int nSubSteps = 10;
    public float edgeCompliance = 1;
    public float volumeCompliance = 0;
    public float gravity = 9.81f;
    public ComputeShader shader;

    // Kernel indices
    static int kiIntegrate;
    static int kiSolveEdges;
    static int kiSolveVolumes;

    // Particles and  constraints
    int nParticles;
    int nEdges;
    int nTets;
    int nTriangles;

    Vector3[] vertices;
    Particle[] particles;
    LengthConstraint[] lc;
    VolumeConstraint[] vc;

    // Constraint cluster variables
    int nEdgeClusters;
    int[] edgesInCluster;
    int[] edgeClusters;

    int nTetClusters;
    int[] tetsInCluster;
    int[] tetClusters;

    // Buffers to pass to shader
    ComputeBuffer particleBuffer;
    ComputeBuffer lcBuffer;
    ComputeBuffer vcBuffer;
    ComputeBuffer eicBuffer;
    ComputeBuffer ecsBuffer;
    ComputeBuffer ticBuffer;
    ComputeBuffer tcsBuffer;

    // Surface mesh for display
    Mesh mesh;
    MeshFilter mf;


    void Start()
    {
        // Initialise soft body data from tetrahedral mesh
        InitializeDataFromTetMesh();

        // Set sim parameters
        shader.SetFloat("edgeCompliance", edgeCompliance);
        shader.SetFloat("volumeCompliance", volumeCompliance);
        shader.SetFloats("gravity", new float[] { 0, -gravity, 0 });
        shader.SetInt("nParticles", nParticles);
        shader.SetInt("nEdges", nEdges);
        shader.SetInt("nTets", nTets);
        shader.SetInt("nTriangles", nTriangles);
        shader.SetInt("nEdgeClusters", nEdgeClusters);
        shader.SetInt("nTetClusters", nTetClusters);

        // Create buffers
        particleBuffer = new ComputeBuffer(nParticles, (2 * 3 + 1) * sizeof(float));
        lcBuffer = new ComputeBuffer(nEdges, 2 * sizeof(int) + sizeof(float));
        vcBuffer = new ComputeBuffer(nTets, 4 * sizeof(int) + sizeof(float));
        eicBuffer = new ComputeBuffer(nEdgeClusters, sizeof(int));
        ecsBuffer = new ComputeBuffer(nEdges, sizeof(int));
        ticBuffer = new ComputeBuffer(nTetClusters, sizeof(int));
        tcsBuffer = new ComputeBuffer(nTets, sizeof(int));

        // Set buffer data
        particleBuffer.SetData(particles);
        lcBuffer.SetData(lc);
        vcBuffer.SetData(vc);
        eicBuffer.SetData(edgesInCluster);
        ecsBuffer.SetData(edgeClusters);
        ticBuffer.SetData(tetsInCluster);
        tcsBuffer.SetData(tetClusters);

        // Bind buffers
        kiIntegrate = shader.FindKernel("integrate");
        shader.SetBuffer(kiIntegrate, "ps", particleBuffer);

        kiSolveEdges = shader.FindKernel("solveEdges");
        shader.SetBuffer(kiSolveEdges, "ps", particleBuffer);
        shader.SetBuffer(kiSolveEdges, "lc", lcBuffer);
        shader.SetBuffer(kiSolveEdges, "edgesInCluster", eicBuffer);
        shader.SetBuffer(kiSolveEdges, "edgeClusters", ecsBuffer);

        kiSolveVolumes = shader.FindKernel("solveVolumes");
        shader.SetBuffer(kiSolveVolumes, "ps", particleBuffer);
        shader.SetBuffer(kiSolveVolumes, "vc", vcBuffer);
        shader.SetBuffer(kiSolveVolumes, "tetsInCluster", ticBuffer);
        shader.SetBuffer(kiSolveVolumes, "tetClusters", tcsBuffer);
    }

    void FixedUpdate()
    {
        float sdt = Time.fixedDeltaTime / nSubSteps;
        shader.SetFloat("dt", sdt);
        shader.SetFloat("edgeCompliance", edgeCompliance);
        shader.SetFloat("volumeCompliance", volumeCompliance);

        for (int i = 0; i < nSubSteps; i++)
        {
            // Dispatch with one thread per particle/cluster
            shader.Dispatch(kiIntegrate, (nParticles + 127) / 128, 1, 1);

            for (int j = 0; j < nEdgeClusters; j++)
            {
                shader.SetInt("currentEdgeCluster", j);
                shader.Dispatch(kiSolveEdges, (edgesInCluster[j] + 63) / 64, 1, 1);
            }
            for (int j = 0; j < nTetClusters; j++)
            {
                shader.SetInt("currentTetCluster", j);
                shader.Dispatch(kiSolveVolumes, (tetsInCluster[j] + 63) / 64, 1, 1);
            }
            shader.SetFloat("dtm1", sdt);
        }
        particleBuffer.GetData(particles);
        UpdateMeshVertices();
    }

    void OnDestroy()
    {
        particleBuffer.Release();
        lcBuffer.Release();
        vcBuffer.Release();
        eicBuffer.Release();
        ecsBuffer.Release();
        ticBuffer.Release();
        tcsBuffer.Release();
    }

    void InitializeDataFromTetMesh()
    {
        TetMesh tm = new TetMesh();
        tm.LoadTestModel();

        InitializeParticles(tm);
        InitializeLengthConstraints(tm);
        InitializeVolumeConstraints(tm);
        InitializeSurfaceMesh(tm);
    }

    void InitializeParticles(in TetMesh tm)
    {
        nParticles = tm.nVertices;
        particles = new Particle[tm.nVertices];
        for (int i = 0; i < nParticles; i++)
        {
            // Set particle positions according to tet mesh
            particles[i] = new Particle
            {
                x = new Vector3(tm.vertices[3 * i], tm.vertices[3 * i + 1], tm.vertices[3 * i + 2]),
                xm1 = new Vector3(tm.vertices[3 * i], tm.vertices[3 * i + 1], tm.vertices[3 * i + 2])
            };
        }
    }

    void InitializeLengthConstraints(in TetMesh tm)
    {
        nEdges = tm.nEdges;
        lc = new LengthConstraint[nEdges];
        for (int i = 0; i < nEdges; i++)
        {
            // Assign edge points
            lc[i] = new LengthConstraint
            {
                p1 = tm.edgeIndices[2 * i],
                p2 = tm.edgeIndices[2 * i + 1],
            };

            // Calculate edge length
            Vector3 e1 = new Vector3(tm.vertices[3 * lc[i].p1], tm.vertices[3 * lc[i].p1 + 1], tm.vertices[3 * lc[i].p1 + 2]);
            Vector3 e2 = new Vector3(tm.vertices[3 * lc[i].p2], tm.vertices[3 * lc[i].p2 + 1], tm.vertices[3 * lc[i].p2 + 2]);
            lc[i].l0 = (e2 - e1).magnitude;
        }

        CreateEdgeClusters();
    }

    void InitializeVolumeConstraints(in TetMesh tm)
    {
        nTets = tm.nTets;
        vc = new VolumeConstraint[nTets];
        for (int i = 0; i < nTets; i++)
        {
            // Assign tet points
            vc[i] = new VolumeConstraint
            {
                p1 = tm.tetIndices[4 * i],
                p2 = tm.tetIndices[4 * i + 1],
                p3 = tm.tetIndices[4 * i + 2],
                p4 = tm.tetIndices[4 * i + 3],
            };

            // Calculate volume
            Vector3 e1 = new Vector3(tm.vertices[3 * vc[i].p1], tm.vertices[3 * vc[i].p1 + 1], tm.vertices[3 * vc[i].p1 + 2]);
            Vector3 e2 = new Vector3(tm.vertices[3 * vc[i].p2], tm.vertices[3 * vc[i].p2 + 1], tm.vertices[3 * vc[i].p2 + 2]);
            Vector3 e3 = new Vector3(tm.vertices[3 * vc[i].p3], tm.vertices[3 * vc[i].p3 + 1], tm.vertices[3 * vc[i].p3 + 2]);
            Vector3 e4 = new Vector3(tm.vertices[3 * vc[i].p4], tm.vertices[3 * vc[i].p4 + 1], tm.vertices[3 * vc[i].p4 + 2]);

            Vector3[] temp = new Vector3[4];
            temp[0] = e2 - e1;
            temp[1] = e3 - e1;
            temp[2] = e4 - e1;
            temp[3] = Vector3.Cross(temp[0], temp[1]);
            vc[i].V0 = Vector3.Dot(temp[3], temp[2]) / 6;

            // Add inv masses to particles
            float pInvMass = vc[i].V0 > 0.0f ? 1.0f / (vc[i].V0 / 4.0f) : 0.0f;
            particles[vc[i].p1].invm += pInvMass;
            particles[vc[i].p2].invm += pInvMass;
            particles[vc[i].p3].invm += pInvMass;
            particles[vc[i].p4].invm += pInvMass;
        }

        CreateTetClusters();
    }

    void InitializeSurfaceMesh(in TetMesh tm)
    {
        nTriangles = tm.nTriangles;
        vertices = new Vector3[nParticles];
        for (int i = 0; i < nParticles; i++)
        {
            vertices[i] = particles[i].x;
        }

        mesh = new Mesh
        {
            name = "Soft Body Mesh",
            vertices = vertices,
            triangles = tm.surfaceTriangleIndices
        };

        mf = GetComponent<MeshFilter>();
        mf.mesh = mesh;
    }

    void UpdateMeshVertices()
    {
        for (int i = 0; i < nParticles; i++)
        {
            vertices[i] = particles[i].x;
        }
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }

    void CreateEdgeClusters()
    {
        var edgeGraph = new UndirectedGraph<int, UndirectedEdge<int>>();

        // Add vertex for each constraint
        for (int i = 0; i < nEdges; i++)
        {
            edgeGraph.AddVertex(i);
        }

        // Add edge between each constraint that shares particle
        for (int i = 0; i < nEdges - 1; i++)
        {
            for (int j = i + 1; j < nEdges; j++)
            {
                int[] c1 = { lc[i].p1, lc[i].p2 };
                int[] c2 = { lc[j].p1, lc[j].p2 };
                if (c1.Intersect(c2).Any())
                {
                    edgeGraph.AddEdge(new UndirectedEdge<int>(i, j));
                }
            }
        }

        // Compute vertex coloring
        var edgeColoring = new VertexColoringAlgorithm<int, UndirectedEdge<int>>(edgeGraph);
        edgeColoring.Compute();

        // Set up the clusters
        nEdgeClusters = edgeColoring.Colors.Values.Distinct().Count();
        edgesInCluster = new int[nEdgeClusters];
        foreach (int color in edgeColoring.Colors.Values)
        {
            edgesInCluster[color]++;
        }
        var sortedColoring = from entry in edgeColoring.Colors orderby entry.Value ascending select entry.Key;
        edgeClusters = sortedColoring.ToArray();
    }

    void CreateTetClusters()
    {
        var tetGraph = new UndirectedGraph<int, UndirectedEdge<int>>();

        // Add vertex for each constraint
        for (int i = 0; i < nTets; i++)
        {
            tetGraph.AddVertex(i);
        }

        // Add edge between each constraint that shares particle
        for (int i = 0; i < nTets - 1; i++)
        {
            for (int j = i + 1; j < nTets; j++)
            {
                int[] c1 = { vc[i].p1, vc[i].p2, vc[i].p3, vc[i].p4 };
                int[] c2 = { vc[j].p1, vc[j].p2, vc[j].p3, vc[j].p4 };
                if (c1.Intersect(c2).Any())
                {
                    tetGraph.AddEdge(new UndirectedEdge<int>(i, j));
                }
            }
        }

        // Compute vertex coloring
        var tetColoring = new VertexColoringAlgorithm<int, UndirectedEdge<int>>(tetGraph);
        tetColoring.Compute();

        // Set up the clusters
        nTetClusters = tetColoring.Colors.Values.Distinct().Count();
        tetsInCluster = new int[nTetClusters];
        foreach (int color in tetColoring.Colors.Values)
        {
            tetsInCluster[color]++;
        }
        var sortedColoring = from entry in tetColoring.Colors orderby entry.Value ascending select entry.Key;
        tetClusters = sortedColoring.ToArray();
    }
}
