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

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.IO;

public class MyCameraPluginSettings : IPluginSettings
{
};

// The class must implement IPluginCameraBehaviour to be recognized by LIV as a plugin.
public class MyCameraPlugin : IPluginCameraBehaviour
{
    MyCameraPluginSettings _settings = new MyCameraPluginSettings();
    public IPluginSettings settings => _settings;

    // Invoke ApplySettings event when you need to save your settings.
    // Do not invoke event every frame if possible.
    public event EventHandler ApplySettings;

    // ID is used for the camera behaviour identification when the behaviour is selected by the user.
    // It has to be unique so there are no plugin collisions.
    public string ID => "FriesBSCameraPlugin";
    // Readable plugin name "Keep it short".
    public string name => "Fries BS Camera";
    // Author name.
    public string author => "fries";
    // Plugin version.
    public string version => "1.0";

    // Locally store the camera helper provided by LIV.
    PluginCameraHelper _helper;
    float _elaspedTime = 0.0f;
    float nextChangeTimer = 0.0f;

    CameraType currentCameraType = CameraType.Orbital;
    CameraData currentCameraData = null;
    System.Random rand = new System.Random();

    public Vector3 CurrentCameraPosition = Vector3.zero;
    public Quaternion CurrentCameraRotation = Quaternion.identity;

    public Vector3 TargetCameraPosition = Vector3.zero;
    public Quaternion TargetCameraRotation = Quaternion.identity;

    public float currentOrbitalAngle = 0.0f;
    public float orbitalDirection = 1.0f;
    public float currentOrbitalHeight = 1.0f;
    public float orbitalHeightTarget = 1.0f;
    public float currentOrbitalDistance = 1.0f;
    public float orbitalDistance = 1.0f;
    public int currentCameraIndex = 0;

    public float cameraLerpValue = 0.02f;

    public List<int> previousCameraIndices = new List<int>();

    public static StreamWriter logStream;

    // Constructor is called when plugin loads
    public MyCameraPlugin() { }

