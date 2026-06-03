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
            CREATE TABLE IF NOT EXISTS sessions (
                xi         TEXT PRIMARY KEY,
                from_      TEXT NOT NULL,
                callid     TEXT NOT NULL,
                updated_at INTEGER NOT NULL DEFAULT 0
            )
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Returns the previous record (or null if not found), then upserts the new values.
    /// </summary>
    public (string? prevFrom, string? prevCallId) Upsert(string xi, string from, string callId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        // Read existing
        string? prevFrom = null;
        string? prevCallId = null;

        var selectCmd = conn.CreateCommand();
        selectCmd.CommandText = "SELECT from_, callid FROM sessions WHERE xi = $xi";
        selectCmd.Parameters.AddWithValue("$xi", xi);

        using (var reader = selectCmd.ExecuteReader())
        {
            if (reader.Read())
            {
                prevFrom = reader.GetString(0);
                prevCallId = reader.GetString(1);
            }
        }

        // Upsert new values
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var upsertCmd = conn.CreateCommand();
        upsertCmd.CommandText = """
            INSERT INTO sessions (xi, from_, callid, updated_at) VALUES ($xi, $from, $callid, $now)
            ON CONFLICT(xi) DO UPDATE SET from_ = excluded.from_, callid = excluded.callid, updated_at = excluded.updated_at
            """;
        upsertCmd.Parameters.AddWithValue("$xi", xi);
        upsertCmd.Parameters.AddWithValue("$from", from);
        upsertCmd.Parameters.AddWithValue("$callid", callId);
        upsertCmd.Parameters.AddWithValue("$now", now);
        upsertCmd.ExecuteNonQuery();

        return (prevFrom, prevCallId);
    }

    public int DeleteOlderThan(TimeSpan age)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(age).ToUnixTimeSeconds();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM sessions WHERE updated_at < $cutoff";
        cmd.Parameters.AddWithValue("$cutoff", cutoff);
        return cmd.ExecuteNonQuery();
    }
}
