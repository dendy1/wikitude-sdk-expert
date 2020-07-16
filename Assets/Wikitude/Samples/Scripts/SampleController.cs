using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using UnityEngine.SceneManagement;
using Wikitude;
using System.Text;
using System.Collections;

/* Base class for all sample controllers. Defines functionality that all samples needs. */
public class SampleController : MonoBehaviour
{
    private const int _modalWindowWidth = 600;
    private const int _modalWindowHeight = 300;
#if UNITY_ANDROID
    private const string _cameraPermissionPopupMessage = "To enable the Wikitude AR experience, we require the camera permission to access the camera.";
#endif

    protected bool _showCameraPermissionPopup = false;

    protected virtual bool ShouldRequestCameraPermission { get; } = true;

    protected virtual void Awake() {
#if UNITY_ANDROID
        if (ShouldRequestCameraPermission && !Permission.HasUserAuthorizedPermission(Permission.Camera)) {
            _showCameraPermissionPopup = true;
        }
#endif
    }

    protected virtual void Start() {
        /* Default shadow distance is set to 60. Because some samples have different scales, they might overwrite this value. */
        QualitySettings.shadowDistance = 60.0f;

        /* Targeted frame rate is set to 60. The actual framerate can differ based on device performance. */
        Application.targetFrameRate = 60;

        /* Prevent the screen from dimming and turning off in the samples. */
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    public void OnBackButtonClicked() {
        /* Return to the Main Menu when the Back button is pressed, either from the UI, or when pressing the Android back button. */
        SceneManager.LoadScene("Main Menu");
    }

    /* Defines default implementation for some of the most common Wikitude SDK events. */
    /* URLResource events */
    public void OnFinishLoading() {
        Debug.Log("URL Resource loaded successfully.");
    }

    public void OnErrorLoading(Error error) {
        PrintError("Error loading URL Resource!", error, true);
    }

    public void OnInitializationError(Error error) {
        PrintError("Error initializing tracker!", error, true);
    }

    /* Tracker events */
    public void OnTargetsLoaded() {
        Debug.Log("Targets loaded successfully.");
    }

    public void OnErrorLoadingTargets(Error error) {
        PrintError("Error loading targets!", error, true);
    }

    public virtual void OnCameraError(Error error) {
        if (error.Code == 1007 && error.Domain == "com.wikitude.camera_module") {
            PrintError("Camera Error!", error, false);
        } else {
            PrintError("Camera Error!", error, true);
        }
    }

    protected virtual void Update() {
        /* Handles the back button on Android */
        if (Input.GetKeyDown(KeyCode.Escape)) {
            OnBackButtonClicked();
        }
    }

    /* Prints a Wikitude SDK error to the Unity console, as well as the onscreen console defined below. */
    protected void PrintError(string message, Error error, bool addToConsole) {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(message);
        AppendError(stringBuilder, error);

        Debug.LogError(stringBuilder.ToString());

        if (addToConsole) {
            /* Adds the error to the log and displays the Error Console */
            _errorLog.AppendLine(stringBuilder.ToString());
            _showConsole = true;
        }
    }

    /* Handles underlying errors by recursively calling itself */
    private void AppendError(StringBuilder stringBuilder, Error error) {
        stringBuilder.AppendLine($"        Error Code: {error.Code}");
        stringBuilder.AppendLine($"        Error Domain: {error.Domain}");
        stringBuilder.AppendLine($"        Error Message: {error.Message}");

        if (error.UnderlyingError != null) {
            stringBuilder.AppendLine("    Underlying Error: ");
            AppendError(stringBuilder, error.UnderlyingError);
        }
    }

    /* Handles drawing of the Error Console GUI, where all errors are diplayed on the screen to the user. */
    #region Error Console GUI

    /* Contains all the errors that were encountered up to this point. */
    protected StringBuilder _errorLog = new StringBuilder();

    /* The position of the vertical scrollbar for the onscreen console log. */
    private Vector2 _scrollPosition = Vector2.zero;

    /* Defines whether the console should be shown, or if it was hidden. */
    protected bool _showConsole;

#if UNITY_ANDROID
    private void CreatePermissionPopupWindow(int id) {
        GUIStyle labelGuiStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 34,
            padding = new RectOffset(10, 20, 0, 0)
        };
        labelGuiStyle.normal.textColor = Color.black;
        GUIStyle buttonGuiStyle = new GUIStyle(GUI.skin.button) {
            fontSize = 32
        };
        buttonGuiStyle.normal.textColor = Color.blue;
        buttonGuiStyle.active.background = Texture2D.whiteTexture;
        buttonGuiStyle.normal.background = Texture2D.whiteTexture;
        buttonGuiStyle.focused.background = Texture2D.whiteTexture;
        buttonGuiStyle.hover.background = Texture2D.whiteTexture;
        GUI.Label(new Rect(20, 30, _modalWindowWidth - 20, _modalWindowHeight - 50), _cameraPermissionPopupMessage, labelGuiStyle);
        if (GUI.Button(new Rect(20, _modalWindowHeight - 70, 140, 60), "Cancel", buttonGuiStyle)) {
            _showCameraPermissionPopup = false;
        }
        if (GUI.Button(new Rect(_modalWindowWidth - 160, _modalWindowHeight - 70, 140, 60), "OK", buttonGuiStyle)) {
            Permission.RequestUserPermission(Permission.Camera);
        }
    }
#endif

    protected virtual void OnGUI() {
        if (_showCameraPermissionPopup) {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera)) {
                Rect rect = new Rect((Screen.width / 2) - (_modalWindowWidth / 2), (Screen.height / 2) - (_modalWindowHeight / 2), _modalWindowWidth, _modalWindowHeight);
                GUIStyle windowGuiStyle = new GUIStyle(GUI.skin.box);
                windowGuiStyle.normal.background = Texture2D.whiteTexture;
                GUI.Window(0, rect, CreatePermissionPopupWindow, "", windowGuiStyle);
            } else {
                /*
                    When requesting for the camera permission the scene is not stopped at all and the WikitudeSDK
                    is loaded as usual, with a black screen because of the missing permission. Once the permission
                    is accepted we need to reload the scene to initialize everything in a proper way.
                */
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
#endif
        } else if (_showConsole) {
            /* Define the onscreen console to be one fifth of the screen in height. */
            int panelHeight = Screen.height / 5;
            int panelWidth = Screen.width;

            /* The size of the Clear and Hide buttons. */
            int buttonHeight = 30;
            int buttonWidth = 100;

            GUILayout.BeginArea(new Rect(0, Screen.height - panelHeight - buttonHeight, panelWidth, panelHeight + buttonHeight));
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal(GUILayout.MaxWidth(2 * buttonWidth));
                    {
                        if (GUILayout.Button("Clear", GUILayout.Height(buttonHeight), GUILayout.Width(buttonWidth))) {
                            _errorLog = new StringBuilder();
                        }
                        if (GUILayout.Button("Hide", GUILayout.Height(buttonHeight), GUILayout.Width(buttonWidth))) {
                            _showConsole = false;
                        }
                    }
                    GUILayout.EndHorizontal();
                    _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(panelHeight), GUILayout.ExpandHeight(false));
                    {
                        GUILayout.TextArea(_errorLog.ToString(), GUILayout.ExpandHeight(true));
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndArea();
        }
    }
    #endregion
}
