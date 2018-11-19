using System.Collections.Immutable;

namespace ConvertProjectToCore3
{
    class Facts
    {
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
