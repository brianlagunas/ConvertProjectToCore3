using System;
using System.Collections.Generic;

namespace ConvertProjectToCore3
{
    class ProjectData
    {
        public string AssemblyName { get; set; }
        public string AssemblyVersion { get; set; }
        public string FilePath { get; set; }
        public string OutputType { get; set; }
        public Guid ProjectGuid { get; set; }
        public List<string> ProjectReferences { get; set; }
        public string ProjectTypeGuids { get; set; }
        public List<string> Resources { get; set; }
        public string UsePlatform { get; set; }
    }
}
