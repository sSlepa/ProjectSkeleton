using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheAdventure.Game;

public class HighScoreStore
{
    private readonly string _filePath;

    public HighScoreStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BombermanSkeleton");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "scores.json");
    }

    public async Task<HighScoreData> LoadAsync()
    {
        if (!File.Exists(_filePath)) return new HighScoreData();
        try
        {
            await using var stream = File.OpenRead(_filePath);
            var data = await JsonSerializer.DeserializeAsync<HighScoreData>(stream);
            return data ?? new HighScoreData();
        }
        catch (JsonException)
        {
            // Corrupted file: start fresh rather than crashing the game.
            return new HighScoreData();
        }
    }

    public async Task SaveAsync(HighScoreData data)
    {
        // Unique temp name so an in-flight save (e.g. fired when the round ends) and the
        // final save on shutdown can never collide on the same file handle.
        var tmp = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        await using (var stream = File.Create(tmp))
        {
            await JsonSerializer.SerializeAsync(stream, data,
                new JsonSerializerOptions { WriteIndented = true });
        }
        File.Move(tmp, _filePath, overwrite: true);
    }
}

public class HighScoreData
{
    [JsonPropertyName("highScore")]
    public int HighScore { get; set; }

    [JsonPropertyName("totalGames")]
    public int TotalGames { get; set; }

    [JsonPropertyName("totalWins")]
    public int TotalWins { get; set; }
}
