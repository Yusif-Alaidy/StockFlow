using Microsoft.EntityFrameworkCore;
using stockFlow.Domain.Entities;
using stockFlow.Infrastructure.Persistence;
using Xunit;

namespace stockFlow.IntegrationTests.Persistence;

public class BranchPersistenceTests
{
    [Fact]
    public async Task Branch_can_be_saved_and_loaded()
    {
        // Arrange: قاعدة منفصلة لكل تشغيل
        var databaseName = $"StockFlowTests_{Guid.NewGuid():N}";

        var connectionString =
            $@"Server=.;Database={databaseName};" +
            "Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<StockFlowDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var setupContext = new StockFlowDbContext(options);

        try
        {
            await setupContext.Database.MigrateAsync();

            var createdAt = new DateTime(
                2026, 10, 21, 10, 0, 0, DateTimeKind.Utc);

            var branch = new Branch(" cai ", " Cairo ", createdAt);

            // Act: حفظ الفرع
            await using (var writeContext = new StockFlowDbContext(options))
            {
                writeContext.Branches.Add(branch);
                await writeContext.SaveChangesAsync();
            }

            // القراءة بـ Context جديد من قاعدة البيانات
            await using var readContext = new StockFlowDbContext(options);

            var savedBranch = await readContext.Branches
                .AsNoTracking()
                .SingleAsync(x => x.Id == branch.Id);

            // Assert
            Assert.Equal("CAI", savedBranch.Code);
            Assert.Equal("Cairo", savedBranch.Name);
            Assert.True(savedBranch.IsActive);
            Assert.Equal(createdAt, savedBranch.CreatedAt);
            Assert.Equal(DateTimeKind.Utc, savedBranch.CreatedAt.Kind);
        }
        finally
        {
            // حذف قاعدة الاختبار المؤقتة فقط
            await setupContext.Database.EnsureDeletedAsync();
        }
    }
}
