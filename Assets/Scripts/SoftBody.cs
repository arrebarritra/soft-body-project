using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Particle
{
    Vector3 x, xm1, v;
}

struct LengthConstraint
{
    uint p1, p2;
    float l0;
}

struct VolumeConstraint
{
    uint p1, p2, p3, p4;
    float V0t6;
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

    Particle[] particles;
    LengthConstraint[] lc;
    VolumeConstraint[] vc;

    // Buffers to pass to shader
    ComputeBuffer particleBuffer;
    ComputeBuffer lcBuffer;
    ComputeBuffer vcBuffer;


    void Start()
    {
        // Initialise soft body data from tetrahedral mesh
        InitializeDataFromTetMesh();

        // Create buffers
        particleBuffer = new ComputeBuffer(nParticles, 3 * 3 * sizeof(float));
        lcBuffer = new ComputeBuffer(nEdges, 2 * sizeof(uint) + sizeof(float));
        vcBuffer = new ComputeBuffer(nTets, 4 * sizeof(uint) + sizeof(float));

        // Constraint buffers are constant and can already be set
        lcBuffer.SetData(lc);
        vcBuffer.SetData(vc);

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
            shader.Dispatch(kiPreSolve, nParticles / 128, 1, 1);
            shader.Dispatch(kiSolveEdges, nEdges / 128, 1, 1);
            shader.Dispatch(kiSolveVolumes, nTets / 128, 1, 1);
            shader.Dispatch(kiPostSolve, nParticles / 128, 1, 1);
        }

        particleBuffer.GetData(particles);
    }

    void InitializeDataFromTetMesh()
    {
        InitializeParticles();
        InitializeLengthConstraints();
        InitializeVolumeConstraints();
    }

    void InitializeParticles()
    {

    }

    void InitializeLengthConstraints()
    {

    }

    void InitializeVolumeConstraints()
    {

    }
}
