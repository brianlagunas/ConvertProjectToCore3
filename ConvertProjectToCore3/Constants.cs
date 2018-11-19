using System;
using System.Collections.Immutable;

namespace ConvertProjectToCore3
{
    class Constants
    {
        public const string AssemblyInfoFilePath = "Properties\\AssemblyInfo.cs";
        public const string AssemblyAttributeSearchPattern = "[assembly: Assembly";
        public const string AssemblyName = "AssemblyName";
        public const string AssemblyVersion = "AssemblyVersion";
        public const string CommentPrefix = "//";
        public const string NetFramework = ".NETFramework, Version=v4.5";
        public const string NetCoreApp3 = "netcoreapp3.0";
        public const string NuGetPackagesConfigFileName = "packages.config";
        public const string OutputType = "OutputType";
        public const string PackageReference = "PackageReference";
        public const string ProjectGuid = "ProjectGuid";
        public const string ProjectReference = "ProjectReference";
        public const string ProjectTypeGuids = "ProjectTypeGuids";
        public const string Reference = "Reference";
        public const string Resource = "Resource";
        public const string Sdk = "Microsoft.NET.Sdk.WindowsDesktop";
        public const string TargetFramework = "TargetFramework";
        public const string True = "true";
        public const string UseWPF = "UseWPF";
        public const string Version = "Version";
        //public const string WinFormsProjectGuidString = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC"; //this is technically any library C#
        public const string WpfProjectGuidString = "60DC8134-EBA5-43B8-BCC9-BB4BC16C2548";

        public static ImmutableArray<string> PropertiesNotNeeded => ImmutableArray.Create(
            "ProjectGuid", // Guids are in-memory
            "ProjectTypeGuids", // Not used - capabilities are used instead
            "TargetFrameworkIdentifier", // Inferred from TargetFramework
            "TargetFrameworkVersion", // Inferred from TargetFramework
            "TargetFrameworkProfile" // Inferred from TargetFramework
            );

        public static ImmutableArray<string> ItemTypesNotNeeded => ImmutableArray.Create(
            "Compile",
            "EmbeddedResource",
            "None",
            "Reference",
            "ApplicationDefinition",
            "Page"
            );
    }
}
