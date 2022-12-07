// registry path: HKEY_CLASSES_ROOT\*\shell
// maybe also add for right clicking the dir background

// If registry key does not exist AND is admin, try to create registry key
// If no CLI parameters, exit
// Check if CLI parameters are files that exist
//   If so, tar.gz to the topmost directory
// Check if CLI parameter single directory
//  If so, auto-OSR-tar

Console.WriteLine("args:");
Console.WriteLine(string.Join(Environment.NewLine, args));
Console.WriteLine("stdin:");
var readvalue = Console.Read();
while (readvalue != -1)
{
    Console.Write((char)readvalue);
    readvalue = Console.Read();
}
Console.WriteLine("done.");