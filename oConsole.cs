///
/// To use : 
/// 
/// - Attach it to whatever you want, camera, empty object, etc.
/// 
/// - Start calling console methods (See "Console loggin functions" region to see expected to be called methods)
///     oConsole.instance.SetString(...) // for fixed string at the view top
///     oConsole.instance.WriteString(...) // for log style strings
///     
/// - Or check HookUnityConsole and start to call standar Unity log functions Debug.Log, Debug.LogWarning, etc.
/// 
/// - Or mix both type of calls
///     
/// docs:
///     https://github.com/MattRix/UnityDecompiled/blob/master/UnityEngine/UnityEngine/GUISkin.cs
///     https://gist.github.com/975374
///     
/// TODO:
///     2. Coloring log??
///
using System.Collections.Generic;
using UnityEngine;

namespace oIndieUnity
{

    public class oConsole : MonoBehaviour
    {

        #region Singleton Instance
        /// <summary>
        /// Singleton partern without Awake() 
        /// https://github.com/prime31/TransitionKit
        /// </summary>
        private static oConsole _instance;
        public static oConsole instance
        {
            get
            {
                if (!_instance)
                {
                    // check if there is a oConsole instance already available in the scene graph before creating one
                    _instance = FindObjectOfType(typeof(oConsole)) as oConsole;

                    if (!_instance)
                    {
                        var obj = new GameObject("oConsole");
                        obj.transform.position = new Vector3(0f, 0f, 0f);
                        _instance = obj.AddComponent<oConsole>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return _instance;
            }
        }
        #endregion

        // Editor
        public float X;                                           // Console X position
        public float Y;                                           // Console Y position
        public float Width;                                       // Console width
        public float Height;                                      // Console heeight
        [Tooltip("Max number of free strings stored")]
        [Range(1, 1000)]
        public int MaxFreeLines = 100;                            // Max number of free strings stored
        [Range(4, 18)]
        public int FontSize;                                      // Font Size
        public Color FontColor;                                   // Font color
        public Color ConsoleColor;                                // Font color
        [Tooltip("Optional (can leave empty)")]
        public Font ConsoleFont;                                  // Console font
        [Tooltip("If set shows standard unity logs")]
        public bool HookUnityConsole = true;                      // If set If set shows standard unity logs Debug.LogXXX
        [Tooltip("Max number of unity log lines stored")]
        [Range(1, 1000)]
        public int MaxUnityLines = 100;                           // Max number of unity log lines stored


        #region internal
        private Dictionary<string, string> _consoleFixedStrings;  // Strings with fixed position 
        private List<string> _consoleFreeLines;                   // Strings with free position
        private bool printId;                                     // do we print string ID or only value
        private bool toggleConsole = true;                        // Is console ON
        private Rect consoleRect;                                 // Whole console area
        private Rect dragRect;                                    // Rectangle to detect drag area
        private Texture2D BackGround;                             // 1x1 window background texture, necessary for alpha effect
        private Texture2D ResizeCursor;                           // 32x32 texture for mouse hint cursor when over resize widget
        private Texture2D ResizerTexture;                         // 1x1 texture for resizer widget 
        private Vector2 mouseToGUIPoint;                          // stores mouse position trasnlated to GUI
        private bool resizing;                                    // are we resizing ?
        private bool inWidget;                                    // is the mouse in resize widget ?
        private KeyCode ToggleKey = KeyCode.Backslash;            // 
        private int consoleWindowID = 10102014;                   // 
        private string consoleWindowTitle = "";                   //
        private GUISkin consoleGUISkin;                           // 
        private Vector2 scrollPos;                                // 
        private float resizerMargin = 10f;                        //
        private Rect widgetRect;                                  //
        private Rect tempRect;                                    // 
        #endregion

        #region Console loggin functions
        public void SetString(string idCadena, string cadena)
        {
            if (_consoleFixedStrings.ContainsKey(idCadena))
            {
                _consoleFixedStrings[idCadena] = cadena;
            }
            else
            {
                _consoleFixedStrings.Add(idCadena, cadena);
            }
        }
        public void SetString(string idCadena, object obj)
        {
            if (_consoleFixedStrings.ContainsKey(idCadena))
            {
                _consoleFixedStrings[idCadena] = obj.ToString();
            }
            else
            {
                _consoleFixedStrings.Add(idCadena, obj.ToString());
            }
        }
        public void ClearString(string idCadena)
        {
            _consoleFixedStrings.Remove(idCadena);
        }
        public void ClearStrings()
        {
            _consoleFixedStrings.Clear();
        }
        public void WriteString(string s)
        {
            if (_consoleFreeLines.Count > MaxFreeLines)
                _consoleFreeLines.Clear();
            _consoleFreeLines.Add(s);
        }
        public void ClearFreeLines()
        {
            _consoleFreeLines.Clear();
        }
        #endregion

        #region Background Textures
        private void CreateBackgroundTexture()
        {
            // Create a new texture ARGB32 (32 bit with alpha) and no mipmaps
            BackGround = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            //Color C = new Color(.3f, .3f, .3f, .80f);
            BackGround.SetPixel(0, 0, ConsoleColor);
            BackGround.Apply();
        }
        private void CreateResizerTexture()
        {
            // Create a new 1x1 texture ARGB32 (32 bit with alpha) and no mipmaps
            ResizerTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            ResizerTexture.SetPixel(0, 0, Color.black);
            ResizerTexture.Apply();
        }
        private void CreateMouseCursorTexture()
        {
            int w = 32;
            int h = 32;
            ResizeCursor = new Texture2D(w, h, TextureFormat.ARGB32, false);
            // Transparent
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    ResizeCursor.SetPixel(i, j, Color.clear);
                }
            }
            // circle
            double radius = 8;
            for (double i = 0; i < 360f; i++)
            {
                double angle = i * System.Math.PI / 180;
                int x = (int)(16 + radius * System.Math.Cos(angle));
                int y = (int)(16 + radius * System.Math.Sin(angle));
                ResizeCursor.SetPixel(x, y, Color.magenta);
            }
            ResizeCursor.Apply();
        }
        #endregion

