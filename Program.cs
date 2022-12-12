// registry path: HKEY_CLASSES_ROOT\*\shell
// maybe also add for right clicking the dir background
// If registry key does not exist AND is admin, try to create registry key
// If no CLI parameters, exit
// Check if CLI parameters are files that exist
//   If so, tar.gz to the topmost directory
// Check if CLI parameter single directory
//  If so, auto-OSR-tar

using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Spectre.Console;
using targz_helper;

#region Create config file if it doesn't exist

var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "targz-helper");
var configFilePath = Path.Combine(configDir, "targz-helper-config.json");

if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);

if (!File.Exists(configFilePath))
{
    var defaultConfigObject = new ConfigModel
    {
        IsWhitelistEnabled = false,
        Excludes = new List<string> { "*.tar", "*.gz" },
        Includes = new List<string> { "*.py", "*.txt", "*.csv" }
    };

    File.WriteAllText(configFilePath, JsonConvert.SerializeObject(defaultConfigObject, Formatting.Indented));
}

#endregion

#region Parse the config file

var configObject = JsonConvert.DeserializeObject<ConfigModel>(File.ReadAllText(configFilePath));
if (configObject == null) throw new Exception($"Could not read the config file from {configFilePath}");

var isWhitelistEnabled = configObject.IsWhitelistEnabled;
var includes = configObject.Includes;
var excludes = configObject.Excludes;

#endregion

#region Get files in the directory

var workingDir = Directory.GetCurrentDirectory();
var allFiles = Directory.GetFiles(
    workingDir,
    "*",
    new EnumerationOptions()
    {
        MaxRecursionDepth = 1,
        RecurseSubdirectories = true
    })
    .Select(fse => Path.GetRelativePath(workingDir, fse))
    .Where(f => !f.StartsWith('.'))
    .Select(f => f.Replace('\\', '/')) // TODO: This could be problematic
    .OrderBy(f => f.Split('/').Length)
    .ThenBy(f => f);

#endregion


var multiSelect = new MultiSelectionPrompt<string>()
    .Title("Select files to include in the tarball:")
    .PageSize(8);

foreach (var file in allFiles)
{
    if (isWhitelistEnabled)
    {
        if (includes.Any(m => Regex.IsMatch(file, m)))
        {
            multiSelect.AddChoice(file).Select();
        }
        else
        {
            multiSelect.AddChoice(file);
        }
    }
    else
    {
        if (excludes.Any(m => Regex.IsMatch(file, m)))
        {
            multiSelect.AddChoice(file);
        }
        else
        {
            multiSelect.AddChoice(file).Select();
        }
    }
}

var selectedFiles = AnsiConsole.Prompt(multiSelect);
Console.WriteLine($"Selected {selectedFiles.Count} options.");