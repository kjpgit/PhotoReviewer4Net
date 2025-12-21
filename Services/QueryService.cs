// Query the ratings.json file from the CLI.
// Easier for some people than using/installing jq (especially on Windows)
//
// But if you like jq magic:
//   jq --raw-output0 'to_entries[] | select(.value == "Bad") | .key' /tmp/ratings.json
//

namespace photo_reviewer_4net;

public class QueryService
{
    public static void PrintFiles(string[] args) {
        if (args.Length != 3) {
            Console.Error.WriteLine($"Error: {args[0]} needs exactly two arguments: RATINGS_FILE and RATING");
            Environment.Exit(2);
        }
        bool useNullTermination = (args[0] == "--print0");
        string ratingsPath = args[1];
        string ratingFilter = args[2];

        if (!File.Exists(ratingsPath)) {
            Console.Error.WriteLine($"Error: {ratingsPath} is not a valid file");
            Environment.Exit(2);
        }

        if (ValidFiltersCLI.Count(x => x.EqualsIgnoreCase(ratingFilter)) == 0) {
            string valid = String.Join(", ", ValidFiltersCLI);
            Console.Error.WriteLine($"Error: {ratingFilter} is not a valid rating");
            Console.Error.WriteLine($"Please use one of: {valid}");
            Environment.Exit(2);
        }

        using var stream = File.OpenRead(ratingsPath);
        var database = JsonService.LoadRatings(stream);
        foreach ((string path, string rating) in database) {
            if (!RatingMatchesFilter(rating, ratingFilter)) {
                continue;
            }

            if (useNullTermination) {
                Console.Write(path);
                Console.Write('\0');
            } else {
                if (path.Contains("\n") || path.Contains("\r")) {
                    Console.Error.WriteLine($"Error: Cowardly refusing to print a path containing a newline: '{path}'");
                    Console.Error.WriteLine($"Please use --print0, and carefully check your filenames");
                    Environment.Exit(1);
                }
                Console.Write(path);
                Console.Write('\n');
            }
        }
    }

    // NB: The API also calls this, for downloading the file list, so we allow Categories too.
    // The CLI only allows exact rating match.
    public static bool RatingMatchesFilter(string rating, string filter) {
        return (filter.EqualsIgnoreCase(Categories.Everything)
                || (filter.EqualsIgnoreCase(Categories.Rated) && rating != "")
                || (filter.EqualsIgnoreCase(Categories.Unrated) && rating == "")
                || filter.EqualsIgnoreCase(rating));
    }

    // The Categories only make sense for the GUI/server, because it has all files loaded.
    private static string[] ValidFiltersCLI => [Ratings.Good, Ratings.Bad, Ratings.Unsure];
}
