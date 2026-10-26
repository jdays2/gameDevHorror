using System.IO;
using UnityPipeline.Microsoft.CodeAnalysis;
using UnityPipeline.Microsoft.CodeAnalysis.CSharp;

public static class FormatClawAuthoring
{
    public static object Run()
    {
        string[] paths = { "AgentScripts/InstallClawSandbox.cs", "AgentScripts/VerifyClawSandbox.cs", "AgentScripts/FinalizeClawSandbox.cs" };
        foreach (string path in paths)
        {
            var syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot();
            File.WriteAllText(path, syntax.NormalizeWhitespace("    ", "\n").ToFullString() + "\n");
        }
        return paths;
    }
}
