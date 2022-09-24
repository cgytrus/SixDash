using System.Linq;

using BepInEx.Configuration;

using UnityEngine;

using Gizmos = Popcron.Gizmos;

namespace SixDash;

internal class GizmosCamera : MonoBehaviour {
    private Camera? _camera;
    private Transform? _transform;
    private Transform? _followTransform;

    private readonly ConfigEntry<bool> _enabled;
    private readonly ConfigEntry<bool> _alwaysOnTop;

    public GizmosCamera() {
        ConfigFile config = Plugin.instance!.Config;

        _enabled = config.Bind("Gizmos", "Enabled", true, "");
        _enabled.SettingChanged += (_, _) => { SetEnabled(_enabled.Value); };

        _alwaysOnTop = config.Bind("Gizmos", "AlwaysOnTop", true, "");
        _alwaysOnTop.SettingChanged += (_, _) => { SetAlwaysOnTop(_alwaysOnTop.Value); };
    }

    private void Awake() {
        _transform = transform;
        SetEnabled(_enabled.Value);
        SetAlwaysOnTop(_alwaysOnTop.Value);
    }

    private void SetEnabled(bool value) {
        Gizmos.Enabled = value;
        if(_camera)
            _camera!.enabled = value && _alwaysOnTop.Value;
    }

    private void SetAlwaysOnTop(bool value) {
        if(_camera)
            _camera!.enabled = value && _enabled.Value;
        if(value) {
            Gizmos.CameraFilter += GizmosCameraFilter;
            Gizmos.CameraFilter -= MainCameraFilter;
        }
        else {
            Gizmos.CameraFilter -= GizmosCameraFilter;
            Gizmos.CameraFilter += MainCameraFilter;
        }
    }

    private bool GizmosCameraFilter(Object camera) => camera == _camera;
    private bool MainCameraFilter(Object camera) => camera != _camera;

    private void Update() {
        if(!_enabled.Value || !_transform)
            return;
        if(!_followTransform) {
            Camera? mainCamera = FindObjectsOfType<Camera>().FirstOrDefault(cam => cam != _camera);
            if(!mainCamera)
                return;
            InitializeCamera(mainCamera!);
        }
        _transform!.position = _followTransform!.position;
        _transform.rotation = _followTransform.rotation;
        _transform.localScale = _followTransform.lossyScale;
    }

    private void InitializeCamera(Camera copyFrom) {
        _followTransform = copyFrom.transform;
        if(!_camera)
            _camera = gameObject.AddComponent<Camera>();
        _camera!.clearFlags = CameraClearFlags.Depth;
        _camera.cullingMask = 0;
        _camera.depth = copyFrom.depth + 1;
        _camera.aspect = copyFrom.aspect;
        _camera.farClipPlane = copyFrom.farClipPlane;
        _camera.fieldOfView = copyFrom.fieldOfView;
        _camera.nearClipPlane = copyFrom.nearClipPlane;
        _camera.useOcclusionCulling = false;
        _camera.allowHDR = false;
        _camera.allowMSAA = false;
    }
}
