using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EmergencyShift.Editor.Build
{
    public sealed class ThirdPartyLicenseBuildStep : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string source = Path.Combine(projectRoot, "Docs", "ThirdParty", "Extenject-LICENSE.txt");
            string outputFolder = Path.GetDirectoryName(report.summary.outputPath);
            string licenseFolder = Path.Combine(outputFolder, "ThirdPartyLicenses");
            Directory.CreateDirectory(licenseFolder);
            File.Copy(source, Path.Combine(licenseFolder, "Extenject-LICENSE.txt"), true);
        }
    }
}
