using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
public struct MeshObject
{
    public Transform model_transform;
    public int indices_offset;
    public int indices_count;
    public Texture2D albedoTexture, aoTexture, roughTexture, metalTexture, normalTexture;
}
public class RenderingMaster : MonoBehaviour
{
    public static RenderingMaster _instance;
    [Range(0, 1280)]
    public float _SampleSize = Screen.width/2;

    public ComputeShader RayTracingShader;
    public Light DirectionalLight;

    private Camera _camera;
    private float _lastFieldOfView;
    private static List<Transform> _transformsToWatch = new List<Transform>();

    private RenderTexture _target;
    private RenderTexture _converged;
    private Material _addMaterial;

    public static bool _meshObjectsNeedRebuilding = false;
    public static List<RenderingObject> _rayTracingObjects = new List<RenderingObject>();
    public static List<MeshObject> _meshObjects = new List<MeshObject>();

    public  List<int> _indices = new List<int>();
    public  List<Vector3> _vertices = new List<Vector3>();
    public  List<Vector3> _normals = new List<Vector3>();
    public  List<Vector2> _textureVals = new List<Vector2>();
    public List<Vector4> _tangents = new List<Vector4>();

    private ComputeBuffer _pixelBuffer;

    public SoftwareRenderer softwareRenderer;
    public Rasterizer rasterizer;

    private bool isBackFaceCulling,isOpenZDepth;
    private int drawTrianglesType = 0;

    public bool GetBackFaceCulling()
    {
        return isBackFaceCulling;
    }

    public void SetBackFaceCulling(bool isSwitch)
    {
        isBackFaceCulling = isSwitch;
        _meshObjectsNeedRebuilding = true;
    }

    public int GetDrawTrianglesType()
    {
        return drawTrianglesType;
    }

    public void SetDrawTrianglesType(int type)
    {
        drawTrianglesType = type;
        _meshObjectsNeedRebuilding = true;
    }

    public bool GetOpenZDepth()
    {
        return isOpenZDepth;
    }

    public void SetOpenZDepth(bool isSwitch)
    {
        isOpenZDepth = isSwitch;
        _meshObjectsNeedRebuilding = true;
    }

    private void Awake()
    {
        _instance = this;

        _camera = Camera.main;

        _transformsToWatch.Add(_camera.transform);
        _transformsToWatch.Add(DirectionalLight.transform);

        softwareRenderer = new SoftwareRenderer();
        rasterizer = new Rasterizer();
    }

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
        _pixelBuffer?.Release();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            //ScreenCapture.CaptureScreenshot(Time.time + "-" + _currentSample + ".png");
        }

        if (_camera.fieldOfView != _lastFieldOfView)
        {
            _lastFieldOfView = _camera.fieldOfView;
            _meshObjectsNeedRebuilding = true;
        }

        foreach (Transform t in _transformsToWatch)
        {
            if (t.hasChanged)
            {
                t.hasChanged = false;
                _meshObjectsNeedRebuilding = true;
            }
        }
    }

    public static void RegisterObject(RenderingObject obj)
    {
        _transformsToWatch.Add(obj.transform) ;
        _rayTracingObjects.Add(obj);
        _meshObjectsNeedRebuilding = true;
    }
    public static void UnregisterObject(RenderingObject obj)
    {
        _transformsToWatch.Remove(obj.transform);
        _rayTracingObjects.Remove(obj);
        _meshObjectsNeedRebuilding = true;
    }
    private void RebuildMeshObjectBuffers()
    {
        if (!_meshObjectsNeedRebuilding)
        {
            return;
        }

        _meshObjectsNeedRebuilding = false;

        softwareRenderer.Init();
        // Clear all lists
        _meshObjects.Clear();
        _indices.Clear();
        _vertices.Clear();
        _normals.Clear();
        _textureVals.Clear();
        _tangents.Clear();

        // Loop over all objects and gather their data
        foreach (RenderingObject obj in _rayTracingObjects)
        {
            Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;

            _normals.AddRange(mesh.normals);
            _textureVals.AddRange(mesh.uv);
            _tangents.AddRange(mesh.tangents);

            // Add vertex data
            int firstVertex = _vertices.Count;
            _vertices.AddRange(mesh.vertices);

            // Add index data - if the vertex buffer wasn't empty before, the
            // indices need to be offset
            int firstIndex = _indices.Count;
            var indices = mesh.GetIndices(0);

            _indices.AddRange(indices.Select(index => index + firstVertex));

            // Add the object itself
            _meshObjects.Add(new MeshObject()
            {
                model_transform = obj.transform,
                indices_offset = firstIndex,
                indices_count = indices.Length,
                albedoTexture = obj.albedoTexture,
                aoTexture = obj.aoTexture,
                roughTexture = obj.roughTexture,
                metalTexture = obj.metalTexture,
                normalTexture = obj.normalTexture,
            });
        }


        for (int i = 0; i < _meshObjects.Count; i++)
        {
            softwareRenderer.DrawTriangularMesh(_meshObjects[i]);
        }
        CreateComputeBuffer(ref _pixelBuffer, SoftwareRenderer._pixels, 16);
    }
    
    private static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
        where T : struct
    {
        // Do we already have a compute buffer?
        if (buffer != null)
        {
            // If no data or buffer doesn't match the given criteria, release it
            if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
            {
                buffer.Release();
                buffer = null;
            }
        }

        if (data.Count != 0)
        {
            // If the buffer has been released or wasn't there to
            // begin with, create it
            if (buffer == null)
            {
                buffer = new ComputeBuffer(data.Count, stride);
            }

            // Set data on the buffer
            buffer.SetData(data);
        }
    }

    private void SetComputeBuffer(string name, ComputeBuffer buffer)
    {
        if (buffer != null)
        {
            RayTracingShader.SetBuffer(0, name, buffer);
        }
    }

    private void SetShaderParameters()
    {
        SetComputeBuffer("_PixelBuffer", _pixelBuffer);
    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
        {
            // Release render texture if we already have one
            if (_target != null)
            {
                _target.Release();
                _converged.Release();
            }

            // Get a render target for Ray Tracing
            _target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();

            _converged = new RenderTexture(Screen.width, Screen.height, 0,
             RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _converged.enableRandomWrite = true;
            _converged.Create();
        }
    }

    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();

        // Set the target and dispatch the compute shader
        RayTracingShader.SetTexture(0, "Result", _target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        // Blit the result texture to the screen
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        _addMaterial.SetFloat("_Size", _SampleSize);


        Graphics.Blit(_target, _converged, _addMaterial);
        Graphics.Blit(_converged, destination);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        RebuildMeshObjectBuffers();
        SetShaderParameters();
        Render(destination);
    }
}