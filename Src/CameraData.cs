using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public enum CameraType
{
    Orbital,
    LookAt,
    NumTypes,
}

public class CameraData
{
    public String Name;
    public CameraType Type;
    public Vector3 PositionOffset;
    public Vector3 LookAt;
    public String PositionBinding;
    public String LookAtBinding;
    public float Distance;
    public float Speed;
    public float MinTime;
    public float MaxTime;

    public Vector3 SmoothedPositionBinding = Vector3.zero;
    public Vector3 SmoothedLookAtBinding = Vector3.zero;

    public Vector3 EvaluatePositionBinding(PluginCameraHelper helper)
    {
        var newPos = EvaluateBinding(helper, PositionBinding) + PositionOffset;

        var filter = 0.05f;
        SmoothedPositionBinding = SmoothedPositionBinding * (1.0f - filter) + newPos * filter;
        return SmoothedPositionBinding;
    }

    public Vector3 EvaluateLookAtBindingBinding(PluginCameraHelper helper)
    {
        var newLookat = EvaluateBinding(helper, LookAtBinding) + LookAt;

        var filter = 0.05f;
        SmoothedLookAtBinding = SmoothedLookAtBinding * (1.0f - filter) + newLookat * filter;
        return SmoothedLookAtBinding;
    }

    Vector3 EvaluateBinding(PluginCameraHelper helper, string binding)
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
}

public static class CameraPluginSettings
{
    public static float GlobalBias;
//    public static float BlendSpeed;
    public static List<CameraData> CameraDataList = new List<CameraData>();

