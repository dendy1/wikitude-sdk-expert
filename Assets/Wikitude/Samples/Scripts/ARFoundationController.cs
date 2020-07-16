using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Wikitude;

public class ARFoundationController : SampleController
{
    public GameObject Instructions;
    public GameObject UnsupportedDeviceText;

    protected override bool ShouldRequestCameraPermission { get; } = false;

    private bool _permissionGranted = false;

    private IEnumerator CheckARFoundationSupport() {
        void ShowUnsupportedDeviceMessage() {
            Instructions.SetActive(false);
            UnsupportedDeviceText.SetActive(true);
        }

        if (Application.platform == RuntimePlatform.Android) {
            /* On Android, first check if the Android version could support ARFoundation, because otherwise the API would not work */
            using (var version = new AndroidJavaClass("android.os.Build$VERSION")) {
                int versionNumber = version.GetStatic<int>("SDK_INT");
                if (versionNumber < 24) {
                    ShowUnsupportedDeviceMessage();
                }
            }
        }

        bool arFoundationStateDetermined = false;
        while (!arFoundationStateDetermined) {
            Debug.Log($"Checking ARFoundation support - {ARSession.state}.");
            switch (ARSession.state) {
                case ARSessionState.CheckingAvailability:
                case ARSessionState.Installing:
                case ARSessionState.SessionInitializing:
                    yield return new WaitForSeconds(0.1f);
                    break;
                case ARSessionState.None:
                case ARSessionState.Unsupported:
                    ShowUnsupportedDeviceMessage();
                    arFoundationStateDetermined = true;
                    break;
                default:
                    arFoundationStateDetermined = true;
                    break;
            }
        }
    }

    public void OnArFoundationCameraPermissionGranted() {
        _permissionGranted = true;
        _showConsole = false;
    }

    public void OnArFoundationCameraError(Error error) {
        PrintError("AR Foundation Camera Error!", error, true);
    }

    protected override void Awake() {
        base.Awake();
        StartCoroutine(CheckARFoundationSupport());
    }

    protected override void OnGUI() {
        if (_showConsole && (ARSession.state == ARSessionState.Unsupported || _permissionGranted)) {
            _showConsole = false;
        } else if (!_showConsole && _errorLog.Length != 0 && !_permissionGranted) {
            _showConsole = true;
        }
        base.OnGUI();
    }
}
