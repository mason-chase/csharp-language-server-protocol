using Microsoft.Extensions.Logging;

namespace SampleServer
{
    public class WorkspaceHandler
    {
        private readonly ILogger<WorkspaceHandler> _logger;
        private MSBuildWorkspace _workspace;
        private Solution _solution;

        public WorkspaceHandler(ILogger<WorkspaceHandler> logger)
        {
            _logger = logger;
            
            // Register MSBuild (required for loading solutions)
            if (!MSBuildLocator.IsRegistered)
            {
                var instances = MSBuildLocator.QueryVisualStudioInstances();
                MSBuildLocator.RegisterDefaults();
            }
        }

        public async Task<bool> LoadSolutionAsync(string solutionPath)
        {
            try
            {
                _logger.LogInformation("Loading solution: {SolutionPath}", solutionPath);
                
                _workspace = MSBuildWorkspace.Create();
                _workspace.WorkspaceFailed += (sender, e) => 
                {
                    _logger.LogWarning("Workspace failure: {Diagnostic}", e.Diagnostic.Message);
                };

                _solution = await _workspace.OpenSolutionAsync(solutionPath);
                
                _logger.LogInformation("Loaded solution with {ProjectCount} projects", _solution.Projects.Count());
                
                foreach (var project in _solution.Projects)
                {
                    _logger.LogInformation("  - Project: {ProjectName}", project.Name);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load solution");
                return false;
            }
        }

        public async Task<Document> GetDocumentAsync(string filePath)
        {
            if (_solution == null) return null;

            foreach (var project in _solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    if (document.FilePath == filePath)
                    {
                        return document;
                    }
                }
            }

            return null;
        }
    }
}