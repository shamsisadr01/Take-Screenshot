using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.LightProbeProxyVolume;

public class Screenshot : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    private static string[] DateFormats = new string[] { "MM-dd-yyyy", "dd-MM-yyyy", "yyyy-MM-dd" };

    private static string[] TimeFormats = new string[] { "24 Hour", "AM-PM" };

    private static int TimeFormat
    {
        get { return EditorPrefs.GetInt("TimeFormat", 0); }
        set { EditorPrefs.SetInt("TimeFormat", value); }
    }

    private static int DateFormat
    {
        get { return EditorPrefs.GetInt("DateFormat", 1); }
        set { EditorPrefs.SetInt("DateFormat", value); }
    }

    private string Path
    {
        get { return EditorPrefs.GetString("ScreenDir", Application.dataPath.Replace("Assets", "Screenshots/")); }
        set { EditorPrefs.SetString("ScreenDir", value); }
    }

    private int OutputPNG
    {
        get { return EditorPrefs.GetInt("Output", 0); }
        set { EditorPrefs.SetInt("Output", value); }
    }

    private bool AutoOpen
    {
        get { return EditorPrefs.GetBool("AutoOpen", true); }
        set { EditorPrefs.SetBool("AutoOpen", value); }
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);

        SetupHandler();
    }

    [MenuItem("Tools/Screenshot")]
    public static void ShowExample()
    {
        Screenshot wnd = GetWindow<Screenshot>();
       /* wnd.maxSize = new Vector2(300f, 260f);
        wnd.minSize = wnd.maxSize;*/
        wnd.titleContent = new GUIContent("ScreenShot");
        wnd.titleContent.image = EditorGUIUtility.IconContent("Camera Gizmo").image;
    }

    private void SetupHandler()
    {
        VisualElement root = rootVisualElement;

        Button settings = root.Q<Button>("Settings");
        settings.RegisterCallback<ClickEvent>(Settings);

        TextField path = root.Q<TextField>("Path");
        path.value = Path;

        Button selectPath = root.Q<Button>("SelectPath");
        selectPath.RegisterCallback<ClickEvent>(SelectPath);

        Button showPath = root.Q<Button>("ShowPath");
        showPath.RegisterCallback<ClickEvent>(ShowPath);

        DropdownField output = root.Q<DropdownField>("Output");
        output.index = OutputPNG;
        output.RegisterValueChangedCallback(Output);

        Toggle autoOpen = root.Q<Toggle>("AutoOpen");
        autoOpen.value = AutoOpen;
        autoOpen.RegisterCallback<ClickEvent>(AutoOpenMethod);

        Button capture = root.Q<Button>("Capture");
        capture.RegisterCallback<ClickEvent>(Capture);
    }

    private void Settings(ClickEvent evt)
    {
        SettingsService.OpenUserPreferences("Preferences/ScreenShot");
    }

    private void SelectPath(ClickEvent evt)
    {
        string path = EditorUtility.SaveFolderPanel("Screenshot destination folder", Path, Application.dataPath);

        VisualElement root = rootVisualElement;

        var showPath = root.Q<TextField>("Path");
        if (path != string.Empty)
            Path = path;

        showPath.value = Path;
    }

    private void ShowPath(ClickEvent evt)
    {
        CheckFolderValidity();
        Application.OpenURL(Path);
    }

    private void CheckFolderValidity()
    {
        if (!Directory.Exists(Path))
        {
            Debug.Log("Directory <i>\"" + Path + "\"</i> didn't exist and was created...");
            Directory.CreateDirectory(Path);

            AssetDatabase.Refresh();
        }
    }

    private void Output(ChangeEvent<string> evt)
    {
        var dropdown = evt.target as DropdownField;
        OutputPNG = dropdown.index;
    }

    private void AutoOpenMethod(ClickEvent evt)
    {
        var auto = evt.target as Toggle;
        AutoOpen = auto.value;
    }

    private void Capture(ClickEvent evt)
    {
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        CheckFolderValidity();

        string path = Path + "/" + FormatFileName(DateFormats[DateFormat]) + "." + (OutputPNG == 0 ? "png" : "jpg");
        Debug.Log(path);

       // ScreenCapture.CaptureScreenshot(path);
        CaptureScreenshot(path);
    }

    private void CaptureScreenshot(string path)
    {
        Texture2D screenshot;
        Camera cam = Camera.main;
        if (cam != null)
        {
            var tmp = RenderTexture.GetTemporary(cam.pixelWidth, cam.pixelHeight);
            var cache = cam.targetTexture;
            RenderTexture.active = tmp;
            cam.targetTexture = tmp;
            cam.Render();

            screenshot = new Texture2D(tmp.width, tmp.height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);

            RenderTexture.active = null;

            cam.targetTexture = cache;
            tmp.Release();
        }
        else
        {
            screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        }
        screenshot.Apply();

        byte[] bytes = OutputPNG == 0 ? screenshot.EncodeToPNG() : screenshot.EncodeToJPG();
        File.WriteAllBytes(path, bytes);

        if (AutoOpen)
        {
            Application.OpenURL(Path);
        }
    }

    [SettingsProvider]
    public static SettingsProvider ScreenshotSettings()
    {
        var provider = new SettingsProvider("Preferences/ScreenShot", SettingsScope.User);
        provider.label = "ScreenShot";
        provider.guiHandler = (searchContent) =>
        {
            EditorGUILayout.Space();

            DateFormat = EditorGUILayout.Popup("Date format", DateFormat, DateFormats, GUILayout.MaxWidth(250f));
            TimeFormat = EditorGUILayout.Popup("Time format", TimeFormat, TimeFormats, GUILayout.MaxWidth(250f));
            EditorGUILayout.LabelField("Filename example: " + FormatFileName(DateFormats[DateFormat]), EditorStyles.miniLabel);

            EditorGUILayout.Space();
        };

        return provider;
    }

    private static string FormatFileName(string dateFormat)
    {
        string filename = "Screenshot_";
        filename += System.DateTime.Now.ToString(dateFormat).Replace("-", ".") + "_";
        filename += System.DateTime.Now.ToString(TimeFormat == 0 ? "HH-mm-ss" : "hh-mm-ss tt") + "_";
        filename += System.DateTime.Now.Ticks.ToString();
        return filename;
    }
}
