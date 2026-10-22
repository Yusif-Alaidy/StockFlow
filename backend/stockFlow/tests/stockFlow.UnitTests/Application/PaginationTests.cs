using stockFlow.Application.Common.Errors;
using stockFlow.Application.Common.Pagination;
using Xunit;

namespace stockFlow.UnitTests.Application;

public class PaginationTests
{
    [Fact]
    public void Defaults_and_valid_offsets_are_predictable()
    {
        var defaults = new PageRequest();
        Assert.Equal(1, defaults.PageNumber); Assert.Equal(20, defaults.PageSize); Assert.Equal(0, defaults.GetSkip());
        Assert.Equal(40, new PageRequest { PageNumber = 3, PageSize = 20 }.GetSkip());
    }
    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(2147483647, 100)]
    public void Invalid_bounds_or_offset_overflow_are_rejected(int page, int size)
    {
        var request = new PageRequest { PageNumber = page, PageSize = size };
        var error = Assert.Throws<AppException>(() => request.GetSkip());
        Assert.Equal(ErrorKind.Validation, error.Kind);
        Assert.NotEmpty(error.Errors!);
    }
}
