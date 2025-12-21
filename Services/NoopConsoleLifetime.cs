// Override the default asp.net lifetime handler, because it always
// seems to hang on ctrl-c (It annoys the user).
// We have nothing in memory to flush, worst case is we leave a tmp file on disk,
// which will be cleaned up on the next launch.

public class NoopConsoleLifetime : IHostLifetime, IDisposable
{
    public NoopConsoleLifetime() { }

    public Task StopAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    public Task WaitForStartAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    public void Dispose() { }
}
