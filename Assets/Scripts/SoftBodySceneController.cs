using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

struct Cube
{
    public Vector3 min, max;
}

public class SoftBodySceneController : MonoBehaviour
{
    public int nSubSteps = 10;
    public float gravity = 9.81f;
    public ComputeShader shader;
    public Dropdown sceneDropdown;
    public Text edgeComplianceText;
    public Text volComplianceText;


    int kiSolveCubeCollisions;
    int nCubes;
    int[] potentialCollisions;

    SoftBody sb;
    Cube[] cubes;
    GameObject[] cubeObjects;

    ComputeBuffer cubesBuffer;
    ComputeBuffer potentialCollisionBuffer;


    void Start()
    {
        FillDropdown();
        LoadScene("plane");
    }

    void FixedUpdate()
    {
        DetectPotentialCollisions();
        UpdateCubes();
    }

    void OnDestroy()
    {
        cubesBuffer.Release();
        potentialCollisionBuffer.Release();
    }

    void LoadScene(string scene)
    {
        string[] scenetext = Resources.Load<TextAsset>("SoftBodyScenes/" + scene).text.Split(new string[] { "\r\n" }, System.StringSplitOptions.None);
        int currentLine = 0;

        nCubes = int.Parse(scenetext[currentLine++]);
        shader.SetInt("nCubes", nCubes);
        cubes = new Cube[nCubes];
        cubeObjects = new GameObject[nCubes];
        potentialCollisions = new int[nCubes];

        string meshfile = scenetext[currentLine++];
        Matrix4x4 transform = new Matrix4x4();
        string[] matVals = scenetext[currentLine++].Split(null);
        for (int j = 0; j < 16; j++)
        {
            transform[j] = float.Parse(matVals[j]);
        }
        Material mat = Resources.Load<Material>("Materials/" + scenetext[currentLine++]);
        float edgeCompliance = float.Parse(scenetext[currentLine++]);
        float volumeCompliance = float.Parse(scenetext[currentLine++]);

        AddSoftBody(meshfile, transform, mat, edgeCompliance, volumeCompliance);
        UpdateComplianceText();

        for (int i = 0; i < nCubes; i++)
        {
            string[] minString = scenetext[currentLine++].Split(null);
            string[] maxString = scenetext[currentLine++].Split(null);
            Vector3 min = Vector3.zero;
            Vector3 max = Vector3.zero;
            for (int j = 0; j < 3; j++)
            {
                min[j] = float.Parse(minString[j]);
                max[j] = float.Parse(maxString[j]);
            }

            AddCube(i, min, max);
        }

        cubesBuffer = new ComputeBuffer(nCubes, 2 * 3 * sizeof(float));
        potentialCollisionBuffer = new ComputeBuffer(nCubes, sizeof(int));

        kiSolveCubeCollisions = shader.FindKernel("solveCollisions");
        shader.SetBuffer(kiSolveCubeCollisions, "sceneCubes", cubesBuffer);
        shader.SetBuffer(kiSolveCubeCollisions, "potentialCollisions", potentialCollisionBuffer);
    }

    void DestroyScene()
    {
        Destroy(sb.gameObject);
        for (int i = 0; i < nCubes; i++)
        {
            Destroy(cubeObjects[i]);
        }

        cubesBuffer.Release();
        potentialCollisionBuffer.Release();
    }

    void AddSoftBody(string meshfile, Matrix4x4 transform, Material mat, float edgeCompliance, float volumeCompliance)
    {
        GameObject g = Resources.Load<GameObject>("Prefabs/SoftBody");
        SoftBody s = g.GetComponent<SoftBody>();
        s.nSubSteps = nSubSteps;
        s.edgeCompliance = edgeCompliance;
        s.volumeCompliance = volumeCompliance;
        s.gravity = gravity;
        s.meshfile = meshfile;
        s.initTransform = transform;
        s.mat = mat;
        s.shader = shader;
        sb = Instantiate(s);
    }

    void AddCube(int index, Vector3 min, Vector3 max)
    {
        GameObject cubeObject = Instantiate(Resources.Load<GameObject>("Prefabs/Cube"));
        Vector3 center = (min + max) / 2;
        Vector3 size = max - min;
        cubeObject.transform.Translate(center);
        cubeObject.transform.localScale = size;

        cubes[index] = new Cube { min = min, max = max };
        cubeObjects[index] = cubeObject;
    }

    void DetectPotentialCollisions()
    {
        for (int i = 0; i < nCubes; i++)
        {
            Bounds cubeBounds = cubeObjects[i].GetComponent<Collider>().bounds;
            potentialCollisions[i] = cubeBounds.Intersects(sb.mesh.bounds) ? 1 : 0;
        }
        potentialCollisionBuffer.SetData(potentialCollisions);
    }

    void UpdateCubes()
    {
        for (int i = 0; i < nCubes; i++)
        {
            cubes[i].min = cubeObjects[i].transform.position - cubeObjects[i].transform.localScale / 2;
            cubes[i].max = cubeObjects[i].transform.position + cubeObjects[i].transform.localScale / 2;
        }
        cubesBuffer.SetData(cubes);
    }

    void FillDropdown()
    {
        string[] scenes = { "plane", "platforms", "stairs", "squeeze" };
        foreach (string scene in scenes)
        {
            sceneDropdown.options.Add(new Dropdown.OptionData(scene));
        }
        sceneDropdown.captionText.text = sceneDropdown.options[0].text;
    }

    void UpdateComplianceText()
    {
        edgeComplianceText.text = "Edge compliance: " + sb.edgeCompliance;
        volComplianceText.text = "Vol compliance: " + sb.volumeCompliance;
    }

    public void OnLoadSceneClicked()
    {
        string scene = sceneDropdown.options[sceneDropdown.value].text;
        DestroyScene();
        LoadScene(scene);
    }
}
