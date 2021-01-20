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

// User defined settings which will be serialized and deserialized with Newtonsoft Json.Net.
// Only public variables will be serialized.
public class MyCameraPluginSettings : IPluginSettings {
    public float fov = 60f;
    public float distance = 4f;
    public float speed = 1f;
}

public enum CameraMode
{
    Obrital,
    Distance,
    NumModes,
};

// The class must implement IPluginCameraBehaviour to be recognized by LIV as a plugin.
public class MyCameraPlugin : IPluginCameraBehaviour {

    // Store your settings localy so you can access them.
    MyCameraPluginSettings _settings = new MyCameraPluginSettings();

    // Provide your own settings to store user defined settings .   
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
    float _elaspedTime;

    CameraMode cameraMode = CameraMode.Obrital;
    float nextChangeTimer = 0.0f;
    System.Random rand = new System.Random();

    float minChangeTime = 4.0f;
    float maxChangeTime = 8.0f;

    public Vector3 distanceOffset = Vector3.zero;
    public Vector3 distanceLookAt = Vector3.zero;

    public Vector3 CurrentCameraPosition = Vector3.zero;
    public Quaternion CurrentCameraRotation = Quaternion.identity;

    public Vector3 TargetCameraPosition = Vector3.zero;
    public Quaternion TargetCameraRotation = Quaternion.identity;

    public float orbitalAngleOffset = 0.0f;
    public float orbitalDirection = 1.0f;
    public float orbitalHeight = 1.0f;
    public int currentDistancePoint = 0;

    public float cameraLerpValue = 0.02f;

    public List<int> previousPoints = new List<int>();

    // Constructor is called when plugin loads
    public MyCameraPlugin() { }

    // OnActivate function is called when your camera behaviour was selected by the user.
    // The pluginCameraHelper is provided to you to help you with Player/Camera related operations.
    public void OnActivate(PluginCameraHelper helper) {
        _helper = helper;
        _helper.UpdateFov(_settings.fov);
    }

    // OnSettingsDeserialized is called only when the user has changed camera profile or when the.
    // last camera profile has been loaded. This overwrites your settings with last data if they exist.
    public void OnSettingsDeserialized() {

    }

    // OnFixedUpdate could be called several times per frame. 
    // The delta time is constant and it is ment to be used on robust physics simulations.
    public void OnFixedUpdate() {

    }