    public static void LoadSettings()
    {
        string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string settingLoc = Path.Combine(docPath, @"LIV\Plugins\CameraBehaviours\FriesBSCam\");

        settingLoc = Path.Combine(settingLoc, "settings.txt");

        // Check to see if the file exists.
        if (!File.Exists(@settingLoc))
        {
            // Create a default settings file.
            using (var ws = new StreamWriter(@settingLoc))
            {
                ws.WriteLine("GlobalBias='0.0'");
                ws.WriteLine("Camera={");
                ws.WriteLine("	Name='TopRight'");
                ws.WriteLine("	Type='LookAt'");
                ws.WriteLine("	PositionBinding='playerWaist'");
                ws.WriteLine("	PositionOffset={x='0.5', y='1.3', z='-2.0'} ");
                ws.WriteLine("	LookAt={x='0.0', y='-0.5', z='1.0'} ");
                ws.WriteLine("	MinTime='4.0' ");
                ws.WriteLine("	MaxTime='8.0' ");
                ws.WriteLine("}");
                ws.WriteLine("Camera={");
                ws.WriteLine("	Name='Top'");
                ws.WriteLine("	Type='LookAt'");
                ws.WriteLine("	PositionBinding='playerWaist'");
                ws.WriteLine("	PositionOffset={x='0.0', y='2.0', z='-2.0'}  ");
                ws.WriteLine("	LookAt={x='0.0', y='-0.5', z='1.0'} ");
                ws.WriteLine("	MinTime='4.0' ");
                ws.WriteLine("	MaxTime='8.0' ");
                ws.WriteLine("}");
                ws.WriteLine("Camera={");
                ws.WriteLine("	Name='TopLeft'");
                ws.WriteLine("	Type='LookAt'");
                ws.WriteLine("	PositionBinding='playerWaist'");
                ws.WriteLine("	PositionOffset={x='-0.5', y='1.3', z='-2.0'}  ");
                ws.WriteLine("	LookAt={x='0.0', y='-0.5', z='1.0'} ");
                ws.WriteLine("	MinTime='4.0' ");
                ws.WriteLine("	MaxTime='8.0' ");
                ws.WriteLine("}");
                ws.WriteLine("Camera={");
                ws.WriteLine("	Name='BottomRight'");
                ws.WriteLine("	Type='LookAt'");
                ws.WriteLine("	PositionBinding='playerWaist'");
                ws.WriteLine("	PositionOffset={x='0.75', y='0.0', z='-2.0'} ");
                ws.WriteLine("	LookAt={x='0.0', y='0.0', z='1.0'}  ");
                ws.WriteLine("	MinTime='4.0' ");
                ws.WriteLine("	MaxTime='8.0' ");
                ws.WriteLine("}");
                ws.WriteLine("Camera={");
                ws.WriteLine("	Name='BottomLeft'");
                ws.WriteLine("	Type='LookAt'");
                ws.WriteLine("	PositionBinding='playerWaist'");
                ws.WriteLine("	PositionOffset={x='-0.75', y='0.0', z='-2.0'}  ");
                ws.WriteLine("	LookAt={x='0.0', y='0.0', z='1.0'} ");
                ws.WriteLine("	MinTime='4.0' ");
                ws.WriteLine("	MaxTime='8.0' ");
                ws.WriteLine("}");
                ws.WriteLine("Camera={");
                ws.WriteLine("	Name='Orbital1'");
                ws.WriteLine("	Type='Orbital'");
                ws.WriteLine("	Distance='3.0'");
                ws.WriteLine("	Speed='1.0'");
                ws.WriteLine("	PositionBinding='playerWaist'");
                ws.WriteLine("	PositionOffset={x='0.0', y='0.5', z='0.0'}  ");
                ws.WriteLine("	LookAtBinding='playerWaist'");
                ws.WriteLine("	LookAt={x='0.0', y='0.0', z='0.0'} ");
                ws.WriteLine("	MinTime='8.0' ");
                ws.WriteLine("	MaxTime='8.0' ");
                ws.WriteLine("}");
                ws.WriteLine("Camera={");
                ws.WriteLine("	Name='Orbital2'");
                ws.WriteLine("	Type='Orbital'");
                ws.WriteLine("	Distance='3.0'");
                ws.WriteLine("	Speed='1.0'");
                ws.WriteLine("	PositionBinding='playerWaist'");
                ws.WriteLine("	PositionOffset={x='0.0', y='0.0', z='0.0'}  ");
                ws.WriteLine("	LookAtBinding='playerWaist'");
                ws.WriteLine("	LookAt={x='0.0', y='0.0', z='0.0'} ");
                ws.WriteLine("	MinTime='8.0' ");
                ws.WriteLine("	MaxTime='8.0' ");
                ws.WriteLine("}");
                ws.WriteLine("Camera={");
                ws.WriteLine("	Name='Orbital3'");
                ws.WriteLine("	Type='Orbital'");
                ws.WriteLine("	Distance='3.0'");
                ws.WriteLine("	Speed='1.0'");
                ws.WriteLine("	PositionBinding='playerWaist'");
                ws.WriteLine("	PositionOffset={x='0.0', y='0.0', z='0.0'}  ");
                ws.WriteLine("	LookAtBinding='playerWaist'");
                ws.WriteLine("	LookAt={x='0.0', y='0.0', z='0.0'} ");
                ws.WriteLine("	MinTime='8.0' ");
                ws.WriteLine("	MaxTime='8.0' ");
                ws.WriteLine("}");
            }
        }

        ParseSettingsFile(@settingLoc);
    }

    public static void ParseSettingsFile(string fileName)
    {
        // Open the file to read from.
        string readText = File.ReadAllText(fileName);

        var root = new ReflectionToken();

        AttributeParser parser = new AttributeParser(root);
        parser.Tokenize(readText);

        GlobalBias = ParseFloat(root.GetChildSafe("GlobalBias"));
//        BlendSpeed = ParseFloat(root.GetChildSafe("BlendSpeed"));

        var camera = root.GetChild("Camera");
        while (camera != null)
        {
            var cameraData = new CameraData();
            cameraData.Name = camera.GetChildSafe("Name").mValue;
            cameraData.Type = GetCameraTypeFromToken(camera.GetChildSafe("Type"));
            cameraData.PositionOffset = GetVector3Token(camera.GetChildSafe("PositionOffset"));
            cameraData.LookAt = GetVector3Token(camera.GetChildSafe("LookAt"));
            cameraData.PositionBinding = camera.GetChildSafe("PositionBinding").mValue;
            cameraData.LookAtBinding = camera.GetChildSafe("LookAtBinding").mValue;
            cameraData.Distance = ParseFloat(camera.GetChildSafe("Distance"));
            cameraData.Speed = ParseFloat(camera.GetChildSafe("Speed"));
            cameraData.MinTime = ParseFloat(camera.GetChildSafe("MinTime"));
            cameraData.MaxTime = ParseFloat(camera.GetChildSafe("MaxTime"));

            CameraDataList.Add(cameraData);

            camera = root.GetNextChild(camera);
        }
    }

    public static float ParseFloat(ReflectionToken token)
    {
        float outFloat = 0.0f;
        if (float.TryParse(token.mValue, out outFloat))
        {
            return outFloat;
        }

        return 0.0f;
    }

    public static CameraType GetCameraTypeFromToken(ReflectionToken token)
    {
        if (token.mValue == "Orbital")
        {
            return CameraType.Orbital;
        }
        if (token.mValue == "LookAt")
        {
            return CameraType.LookAt;
        }

        return CameraType.LookAt;
    }

    public static Vector3 GetVector3Token(ReflectionToken token)
    {
        float x = ParseFloat(token.GetChildSafe("x"));
        float y = ParseFloat(token.GetChildSafe("y"));
        float z = ParseFloat(token.GetChildSafe("z"));

        return new Vector3(x, y, z);
    }
    
}
