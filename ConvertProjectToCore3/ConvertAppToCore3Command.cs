using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Xml;
using EnvDTE;
using Microsoft.Build.Construction;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace ConvertProjectToCore3
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ConvertAppToCore3Command
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("9f281125-01d6-41e6-ba9f-c4be22d0984c");

        private readonly DTE _dte;

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertAppToCore3Command"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private ConvertAppToCore3Command(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
            menuItem.BeforeQueryStatus += MenuItem_BeforeQueryStatus;
            commandService.AddCommand(menuItem);
        }

        private void MenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                menuCommand.Visible = false;
                menuCommand.Enabled = false;

                Project selectedProject = GetSelectedProject();
                IVsSolution solution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));

                IVsHierarchy hierarchy;
                solution.GetProjectOfUniqueName(selectedProject.UniqueName, out hierarchy);

                IVsAggregatableProjectCorrected ap;
                ap = hierarchy as IVsAggregatableProjectCorrected;

                string projTypeGuids;
                ap.GetAggregateProjectTypeGuids(out projTypeGuids);

                if (projTypeGuids.ToUpper().IndexOf(Constants.WpfProjectGuidString) > 0)
                {
                    menuCommand.Visible = true;
                    menuCommand.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ConvertAppToCore3Command Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in ConvertAppToCore3Command's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new ConvertAppToCore3Command(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private async void Execute(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            string title = "Convert Project To .NET Core 3";
            string message = "Are you sure you want to convert this project to .NET Core 3?";

            var result = VsShellUtilities.ShowMessageBox(this.package, message, title, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            if (result == 1)
            {
                try
                {
                    Project activeProject = GetSelectedProject();
                    await ConvertProjectAsync(activeProject);
                }
                catch (Exception ex)
                {
                    VsShellUtilities.ShowMessageBox(this.package, ex.Message, title, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }
            }
        }

        Project GetSelectedProject()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Project activeProject = null;
            DTE dte = (DTE)Package.GetGlobalService(typeof(DTE));
            object[] activeSolutionProjects = dte.ActiveSolutionProjects as object[];
            if (activeSolutionProjects != null)
            {
                activeProject = activeSolutionProjects.GetValue(0) as Project;
            }
            return activeProject;
        }

        async Task ConvertProjectAsync(Project project)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            IVsSolution4 solution = await ServiceProvider.GetServiceAsync(typeof(SVsSolution)) as IVsSolution4;

            ProjectRootElement projectRoot = ProjectRootElement.Open(project.FullName);

            var projectData = ReadProjectData(projectRoot);
            projectData.FilePath = Path.GetDirectoryName(project.FullName);
            projectData.AssemblyVersion = project.Properties.Item(Constants.AssemblyVersion)?.Value.ToString();

            UnloadProject(solution, projectData.ProjectGuid);

            DeleteCSProjContents(projectRoot);

            UpdateCSProjContents(projectRoot, projectData);

            projectRoot.Save();

            UpdateAssemblyInfo(projectData.FilePath);

            ReloadProject(solution, projectData.ProjectGuid);
        }

        ProjectData ReadProjectData(ProjectRootElement projectRoot)
        {
            var projectData = new ProjectData();
            var propertyGroup = projectRoot.PropertyGroups.First();
            projectData.ProjectGuid = Guid.Parse(propertyGroup.Properties.FirstOrDefault(x => x.Name == Constants.ProjectGuid)?.Value.ToString());
            projectData.ProjectTypeGuids = propertyGroup.Properties.FirstOrDefault(x => x.Name == Constants.ProjectTypeGuids)?.Value.ToString();
            //projectData.AssemblyName = propertyGroup.Properties.FirstOrDefault(x => x.Name == Constants.AssemblyName)?.Value.ToString();
            //projectData.OutputType = propertyGroup.Properties.FirstOrDefault(x => x.Name == Constants.OutputType)?.Value.ToString();
            return projectData;
        }

        void DeleteCSProjContents(ProjectRootElement projectRoot)
        {
            projectRoot.ToolsVersion = null;
            RemoveImports(projectRoot);
            RemoveProperties(projectRoot);
            //await RemoveReferences(projectRoot);
            RemoveItems(projectRoot);
        }

        void RemoveImports(ProjectRootElement root)
        {
            foreach (var import in root.Imports)
            {
                root.RemoveChild(import);
            }
        }

        //async Task RemoveReferences(ProjectRootElement root)
        //{
        //    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        //    var frameworkMultiTargeting = await ServiceProvider.GetServiceAsync(typeof(SVsFrameworkMultiTargeting)) as IVsFrameworkMultiTargeting;

        //    foreach (var itemGroup in root.ItemGroups)
        //    {
        //        foreach (var item in itemGroup.Items)
        //        {
        //            if (item.ElementName == Constants.Reference)
        //            {
        //                bool isFrameworkReference = false;
        //                frameworkMultiTargeting.IsReferenceableInTargetFx(item.Include, Constants.NetFramework, out isFrameworkReference);

        //                if (isFrameworkReference)
        //                    itemGroup.RemoveChild(item);
        //            }
        //        }

        //        if (itemGroup.Items.Count == 0)
        //        {
        //            root.RemoveChild(itemGroup);
        //        }
        //    }
        //}

        void RemoveItems(ProjectRootElement root)
        {
            foreach (var itemGroup in root.ItemGroups)
            {
                foreach (var item in itemGroup.Items)
                {
                    if (Constants.ItemTypesNotNeeded.Contains(item.ElementName))
                    {
                        itemGroup.RemoveChild(item);
                    }
                }

                if (itemGroup.Items.Count == 0)
                {
                    root.RemoveChild(itemGroup);
                }
            }
        }

        static void RemoveProperties(ProjectRootElement root)
        {
            foreach (var propGroup in root.PropertyGroups)
            {
                foreach (var property in propGroup.Properties)
                {
                    if (Constants.PropertiesNotNeeded.Contains(property.Name))
                    {
                        propGroup.RemoveChild(property);
                    }
                }
            }
        }

        void UpdateCSProjContents(ProjectRootElement projectRoot, ProjectData projectData)
        {
            projectRoot.Sdk = Constants.Sdk;
            var propertyGroup = projectRoot.PropertyGroups.First();

            propertyGroup.AddProperty(Constants.TargetFramework, Constants.NetCoreApp3);

            //TODO: check to see if the project type is WPF or WinForms.
            propertyGroup.AddProperty(Constants.UseWPF, Constants.True);

            if (!string.IsNullOrWhiteSpace(projectData.AssemblyVersion))
                propertyGroup.AddProperty(Constants.Version, projectData.AssemblyVersion);

            UpdateNuGetPackageReferences(projectRoot, projectData);
        }

        void UpdateNuGetPackageReferences(ProjectRootElement projectRoot, ProjectData projectData)
        {
            //TODO: only use the top-level pacakages, not all packages
            var packageConfigFilePath = Path.Combine(projectData.FilePath, Constants.NuGetPackagesConfigFileName);
            if (File.Exists(packageConfigFilePath))
            {
                XmlDocument document = new XmlDocument();
                document.Load(packageConfigFilePath);

                XmlNodeList packageList = document.GetElementsByTagName("package");

                ProjectItemGroupElement packageRefItemGroup = projectRoot.AddItemGroup();
                foreach (XmlNode package in packageList)
                {
                    var id = package.Attributes["id"].InnerText;
                    var version = package.Attributes["version"].InnerText;
                    var item = packageRefItemGroup.AddItem(Constants.PackageReference, id);
                    item.AddMetadata(Constants.Version, version, true);
                }

                File.Delete(packageConfigFilePath);
            }
        }

        void UpdateAssemblyInfo(string projectFilePath)
        {
            String assemblyInfoFilePath = Path.Combine(projectFilePath, Constants.AssemblyInfoFilePath);
            if (File.Exists(assemblyInfoFilePath))
            {
                var assemblyInfoLines = File.ReadLines(assemblyInfoFilePath);
                var updatedLines = new List<string>();
                foreach (var line in assemblyInfoLines)
                {
                    string newLine = line;
                    if (newLine.StartsWith(Constants.AssemblyAttributeSearchPattern))
                    {
                        newLine = line.Insert(0, Constants.CommentPrefix);
                    }

                    updatedLines.Add(newLine);
                }
                File.WriteAllLines(assemblyInfoFilePath, updatedLines);
            }
        }

        void UnloadProject(IVsSolution4 solution, Guid projectGuid)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            int hr;
            hr = solution.UnloadProject(ref projectGuid, (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser);
            ErrorHandler.ThrowOnFailure(hr);
        }

        void ReloadProject(IVsSolution4 solution, Guid projectGuid)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            int hr;
            hr = solution.ReloadProject(ref projectGuid);
            ErrorHandler.ThrowOnFailure(hr);
        }
    }
}
