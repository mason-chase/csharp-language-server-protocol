using Microsoft.Extensions.Logging;

namespace OmniSharp.Extensions.McpServer.Services;

public interface ISolutionContext
{
    string? SolutionPath { get; }
    string? SolutionDirectory { get; }
    bool IsLoaded { get; }
    Task<bool> LoadSolutionAsync(string solutionPath);
    List<string> GetProjects();
    List<string> FindFiles(string pattern);
}

public class SolutionContext : ISolutionContext
{
    private readonly ILogger<SolutionContext> _logger;
    private string? _solutionPath;
    private List<string> _projects = new();
    
    public string? SolutionPath => _solutionPath;
    public string? SolutionDirectory => _solutionPath != null ? Path.GetDirectoryName(_solutionPath) : null;
    public bool IsLoaded => !string.IsNullOrEmpty(_solutionPath);

    public SolutionContext(ILogger<SolutionContext> logger)
    {
        _logger = logger;
    }

    public async Task<bool> LoadSolutionAsync(string solutionPath)
    {
        try
        {
            if (!File.Exists(solutionPath))
            {
                _logger.LogError("Solution file not found: {SolutionPath}", solutionPath);
                return false;
            }

            _solutionPath = Path.GetFullPath(solutionPath);
            _logger.LogInformation("Loading solution: {SolutionPath}", _solutionPath);

            // Parse solution file to extract projects
            _projects.Clear();
            var solutionContent = await File.ReadAllTextAsync(_solutionPath);
            var lines = solutionContent.Split('\n');
            
            foreach (var line in lines)
            {
                // Look for project lines in solution file
                // Format: Project("{GUID}") = "ProjectName", "RelativePath\Project.csproj", "{GUID}"
                if (line.TrimStart().StartsWith("Project(") && line.Contains(".csproj"))
                {
                    var parts = line.Split('"');
                    if (parts.Length >= 6)
                    {
                        var projectPath = parts[5];
                        if (!string.IsNullOrWhiteSpace(projectPath))
                        {
                            var fullProjectPath = Path.GetFullPath(Path.Combine(SolutionDirectory!, projectPath));
                            _projects.Add(fullProjectPath);
                        }
                    }
                }
            }

            _logger.LogInformation("Loaded solution with {ProjectCount} projects", _projects.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load solution: {SolutionPath}", solutionPath);
            return false;
        }
    }

    public List<string> GetProjects()
    {
        return new List<string>(_projects);
    }

    public List<string> FindFiles(string pattern)
    {
        if (!IsLoaded || SolutionDirectory == null)
            return new List<string>();

        try
        {
            var searchOption = pattern.Contains("**") ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var searchPattern = pattern.Replace("**", "*");
            
            return Directory.GetFiles(SolutionDirectory, searchPattern, searchOption)
                .Select(f => Path.GetRelativePath(SolutionDirectory, f))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding files with pattern: {Pattern}", pattern);
            return new List<string>();
        }
    }
}