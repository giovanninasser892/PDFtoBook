using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ReaderBuild
{
    [Serializable] public sealed class Result { public string state; public string result; public string path; public long bytes; public string[] errors; }
    public static void Start()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)throw new InvalidOperationException("Editor ainda está importando scripts.");
        Directory.CreateDirectory("Builds");
        Write(new Result{state="queued"});
        {
            try {
                Write(new Result{state="building"});
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                    scenes=new[]{"Assets/Scenes/Reader.unity"},
                    locationPathName="Builds/PDFtoBook.apk",target=BuildTarget.Android,options=BuildOptions.None
                });
                Write(new Result{state="completed",result=report.summary.result.ToString(),path=report.summary.outputPath,bytes=File.Exists(report.summary.outputPath)?new FileInfo(report.summary.outputPath).Length:0,
                    errors=report.steps.SelectMany(step=>step.messages).Where(message=>message.type==LogType.Error||message.type==LogType.Exception).Select(message=>message.content).ToArray()});
            }catch(Exception error){Write(new Result{state="completed",result="Failed",errors=new[]{error.ToString()}});}
        }
    }
    private static void Write(Result value)=>File.WriteAllText("Builds/build-result.json",JsonUtility.ToJson(value,true));
}
