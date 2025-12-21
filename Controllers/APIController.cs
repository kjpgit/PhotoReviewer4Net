using Microsoft.AspNetCore.Mvc;

namespace photo_reviewer_4net;

public class SetRatingRequest {
    public required string FilePath { get; set; }
    public required string Rating { get; set; }
}

//
// This uses the minimal APIs, so we can have trimming support when publishing the exe.
// A 20MB windows binary is pretty sweet, compared to a 100MB one!
// However, we try to look as much like a normal controller as we can.
// Thanks to: https://blog.variant.no/moving-from-controllers-to-minimal-api-bda56a223cc8
//
public static class APIController
{
    public static void MapEndpoints(WebApplication app)
    {
        app.MapGet("/api/GetMediaList", GetMediaList);
        app.MapGet("/api/GetMediaStream", GetMediaStream);
        app.MapPost("/api/SetRating", SetRating);
        app.MapGet("/api/DownloadFileList", DownloadFileList);
    }

    private static List<MediaAPIEntry> GetMediaList(APIService service, HttpContext context)
    {
        // no-store just in case a future browser version tries to be too aggressive
        context.Response.Headers.Append("Cache-Control", "no-store");
        return service.GetMediaList();
    }

    // This supports the Range header, which is needed for Chrome to support
    // seeking in a video file.
    private static IResult GetMediaStream(string filePath, APIService service, HttpContext context)
    {
        // For security, make sure the request is for a file that exists in the media list
        // (Not some random file from /etc or your home dir)
        if (!service.MediaListContainsFile(filePath)) {
            return TypedResults.NotFound("That file is not in your media list.");
        }

        // "Require" the media gets revalidated, in case the user is making edits to the files.
        // Require == Chrome doesn't give a damn about web standards, maybe it will revalidate...
        context.Response.Headers.Append("Cache-Control", "no-cache");
        return TypedResults.PhysicalFile(filePath,
                contentType: service.GetContentType(filePath),
                fileDownloadName: Path.GetFileName(filePath),
                enableRangeProcessing: true);
    }

    private static IResult SetRating([FromBody]SetRatingRequest entry, APIService service)
    {
        // For sanity, check that the filepath is in the current media list.
        // (The server could have restarted, and the browser hasn't refreshed.)
        // Aren't distributed systems fun?
        if (!service.MediaListContainsFile(entry.FilePath)) {
            return TypedResults.NotFound("That file is not in your media list.");
        }

        service.SetRating(entry.FilePath, entry.Rating);
        return TypedResults.Ok();
    }

    private static IResult DownloadFileList(string ratingFilter, APIService service, HttpContext context)
    {
        // We don't need a BOM
        var encoding = new System.Text.UTF8Encoding(false);
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, encoding);

        var mediaList = service.GetMediaList();
        foreach (var entry in mediaList) {
            if (QueryService.RatingMatchesFilter(entry.Rating, ratingFilter)) {
                writer.Write(entry.DisplayName);
                writer.Write('\n');
            }
        }

        writer.Flush();

        byte[] data = stream.ToArray();
        var ret = TypedResults.File(data,
                contentType: "text/plain",
                fileDownloadName: $"PhotoReviewer4Net-{ratingFilter}.txt");
        context.Response.Headers.Append("Cache-Control", "no-store");
        return ret;
    }
}
