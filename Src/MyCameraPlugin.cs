/*
Copyright 2019 LIV inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software 
and associated documentation files (the "Software"), to deal in the Software without restriction, 
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FriesBSCameraPlugin.Camera;
using UnityEngine;
using CameraType = FriesBSCameraPlugin.Camera.CameraType;

[assembly: AssemblyVersion("1.3.0.0")]
[assembly: AssemblyFileVersion("1.3.0.0")]

namespace FriesBSCameraPlugin
{
    public class MyCameraPluginSettings : IPluginSettings
    {
    };

// The class must implement IPluginCameraBehaviour to be recognized by LIV as a plugin.
    public class MyCameraPlugin : IPluginCameraBehaviour
    {
        private readonly MyCameraPluginSettings _settings = new MyCameraPluginSettings();
        public IPluginSettings settings => _settings;

        // Invoke ApplySettings event when you need to save your settings.
        // Do not invoke event every frame if possible.
        public event EventHandler ApplySettings;
        private BeatSaberStatus _beatSaberStatus;

        // ID is used for the camera behaviour identification when the behaviour is selected by the user.
        // It has to be unique so there are no plugin collisions.
        public string ID => "FriesBSCameraPlugin";
        // Readable plugin name "Keep it short".
        public string name => "Fries BS Camera";
        // Author name.
        public string author => "fries";
        // Plugin version.
        public string version => "1.3.0";

        // Locally store the camera helper provided by LIV.
        private PluginCameraHelper _helper;
        private float _elapsedTime;
        private float _timeSinceSceneStarted;
        private float _nextChangeTimer;
        private bool _useHttpStatus;
        private bool _inMenu;
        private bool _inGame;

        private CameraType _currentCameraType = CameraType.Orbital;
        private CameraData _currentCameraData;
        private readonly System.Random _rand = new System.Random();

        private Vector3 _currentCameraPosition = Vector3.zero;
        private Quaternion _currentCameraRotation = Quaternion.identity;

        private float _currentOrbitalAngle;
        private float _orbitalDirection = 1.0f;
        private float _currentOrbitalHeight = 1.0f;
        private float _orbitalHeightTarget = 1.0f;
        private float _currentOrbitalDistance = 1.0f;
        private float _orbitalDistance = 1.0f;
        private int _currentCameraIndex;

        private CameraTransition _currentCameraTransition = new CameraTransition
        {
            originPosition = Vector3.zero,
            originRotation = Quaternion.identity,
            targetPosition = Vector3.zero,
            targetRotation = Quaternion.identity,
        };

        private float _cameraLerpValue = 0.02f;

        private readonly List<int> _previousCameraIndices = new List<int>();

        private static StreamWriter _logStream;

        private int _frameLogAttemptCounter;


        // Constructor is called when plugin loads
        public MyCameraPlugin() { }

        // OnActivate function is called when your camera behaviour was selected by the user.
        // The pluginCameraHelper is provided to you to help you with Player/Camera related operations.
        public void OnActivate(PluginCameraHelper helper)
        {

            Application.logMessageReceived += LogError;

            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string outputLoc = Path.Combine(docPath, @"LIV\Plugins\CameraBehaviours\FriesBSCam\");

            // Create the directory in the off-chance that it doesn't exist.
            Directory.CreateDirectory(outputLoc);

            outputLoc = Path.Combine(outputLoc, "output.txt");
            _logStream = new StreamWriter(outputLoc);

            Log("Startup");

            Log("Loading Settings");
            CameraPluginSettings.LoadSettings();
            Log("Done Loading Settings");
            Log("Camera Count: " + CameraPluginSettings.CameraDataList.Count);

            _helper = helper;

            _beatSaberStatus = new BeatSaberStatus();

            _helper.UpdateFov(60.0f);
            UpdateCameraChange();

        }

        private void LogError(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error) return;
            Log(condition);
            Log(stackTrace);
        }

        private static void Log(string s)
        {
            _logStream.WriteLine(s);
            _logStream.Flush();
        }

        // OnSettingsDeserialized is called only when the user has changed camera profile or when the.
        // last camera profile has been loaded. This overwrites your settings with last data if they exist.
        public void OnSettingsDeserialized()
        {
        }

        // OnFixedUpdate could be called several times per frame. 
        // The delta time is constant and it is ment to be used on robust physics simulations.
        public void OnFixedUpdate()
        {
        }

        private void UpdateCameraChange()
        {
            var transitionToMenu = false;

            // Test if we're in menu/game, or some combination thereof as we may want to do different things based on that
            // This will always be inMenu = false and inGame = false if there is no HTTPStatus available
            // inMenu = true and inGame = true occurs when we first load HTTPStatus which should transition you to menu view
            if (_inMenu)
            {
                if (_inGame)
                {
                    Log("Transitioning to inMenu");
                    _inGame = false;
                    _elapsedTime = 2.0f;
                    _nextChangeTimer = 1.0f;
                    transitionToMenu = true;

                    // If we previously were in a Song Specific settings file, go back to the default settings file
                    // if (CameraPluginSettings.SongSpecific) // try loading the setting each time we go into the menu - they can become corrupt when beat saber restarts
                    {
                        Log("Loading up default settings file");
                        CameraPluginSettings.LoadSettings();
                        Log("Done Loading Settings");
                        Log("New Camera Count: " + CameraPluginSettings.CameraDataList.Count);
                    }
                }
                else
                {
                    // We're still in the menu so reset the timer
                    _elapsedTime = 0.0f;
                    _nextChangeTimer = 1.0f;
                }
            }
            else
            {
                if (!_inGame)
                {
                    Log("Transitioning to inGame");
                    _inGame = true;
                    _elapsedTime = 2.0f;
                    _nextChangeTimer = 1.0f;

                    if (_useHttpStatus)
                    {
                        // If HTTPStatus is available, see if we can extract some of the song details
                        Log("Song Name: " + _beatSaberStatus.map.SongAuthorName);
                        Log("Song SubName: " + _beatSaberStatus.map.SongSubName);
                        Log("Song AuthorName: " + _beatSaberStatus.map.SongAuthorName);
                        Log("Level Author: " + _beatSaberStatus.map.LevelAuthorName);
                        Log("Song Hash: " + _beatSaberStatus.map.SongHash);
                        Log("Level ID: " + _beatSaberStatus.map.LevelId);

                        Log("Other BSS Data:");
                        Log("score: " + _beatSaberStatus.score);
                        Log("currentMaxScore: " + _beatSaberStatus.paused);
                        Log("connected: " + _beatSaberStatus.connected);
                        Log("menu: " + _beatSaberStatus.menu);

                        // We need to check to see if there is a song-specific settings file, and if so, load that instead
                        Log("Checking to see if there are song-specific Settings");

                        if (_beatSaberStatus.map.SongHash != null && _beatSaberStatus.map.SongHash != "")
                        {
                            CameraPluginSettings.LoadSettings(_beatSaberStatus.map.SongHash + ".txt");
                        }

                        if (CameraPluginSettings.SongSpecific)
                        {
                            Log("Song specific settings found!");
                            Log("New Camera Count: " + CameraPluginSettings.CameraDataList.Count);
                        }
                        else
                            Log("No song specific settings file found");
                    }
                }
            }

            if (_useHttpStatus && _beatSaberStatus.paused)
            {
                //We're paused, don't do anything
            }
            else if (_elapsedTime >= _nextChangeTimer || transitionToMenu)
                TransitionToNextCamera(transitionToMenu);
        }


        // OnUpdate is called once every frame and it is used for moving with the camera so it can be smooth as the framerate.
        // When you are reading other transform positions during OnUpdate it could be possible that the position comes from a previus frame
        // and has not been updated yet. If that is a concern, it is recommended to use OnLateUpdate instead.
        public void OnUpdate()
        {
            // Allows us to track when HTTPStatus first makes a connecction
            if (!_useHttpStatus && _beatSaberStatus.connected)
            {
                _useHttpStatus = true;
                _inMenu = true;
                _inGame = true;
            }

            if (_useHttpStatus)
            {
                if (CameraPluginSettings.Debug && _beatSaberStatus.debug.Count > 0)
                {
                    // Write debug messages from HTTPStatus
                    Log("BSS: " + _beatSaberStatus.debug[0]);
                    _beatSaberStatus.debug.RemoveAt(0);
                }

                if (_beatSaberStatus.menu)
                {
                    // This is a silly way to do this but it allows us to keep track of when we transition between menu and game mode
                    // That still seems silly, but when juggling random songs, and song-specific camera update files it is helpful. Maybe.
                    _inMenu = true;
                }
                else
                {
                    _inMenu = false;

                    // Don't start the timer until we hit our first note, also check for pause
                    // This allows us to have 'synced' song-specific camera files as different systems will load songs at different speeds
                    // based on storage and cpu performance. Just...don't miss the first note I guess?
                    if ((_beatSaberStatus.score > 0 && !_beatSaberStatus.paused) || _beatSaberStatus.map.SongHash == null || _beatSaberStatus.map.SongHash == "")
                    {
                        _elapsedTime += Time.deltaTime;
                    }
                }
            }
            else
            {
                // No HTTPStatus available so always increment the timer
                _elapsedTime += Time.deltaTime;
                _inMenu = false;
            }

            // Moving _timeSinceSceneStarted out here to always update, as opposed to only updating when either inGame or when HTTPStatus isn't used
            // As otherwise it seems to break transitioning into the menu
            _timeSinceSceneStarted += Time.deltaTime;
            UpdateCameraPose();
        }

        /**
     * Called on every tick to update the camera transform and configuration
     */
        private void UpdateCameraPose()
        {
            switch (_currentCameraType)
            {
                case CameraType.Orbital:
                {
                    Vector3 targetPosition = _currentCameraData.EvaluatePositionBinding(_helper);
                    Vector3 targetLookAtPosition = _currentCameraData.EvaluateLookAtBindingBinding(_helper);
                    _orbitalHeightTarget = targetPosition.y;

                    if (_inMenu || !_currentCameraData.ReleaseBehindPlayer || Mathf.Sin(_currentOrbitalAngle) < -0.5f)
                    {
                        UpdateCameraChange();
                    }

                    var blendSpeed = (_cameraLerpValue - 0.02f) / (0.2f - 0.02f);
                    _currentOrbitalAngle += Time.deltaTime * _orbitalDirection * _currentCameraData.Speed * blendSpeed;

                    Vector3 rotationVector = new Vector3(
                        Mathf.Cos(_currentOrbitalAngle),
                        0f,
                        Mathf.Sin(_currentOrbitalAngle));

                    _currentCameraTransition.targetPosition = targetPosition + rotationVector * _currentOrbitalDistance;
                    _currentCameraTransition.targetPosition.y = _currentOrbitalHeight;
                    _currentCameraTransition.targetRotation = Quaternion.LookRotation((targetLookAtPosition - _currentCameraTransition.targetPosition).normalized);
                    break;
                }
                case CameraType.LookAt:
                {
                    UpdateCameraChange();
                    Vector3 targetPosition = _currentCameraData.EvaluatePositionBinding(_helper);
                    _currentCameraTransition.targetPosition = targetPosition;
                    _currentCameraTransition.targetRotation = Quaternion.LookRotation(_currentCameraData.LookAt);
                    break;
                }
            }

            var biasAxis = _currentCameraTransition.targetRotation * Vector3.up;
            var biasQuat = Quaternion.AngleAxis(CameraPluginSettings.GlobalBias, biasAxis);
            _currentCameraTransition.targetRotation = biasQuat * _currentCameraTransition.targetRotation;

            BlendCameraPose();
        }

        private void BlendCameraPose()
        {
            if (_currentCameraType == CameraType.Orbital)
            {
                var targetLerpBlendTime = 10.0f;
                targetLerpBlendTime = 1.0f / targetLerpBlendTime;
                var targetLerp = 0.2f;
                var direction = (targetLerp - _cameraLerpValue) > 0.0f ? targetLerpBlendTime : -targetLerpBlendTime;
                _cameraLerpValue += direction * Time.deltaTime;

                _currentOrbitalHeight = _currentOrbitalHeight * (1.0f - _cameraLerpValue) + _orbitalHeightTarget * _cameraLerpValue;
                _currentOrbitalDistance = _currentOrbitalDistance * (1.0f - _cameraLerpValue) + _orbitalDistance * _cameraLerpValue;
            }

            PositionAndRotation frameCameraPose = _currentCameraTransition.GetInterTransitionPositionAndRotation(_timeSinceSceneStarted);
            UpdateGameCameraPose(frameCameraPose.position, frameCameraPose.rotation);
        }

        // OnLateUpdate is called after OnUpdate also everyframe and has a higher chance that transform updates are more recent.
        public void OnLateUpdate() {

        }

        // OnDeactivate is called when the user changes the profile to other camera behaviour or when the application is about to close.
        // The camera behaviour should clean everything it created when the behaviour is deactivated.
        public void OnDeactivate() => ApplySettings?.Invoke(this, EventArgs.Empty);

        // OnDestroy is called when the users selects a camera behaviour which is not a plugin or when the application is about to close.
        // This is the last chance to clean after your self.
        public void OnDestroy() {

        }

        private void TransitionToNextCamera(bool transitionToMenu)
        {
            _timeSinceSceneStarted = 0;

            _cameraLerpValue = 0.02f;
            Log("New Camera Selection started");

            var newCameraIndex = _currentCameraIndex;

            if (transitionToMenu)
            {
                // Transition to the Menu Camera and mark that we've done so. Update currentCameraIndex to -1 so we don't skip over Index 0 when we move to another camera
                Log("Preparing to change to Menu Camera");
                transitionToMenu = false;
                _currentCameraIndex = -1;
                _currentCameraData = CameraPluginSettings.MenuCamera;
                _currentCameraType = _currentCameraData.Type;
                Log(_currentCameraData.ToString());
            }
            else
            {
                Log("Preparing to change to new Game Camera, currentCameraIndex = " + _currentCameraIndex.ToString());
                if (CameraPluginSettings.SongSpecific)
                {
                    Log("Song specific settings file active, increment to next camera");
                    // Increment camera by 1 but make sure we don't skip over index 0 now
                    if (newCameraIndex < CameraPluginSettings.CameraDataList.Count)
                        newCameraIndex++;
                }
                else
                {
                    Log("Default settings file active, preparing to pick next camera");
                    // If we don't have enough cameras to truly randomize...uh...I guess don't
                    if (CameraPluginSettings.CameraDataList.Count > 2)
                    {
                        // Don't pick the same camera again for a few times
                        while (_previousCameraIndices.Contains(newCameraIndex) || (_currentCameraType == CameraType.Orbital && CameraPluginSettings.CameraDataList[newCameraIndex].Type == CameraType.Orbital))
                        {
                            newCameraIndex = _rand.Next() % CameraPluginSettings.CameraDataList.Count;
                        }
                    }
                    else if (CameraPluginSettings.CameraDataList.Count == 2)
                    {
                        Log("Only 2 cameras available, swapping between them");
                        // If you've got two cameras switch between them, otherwise just stick with the current one
                        switch (_currentCameraIndex)
                        {
                            case 0:
                                newCameraIndex = 1;
                                break;
                            case 1:
                                newCameraIndex = 0;
                                break;
                            default:
                                newCameraIndex = 0;
                                break;
                        }
                    }
                    else
                    {
                        newCameraIndex = 0;
                    }
                }

                if (_previousCameraIndices.Count > 1)
                    _previousCameraIndices.RemoveAt(0);

                _previousCameraIndices.Add(newCameraIndex);

                _currentCameraIndex = newCameraIndex;
                _currentCameraData = CameraPluginSettings.CameraDataList[_currentCameraIndex];
                _currentCameraData.ResetSmoothing(_helper);
                _currentCameraType = _currentCameraData.Type;

                if (CameraPluginSettings.SongSpecific)
                {
                    _nextChangeTimer = _elapsedTime + _currentCameraData.ActualTime;
                }
                else
                {
                    var minTime = _currentCameraData.MinTime;
                    var maxTime = _currentCameraData.MaxTime;
                    _nextChangeTimer = _elapsedTime + minTime + (float)(_rand.NextDouble() * (maxTime - minTime));
                }

                Log("New Camera: " + newCameraIndex + ", " + _currentCameraData.Name + ", " + _currentCameraData.Type + ", " + _nextChangeTimer + "s");
                Log(_timeSinceSceneStarted + "s since last scene change.");
            }

            switch (_currentCameraType)
            {
                case CameraType.Orbital:
                {
                    Vector3 targetPosition = _currentCameraData.EvaluatePositionBinding(_helper);
                    var direction = _currentCameraPosition - targetPosition;
                    direction.y = 0.0f;
                    direction.Normalize();

                    if ((_currentCameraPosition.x < targetPosition.x && _currentCameraData.Direction == OrbitalDirection.Dynamic)
                        || _currentCameraData.Direction == OrbitalDirection.Right)
                        _orbitalDirection = -1;
                    else if ((_currentCameraPosition.x > targetPosition.x && _currentCameraData.Direction == OrbitalDirection.Dynamic)
                             || _currentCameraData.Direction == OrbitalDirection.Left)
                        _orbitalDirection = 1;
                    else
                        _orbitalDirection = ((_rand.Next() & 1) == 1) ? 1.0f : -1.0f;

                    var angle = (float)Math.Atan2(direction.z, direction.x);
                    _currentOrbitalAngle = angle;
                    _currentOrbitalHeight = _currentCameraPosition.y;
                    _orbitalHeightTarget = targetPosition.y;
                    _currentOrbitalDistance = (_currentCameraPosition - targetPosition).magnitude;
                    _orbitalDistance = _currentCameraData.Distance;

                    Log("orbitalDirection: " + _orbitalDirection);
                    Log("currentOrbitalHeight: " + _currentOrbitalHeight);
                    Log("orbitalHeightTarget: " + _orbitalHeightTarget);
                    Log("currentOrbitalDistance: " + _currentOrbitalDistance);
                    Log("orbitalDistance: " + _orbitalDistance);
                    Log("currentOrbitalAngle: " + _currentOrbitalAngle);

                    break;
                }
                case CameraType.LookAt:
                {
                    break;
                }
            }

            _currentCameraTransition = new CameraTransition
            {
                originPosition = _currentCameraPosition,
                originRotation = _currentCameraRotation,

                //NOTE:  These two seem to be updated every tick in UpdateCameraPose.  I don't think that's a bad thing if this is still the case.
                targetPosition = _currentCameraTransition.targetPosition,
                targetRotation = _currentCameraTransition.targetRotation,
                transitionDuration = _currentCameraData.TransitionTime,
                transitionCurveType = _currentCameraData.TransitionCurve,
            };

            Log("New Camera Transition:" + _currentCameraTransition);
        }

        private void UpdateGameCameraPose(Vector3 position, Quaternion rotation)
        {
            _currentCameraPosition = position;
            _currentCameraRotation = rotation;
            _helper.UpdateCameraPose(position, rotation);
            _frameLogAttemptCounter += 1;
            if (_frameLogAttemptCounter % 30 != 0) return;
        
            Log("\tScene Time: " + Math.Round(_timeSinceSceneStarted, 2) );
            Log("\tElapsedTime Time: " + Math.Round(_elapsedTime, 2) );
            Log("\tNextChangeTimer: " + Math.Round(_nextChangeTimer, 2) );
            Log("\tPosition: " + position + "\n\tRotation: " + rotation);
            Log("\tPaused: " + (_useHttpStatus && _beatSaberStatus.paused) + "\n");
        }
    }
}