using System.Threading.Channels;
using Npgsql;

namespace SbomQualityGate.Worker.Services;

public class PostgresNotificationListener(string connectionString) : IDisposable
{
    private NpgsqlConnection? _connection;

    private readonly Channel<bool> _channel = Channel.CreateUnbounded<bool>();

    public ChannelReader<bool> Reader => _channel.Reader;
    
    private Task? _waitTask;
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _connection = new NpgsqlConnection(connectionString);
        await _connection.OpenAsync(cancellationToken);

        _connection.Notification += (_, _) =>
        {
            _channel.Writer.TryWrite(true);
        };

        await using var cmd = new NpgsqlCommand("LISTEN validation_jobs;", _connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public Task WaitAsync(CancellationToken cancellationToken)
    {
        if (_connection == null)
            return Task.CompletedTask;

        if (_waitTask == null || _waitTask.IsCompleted)
        {
            _waitTask = _connection.WaitAsync(cancellationToken);
        }

        return _waitTask;
    }

    public void Dispose()
    {
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}