    // OnActivate function is called when your camera behaviour was selected by the user.
    // The pluginCameraHelper is provided to you to help you with Player/Camera related operations.
    public void OnActivate(PluginCameraHelper helper)
    {
        string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string outputLoc = Path.Combine(docPath, @"LIV\Plugins\CameraBehaviours\FriesBSCam\");

        // Create the directory in the off-chance that it doesn't exist.
        Directory.CreateDirectory(outputLoc);

        outputLoc = Path.Combine(outputLoc, "output.txt");
        logStream = new StreamWriter(outputLoc);

        Log("Startup");

        Log("Loading Settings");
        CameraPluginSettings.LoadSettings();
        Log("Done Loading Settings");

        _helper = helper;
        _helper.UpdateFov(60.0f);
        UpdateCameraChange();
    }

    public static void Log(string s)
    {
        if (true)
        {
            logStream.WriteLine(s);
            logStream.Flush();
        }
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

    public void UpdateCameraChange()
    {
        if (_elaspedTime >= nextChangeTimer)
        {
            cameraLerpValue = 0.02f;
            Log("New Camera Selection started");

            var newCameraIndex = currentCameraIndex;

            // Don't pick the same camera again for a few times
            while (previousCameraIndices.Contains(newCameraIndex) || (currentCameraType == CameraType.Orbital && CameraPluginSettings.CameraDataList[newCameraIndex].Type == CameraType.Orbital))
            {
                newCameraIndex = rand.Next() % CameraPluginSettings.CameraDataList.Count;
            }

            if (previousCameraIndices.Count > 1)
            {
                previousCameraIndices.RemoveAt(0);
            }

            previousCameraIndices.Add(newCameraIndex);
            currentCameraIndex = newCameraIndex;
            currentCameraData = CameraPluginSettings.CameraDataList[currentCameraIndex];
            currentCameraType = currentCameraData.Type;
            var minTime = currentCameraData.MinTime;
            var maxTime = currentCameraData.MaxTime;
            nextChangeTimer = _elaspedTime + minTime + (float)(rand.NextDouble() * (maxTime - minTime));

            Log("New Camera: " + newCameraIndex + ", " + currentCameraData.Name + ", " + currentCameraData.Type.ToString());

            switch (currentCameraType)
            {
                case CameraType.Orbital:
                    {
                        Vector3 targetPosition = currentCameraData.EvaluatePositionBinding(_helper);
                        var direction = CurrentCameraPosition - targetPosition;
                        direction.y = 0.0f;
                        direction.Normalize();

                        if (CurrentCameraPosition.x < targetPosition.x)
                        {
                            orbitalDirection = -1;
                        }
                        else if (CurrentCameraPosition.x > targetPosition.x)
                        {
                            orbitalDirection = 1;
                        }
                        else
                        {
                            orbitalDirection = ((rand.Next() & 1) == 1) ? 1.0f : -1.0f;
                        }

                        var angle = (float)Math.Atan2(direction.z, direction.x);
                        currentOrbitalAngle = angle;
                        currentOrbitalHeight = CurrentCameraPosition.y;
                        orbitalHeightTarget = targetPosition.y;
                        currentOrbitalDistance = (CurrentCameraPosition - targetPosition).magnitude;
                        orbitalDistance = currentCameraData.Distance;

                        Log("orbitalDirection: " + orbitalDirection);
                        Log("currentOrbitalHeight: " + currentOrbitalHeight);
                        Log("orbitalHeightTarget: " + orbitalHeightTarget);
                        Log("currentOrbitalDistance: " + currentOrbitalDistance);
                        Log("orbitalDistance: " + orbitalDistance);
                        Log("currentOrbitalAngle: " + currentOrbitalAngle);

                        break;
                    }
                case CameraType.LookAt:
                    {
                        break;
                    }
            }
        }
    }


    // OnUpdate is called once every frame and it is used for moving with the camera so it can be smooth as the framerate.
    // When you are reading other transform positions during OnUpdate it could be possible that the position comes from a previus frame
    // and has not been updated yet. If that is a concern, it is recommended to use OnLateUpdate instead.
    public void OnUpdate()
    {
        _elaspedTime += Time.deltaTime;
        UpdateCameraPose();
    }

    public void UpdateCameraPose()
    {
        switch (currentCameraType)
        {
             case CameraType.Orbital:
                {
                    Vector3 targetPosition = currentCameraData.EvaluatePositionBinding(_helper);
                    Vector3 targetLookAtPosition = currentCameraData.EvaluateLookAtBindingBinding(_helper);
                    orbitalHeightTarget = targetPosition.y;

                    // TODO: Change this test to be based on the currentOrbitalAngle
                    if (Mathf.Sin(currentOrbitalAngle) < -0.5f)
                    {
                        UpdateCameraChange();
                    }

                    var blendSpeed = (cameraLerpValue - 0.02f) / (0.2f - 0.02f);
                    currentOrbitalAngle += Time.deltaTime * orbitalDirection * currentCameraData.Speed * blendSpeed;

                    Vector3 rotationVector = new Vector3(
                        Mathf.Cos(currentOrbitalAngle), 
                        0f, 
                        Mathf.Sin(currentOrbitalAngle));

                    TargetCameraPosition = targetPosition + rotationVector * currentOrbitalDistance;
                    TargetCameraPosition.y = currentOrbitalHeight;
                    TargetCameraRotation = Quaternion.LookRotation((targetLookAtPosition - TargetCameraPosition).normalized);
                    break;
                }
            case CameraType.LookAt:
                {
                    UpdateCameraChange();
                    Vector3 targetPosition = currentCameraData.EvaluatePositionBinding(_helper);
                    TargetCameraPosition = targetPosition;
                    TargetCameraRotation = Quaternion.LookRotation(currentCameraData.LookAt);
                    break;
                }
        }

        var biasAngle = CameraPluginSettings.GlobalBias * (float)Math.PI / 180.0f;
        TargetCameraRotation = Quaternion.EulerAngles(0.0f, biasAngle, 0.0f) * TargetCameraRotation;

        BlendCameraPose();
    }

    void BlendCameraPose()
    {
        float distance = (CurrentCameraPosition - TargetCameraPosition).magnitude;
        if (currentCameraType == CameraType.Orbital)
        {
            var targetLerpBlendTime = 10.0f;
            targetLerpBlendTime = 1.0f / targetLerpBlendTime;
            var targetLerp = 0.2f;
            var direction = (targetLerp - cameraLerpValue) > 0.0f ? targetLerpBlendTime : -targetLerpBlendTime;
            cameraLerpValue += direction * Time.deltaTime;
        }

        CurrentCameraRotation = Quaternion.Slerp(CurrentCameraRotation, TargetCameraRotation, cameraLerpValue);
        CurrentCameraPosition = CurrentCameraPosition * (1.0f - cameraLerpValue) + TargetCameraPosition * cameraLerpValue;

        currentOrbitalHeight = currentOrbitalHeight * (1.0f - cameraLerpValue) + orbitalHeightTarget * cameraLerpValue;
        currentOrbitalDistance = currentOrbitalDistance * (1.0f - cameraLerpValue) + orbitalDistance * cameraLerpValue;

        _helper.UpdateCameraPose(CurrentCameraPosition, CurrentCameraRotation);
    }

    // OnLateUpdate is called after OnUpdate also everyframe and has a higher chance that transform updates are more recent.
    public void OnLateUpdate() {

    }

    // OnDeactivate is called when the user changes the profile to other camera behaviour or when the application is about to close.
    // The camera behaviour should clean everything it created when the behaviour is deactivated.
    public void OnDeactivate() {
        ApplySettings?.Invoke(this, EventArgs.Empty);
    }

    // OnDestroy is called when the users selects a camera behaviour which is not a plugin or when the application is about to close.
    // This is the last chance to clean after your self.
    public void OnDestroy() {

    }
}
