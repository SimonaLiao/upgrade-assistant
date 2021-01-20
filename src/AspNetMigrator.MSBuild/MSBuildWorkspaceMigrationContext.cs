﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

namespace AspNetMigrator.MSBuild
{
    public sealed class MSBuildWorkspaceMigrationContext : IMigrationContext, IDisposable
    {
        private readonly IVisualStudioFinder _vsFinder;
        private readonly string _path;
        private readonly ILogger<MSBuildWorkspaceMigrationContext> _logger;

        private string? _projectPath;
        private MSBuildWorkspace? _workspace;

        public MSBuildWorkspaceMigrationContext(
            MigrateOptions options,
            IVisualStudioFinder vsFinder,
            ILogger<MSBuildWorkspaceMigrationContext> logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _vsFinder = vsFinder;
            _path = options.ProjectPath;
            _logger = logger;
        }

        public void Dispose()
        {
            _workspace?.Dispose();
            _workspace = null;
        }

        public ICollection<string> CompletedProjects { get; set; } = Array.Empty<string>();

        public async IAsyncEnumerable<IProject> GetProjects([EnumeratorCancellation] CancellationToken token)
        {
            var ws = await GetWorkspaceAsync(token).ConfigureAwait(false);

            foreach (var project in ws.CurrentSolution.Projects)
            {
                if (project.FilePath is null)
                {
                    _logger.LogWarning("Found a project with no file path {Project}", project);
                }
                else
                {
                    yield return new MSBuildProject(ws, project.FilePath, _logger);
                }
            }
        }

        public void SetProject(IProject? projectId)
        {
            _projectPath = projectId?.FilePath;
        }

        private Dictionary<string, string> CreateProperties()
        {
            var properties = new Dictionary<string, string>();
            var vs = _vsFinder.GetLatestVisualStudioPath();

            if (vs is not null)
            {
                properties.Add("VSINSTALLDIR", vs);
                properties.Add("MSBuildExtensionsPath32", Path.Combine(vs, "MSBuild"));
            }

            return properties;
        }

        public async ValueTask<Workspace> GetWorkspaceAsync(CancellationToken token)
            => await GetMsBuildWorkspaceAsync(token).ConfigureAwait(false);

        public async ValueTask<MSBuildWorkspace> GetMsBuildWorkspaceAsync(CancellationToken token)
        {
            if (_workspace is null)
            {
                var properties = CreateProperties();
                var workspace = MSBuildWorkspace.Create(properties);

                workspace.WorkspaceFailed += Workspace_WorkspaceFailed;

                if (_path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                {
                    await workspace.OpenSolutionAsync(_path, cancellationToken: token).ConfigureAwait(false);
                }
                else
                {
                    var project = await workspace.OpenProjectAsync(_path, cancellationToken: token).ConfigureAwait(false);

                    _projectPath = project.FilePath;
                }

                _workspace = workspace;
            }

            return _workspace;
        }

        public void UnloadWorkspace()
        {
            _workspace?.CloseSolution();
            _workspace?.Dispose();
            _workspace = null;
        }

        public async ValueTask ReloadWorkspaceAsync(CancellationToken token)
        {
            UnloadWorkspace();

            if (string.IsNullOrWhiteSpace(_projectPath))
            {
                return;
            }

            await foreach (var project in GetProjects(token))
            {
                if (string.Equals(project.FilePath, _projectPath, StringComparison.OrdinalIgnoreCase))
                {
                    SetProject(project);
                    return;
                }
            }
        }

        private void Workspace_WorkspaceFailed(object? sender, WorkspaceDiagnosticEventArgs e)
        {
            var diagnostic = e.Diagnostic!;

            _logger.LogDebug("[{Level}] Problem loading file in MSBuild workspace {Message}", diagnostic.Kind, diagnostic.Message);
        }

        public async IAsyncEnumerable<(string Name, string Value)> GetWorkspaceProperties([EnumeratorCancellation] CancellationToken token)
        {
            var ws = await GetMsBuildWorkspaceAsync(token).ConfigureAwait(false);

            foreach (var property in ws.Properties)
            {
                yield return (property.Key, property.Value);
            }
        }

        public async ValueTask<IProject?> GetProjectAsync(CancellationToken token)
        {
            if (_projectPath is null)
            {
                return null;
            }

            var ws = await GetWorkspaceAsync(token).ConfigureAwait(false);

            if (ws is not null)
            {
                return new MSBuildProject(ws, _projectPath, _logger);
            }

            return null;
        }

        public bool UpdateSolution(Solution updatedSolution)
        {
            if (_workspace is null)
            {
                _logger.LogWarning("Cannot update solution if no workspace is loaded.");
                return false;
            }

            if (_workspace.TryApplyChanges(updatedSolution))
            {
                _logger.LogDebug("Source successfully updated");
                return true;
            }
            else
            {
                _logger.LogDebug("Failed to apply changes to source");
                return false;
            }
        }
    }
}