        private void CreateLayout(int windowID)
        {
            consoleGUISkin.label.normal.textColor = FontColor;

            // resize widget
            Rect trect = new Rect(consoleRect.width - 4, consoleRect.height - 4, consoleRect.width, consoleRect.height);
            GUI.DrawTexture(trect, ResizerTexture, ScaleMode.StretchToFill, true);
            // Scroll view
            scrollPos = GUILayout.BeginScrollView(scrollPos, consoleGUISkin.scrollView);

            // fixed lines
            foreach (KeyValuePair<string, string> cadena in _consoleFixedStrings)
            {
                string consoleString;
                consoleString = printId ? cadena.Key + cadena.Value : cadena.Value;
                GUILayout.Label(consoleString, consoleGUISkin.label);
            }

            // Free lines
            for (int i = 0; i < _consoleFreeLines.Count; i++)
            {
                GUILayout.Label(_consoleFreeLines[i], consoleGUISkin.label);
            }

            // Unity hook
            if (HookUnityConsole)
            {
                for (int i = 0; i < logs.Count; i++)
                {
                    consoleGUISkin.label.normal.textColor = logTypeColors[logs[i].type];
                    GUILayout.Label(logs[i].message, consoleGUISkin.label);
                }
            }

            GUILayout.EndScrollView();
            GUI.DragWindow(dragRect);
        }

        public void Awake()
        {
            printId = true;
            // String containers (don't move from here if you want call console in other MonoBehaviours Start() methods)
            _consoleFixedStrings = new Dictionary<string, string>();
            _consoleFreeLines = new List<string>(MaxFreeLines);
        }

        void OnEnable()
        {
            consoleRect = new Rect(X, Y, Width, Height);

            // This code is specific to hook unity console
            if (HookUnityConsole)
                Application.logMessageReceived += HandleLog;
        }

