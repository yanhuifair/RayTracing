using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
public class Automaton : MonoBehaviour
{
    public ComputeShader computeShader;
    public Gradient gradient;
    public bool useGradient;

    int kernalHandle;

    //Automaton
    [DrawWithUnity][MinMax(MinMaxAttributeType.Int, 0, 26)][OnValueChanged("SetRule")] public Vector4 survive = new Vector4(0, 5, 10, 26);
    [DrawWithUnity][MinMax(MinMaxAttributeType.Int, 0, 26)][OnValueChanged("SetRule")] public Vector4 rebirth = new Vector4(0, 5, 10, 26);

    [OnValueChanged("SetRule")] public int stateMax;
    void SetRule()
    {
        rule = $"{survive.y}/{survive.z} {rebirth.y}/{rebirth.z} {stateMax}M";
    }

    [ReadOnly] public string rule;

    [ShowInInspector] const int spaceSize = 8 * 10;
    [ReadOnly] public int cellNumber;
    struct Cell
    {
        public int state;
        public float x;
        public float y;
        public float z;
    }

    [SerializeField] Cell[, , ] cells;
    Cell[, , ] Cells
    {
        get
        {
            if (cells == null || cells.GetLength(0) != spaceSize)
            {
                cells = new Cell[spaceSize, spaceSize, spaceSize];
            }
            return cells;
        }
    }
    public GameObject cube;
    public GameObject parent;
    public GameObject Parent
    {
        get
        {
            if (parent == null) parent = new GameObject();
            return parent;
        }
    }

    [SerializeField] GameObject[, , ] gameObjects;
    GameObject[, , ] GameObjects
    {
        get
        {
            if (gameObjects == null || gameObjects.GetLength(0) != spaceSize)
            {
                gameObjects = new GameObject[spaceSize, spaceSize, spaceSize];
            }
            return gameObjects;
        }
    }

    ComputeBuffer buffer;
    ComputeBuffer Buffer
    {
        get
        {
            unsafe
            {
                if (buffer == null) buffer = new ComputeBuffer(Cells.Length, sizeof(Cell));
                return buffer;
            }
        }
    }

    public bool autoUpdate = false;
    public float updateTime;
    float updateTimeCount;
    void Start()
    {
        if (autoUpdate)
        {
            Seed();
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (autoUpdate)
        {
            updateTimeCount += Time.deltaTime;
            if (updateTimeCount > updateTime)
            {
                updateTimeCount -= updateTime;
                Iteration();
            }
        }
    }

    public int seedSize;
    [Button]
    void Seed()
    {
        Clear();
        int seedx = seedSize / 2;
        int min = spaceSize / 2 - seedx;
        int max = spaceSize / 2 + seedSize - seedx;
        //Seed
        cellNumber = 0;
        if (cube != null)
        {
            for (int x = min; x < max; x++)
            {
                for (int y = min; y < max; y++)
                {
                    for (int z = min; z < max; z++)
                    {
                        Cells[x, y, z].state = Random.value > 0.9f ? stateMax : stateMax;
                    }
                }
            }
        }

        UpdateCube();
        //Debug.Log("Seed");
    }

    [Button]
    void Iteration()
    {
        kernalHandle = computeShader.FindKernel("CSMain");
        Buffer.SetData(Cells);
        computeShader.SetBuffer(kernalHandle, "cell", Buffer);

        unsafe
        {
            var outbuffer = new ComputeBuffer(Cells.Length, sizeof(Cell));
            outbuffer.SetData(Cells);
            computeShader.SetBuffer(kernalHandle, "cellOut", outbuffer);

            var setting = new Vector4(survive.y, survive.z, rebirth.y, rebirth.z);
            //setting 
            computeShader.SetVector("setting", setting);
            computeShader.SetInt("spaceSize", spaceSize);
            computeShader.SetInt("stateMax", stateMax);
            computeShader.Dispatch(kernalHandle, spaceSize / 8, spaceSize / 8, spaceSize / 8);

            outbuffer.GetData(Cells);
        }

        //
        UpdateCube();
    }

    [Button]
    void Clear()
    {
        for (int x = 0; x < spaceSize; x++)
        {
            for (int y = 0; y < spaceSize; y++)
            {
                for (int z = 0; z < spaceSize; z++)
                {
                    if (GameObjects[x, y, z] != null)
                    {
                        DestroyImmediate(GameObjects[x, y, z]);
                        GameObjects[x, y, z] = null;
                    }

                    Cells[x, y, z].state = 0;
                }
            }
        }

        //Debug.Log("Clear");
    }

    void UpdateCube()
    {
        if (cube != null)
        {
            for (int x = 0; x < spaceSize; x++)
            {
                for (int y = 0; y < spaceSize; y++)
                {
                    for (int z = 0; z < spaceSize; z++)
                    {
                        if (Cells[x, y, z].state > 0)
                        {
                            if (GameObjects[x, y, z] == null)
                            {
                                GameObjects[x, y, z] = PrefabUtility.InstantiatePrefab(cube) as GameObject;
                                GameObjects[x, y, z].transform.parent = Parent.transform;
                                GameObjects[x, y, z].transform.position = new Vector3(x, y, z);
                                GameObjects[x, y, z].SetActive(Cells[x, y, z].state > 0);
                            }
                        }
                        else
                        {
                            if (GameObjects[x, y, z] != null) GameObjects[x, y, z].SetActive(Cells[x, y, z].state > 0);
                        }
                    }
                }
            }
        }
    }
    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(new Vector3(spaceSize / 2, spaceSize / 2, spaceSize / 2), Vector3.one * spaceSize);

        return;
        for (int x = 0; x < spaceSize; x++)
        {
            for (int y = 0; y < spaceSize; y++)
            {
                for (int z = 0; z < spaceSize; z++)
                {
                    if (Cells[x, y, z].state > 0)
                    {
                        if (useGradient) Gizmos.color = gradient.Evaluate((float) Cells[x, y, z].state / stateMax);
                        // Gizmos.DrawWireCube(new Vector3(x, y, z), Vector3.one * Cells[x, y, z].state / stateMax);
                        //Gizmos.DrawWireCube(new Vector3(x, y, z), Vector3.one);
                        Gizmos.DrawCube(new Vector3(x, y, z), Vector3.one);
                    }
                }
            }
        }
    }
}