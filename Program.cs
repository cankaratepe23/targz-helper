// registry path: HKEY_CLASSES_ROOT\*\shell
// maybe also add for right clicking the dir background
// If registry key does not exist AND is admin, try to create registry key
// If no CLI parameters, exit
// Check if CLI parameters are files that exist
//   If so, tar.gz to the topmost directory
// Check if CLI parameter single directory
//  If so, auto-OSR-tar

using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
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
        Excludes = new List<string>
        {
            "^.*\\.tar$",
            "^.*\\.gz$"
        },
        Includes = new List<string>
        {
            "^.*\\.py$",
            "^.*\\.txt$",
            "^.*\\.csv$",
            "^configuration_info.xml$",
            "^scope.xml$",
            "^fallback/configuration_info.xml$",
            "^fallback/scope.xml$"
        }
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
    .PageSize(12);

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

var pythonFile = selectedFiles.FirstOrDefault(f => f.EndsWith(".py"));

var outputFilenameWithoutExtension = "LOY-DBF-OSR-" +
                                     DateTime.Now.Year.ToString().Substring(2, 2) +
                                     DateTime.Now.Month.ToString("D2") +
                                     DateTime.Now.Day.ToString("D2") +
                                     "-" +
                                     Path.GetFileNameWithoutExtension(pythonFile)?.Replace("execute_", "")
                                         .Replace("_LCP", "");
//TODO: Consider making the (python file --to-> tarball name) conversion configurable via settings

var userFilenameWithoutExtension = AnsiConsole.Prompt(
    new TextPrompt<string>(
            "Enter the filename for the output tarball, without extension (leave blank to accept default value):")
        .DefaultValue(outputFilenameWithoutExtension!)
        .ShowDefaultValue(!string.IsNullOrWhiteSpace(outputFilenameWithoutExtension))
);

var tarGzFilename = userFilenameWithoutExtension + ".tar.gz";
var i = 0;
var match = Regex.Match(userFilenameWithoutExtension, @"^(.*)([0-9]+)$");
if (match.Success && match.Groups.TryGetValue("1", out var filenameValue) &&
    match.Groups.TryGetValue("2", out var numberValue))
{
    userFilenameWithoutExtension = filenameValue.Value;
    i = Convert.ToInt32(numberValue.Value);
}

while (File.Exists(Path.Combine(workingDir, tarGzFilename)))
{
    i++;
    tarGzFilename = userFilenameWithoutExtension + i + ".tar.gz";
}

Console.WriteLine($"Creating tarball ({tarGzFilename})...");

using (var fs = new FileStream(Path.Combine(workingDir, tarGzFilename), FileMode.CreateNew, FileAccess.Write,
           FileShare.None))
using (Stream gzipStream = new GZipOutputStream(fs))
using (var tarArchive = TarArchive.CreateOutputTarArchive(gzipStream))
{
    foreach (string filename in selectedFiles)
    {
        {
            // TODO: This is sample code from the internet, fix this
            TarEntry tarEntry = TarEntry.CreateEntryFromFile(filename);
            tarArchive.WriteEntry(tarEntry, false);
        }
    }
}

Console.ReadLine();