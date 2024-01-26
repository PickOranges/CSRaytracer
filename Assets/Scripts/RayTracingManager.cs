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

    private void Awake()
    {
        _camera = GetComponent<Camera>();

        // Set computer shader camera parameters 
        SetShaderParameters();
    }

    void SetShaderParameters()
    {
        _shader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        _shader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        _shader.SetTexture(0, "_SkyboxTexture", _skybox);
        _shader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));  // random range: [0.0, 1.0]
        _shader.SetInt("_NumSpheres", _numSpheres);
        _shader.SetFloat("_SphereOffsetX", _sphereOffsetX);
    }

    void Start()
    {
        InitRenderTexture();

        _shader.SetTexture(0, "Result", _target);
        int groupX = _target.width / 8;
        int groupY = _target.height / 8;
        _shader.Dispatch(0, groupX, groupY, 1);

        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }
    void OnDestroy()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
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

    void OnCameraRendering(ScriptableRenderContext context, Camera camera)
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
        //if (transform.hasChanged)
        //{
        //    _currentSample = 0;
        //    transform.hasChanged = false;
        //}
        //if (_addMaterial == null)
        //    _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        //_addMaterial.SetFloat("_Sample", _currentSample);

        // Add post-processing(i.e. here is anti-aliasing) to the result obtained from ComputeShader, and then
        // copy from tmp texture to camera target, by default is screen.
        //Graphics.Blit(_target, _camera.targetTexture, _addMaterial);
        //_currentSample++;
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
