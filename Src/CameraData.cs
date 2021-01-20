using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public enum CameraType
{
    Obrital,
    LookAt,
    NumTypes,
}

public class CameraData
{
    public String Name;
    public CameraType Type;
    public Vector3 Offset;
    public Vector3 LookAt;
    public String PositionBinding;
    public String LookAtBinding;
    public float Distance;
    public float Speed;
}

public static class CameraSettings
{
    public static float GlobalBias;
    public static float BlendSpeed;
    public static List<CameraData> CameraDataList = new List<CameraData>();

    public static void LoadSettings()
    {
        string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string settingLoc = Path.Combine(docPath, @"LIV\Plugins\CameraBehaviours\FriesBSCam\");
        
        // Create the directory in the off-chance that it doesn't exist.
        Directory.CreateDirectory(settingLoc);

        settingLoc = Path.Combine(settingLoc, "settings.txt");

        // Check to see if the file exists.
        if (File.Exists(@settingLoc))
        {
            ParseSettingsFile(@settingLoc);
        }
        else
        {
            // Create a default settings file.
            using (var ws = new StreamWriter(@settingLoc))
            {
                ws.WriteLine("GlobalBias='20'");
                ws.WriteLine("BlendSpeed='0.05'");
                ws.WriteLine("Camera={");
                ws.WriteLine("	Name='TopRight'");
                ws.WriteLine("	Type='LookAt'");
                ws.WriteLine("	PositionBinding='playerWaist'");
                ws.WriteLine("	Offset={x='0.5', y='1.3', z='-2.0'} ");
                ws.WriteLine("	LookAt={x='0.0', y='-0.5', z='1.0'} ");
                ws.WriteLine("}");
                ws.WriteLine("Camera={");
                ws.WriteLine("	Name='Top'");
                ws.WriteLine("	Type='LookAt'");
                ws.WriteLine("	PositionBinding='playerWaist'");
                ws.WriteLine("	Offset={x='0.0', y='2.0', z='-2.0'}  ");
                ws.WriteLine("	LookAt={x='0.0', y='-0.5', z='1.0'} ");
                ws.WriteLine("}");
                ws.WriteLine("Camera={");
                ws.WriteLine("	Name='TopLeft'");
                ws.WriteLine("	Type='LookAt'");
                ws.WriteLine("	PositionBinding='playerWaist'");
                ws.WriteLine("	Offset={x='-0.5', y='1.3', z='-2.0'}  ");
                ws.WriteLine("	LookAt={x='0.0', y='-0.5', z='1.0'} ");
                ws.WriteLine("}");
                ws.WriteLine("Camera={");
                ws.WriteLine("	Name='BottomRight'");
                ws.WriteLine("	Type='LookAt'");
                ws.WriteLine("	PositionBinding='playerWaist'");
                ws.WriteLine("	Offset={x='1.0', y='0.0', z='-2.0'} ");
                ws.WriteLine("	LookAt={x='0.0', y='0.0', z='1.0'}  ");
                ws.WriteLine("}");
                ws.WriteLine("Camera={");
                ws.WriteLine("	Name='BottomRight'");
                ws.WriteLine("	Type='LookAt'");
                ws.WriteLine("	PositionBinding='playerWaist'");
                ws.WriteLine("	Offset={x='-1.0', y='0.0', z='-2.0'}  ");
                ws.WriteLine("	LookAt={x='0.0', y='0.0', z='1.0'} ");
                ws.WriteLine("}");
                ws.WriteLine("Camera={");
                ws.WriteLine("	Name='Orbital'");
                ws.WriteLine("	Type='Orbital'");
                ws.WriteLine("	Distance='4.0'");
                ws.WriteLine("	Speed='1.0'");
                ws.WriteLine("	PositionBinding='playerHead'");
                ws.WriteLine("	LookAtBinding='playerWaist'");
                ws.WriteLine("}");
            }
        }
    }

    public static void ParseSettingsFile(string fileName)
    {
        // Open the file to read from.
        string readText = File.ReadAllText(fileName);

        var root = new ReflectionToken();

        AttributeParser parser = new AttributeParser(root);
        parser.Tokenize(readText);

        GlobalBias = ParseFloat(root.GetChild("GlobalBias"));
        BlendSpeed = ParseFloat(root.GetChild("BlendSpeed"));

        var camera = root.GetChild("Camera");
        while (camera != null)
        {
            var cameraData = new CameraData();
            cameraData.Name = camera.GetChild("Name").mValue;
            cameraData.Type = GetCameraTypeFromToken(camera.GetChild("Type"));
            cameraData.Offset = GetVector3Token(camera.GetChild("Offset"));
            cameraData.LookAt = GetVector3Token(camera.GetChild("LookAt"));
            cameraData.PositionBinding = camera.GetChild("PositionBinding").mValue;
            cameraData.LookAtBinding = camera.GetChild("LookAtBinding").mValue;
            cameraData.Distance = ParseFloat(camera.GetChild("Distance"));
            cameraData.Speed = ParseFloat(camera.GetChild("Speed"));

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
        if (token.mValue == "Obrital")
        {
            return CameraType.Obrital;
        }
        if (token.mValue == "LookAt")
        {
            return CameraType.LookAt;
        }

        return CameraType.LookAt;
    }

    public static Vector3 GetVector3Token(ReflectionToken token)
    {
        float x = ParseFloat(token.GetChild("x"));
        float y = ParseFloat(token.GetChild("y"));
        float z = ParseFloat(token.GetChild("z"));

        return new Vector3(x, y, z);
    }
    
}