    public void UpdateCameraChange()
    {
        if (_elaspedTime >= nextChangeTimer)
        {
            cameraLerpValue = 0.02f;

            if (cameraMode == CameraMode.Obrital)
            {
                cameraMode = CameraMode.Distance;
            }
            else
            {
                cameraMode = rand.NextDouble() > 10.8 ? CameraMode.Obrital : CameraMode.Distance;
            }

            if (cameraMode == CameraMode.Obrital)
            {
                nextChangeTimer = _elaspedTime + minChangeTime * 2 + (float)(rand.NextDouble() * (maxChangeTime - minChangeTime * 2));
            }
            else
            {
                nextChangeTimer = _elaspedTime + minChangeTime + (float)(rand.NextDouble() * (maxChangeTime - minChangeTime));
            }

            switch (cameraMode)
            {
                case CameraMode.Obrital:
                    {
                        Transform headTransform = _helper.playerHead;
                        var direction = CurrentCameraPosition - headTransform.position;

                        var angle = (float)Math.Atan2(direction.z, direction.x);

                        if (currentDistancePoint == 0 || currentDistancePoint == 3)
                        {
                            orbitalDirection = 1;
                        }
                        else if (currentDistancePoint == 1 || currentDistancePoint == 4)
                        {
                            orbitalDirection = -1;
                        }
                        else
                        {
                            orbitalDirection = ((rand.Next() & 1) == 1) ? 1.0f : -1.0f;
                        }

                        orbitalAngleOffset = -(_elaspedTime + 0.15f) * orbitalDirection + angle;
                        orbitalHeight = CurrentCameraPosition.y;
                        break;
                    }
                case CameraMode.Distance:
                    {
                        Vector3[] offsets =
                        {
                            Vector3.right * 0.5f  -Vector3.forward * 2.0f +Vector3.up * 1.3f,
                                                  -Vector3.forward * 2.0f +Vector3.up * 2.0f,
                            -Vector3.right * 1.0f -Vector3.forward * 2.0f +Vector3.up * 1.3f,

                            Vector3.right * 1.0f -Vector3.forward * 2.0f,
                            -Vector3.right * 1.5f -Vector3.forward * 2.0f,
                        };

                        Vector3[] lookats =
                        {
                            -Vector3.right * 0.125f -Vector3.right * 0.3f + Vector3.forward -Vector3.up * 0.5f,
                            -Vector3.right * 0.125f                        + Vector3.forward -Vector3.up * 0.5f,
                            -Vector3.right * 0.125f +Vector3.right * 0.15f + Vector3.forward -Vector3.up * 0.5f,

                            -Vector3.right * 0.125f -Vector3.right * 0.5f + Vector3.forward,
                            -Vector3.right * 0.125f +Vector3.right * 0.15f + Vector3.forward,
                        };

                        currentDistancePoint = rand.Next() % offsets.Length;
                        while (previousPoints.Contains(currentDistancePoint))
                        {
                            currentDistancePoint = rand.Next() % offsets.Length;
                        }

                        if (previousPoints.Count > 1)
                        {
                            previousPoints.RemoveAt(0);
                        }

                        previousPoints.Add(currentDistancePoint);

                        distanceOffset = offsets[currentDistancePoint];
                        distanceLookAt = lookats[currentDistancePoint];
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
        _elaspedTime += Time.deltaTime * _settings.speed;
        UpdateCameraPose();
    }

    public void UpdateCameraPose()
    { 
        switch (cameraMode)
        {
             case CameraMode.Obrital:
                {
                    Vector3 headTransform = _helper.playerHead.position;
                    Vector3 waistTransform = _helper.playerWaist.position;

                    if (headTransform.z > TargetCameraPosition.z + _settings.distance * 0.9f)
                    {
                        UpdateCameraChange();
                    }

                    Vector3 rotationVector = new Vector3(Mathf.Cos(_elaspedTime * orbitalDirection + orbitalAngleOffset), 0f, Mathf.Sin(_elaspedTime * orbitalDirection + orbitalAngleOffset)) * _settings.distance;
                    TargetCameraPosition = headTransform + rotationVector;
                    TargetCameraPosition.y = orbitalHeight;
                    TargetCameraRotation = Quaternion.LookRotation((waistTransform - TargetCameraPosition).normalized);

                //    TargetCameraPosition -= Quaternion.Inverse(TargetCameraRotation) * Vector3.right * 0.25f;
                    break;
                }
            case CameraMode.Distance:
                {
                    UpdateCameraChange();
                    TargetCameraPosition = _helper.playerWaist.position + distanceOffset;
                    TargetCameraRotation = Quaternion.LookRotation(distanceLookAt);
                    break;
                }
        }

        BlendCameraPose();
    }

    void BlendCameraPose()
    {
        float distance = (CurrentCameraPosition - TargetCameraPosition).magnitude;
        if (cameraMode == CameraMode.Obrital)
        {
            float l = 0.003f;
            cameraLerpValue = cameraLerpValue * (1.0f - l) + 0.1f * l;
        }

        float lerpValue = cameraLerpValue;

        if (distance > 0.5f)
        {
            lerpValue /= (distance / 0.5f);
        }

        CurrentCameraRotation = Quaternion.Slerp(CurrentCameraRotation, TargetCameraRotation, lerpValue);
        CurrentCameraPosition = CurrentCameraPosition * (1.0f - lerpValue) + TargetCameraPosition * lerpValue;

        orbitalHeight = orbitalHeight * (1.0f - 0.0025f) + _helper.playerHead.position.y * 0.0025f;

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
