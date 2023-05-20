using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Particle
{
    public Vector3 x, xm1, v;
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
    public float V0t6;
}


public class SoftBody : MonoBehaviour
{
    public static int nSubSteps = 10;
    public ComputeShader shader;

    // Kernel indices
    public static int kiPreSolve;
    public static int kiSolveEdges;
    public static int kiSolveVolumes;
    public static int kiPostSolve;

    int nParticles;
    int nEdges;
    int nTets;
    int nTriangles;

    Vector3[] vertices;
    Particle[] particles;
    LengthConstraint[] lc;
    VolumeConstraint[] vc;

    // Buffers to pass to shader
    ComputeBuffer particleBuffer;
    ComputeBuffer lcBuffer;
    ComputeBuffer vcBuffer;

    // Surface mesh for display
    Mesh mesh;
    MeshFilter mf;


    void Start()
    {
        // Initialise soft body data from tetrahedral mesh
        InitializeDataFromTetMesh();

        // Set int parameters
        shader.SetInt("nParticles", nParticles);
        shader.SetInt("nEdges", nEdges);
        shader.SetInt("nTets", nTets);
        shader.SetInt("nTriangles", nTriangles);

        // Create buffers
        particleBuffer = new ComputeBuffer(nParticles, (3 * 3 + 1) * sizeof(float));
        lcBuffer = new ComputeBuffer(nEdges, 2 * sizeof(uint) + sizeof(float));
        vcBuffer = new ComputeBuffer(nTets, 4 * sizeof(uint) + sizeof(float));

        // Constraint buffers are constant and can already be set
        lcBuffer.SetData(lc);
        vcBuffer.SetData(vc);
        particleBuffer.SetData(particles);

        // Bind buffers
        kiPreSolve = shader.FindKernel("preSolve");
        shader.SetBuffer(kiPreSolve, "particles", particleBuffer);

        kiSolveEdges = shader.FindKernel("solveEdges");
        shader.SetBuffer(kiSolveEdges, "particles", particleBuffer);
        shader.SetBuffer(kiSolveEdges, "lc", lcBuffer);

        kiSolveVolumes = shader.FindKernel("solveVolumes");
        shader.SetBuffer(kiSolveVolumes, "particles", particleBuffer);
        shader.SetBuffer(kiSolveVolumes, "vc", vcBuffer);

        kiPostSolve = shader.FindKernel("postSolve");
        shader.SetBuffer(kiPostSolve, "particles", particleBuffer);
    }

    void Update()
    {
        float sdt = Time.deltaTime / nSubSteps;
        shader.SetFloat("dt", sdt);

        for (int i = 0; i < nSubSteps; i++)
        {
            // Dispatch with one thread per particle/constraint
            shader.Dispatch(kiPreSolve, (nParticles + 127) / 128, 1, 1);
            shader.Dispatch(kiSolveEdges, (nEdges + 127) / 128, 1, 1);
            shader.Dispatch(kiSolveVolumes, (nTets + 127) / 128, 1, 1);
            shader.Dispatch(kiPostSolve, (nParticles + 127) / 128, 1, 1);
        }

        particleBuffer.GetData(particles);
        UpdateMeshVertices();
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
                xm1 = new Vector3(tm.vertices[3 * i], tm.vertices[3 * i + 1], tm.vertices[3 * i + 2]),
                v = Vector3.zero
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
            vc[i].V0t6 = Vector3.Dot(temp[3], temp[2]);

            // Add inv masses to particles
            float pInvMass = vc[i].V0t6 > 0.0f ? 1.0f / (vc[i].V0t6 / (6.0f * 4.0f)) : 0.0f;
            particles[vc[i].p1].invm = pInvMass;
            particles[vc[i].p2].invm = pInvMass;
            particles[vc[i].p3].invm = pInvMass;
            particles[vc[i].p4].invm = pInvMass;
        }
    }

    void InitializeSurfaceMesh(in TetMesh tm)
    {
        mesh = new Mesh();
        mesh.name = "Soft Body Mesh";
        mf = GetComponent<MeshFilter>();
        vertices = new Vector3[nParticles];

        mesh.vertices = new Vector3[nParticles];
        for (int i = 0; i < nParticles; i++)
        {
            mesh.vertices[i] = particles[i].x;
        }

        nTriangles = tm.nTriangles;
        mesh.triangles = tm.surfaceTriangleIndices;
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
}
