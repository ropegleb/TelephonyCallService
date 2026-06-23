using Microsoft.Data.Sqlite;

namespace TelephonyCallService;

public class SessionRepository
{
    private readonly string _connectionString;

    public SessionRepository(IConfiguration config)
    {
        var dbPath = config["Database:Path"] ?? "data/sessions.db";
        _connectionString = $"Data Source={dbPath}";
        EnsureTable();
    }

    private void EnsureTable()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS from_store (
                xi         TEXT PRIMARY KEY,
                from_      TEXT NOT NULL,
                updated_at INTEGER NOT NULL DEFAULT 0
            )
            """;
        cmd.ExecuteNonQuery();
    }

    public void Save(string xi, string from)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO from_store (xi, from_, updated_at) VALUES ($xi, $from, $now)
            ON CONFLICT(xi) DO UPDATE SET from_ = excluded.from_, updated_at = excluded.updated_at
            """;
        cmd.Parameters.AddWithValue("$xi", xi);
        cmd.Parameters.AddWithValue("$from", from);
        cmd.Parameters.AddWithValue("$now", now);
        cmd.ExecuteNonQuery();
    }

    public string? GetFrom(string xi)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT from_ FROM from_store WHERE xi = $xi";
        cmd.Parameters.AddWithValue("$xi", xi);
        var result = cmd.ExecuteScalar();
        return result as string;
    }

    public int DeleteOlderThan(TimeSpan age)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(age).ToUnixTimeSeconds();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM from_store WHERE updated_at < $cutoff";
        cmd.Parameters.AddWithValue("$cutoff", cutoff);
        return cmd.ExecuteNonQuery();
    }
}
