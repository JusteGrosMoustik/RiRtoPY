using System;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
class RunIronPython
{
    static void Main(string[] args)
    {
        var engine=Python.CreateEngine();
        engine.GetSysModule().SetVariable("argv",new string[]{args[0],args[1]});
        engine.ExecuteFile(args[0]);
    }
}
