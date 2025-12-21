using System.Text.Json;
using System.Text.Json.Serialization;

namespace photo_reviewer_4net;

// Source-generator JSON is a pain in the ass, but worth it to
// get trimming working.

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string,string>))]
internal partial class MyJsonContext : JsonSerializerContext { }

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(List<MediaAPIEntry>))]
[JsonSerializable(typeof(SetRatingRequest))]
internal partial class MyApiJsonContext : JsonSerializerContext { }

public static class JsonService
{
    public static Dictionary<string,string> LoadRatings(Stream stream) {
        return JsonSerializer.Deserialize(stream,
                MyJsonContext.Default.DictionaryStringString)
            ?? throw new Exception("invalid ratings file");
    }

    public static void SaveRatings(Dictionary<string,string> ratings, Stream stream) {
        JsonSerializer.Serialize(stream, ratings,
                MyJsonContext.Default.DictionaryStringString);
    }
}
