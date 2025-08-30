using Microsoft.AspNetCore.Mvc;
using MDS.Api.Controllers;
using MDS.Api.Tests.Support;
using MDS.Application.Abstractions.Data;

namespace MDS.Api.Tests.Controllers;

public sealed class Classifieds_Pagination_Edge_Tests
{
    [Fact]
    public async Task GetTop_Take_Negative_Returns_Empty()
    {
        IClassifiedsUnifiedReadRepository repo = new FakeClassifiedsUnifiedReadRepository();
        var sut = new ClassifiedsController(repo);

        var result = await sut.GetTop(take: -5, skip: 0, ct: default);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value!);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetTop_Skip_Negative_Treated_As_Zero()
    {
        IClassifiedsUnifiedReadRepository repo = new FakeClassifiedsUnifiedReadRepository();
        var sut = new ClassifiedsController(repo);

        var resultNeg = await sut.GetTop(take: 5, skip: -10, ct: default);
        var okNeg = Assert.IsType<OkObjectResult>(resultNeg);
        var listNeg = Assert.IsAssignableFrom<IEnumerable<object>>(okNeg.Value!);
        var countNeg = listNeg.Count();

        var resultZero = await sut.GetTop(take: 5, skip: 0, ct: default);
        var okZero = Assert.IsType<OkObjectResult>(resultZero);
        var listZero = Assert.IsAssignableFrom<IEnumerable<object>>(okZero.Value!);
        var countZero = listZero.Count();

        Assert.Equal(countZero, countNeg);
    }

    [Fact]
    public async Task GetTop_Zero_Take_Returns_Empty()
    {
        IClassifiedsUnifiedReadRepository repo = new FakeClassifiedsUnifiedReadRepository();
        var sut = new ClassifiedsController(repo);

        var result = await sut.GetTop(take: 0, skip: 0, ct: default);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value!);
        Assert.Empty(list);
    }
}