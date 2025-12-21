using Microsoft.AspNetCore.StaticFiles; // For the content type db

namespace photo_reviewer_4net;

public class MediaAPIEntry {
    public required string DisplayName { get; set; }
    public required string FilePath { get; set; }
    public required string Rating { get; set; }
    public required string FileType { get; set; }
}

public class MediaScannedEntry {
    public required string FilePath { get; set; }
    public required string FileType { get; set; }
}

public class APIService
{
    private ServerOptions _options;
    private Dictionary<string, string> _ratings;  // filePath -> rating
    private List<MediaScannedEntry> _media;
    private HashSet<string> _mediaPaths;  // for quicker lookup
    private FileExtensionContentTypeProvider _contentTypes;

    public APIService(ServerOptions options) {
        _options = options;
        _ratings = new();
        _media = new();
        _mediaPaths = new();
        _contentTypes = new();

        // The default list is
        // https://github.com/dotnet/aspnetcore/blob/main/src/Middleware/StaticFiles/src/FileExtensionContentTypeProvider.cs#L23
        // You can add/change mappings here, if something doesn't play:
        //_contentTypes.Mappings.Add(".mkv", "video/x-matroska");
    }

    public void Init() {
        Console.WriteLine($"\n  Photo Reviewer 4Net -- https://github.com/kjpgit/PhotoReviewer4Net\n");
        if (!File.Exists(_options.RatingsPath)) {
            // Ensure we can create an empty list
            Console.WriteLine($"Creating new ratings file \"{_options.RatingsPath}\"");
            SaveRatings();
        } else {
            // Ensure we can read and re-save it
            Console.WriteLine($"Loading existing ratings file \"{_options.RatingsPath}\"");
            LoadRatings();
            SaveRatings();
        }

        string extensions = String.Join(",", _options.MediaExtensions);
        Console.WriteLine($"Media extensions: {extensions}");
        Console.WriteLine($"Scanning media directory \"{_options.MediaDirectory}\" ...");
        TryScanMediaDirectory();
        Console.WriteLine($"Found ({_media.Count()}) media files");

        if (_media.Count() == 0) {
            Console.WriteLine("\nError: No media files found. Please check your command and try again.");
            Environment.Exit(1);
        }
    }

    public List<MediaAPIEntry> GetMediaList() {
        List<MediaAPIEntry> ret = new();
        foreach (var media in _media) {
            string displayName = media.FilePath;
            if (media.FilePath.StartsWithIgnoreCase(_options.MediaDirectory)) {
                displayName = media.FilePath.Substring(_options.MediaDirectory.Length);
            }
            var entry = new MediaAPIEntry {
                DisplayName = displayName,
                FilePath = media.FilePath,
                FileType = media.FileType,
                Rating = ""
            };
            if (_ratings.TryGetValue(media.FilePath, out string? rating)) {
                entry.Rating = rating;
            }
            ret.Add(entry);
        }

        ret.Sort((a, b) => a.DisplayName.CompareToIgnoreCase(b.DisplayName));

        return ret;
    }

    public bool MediaListContainsFile(string filePath) {
        return _mediaPaths.Contains(filePath);
    }

    public string GetContentType(string filePath) {
        if (_contentTypes.TryGetContentType(filePath, out string? s)) {
            return s;
        } else {
            return "application/octet-stream";
        }
    }

    // Add the filePath if it doesn't exist in the ratings file, otherwise update its rating.
    public void SetRating(string filePath, string rating) {
        // This is called by concurrent HTTP requests, so we need a mutex
        lock (this) {
            _ratings[filePath] = rating;
            SaveRatings();
            Console.WriteLine($"Saved rating \"{rating}\" for \"{filePath}\"");
        }
    }

    private void TryScanMediaDirectory() {
        // Unfortunately, catching invalid UTF8 filenames is impossible.
        // EnumerateFiles always uses 'replacement characters' for invalid utf8.
        // https://github.com/dotnet/runtime/blob/dc580c18effe5914e91f00ace52e0fc1985b1487/src/libraries/Common/src/Interop/Unix/System.Native/Interop.ReadDir.cs#L43-L45

        try {
            var files = Directory.EnumerateFiles(_options.MediaDirectory,
                    "*.*", SearchOption.AllDirectories);

            foreach (string f in files) {
                string extension = Path.GetExtension(f).ToLower();
                if (_options.MediaExtensions.Contains(extension)) {
                    var entry = new MediaScannedEntry {
                        FilePath = f,
                        FileType = Constants.VideoExtensions.Contains(extension) ? "VIDEO" : "IMAGE"
                    };
                    _media.Add(entry);
                    _mediaPaths.Add(entry.FilePath);
                }
            }
        } catch (System.UnauthorizedAccessException e) {
            Console.WriteLine($"Error: {e.Message}");
            Environment.Exit(1);
        }
    }

    private void LoadRatings() {
        //Console.WriteLine($"Loading {_options.RatingsPath}");
        using var stream = File.OpenRead(_options.RatingsPath);
        _ratings = JsonService.LoadRatings(stream);
    }

    // NB: Make sure you have a lock before calling
    private void SaveRatings() {
        //Console.WriteLine($"Writing {_options.RatingsPath}");

        // Write to temp file and rename, to avoid loss of data on a crash.
        string tmpFile = _options.RatingsPath + ".tmp";
        using (var stream = File.OpenWrite(tmpFile)) {
            JsonService.SaveRatings(_ratings, stream);
        }
        File.Move(tmpFile, _options.RatingsPath, overwrite: true);
    }
}
