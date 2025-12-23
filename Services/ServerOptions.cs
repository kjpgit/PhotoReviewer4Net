using System.Text;

namespace photo_reviewer_4net;

public class ServerOptions
{
    public readonly string MediaDirectory;  // /home/karl/Pictures/
    public readonly string RatingsPath;     // ./ratings.json
    public readonly bool AllowAllIps;       // Bind socket to 0.0.0.0
    public readonly int ListenPort;
    public readonly bool UseDevWebRoot;    // For hot loading files from $PWD/wwwroot/ without recompiling
    public readonly bool VerboseRequestLogging;
    public readonly List<string> MediaExtensions;

    public ServerOptions(string[] raw_args) {
        AllowAllIps = false;
        ListenPort = Constants.DefaultListenPort;
        MediaExtensions = Constants.DefaultMediaExtensions.ToList();
        UseDevWebRoot = Environment.GetEnvironmentVariable("USE_DEV_WEBROOT") != null;
        VerboseRequestLogging = false;

        List<string> positionalArgs = new();
        for (int i = 0; i < raw_args.Length; i++) {
            string arg = raw_args[i];
            //Console.WriteLine($"arg {arg}, {i}");
            if (arg == "-a") {
                if (IsDocker()) {
                    Print($"Error: {arg} doesn't make sense in a container");
                    Environment.Exit(1);
                }
                AllowAllIps = true;
            } else if (arg == "-p") {
                if (IsDocker()) {
                    Print($"Error: {arg} doesn't make sense in a container");
                    Environment.Exit(1);
                }
                string p = RequireArg(raw_args, ++i, "PORT");
                if (Int32.TryParse(p, out int port)) {
                    if (port < 0 || port > 65535) {
                        Print("Error: port is out of range");
                        ExitInvalidArgs();
                    }
                    ListenPort = port;
                } else {
                    Print("Error: PORT must be a valid number");
                    ShowUsage();
                    ExitInvalidArgs();
                }
            } else if (arg == "-e") {
                string ext = RequireArg(raw_args, ++i, "EXTENSIONS");
                MediaExtensions = ext.ToLower().Split(",").Select(x => x.Trim()).ToList();
                foreach (string s in MediaExtensions) {
                    if (!s.StartsWith(".")) {
                        Print($"Error: Extension \"{s}\" must start with a \".\"");
                        ExitInvalidArgs();
                    }
                }
            } else if (arg == "-v") {
                VerboseRequestLogging = true;
            } else if (arg == "-h" || arg == "--help" || arg == "help") {
                ShowUsage(true);
                Environment.Exit(0);
            } else {
                positionalArgs.Add(arg);
            }
        }

        if (positionalArgs.Count < 1) {
            Print("Required argument missing: MEDIA_DIRECTORY");
            ShowUsage();
            ExitInvalidArgs();
        }

        if (positionalArgs.Count < 2) {
            Print("Required argument missing: RATINGS_FILE");
            ShowUsage();
            ExitInvalidArgs();
        }

        if (positionalArgs.Count > 2) {
            string arg = positionalArgs[2];
            Print($"Error: Unknown argument \"{arg}\".");
            ShowUsage();
            ExitInvalidArgs();
        }

        MediaDirectory = positionalArgs[0];
        if (IsWindows() && MediaDirectory.StartsWith("~")) {
            // Because *of course* PowerShell can't be bothered with expanding this.
            string home = Environment.GetEnvironmentVariable("USERPROFILE")
                    ?? throw new Exception("No USERPROFILE env var");
            MediaDirectory = home + MediaDirectory.Substring(1);
        }
        if (!MediaDirectory.EndsWith(Path.DirectorySeparatorChar)) {
            // This is so we can compute a shorter DisplayName easily,
            // and reduces different combinations to test.
            MediaDirectory += Path.DirectorySeparatorChar;
        }
        if (!Path.IsPathFullyQualified(MediaDirectory)) {
            // Asp.net has strange rules for hosting relative/nonrooted files,
            // so just be strict.  Being explicit reduces accidents, too.
            Print($"Error: The media directory \"{MediaDirectory}\" needs to be"
                    + " an absolute / fully qualified path.");
            ExitInvalidArgs();
        }
        if (!Directory.Exists(MediaDirectory)) {
            Print($"Error: \"{MediaDirectory}\" is not a directory.");
            ShowUsage();
            ExitInvalidArgs();
        }

        RatingsPath = positionalArgs[1];
        if (Path.GetExtension(RatingsPath).ToLower() != ".json") {
            Print($"Error: Ratings file \"{RatingsPath}\" must have a .json extension.");
            ShowUsage();
            ExitInvalidArgs();
        }
        if (Directory.Exists(RatingsPath)) {
            Print($"Error: Ratings file \"{RatingsPath}\" is a directory.");
            ShowUsage();
            ExitInvalidArgs();
        }
    }

