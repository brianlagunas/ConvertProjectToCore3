using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
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
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
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
            string message = string.Format(CultureInfo.CurrentCulture, "Are you sure you want to convert this project to .NET Core 3?", this.GetType().FullName);

            // Show a message box to prove we were here
            var result = VsShellUtilities.ShowMessageBox(this.package, message, title, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            if (result == 1)
            {
                DTE dte = await ServiceProvider.GetServiceAsync(typeof(DTE)) as DTE;
                object[] activeSolutionProjects = dte.ActiveSolutionProjects as object[];
                Project activeProject = null;

                if (activeSolutionProjects != null)
                {
                    foreach (object activeSolutionProject in activeSolutionProjects)
                    {
                        activeProject = activeSolutionProject as Project;

                        if (activeProject != null)
                        {
                            break;
                        }
                    }
                }

                ConvertProject(activeProject);
            }
        }

        void ConvertProject(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string projectFullName = project.FullName;
            var projectFilePath = Path.GetDirectoryName(projectFullName);

            var assemblyVersion = project.Properties.Item("AssemblyVersion")?.Value.ToString();

            
        }
    }
}
