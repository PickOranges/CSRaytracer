using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class RayTracingManager : MonoBehaviour
{
    public ComputeShader _shader;
    public RenderTexture _target;
    public Texture _skybox;

    Camera _camera;

    uint _currentSample = 0;
    Material _addMaterial;

    [Range(0,100)]
    public int _numSpheres=10;

    [Range (-500,10)]
    public float _sphereOffsetX=-500;

    public Light DirectionalLight;

    // send sphere data to GPU
    [Header("Sphere Generation Settings")]
    public Vector2 SphereRadius = new Vector2(3.0f, 8.0f);
    public uint SpheresMax = 100;
    public float SpherePlacementRadius = 100.0f;
    private ComputeBuffer _sphereBuffer;

    struct Sphere
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
    };

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    void SetShaderParameters()
    {
        _shader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        _shader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        _shader.SetTexture(0, "_SkyboxTexture", _skybox);
        _shader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));  // random range: [0.0, 1.0]
        _shader.SetInt("_NumSpheres", _numSpheres);
        _shader.SetFloat("_SphereOffsetX", _sphereOffsetX);

        Vector3 l = DirectionalLight.transform.forward;  // direction of the directional light source
        _shader.SetVector("_DirectionalLight", new Vector4(l.x,l.y,l.z, DirectionalLight.intensity));
        _shader.SetBuffer(0, "_Spheres", _sphereBuffer);
    }

    void Start()
    {
        InitRenderTexture();
        SetShaderParameters();
        _shader.SetTexture(0, "Result", _target);
        int groupX = _target.width / 8;
        int groupY = _target.height / 8;
        _shader.Dispatch(0, groupX, groupY, 1);

        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }
    void OnDestroy()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        if (_sphereBuffer != null)
            _sphereBuffer.Release();
    }

    void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!_target)
        {
            Debug.Log("RenderTexture is empty.");
            return;
        }
        Graphics.Blit(_target, camera.targetTexture);
    }


    void InitRenderTexture()
    {
        if(!_target || _target.width!=Screen.width || _target.height!=Screen.height)
        {
            if(_target) _target.Release();

            _target=new RenderTexture(Screen.width, Screen.height, 0, 
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();

            Debug.Log("RenderTexture is created.");
        }
    }

    void Update()
    {  
        _camera = GetComponent<Camera>();
        SetShaderParameters();
        _shader.Dispatch(0, _target.width / 8, _target.height / 8, 1);
        Graphics.Blit(_target, _camera.targetTexture);

        // AA
        if (transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
        }
        //if (_addMaterial == null)
        //    _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        //_addMaterial.SetFloat("_Sample", _currentSample);

        // Add post-processing(i.e. here is anti-aliasing) to the result obtained from ComputeShader, and then
        // copy from tmp texture to camera target, by default is screen.
        //Graphics.Blit(_target, _camera.targetTexture, _addMaterial);
        _currentSample++;
    }

    private void OnEnable()
    {
        _currentSample = 0;
        SetUpScene();
    }

    private void OnDisable()
    {
        if (_sphereBuffer != null)
            _sphereBuffer.Release();
    }

    private void SetUpScene()
    {
        List<Sphere> spheres = new List<Sphere>();

        // Add a number of random spheres
        for (int i = 0; i < SpheresMax; i++)
        {
            Sphere sphere = new Sphere();

            // Radius and radius
            sphere.radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x);
            Vector2 randomPos = Random.insideUnitCircle * SpherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);

            // Reject spheres that are intersecting others
            foreach (Sphere other in spheres)
            {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                    goto SkipSphere;
            }

            // Albedo and specular color
            Color color = Random.ColorHSV();
            bool metal = Random.value < 0.5f;
            sphere.albedo = metal ? Vector4.zero : new Vector4(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector4(color.r, color.g, color.b) : new Vector4(0.04f, 0.04f, 0.04f);

            // Add the sphere to the list
            spheres.Add(sphere);

        SkipSphere:
            continue;
        }

        // Assign to compute buffer
        if (_sphereBuffer != null)
            _sphereBuffer.Release();
        _sphereBuffer = new ComputeBuffer(spheres.Count, 40);
        _sphereBuffer.SetData(spheres);
    }


}

/*  Questions:
 *  1. Pipeline orders? Frame, Camera, Context?
 *  2. Why the skybox look so weird?
 *  3. Why there's no translation but only rotation for the ray?
 */

/*  NOTES:
 *  0. Awake is called before Start.
 *  1. How to get the camera, to which current C# script was attached.
 *      - create a private Camera member.
 *      - in void Awake(), camera=GetComponent<Camera>();
 *  2. Our _skybox texture is a spherical texture, instead of a cube box. 
 *  This is the first time I've ever use it.
 * 
 */
