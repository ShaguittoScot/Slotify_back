using Microsoft.EntityFrameworkCore;
using Slotify.Infrastructure.Data;

namespace Slotify.Tests;

public sealed class CalendarPostgresTests
{
    private const string ConnectionVariable = "SLOTIFY_TEST_POSTGRES";

    [PostgresFact]
    public async Task CalendarDatabase_BloqueaPorNegocioHastaElCommit_YConsultaPersonal()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable(ConnectionVariable)!)
            .Options;

        await using var owner = new AppDbContext(options);
        await using var contender = new AppDbContext(options);
        var database = new CalendarDatabase(owner);
        var businessId = Guid.NewGuid();

        await database.WithBusinessLockAsync(businessId, async token =>
        {
            Assert.False(await TryAcquireLock(contender, businessId, token));
            Assert.True(await TryAcquireLock(contender, Guid.NewGuid(), token));
            return true;
        }, timeout.Token);

        Assert.True(await TryAcquireLock(contender, businessId, timeout.Token));
        Assert.Empty(await database.GetEmployeesAsync(businessId, timeout.Token));
    }

    private static Task<bool> TryAcquireLock(AppDbContext context, Guid businessId, CancellationToken token)
    {
        var key = $"slotify:calendar:{businessId:D}";
        return context.Database.SqlQuery<bool>(
            $"SELECT pg_try_advisory_xact_lock(hashtextextended({key}, 0)) AS \"Value\""
        ).SingleAsync(token);
    }

    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionVariable)))
                Skip = $"Configura {ConnectionVariable} para ejecutar la prueba con PostgreSQL.";
        }
    }
}