    public string GetListenUrl() {
        string host = AllowAllIps ? "0.0.0.0" : "localhost";
        return $"http://{host}:{ListenPort}";
    }

    private void ShowUsage(bool fullHelp = false) {
        string prog = IsWindows() ? "photo_reviewer_4net.exe" : "photo_reviewer_4net";
        string mediaDir = IsWindows() ? "~\\Pictures\\2025" : "~/Pictures/2025";
        string ratingsFile = IsWindows() ? "c:\\temp\\ratings.json" : "/tmp/ratings.json";

        if (fullHelp) {
            Print("-------------------------------------------------------------------------------\n");
        }

        Print($"Usage: {prog} MEDIA_DIRECTORY RATINGS_FILE [OPTIONS]");
        Print($"Example: {prog} {mediaDir} {ratingsFile} -a");

        if (fullHelp) {
            Print($@"
MEDIA_DIRECTORY:     Directory or subdirectory containing images and videos
                     you want to review in this session.

RATINGS_FILE:        JSON file that will be created or updated, and will
                     contain the filename -> rating entries.

OPTIONS:
  -a                 Listen on 0.0.0.0 (All IPs allowed, not just localhost)

  -p PORT            Listen on PORT, instead of the default (8080)

  -e EXTENSIONS      Comma separated list of file extensions that will be
                     accepted during directory scan.  Default:
{DefaultExtensionsHelp()}

  -v                 Be verbose; show all HTTP requests

-------------------------------------------------------------------------------

To list filenames with a specific rating, null-terminated (for xargs -0):
  {prog} --print0 {ratingsFile} Bad

To list filenames with a specific rating, newline-terminated (for PowerShell):
  {prog} --println {ratingsFile} Bad

-------------------------------------------------------------------------------
  ");
        } else {
            Print($"For full help: {prog} -h");
        }
    }

    private string DefaultExtensionsHelp() {
        var lines = new List<StringBuilder>();
        lines.Add(new());
        foreach (var ext in Constants.DefaultMediaExtensions) {
            var line = lines.Last();
            if (line.Length > 0) {
                line.Append(",");
            }
            if (line.Length > 50) {
                lines.Add(new());
                line = lines.Last();
            }
            line.Append(ext);
        }
        foreach (var line in lines) {
            line.Insert(0, "                     ");
        }
        return String.Join("\n", lines);
    }

    private void ExitInvalidArgs() {
        Environment.Exit(2);
    }

    private string RequireArg(string[] args, int i, string label) {
        if (args.Length <= i) {
            Print($"Error: Additional argument {label} is missing.");
            ShowUsage();
            ExitInvalidArgs();
        }
        return args[i];
    }

    private void Print(string m) {
        Console.WriteLine(m);
    }

    public bool IsDocker() {
        return String.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true");
    }

    private bool IsWindows() {
        return Environment.OSVersion.Platform == PlatformID.Win32NT;
    }
}
