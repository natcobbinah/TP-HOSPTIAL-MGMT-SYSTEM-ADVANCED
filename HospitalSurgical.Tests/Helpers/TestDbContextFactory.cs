using HospitalSurgical.Infrastructure.Data;
using HospitalSurgical.Infrastructure.Interceptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HospitalSurgical.Tests.Helpers;

public class TestDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public SurgicalDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SurgicalDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new AuditInterceptor(new CurrentUserService()))
            .Options;

        var context = new SurgicalDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}