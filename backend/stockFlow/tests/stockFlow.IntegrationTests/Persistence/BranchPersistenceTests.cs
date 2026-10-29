using Microsoft.EntityFrameworkCore;
using stockFlow.Domain.Entities;
using stockFlow.Infrastructure.Persistence;
using Xunit;

namespace stockFlow.IntegrationTests.Persistence;

[Trait("Category", "Sql")]
public class BranchPersistenceTests
{
    [Fact]
    public async Task Branch_can_be_saved_and_loaded()
    {
        await using var database = new SqlTestDatabase();
        await database.InitializeAsync();
        await using var setupContext = database.CreateContext();
        {
            var createdAt = new DateTime(
                2026, 10, 21, 10, 0, 0, DateTimeKind.Utc);

            var branch = new Branch(" cai ", " Cairo ", createdAt);

            // Act: حفظ الفرع
            await using (var writeContext = database.CreateContext())
            {
                writeContext.Branches.Add(branch);
                await writeContext.SaveChangesAsync();
            }

            // القراءة بـ Context جديد من قاعدة البيانات
            await using var readContext = database.CreateContext();

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
    }
}
