// Type: ConcordSvc
// Assembly: VerizonLTC, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 420BB13D-AA0E-4037-836F-1D0288F21D13
// Assembly location: C:\VerLTCNV\VerizonLTC051815.exe

using System;
using System.IO;

class Svc
{
    private static StreamWriter file;

    public void OpenLog(string Path)
    {
        if (!File.Exists(Path))
            file = File.CreateText(Path);
        else
            file = File.AppendText(Path);
    }

    public void Log(string Str)
    {
        Str = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  - " + Str + "\t";
        Console.WriteLine(Str);
        if (file == null)
            return;
        file.WriteLine();
        file.WriteLine(Str);
        ((TextWriter)file).Flush();
    }
}
