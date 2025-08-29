using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MDS.Api.Tests.Support;

namespace MDS.Api.Tests.Controllers
{
    public class Classifieds_And_Repo_Tests
    {
        [Fact]
        public async Task GetByDay_Returns_Ok_With_Correct_Shape_And_Count()
        {
            await using var factory = new WebAppFactoryClassifieds();
            var client = factory.CreateClient();
            var day = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var resp = await client.GetAsync($"/api/classifieds/by-day?day={day}&take=5&skip=3");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var items = await resp.Content.ReadFromJsonAsync<List<JsonElement>>();

            items.Should().NotBeNull();
            items!.Should().HaveCount(5);
            items!.All(x =>
                    !string.IsNullOrWhiteSpace(x.GetProperty("title").GetString()) &&
                    !string.IsNullOrWhiteSpace(x.GetProperty("description").GetString()) &&
                    !string.IsNullOrWhiteSpace(x.GetProperty("url").GetString()))
                .Should().BeTrue();

            items!.All(x => x.GetProperty("postDate").GetString() == day).Should().BeTrue();
        }

        [Fact]
        public async Task GetByDay_Invalid_Date_Returns_BadRequest()
        {
            await using var factory = new WebAppFactoryClassifieds();
            var client = factory.CreateClient();

            var resp = await client.GetAsync("/api/classifieds/by-day?day=2025-13-40");

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetTop_Empty_Returns_Empty_List()
        {
            await using var factory = new WebAppFactoryClassifieds();
            var client = factory.CreateClient();

            var resp = await client.GetAsync("/api/classifieds/top?take=10&skip=1000");

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var items = await resp.Content.ReadFromJsonAsync<List<JsonElement>>();
            items!.Should().BeEmpty();
        }

        [Fact]
        public async Task FakeRepo_GetTop_Respects_Skip_Take()
        {
            var repo = new FakeClassifiedsUnifiedReadRepository();

            var first = await repo.GetTopAsync(take: 5, skip: 0);
            var next  = await repo.GetTopAsync(take: 5, skip: 5);

            first.Should().HaveCount(5);
            next.Should().HaveCount(5);

            next.First().Title.Should().NotBeNullOrWhiteSpace();
            next.First().Title.Should().NotBe(first.First().Title);
        }

        [Fact]
        public async Task FakeRepo_GetByDay_Filters_By_Iso_Day()
        {
            var repo = new FakeClassifiedsUnifiedReadRepository();
            var day = DateTime.UtcNow.Date;

            var sameDay = await repo.GetByDayAsync(day, take: 20, skip: 0);

            sameDay.Should().NotBeNull();
            sameDay.Should().OnlyContain(x => x.PostDate == day.ToString("yyyy-MM-dd"));
        }
    }
}
