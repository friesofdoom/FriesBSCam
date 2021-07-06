using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace FriesBSCameraPlugin.Camera
{
    public enum CameraType
    {
        Orbital,
        LookAt,
        NumTypes,
    }

    public enum OrbitalDirection
    {
        Right,
        Left,
        Dynamic
    }

    public class CameraData
    {
        public string Name;
        public CameraType Type;
        public Vector3 PositionOffset;
        public Vector3 LookAt;
        public string PositionBinding;
        public string LookAtBinding;
        public float Distance;
        public float Speed;
        public float MinTime;
        public float MaxTime;
        public float ActualTime;
        public float TransitionTime;
        public CameraTransitionCurve TransitionCurve;
        public bool ReleaseBehindPlayer;
        public OrbitalDirection Direction = OrbitalDirection.Dynamic;

        private Vector3 _smoothedPositionBinding = Vector3.zero;
        private Vector3 _smoothedLookAtBinding = Vector3.zero;

        public Vector3 EvaluatePositionBinding(PluginCameraHelper helper)
        {
            var newPos = EvaluateBinding(helper, PositionBinding) + PositionOffset;

            const float filter = 0.05f;
            _smoothedPositionBinding = _smoothedPositionBinding * (1.0f - filter) + newPos * filter;
            return _smoothedPositionBinding;
        }

        public Vector3 EvaluateLookAtBindingBinding(PluginCameraHelper helper)
        {
            var newLookAt = EvaluateBinding(helper, LookAtBinding) + LookAt;

            const float filter = 0.05f;
            _smoothedLookAtBinding = _smoothedLookAtBinding * (1.0f - filter) + newLookAt * filter;
            return _smoothedLookAtBinding;
        }

        static Vector3 EvaluateBinding(PluginCameraHelper helper, string binding)
        {
            switch (binding)
            {
                case "playerWaist": return helper.playerWaist.position;
                case "playerRightHand": return helper.playerRightHand.position;
                case "playerLeftHand": return helper.playerLeftHand.position;
                case "playerRightFoot": return helper.playerRightFoot.position;
                case "playerLeftFoot": return helper.playerLeftFoot.position;
                case "playerHead": return helper.playerHead.position;
                default: return Vector3.zero;
            }
        }

        public void ResetSmoothing(PluginCameraHelper helper)
        {
            var newLookAt = EvaluateBinding(helper, LookAtBinding) + LookAt;
            var newPos = EvaluateBinding(helper, PositionBinding) + PositionOffset;

            _smoothedLookAtBinding = newLookAt;
            _smoothedPositionBinding = newPos;
        }

        public override string ToString()
        {
            return "Camera: " + Name + ", Type: " + Type + ", PositionBinding: " + PositionBinding +
                   ", PositionOffset: "
                   + PositionOffset + ", LookAt: " + LookAt + ", MinTime: " + MinTime + ", MaxTime: "
                   + MaxTime + ", TransitionTime: " + TransitionTime + ", TransitionCurve: " + TransitionCurve;
        }
    }

    public static class CameraPluginSettings
    {
        public static float GlobalBias;
        public static bool SongSpecific;

        public static bool Debug;

//    public static float BlendSpeed;
        public static List<CameraData> CameraDataList;
        public static CameraData MenuCamera;

        /// <summary>
        /// Loads Default Settings File
        /// </summary>
        public static void LoadSettings()
        {
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string settingLoc = Path.Combine(docPath, @"LIV\Plugins\CameraBehaviours\FriesBSCam\");

            settingLoc = Path.Combine(settingLoc, "settings.txt");

            
            // Check to see if the file exists.
            if (!File.Exists(@settingLoc))
            {
                // Extract the default settings file.
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FriesBSCameraPlugin.Src.DefaultConfig.txt"))
                    using (var fs = new FileStream(settingLoc, FileMode.Create))
                        stream?.CopyTo(fs);
            }

            MenuCamera = new CameraData();
            CameraDataList = new List<CameraData>();
            ParseSettingsFile(@settingLoc);
            SongSpecific = false;
        }

        /// <summary>
        /// Loads a Specific Settings File
        /// </summary>
        /// <param name="settingsFile">Designed for song-specific settings - only checks the latter portion of the filename</param>
        public static void LoadSettings(string settingsFile)
        {
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string settingLoc = Path.Combine(docPath, @"LIV\Plugins\CameraBehaviours\FriesBSCam\");

            string[] files = Directory.GetFiles(settingLoc, "*" + settingsFile, SearchOption.TopDirectoryOnly);

            // Check to see if the file exists.
            if (files.Length <= 0) return;

            MenuCamera = new CameraData();
            CameraDataList = new List<CameraData>();
            ParseSettingsFile(files[0]);
            SongSpecific = true;
        }

        /// <summary>
        /// Parses the settings file and breaks out into appropriate...settings
        /// </summary>
        /// <param name="fileName">File to parse, full path needed</param>
        private static void ParseSettingsFile(string fileName)
        {
            // Open the file to read from.
            string readText = File.ReadAllText(fileName);

            var root = new ReflectionToken();
            AttributeParser parser = new AttributeParser(root);
            parser.Tokenize(readText);

            GlobalBias = ParseFloat(root.GetChildSafe("GlobalBias"));
            Debug = ParseBool(root.GetChildSafe("Debug"));
            //        BlendSpeed = ParseFloat(root.GetChildSafe("BlendSpeed"));

            // Adding support for a custom MenuCamera if it exists
            var menuCamera = root.GetChild("MenuCamera");
            if (menuCamera != null)
            {
                MenuCamera = ParseCamera(menuCamera);
            }
            else
            {
                // Moving Menu Camera 'backup' over here during settings parse time
                MenuCamera = new CameraData
                {
                    Name = "MenuCamera",
                    Type = CameraType.LookAt,
                    PositionBinding = "playerWaist",
                    PositionOffset = new Vector3(2.0f, 1.0f, -3.0f),
                    LookAt = new Vector3(-2.0f, 0.0f, 5.0f),
                    TransitionTime = 2.0f,
                    TransitionCurve = CameraTransitionCurve.Linear
                };
            }

            var camera = root.GetChild("Camera");
            while (camera != null)
            {
                CameraDataList.Add(ParseCamera(camera));
                camera = root.GetNextChild(camera);
            }
        }

        private static CameraData ParseCamera(ReflectionToken camera)
        {
            CameraData outCamera = new CameraData
            {
                Name = camera.GetChildSafe("Name").mValue,
                Type = GetCameraTypeFromToken(camera.GetChildSafe("Type")),
                PositionOffset = GetVector3Token(camera.GetChildSafe("PositionOffset")),
                LookAt = GetVector3Token(camera.GetChildSafe("LookAt")),
                PositionBinding = camera.GetChildSafe("PositionBinding").mValue,
                LookAtBinding = camera.GetChildSafe("LookAtBinding").mValue,
                Distance = ParseFloat(camera.GetChildSafe("Distance")),
                Speed = ParseFloat(camera.GetChildSafe("Speed")),
                MinTime = ParseFloat(camera.GetChildSafe("MinTime")),
                MaxTime = ParseFloat(camera.GetChildSafe("MaxTime")),
                ActualTime = ParseFloat(camera.GetChildSafe("ActualTime")),
                TransitionTime = ParseFloat(camera.GetChildSafe("TransitionTime")),
                TransitionCurve = GetTransitionCurveFromToken(camera.GetChildSafe("TransitionCurve")),
                ReleaseBehindPlayer = ParseBool(camera.GetChildSafe("ReleaseBehindPlayer")),
                Direction = GetOrbitalDirectionFromToken(camera.GetChildSafe("Direction"))
            };

            return outCamera;
        }


        private static bool ParseBool(ReflectionToken token) => bool.TryParse(token.mValue, out var outBool) && outBool;

        private static float ParseFloat(ReflectionToken token) =>
            float.TryParse(token.mValue, out var outFloat) ? outFloat : 0.0f;

        private static OrbitalDirection GetOrbitalDirectionFromToken(ReflectionToken token)
        {
            switch (token.mValue.ToLower())
            {
                case "right":
                    return OrbitalDirection.Right;
                case "left":
                    return OrbitalDirection.Left;
                case "dynamic":
                    return OrbitalDirection.Dynamic;
                default:
                    return OrbitalDirection.Dynamic;
            }
        }

        private static CameraType GetCameraTypeFromToken(ReflectionToken token)
        {
            switch (token.mValue.ToLower())
            {
                case "orbital":
                    return CameraType.Orbital;
                case "lookat":
                    return CameraType.LookAt;
                default:
                    return CameraType.LookAt;
            }
        }

        private static CameraTransitionCurve GetTransitionCurveFromToken(ReflectionToken token)
        {
            switch (token.mValue.ToLower())
            {
                case "easeoutcubic":
                    return CameraTransitionCurve.EaseOutCubic;
                case "easeinoutcubic":
                    return CameraTransitionCurve.EaseInOutCubic;
                case "linear":
                    return CameraTransitionCurve.Linear;
                default:
                    return CameraTransitionCurve.EaseOutCubic;
            }
        }

        private static Vector3 GetVector3Token(ReflectionToken token) =>
            new Vector3(ParseFloat(token.GetChildSafe("x")), ParseFloat(token.GetChildSafe("y")),
                ParseFloat(token.GetChildSafe("z")));
    }
}