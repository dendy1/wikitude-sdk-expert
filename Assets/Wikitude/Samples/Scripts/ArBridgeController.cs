using System;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using Wikitude;
using UnityEngine.UI;

public class ArBridgeController : SampleController
{
    public WikitudeSDK WikitudeSDK;
    public Text ArBridgeAvailabilityText;
#if UNITY_ANDROID
    private bool _cameraPermissionPopupShown = false;
#endif
    protected override void Awake() {
        base.Awake();
        _showCameraPermissionPopup = false;
    }

    protected override void Update() {
        base.Update();

        switch (WikitudeSDK.ArBridgeAvailability) {
            case ArBridgeAvailability.IndeterminateQueryFailed:
                ArBridgeAvailabilityText.text = "AR Bridge support couldn't be determined.";
                break;
            case ArBridgeAvailability.CheckingQueryOngoing:
                ArBridgeAvailabilityText.text = "AR Bridge support check ongoing.";
                break;
            case ArBridgeAvailability.Unsupported:
                ShowCameraPermissionPopup();
                ArBridgeAvailabilityText.text = "AR Bridge is not supported.";
                break;
            case ArBridgeAvailability.SupportedUpdateRequired:
                ShowCameraPermissionPopup();
                ArBridgeAvailabilityText.text = "AR Bridge is supported, but an update is available.";
                break;
            case ArBridgeAvailability.Supported:
                ShowCameraPermissionPopup();
                ArBridgeAvailabilityText.text = "AR Bridge is supported.";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ShowCameraPermissionPopup() {
#if UNITY_ANDROID
        if (!_cameraPermissionPopupShown && !Permission.HasUserAuthorizedPermission(Permission.Camera)) {
            _showCameraPermissionPopup = true;
            _cameraPermissionPopupShown = true;
        }
#endif
    }
}