        void OnDisable()
        {
            // This code is specific to hook unity console
            if (HookUnityConsole)
                Application.logMessageReceived -= HandleLog;
        }

        public void Start()
        {
            // Textures
            CreateBackgroundTexture();
            CreateMouseCursorTexture();
            CreateResizerTexture();
            // Skin
            consoleGUISkin = ScriptableObject.CreateInstance<GUISkin>();
            // window
            consoleGUISkin.window.padding = new RectOffset(0, 1, 0, 1);
            consoleGUISkin.window.alignment = TextAnchor.UpperLeft;
            consoleGUISkin.window.clipping = TextClipping.Overflow;
            consoleGUISkin.window.stretchWidth = false;
            consoleGUISkin.window.stretchHeight = false;
            consoleGUISkin.window.normal.background = BackGround;
            // ScrollView
            consoleGUISkin.scrollView.padding = new RectOffset(1, 0, 1, 0);
            consoleGUISkin.scrollView.wordWrap = false;
            // labels
            consoleGUISkin.label.fontSize = FontSize;
            consoleGUISkin.label.stretchWidth = false;
            consoleGUISkin.label.stretchHeight = false;
            consoleGUISkin.label.normal.textColor = FontColor;
            if (ConsoleFont != null)
                consoleGUISkin.label.font = ConsoleFont;
        }

        public void OnGUI()
        {
            if (!toggleConsole)
                return;
            mouseToGUIPoint = GUIUtility.ScreenToGUIPoint(Event.current.mousePosition);
            inWidget = widgetRect.Contains(mouseToGUIPoint);

            // Cursor hint
            if (inWidget)
                GUI.DrawTexture(new Rect(mouseToGUIPoint.x - (32 / 2), mouseToGUIPoint.y - (32 / 2), 32, 32), ResizeCursor);

            // if resizing then resize
            if (resizing)
            {
                Width = mouseToGUIPoint.x - X;
                Height = mouseToGUIPoint.y - Y;
            }

            // if changed since last OnGui recalculate all ...
            tempRect = new Rect(X, Y, Width, Height);
            if (tempRect != consoleRect)
            {
                Width = Width < 20 ? 20 : Width; // min width
                Height = Height < 20 ? 20 : Height; // min height
                consoleRect = new Rect(X, Y, Width, Height);
            }
            dragRect = new Rect(0, 0, Width - resizerMargin, Height - resizerMargin);
            consoleRect = GUI.Window(consoleWindowID, consoleRect, CreateLayout, consoleWindowTitle, consoleGUISkin.window);
            widgetRect = new Rect(X + consoleRect.width - resizerMargin, Y + consoleRect.height - resizerMargin, 16, 16);

            // save for next round
            X = consoleRect.x;
            Y = consoleRect.y;
            Width = consoleRect.width;
            Height = consoleRect.height;
        }

        public void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
                toggleConsole = !toggleConsole;
            if (toggleConsole)
            {
                if (resizing)
                    resizing = Input.GetMouseButton(0); // once we are resizing cancel resize only when button 0 is released
                else
                    resizing = inWidget && Input.GetMouseButton(0); // we start resizing when button 0 down in resize widget area
            }
        }

        #region This code is specific to hook unity console
        private List<Log> logs = new List<Log>();
        private static readonly Dictionary<LogType, Color> logTypeColors = new Dictionary<LogType, Color>()
        {
            { LogType.Log, Color.white},
            { LogType.Warning, Color.yellow },
            { LogType.Assert, Color.green },
            { LogType.Error, Color.red },
            { LogType.Exception, Color.red },
        };
        private struct Log
        {
            public string message;
            public string stackTrace;
            public LogType type;
        }
        void HandleLog(string message, string stackTrace, LogType type)
        {

            if (logs.Count > MaxUnityLines)
                logs.Clear();

            logs.Add(new Log()
            {
                message = message,
                stackTrace = stackTrace,
                type = type,
            });
        }
        #endregion
    }
}
