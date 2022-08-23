using System;
using System.IO;
using UnityEngine;

public static class Logger
{
    public static StreamWriter? logStream = null;

    public static void Start()
    {
        if (logStream == null)
        {
            var docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var outputLoc = Path.Combine(docPath, @"LIV\Plugins\CameraBehaviours\FriesBSCam\");
            Directory.CreateDirectory(outputLoc);
            if (Application.isEditor)
                outputLoc = Path.Combine(outputLoc, "output_editor.txt");
            else
                outputLoc = Path.Combine(outputLoc, "output.txt");
            logStream = new StreamWriter(outputLoc);
        }
    }

    public static void LogError(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error)
        {
            Log(condition);
            Log(stackTrace);
        }
    }

    public static void Log(string s)
    {
        Start();
        logStream?.WriteLine(s);
        logStream?.Flush();
    }

    public static void Close()
    {
        logStream?.Close();
        logStream = null;
    }

}