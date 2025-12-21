namespace photo_reviewer_4net;

public static class Constants
{
    public const int DefaultListenPort = 8080;

    // NB: .tiff not supported in chrome, complex format (multi-page and layers).
    public static string[] ImageExtensions = [
        ".avif", ".bmp", ".gif", ".ico",
        ".jfif", ".jpg", ".jpeg",
        ".png", ".svg", ".webp"
    ];

    // So the UI knows if it should display as an image or a video.
    public static string[] VideoExtensions = [
        ".avi", ".mpg", ".mpeg", ".mp2", ".mp4", ".m4v",
        ".mov", ".mkv", ".ogg", ".ogv", ".webm"
    ];

    public static string[] DefaultMediaExtensions = [..ImageExtensions, ..VideoExtensions];
}

public static class Categories
{
    public const string Everything = "Everything";
    public const string Unrated    = "Unrated";
    public const string Rated      = "Rated";
}

public static class Ratings
{
    public const string Good   = "Good";
    public const string Unsure = "Unsure";
    public const string Bad    = "Bad";
}
