using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Wikitude;

[RequireComponent(typeof(Camera))]
public class AlignmentInteractionController : MonoBehaviour
{
    public AlignmentDrawable StopSignDrawable;
    public AlignmentDrawable FiretruckDrawable;
    private AlignmentDrawable _activeDrawable;

    public GameObject UIInteractionHint;
    public Slider ZoomSlider;
    public bool ZoomSliderIsDragged { get; set; }

    public GameObject LoadingIndicator;
    public Button StopSignButton;
    public Button FiretruckButton;

    private Camera _sceneCamera;
    private SceneRenderer _sceneRenderer;
    private Camera _alignmentCamera;

    /* The last mouse position has to be stored to calculate drag gestures. */
    private Vector2 _lastMousePosition;

    /* This value is used to restore the previous culling setting after rendering for the camera is done. */
    private bool _wasCullingInverted;

    private bool _alignmentInactive = false;

    private void Start() {
        _sceneCamera = Camera.main;
        _sceneCamera.enabled = false;

        /* Ensure that the drawables are properly initialized */
        FiretruckDrawable.Initialize();
        StopSignDrawable.Initialize();

        _alignmentCamera = GetComponent<Camera>();
        _alignmentCamera.clearFlags = CameraClearFlags.Depth;

        /* The alignment camera has to be drawn on top of the scene camera. */
        _alignmentCamera.depth = _sceneCamera.depth + 1;

        /* Set the stop sign as the alignment initialization object. */
        SetDrawable(StopSignDrawable);

        /* Sets the zoom range. */
        ZoomSlider.minValue = 0.5f;
        FiretruckDrawable.ZoomMin = ZoomSlider.minValue;
        StopSignDrawable.ZoomMin = ZoomSlider.minValue;
        ZoomSlider.maxValue = 1.5f;
        FiretruckDrawable.ZoomMax = ZoomSlider.maxValue;
        StopSignDrawable.ZoomMax = ZoomSlider.maxValue;
        ZoomSlider.value = 1f;

        ZoomSlider.onValueChanged.AddListener(OnZoomSliderValueChanged);

        /* If the Universal Render Pipeline is used, these callbacks are called instead of Pre/PostRender. */
        if (GraphicsSettings.renderPipelineAsset) {
            RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
            RenderPipelineManager.endCameraRendering += EndCameraRendering;
        }

#if !UNITY_EDITOR
        /* Remove zoom slider if outside of the Unity Editor. */
        DestroyImmediate(ZoomSlider.transform.parent.gameObject);
#endif
        /* Show the loading indicator and disable the buttons to prevent interruptions */
        LoadingIndicator.SetActive(true);
        StopSignButton.enabled = false;
        FiretruckButton.enabled = false;
    }

    private void Update() {
        if (_sceneRenderer == null) {
            _sceneRenderer = _sceneCamera.GetComponent<SceneRenderer>();
        }

        if (_activeDrawable == null) {
            /* The tracker is currently loading the target. */
            return;
        }

        /* The alignment drawables have to be notified, if the scene camera FOV changes. */
        if (!Mathf.Approximately(_alignmentCamera.fieldOfView, _sceneCamera.fieldOfView)) {
            _alignmentCamera.fieldOfView = _sceneCamera.fieldOfView;
        }

        if (_activeDrawable.AlignmentDrawableAlignedWithTarget) {
            if (_alignmentInactive != true) {
                _alignmentCamera.enabled = false;
                _sceneCamera.enabled = true;

                UIInteractionHint.SetActive(false);
                if (ZoomSlider != null) {
                    ZoomSlider.transform.parent.gameObject.SetActive(false);
                }
                _alignmentInactive = true;
            }

            return;
        }

        if (_alignmentInactive) {
            _alignmentCamera.enabled = true;
            _sceneCamera.enabled = false;

            UIInteractionHint.SetActive(true);
            if (ZoomSlider != null) {
                ZoomSlider.transform.parent.gameObject.SetActive(true);
            }
            _alignmentInactive = false;
        }


        /* If the view is mirrored in case of using a mirrored webcam or the remote front camera
           for live preview, the rotation gestures also have to be mirrored correctly. */
        float flipHorizontalValue = _sceneRenderer.FlipHorizontal ? -1f : 1f;

        /* Skip gestures if the zoom slider is interacted with. */
        if (ZoomSliderIsDragged == false) {

            /* Interaction logic for handling two-finger scale and rotation gestures. */
            if (Input.touchCount >= 2 && (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(1).phase == TouchPhase.Moved)) {
                Touch touchIdZero = Input.GetTouch(0);
                Touch touchIdOne = Input.GetTouch(1);

                Vector2 prevTouchIdZero = touchIdZero.position - touchIdZero.deltaPosition;
                Vector2 prevTouchIdOne = touchIdOne.position - touchIdOne.deltaPosition;

                float prevTouchDistance = (prevTouchIdZero - prevTouchIdOne).magnitude;
                float touchDistance = (touchIdZero.position - touchIdOne.position).magnitude;
                float touchDistancesDelta = touchDistance - prevTouchDistance;

                _activeDrawable.AddZoom(touchDistancesDelta / Mathf.Min(Screen.width, Screen.height));

                if (ZoomSlider != null) {
                    ZoomSlider.value = _activeDrawable.GetZoom();
                }

                float rotation = Vector2.SignedAngle(prevTouchIdZero - prevTouchIdOne, touchIdZero.position - touchIdOne.position);
                float rotationMultiplier =  180f / Mathf.Min(Screen.width, Screen.height);
                _activeDrawable.AddRotation(new Vector3(0f, 0f, flipHorizontalValue * rotation * rotationMultiplier));

                /* In case one finger gets lifted, the last mouse position has to be invalidated. */
                _lastMousePosition = Vector2.zero;
            } else if (Input.touchCount < 2 ) {
                /* The mouse input works for both, the mouse input and single finger input. */
                if (Input.GetMouseButtonDown(0)){
                    _lastMousePosition = Input.mousePosition;
                } else if (Input.GetMouseButton(0)) {
                    /* This condition is met if a finger during two-finger gestures is lifted. */
                    if (_lastMousePosition.Equals(Vector2.zero)) {
                        _lastMousePosition = Input.mousePosition;
                    } else {
                        Vector2 mousePosition = Input.mousePosition;
                        Vector2 deltaMousePosition = mousePosition - _lastMousePosition;
                        _lastMousePosition = mousePosition;

                        float rotationMultiplier =  180f / Mathf.Min(Screen.width, Screen.height);

                        _activeDrawable.AddRotation(new Vector3(deltaMousePosition.y * rotationMultiplier, -flipHorizontalValue * deltaMousePosition.x  * rotationMultiplier, 0f));
                    }
                }
            }
        }
    }

