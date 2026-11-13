using DTMAPI.Tooling.Metadata;
if (args.Length < 2) { Console.Error.WriteLine("Usage: DTMAPI.NativeReferences <output-directory> <DLL> [...]"); return 2; }
string output = Path.GetFullPath(args[0]);
string[] files = args.Skip(1).Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
string[] directories = files.Select(path => Path.GetDirectoryName(path)!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
if (directories.Any(directory => output.Equals(directory, StringComparison.OrdinalIgnoreCase))) throw new IOException("Output must differ from input directories.");
Directory.CreateDirectory(output);
foreach (string file in files) File.WriteAllBytes(Path.Combine(output, Path.GetFileName(file)), NativeReferenceSurface.Create(File.ReadAllBytes(file), directories));
Console.WriteLine("Compiler reference surfaces: " + output);
return 0;
