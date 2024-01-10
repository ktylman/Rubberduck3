﻿using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.UI.Command.Abstract;
using Rubberduck.UI.Message;
using Rubberduck.UI.Services.Abstract;
using Rubberduck.UI.Windows;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace Rubberduck.UI.Services.NewProject
{
    public interface IWorkspaceModulesService // TODO rename
    {
        /// <summary>
        /// Gets all workspace modules from the specified project loaded in the VBIDE.
        /// </summary>
        /// <param name="scanFolderAnnotations">If <c>true</c>, Rubberduck 2.x <c>@Folder</c> annotations become RD3 workspace folders.</param>
        IEnumerable<Module> GetWorkspaceModules(string projectId, Uri workspaceRoot, bool scanFolderAnnotations);
        /// <summary>
        /// Exports the specified workspace source files from the VBIDE to the project's workspace folder.
        /// </summary>
        void ExportWorkspaceModules(string projectId, Uri workspaceRoot, IEnumerable<Module> modules);
        /// <summary>
        /// Imports the specified workspace source files into the VBIDE from the project's workspace folder.
        /// </summary>
        void ImportWorkspaceModules(string projectId, Uri workspaceRoot, IEnumerable<Module> modules);

        /// <summary>
        /// Gets the references of the specified project from the VBIDE.
        /// </summary>
        IEnumerable<Reference> GetWorkspaceReferences(string projectId);
        /// <summary>
        /// Sets the specified project references as specified.
        /// </summary>
        /// <remarks>
        /// Order of items determines their priority in the references list.
        /// </remarks>
        void SetProjectReferences(string projectId, Reference[] references);
    }

    public class NewProjectCommand : CommandBase
    {
        private readonly IFileSystem _fileSystem;
        private readonly IDialogService<NewProjectWindowViewModel> _dialog;
        private readonly IProjectFileService _projectFileService;
        private readonly ITemplatesService _templatesService;
        private readonly IWorkspaceService? _workspace;
        private readonly IWorkspaceFolderService _workspaceFolderService;
        private readonly IWorkspaceModulesService? _workspaceModulesService;

        public NewProjectCommand(UIServiceHelper service,
            IDialogService<NewProjectWindowViewModel> dialog,
            IWorkspaceService? workspace,
            ITemplatesService templatesService,
            IFileSystem fileSystem,
            IWorkspaceFolderService workspaceFolderService,
            IProjectFileService projectFileService,
            IWorkspaceModulesService? workspaceModulesService)
            : base(service)
        {
            _dialog = dialog;

            _fileSystem = fileSystem;
            _templatesService = templatesService;
            _workspaceFolderService = workspaceFolderService;
            _workspace = workspace;
            _projectFileService = projectFileService;
            _workspaceModulesService = workspaceModulesService;
        }

        protected async override Task OnExecuteAsync(object? parameter)
        {
            Uri? workspaceRootUri = null;

            if (Service.TryRunAction(() =>
            {
                var model = _dialog.ShowDialog();
                if (model.SelectedAction == MessageAction.AcceptAction)
                {
                    if (Service.Settings.LanguageClientSettings.WorkspaceSettings.RequireDefaultWorkspaceRootHost)
                    {
                        if (model.WorkspaceLocation != Service.Settings.LanguageClientSettings.WorkspaceSettings.DefaultWorkspaceRoot.LocalPath)
                        {
                            Service.LogWarning("Cannot create workspace. Project workspace location is required to be under the default workspace root as per current configuration.");
                            throw new InvalidOperationException(); // throwing here because this should have been validated already.
                        }
                    }

                    workspaceRootUri = new Uri(_fileSystem.Path.Combine(model.WorkspaceLocation, model.ProjectName));
                    var workspaceSrcRoot = _fileSystem.Path.Combine(workspaceRootUri.LocalPath, ProjectFile.SourceRoot);

                    var projectFile = CreateProjectFileModel(model);

                    _workspaceFolderService.CreateWorkspaceFolders(projectFile);
                    if (_workspaceModulesService is not null && projectFile.ProjectId is not null)
                    {
                        // command is executed from the VBE add-in;
                        // we're migrating an existing project to RD3 so we need to create the files now:
                        _workspaceModulesService.ExportWorkspaceModules(projectFile.ProjectId, workspaceRootUri, projectFile.VBProject.Modules);
                    }
                    else
                    {
                        // command is executed from the Rubberduck Editor;
                        // no need to write the workspace files changes need to be saved.
                    }

                    _projectFileService.CreateFile(projectFile);

                    if (model.SelectedProjectTemplate is not null)
                    {
                        var template = _templatesService.Resolve(model.SelectedProjectTemplate);
                        var templateName = template.Name;
                        var templatesRoot = _fileSystem.DirectoryInfo.New(Service.Settings.GeneralSettings.TemplatesLocation.LocalPath).FullName;
                        var templateSrcRoot = _fileSystem.Path.Combine(templatesRoot, templateName, ProjectTemplate.TemplateSourceFolderName);

                        _workspaceFolderService.CopyTemplateFiles(template.ProjectFile, templateSrcRoot);
                    }

                    Service.LogInformation("Workspace was successfully created.", $"Workspace root: {workspaceRootUri}");
                }
            }, nameof(NewProjectCommand)) && workspaceRootUri != null)
            {
                if (_workspace != null) // service will be null if invoked from add-in
                {
                    await _workspace.OpenProjectWorkspaceAsync(workspaceRootUri);
                }
            }
        }

        private ProjectFile CreateProjectFileModel(NewProjectWindowViewModel model)
        {
            var builder = new ProjectFileBuilder(_fileSystem, Service.SettingsProvider);

            if (model.SelectedProjectTemplate is not null)
            {
                model.SelectedProjectTemplate = _templatesService.Resolve(model.SelectedProjectTemplate);
                builder = builder.WithModel(model);
            }
            else if (model.SelectedVBProject is not null)
            {
                if (_workspaceModulesService is not null)
                {
                    var workspaceRoot = new Uri(_fileSystem.Path.Combine(model.WorkspaceLocation, model.ProjectName));
                    var modules = _workspaceModulesService.GetWorkspaceModules(model.SelectedVBProject.ProjectId, workspaceRoot, model.ScanFolderAnnotations);
                    foreach (var module in modules)
                    {
                        builder.WithModule(module);
                    }

                    var references = _workspaceModulesService.GetWorkspaceReferences(model.SelectedVBProject.ProjectId);
                    foreach (var reference in references)
                    {
                        builder.WithReference(reference);
                    }
                }
            }
                        
            return builder.WithProjectName(model.ProjectName).Build();
        }
    }
}