    public void OnStopSignTargetFinishedLoading() {
        TargetLoaded(StopSignDrawable);
    }

    public void OnFiretruckTargetFinishedLoading() {
        TargetLoaded(FiretruckDrawable);
    }

    public void OnErrorLoadingTargets(Error error) {
        /* The separate error callback will display the error, so we just hide the LoadingIndicator here. */
        LoadingIndicator.SetActive(false);
    }

    private void TargetLoaded(AlignmentDrawable drawable) {
        LoadingIndicator.SetActive(false);

        StopSignButton.enabled = true;
        FiretruckButton.enabled = true;

        _activeDrawable = drawable;
        _activeDrawable.gameObject.SetActive(true);

        if (ZoomSlider != null) {
            ZoomSlider.value = 1;
        }

        _activeDrawable.ResetPose();
    }

    public void SetDrawable(AlignmentDrawable drawable) {
        FiretruckDrawable.gameObject.SetActive(false);
        StopSignDrawable.gameObject.SetActive(false);

        /* Disable the buttons, so that the user cannot switch until loading has finished. */
        StopSignButton.enabled = false;
        FiretruckButton.enabled = false;

        StopSignDrawable.TargetObjectTracker.enabled = false;
        FiretruckDrawable.TargetObjectTracker.enabled = false;

        /* By enabling the tracker, the target collection will be loaded and the targets will extracted. Depending on the target collection, this can take a while. */
        drawable.TargetObjectTracker.enabled = true;

        LoadingIndicator.SetActive(true);
    }

    private void OnZoomSliderValueChanged(float value) {
        _activeDrawable.SetZoom(value);
    }

    /* This scriptable render pipeline callback calls the default renderer's corresponding callback. */
    private void BeginCameraRendering(ScriptableRenderContext context, Camera camera) {
        if (camera.enabled && camera.GetInstanceID() == _alignmentCamera.GetInstanceID()) {
            OnPreRender();
        }
    }

    /* This scriptable render pipeline callback calls the default renderer's corresponding callback. */
    private void EndCameraRendering(ScriptableRenderContext context, Camera camera) {
        if (camera.enabled && camera.GetInstanceID() == _alignmentCamera.GetInstanceID()) {
            OnPostRender();
        }
    }

    /* If the scene renderer has a mirrored view or inverted culling, the settings are also applied to this camera. */
    private void OnPreRender() {
        _alignmentCamera.ResetWorldToCameraMatrix();
        _alignmentCamera.ResetProjectionMatrix();
        _alignmentCamera.projectionMatrix = _alignmentCamera.projectionMatrix * Matrix4x4.Scale(_sceneRenderer.FlipHorizontal ? new Vector3(-1,1,1) : Vector3.one);

        _wasCullingInverted = GL.invertCulling;
        GL.invertCulling = _sceneRenderer.InvertCulling || _sceneRenderer.FlipHorizontal;
    }

    private void OnPostRender() {
        GL.invertCulling = _wasCullingInverted;
    }
}
