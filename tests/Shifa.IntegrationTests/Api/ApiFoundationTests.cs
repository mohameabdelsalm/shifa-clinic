using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shifa.API.Middleware;
using Shifa.Core;
using Shifa.Core.Common;
using Shifa.IntegrationTests.Infrastructure;

namespace Shifa.IntegrationTests.Api;

[Collection(SqlServerCollection.Name)]
public sealed class ApiFoundationTests
{
    private readonly SqlServerFixture _db;

    public ApiFoundationTests(SqlServerFixture db)
    {
        _db = db;
    }

    [Fact]
    public async Task Live_and_ready_health_checks_pass()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:Shifa", _db.ConnectionString));
        using var client = factory.CreateClient();

        var live = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);
        var ready = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    [Theory]
    [InlineData("en", "Something went wrong. Please try again later.")]
    [InlineData("ar", "حدث خطأ ما. من فضلك حاول مرة أخرى لاحقًا.")]
    public async Task Unexpected_exception_returns_500_with_a_generic_localized_message(string culture, string expected)
    {
        var (status, body) = await HandleAsync(new InvalidOperationException("SELECT * FROM secret_table"), culture);

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
        Assert.Equal(expected, body.GetProperty("message").GetString());
        Assert.False(body.GetProperty("isSuccess").GetBoolean());
        Assert.DoesNotContain("secret_table", body.GetRawText());
    }

    [Fact]
    public async Task Business_exception_returns_its_status_and_message()
    {
        var (status, body) = await HandleAsync(new BusinessException("Branch not found.", HttpStatusCode.NotFound), "en");

        Assert.Equal(StatusCodes.Status404NotFound, status);
        Assert.Equal("Branch not found.", body.GetProperty("message").GetString());
        Assert.Equal(404, body.GetProperty("status").GetInt32());
    }

    private static async Task<(int Status, JsonElement Body)> HandleAsync(Exception exception, string culture)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        try
        {
            var localizerFactory = new ResourceManagerStringLocalizerFactory(
                Options.Create(new LocalizationOptions { ResourcesPath = "Resources" }), NullLoggerFactory.Instance);
            var handler = new GlobalExceptionHandler(
                NullLogger<GlobalExceptionHandler>.Instance, new StringLocalizer<LanguageResource>(localizerFactory));

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await handler.TryHandleAsync(context, exception, TestContext.Current.CancellationToken);

            context.Response.Body.Position = 0;
            var body = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
            return (context.Response.StatusCode, body);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
