using SQLite;
using CryptoFear.Models;
using System.Diagnostics;

namespace CryptoFear.Services;

public class DataService : IDataService
{
    private SQLiteAsyncConnection? _database;
    private readonly string _databasePath;

    public DataService()
    {
        _databasePath = Path.Combine(FileSystem.AppDataDirectory, "cryptofear.db3");
    }

    private Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        if (_database != null)
            return Task.FromResult(_database);

        _database = new SQLiteAsyncConnection(_databasePath);
        return Task.FromResult(_database);
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            var db = await GetDatabaseAsync();
            await db.CreateTableAsync<UserEntry>();
            await db.CreateTableAsync<WatchedWallet>();
            await db.CreateTableAsync<ComputedSentimentPoint>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing database: {ex.Message}");
            throw;
        }
    }

    public async Task<List<UserEntry>> GetAllEntriesAsync()
    {
        try
        {
            var db = await GetDatabaseAsync();
            return await db.Table<UserEntry>()
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting all entries: {ex.Message}");
            return new List<UserEntry>();
        }
    }

    public async Task<UserEntry?> GetEntryByIdAsync(int id)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<UserEntry>()
            .Where(e => e.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> SaveEntryAsync(UserEntry entry)
    {
        try
        {
            var db = await GetDatabaseAsync();
            entry.CreatedAt = DateTime.UtcNow;
            return await db.InsertAsync(entry);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving entry: {ex.Message}");
            throw;
        }
    }

    public async Task<int> DeleteEntryAsync(UserEntry entry)
    {
        var db = await GetDatabaseAsync();
        return await db.DeleteAsync(entry);
    }

    public async Task<List<WatchedWallet>> GetAllWalletsAsync()
    {
        try
        {
            var db = await GetDatabaseAsync();
            return await db.Table<WatchedWallet>()
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting all wallets: {ex.Message}");
            return new List<WatchedWallet>();
        }
    }

    public async Task<int> SaveWalletAsync(WatchedWallet wallet)
    {
        try
        {
            var db = await GetDatabaseAsync();
            wallet.CreatedAt = DateTime.UtcNow;
            return await db.InsertAsync(wallet);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save wallet: {ex.Message}");
            throw;
        }
    }

    public async Task<int> UpdateWalletAsync(WatchedWallet wallet)
    {
        var db = await GetDatabaseAsync();
        wallet.LastUpdated = DateTime.UtcNow;
        return await db.UpdateAsync(wallet);
    }

    public async Task<int> DeleteWalletAsync(WatchedWallet wallet)
    {
        var db = await GetDatabaseAsync();
        return await db.DeleteAsync(wallet);
    }

    public async Task<List<ComputedSentimentPoint>> GetSentimentPointsAsync(int days)
    {
        try
        {
            var db = await GetDatabaseAsync();
            return await db.Table<ComputedSentimentPoint>()
                .OrderByDescending(s => s.TimestampUtc)
                .Take(Math.Max(days, 1))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Couldn't load sentiment points: {ex.Message}");
            return new List<ComputedSentimentPoint>();
        }
    }

    public async Task<ComputedSentimentPoint?> GetLatestSentimentPointAsync()
    {
        var db = await GetDatabaseAsync();
        return await db.Table<ComputedSentimentPoint>()
            .OrderByDescending(s => s.TimestampUtc)
            .FirstOrDefaultAsync();
    }

    public async Task SaveSentimentPointsAsync(IEnumerable<ComputedSentimentPoint> points)
    {
        try
        {
            var db = await GetDatabaseAsync();
            await db.RunInTransactionAsync(tran =>
            {
                foreach (var point in points)
                {
                    tran.InsertOrReplace(point);
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving sentiment points: {ex.Message}");
            throw;
        }
    }
}
