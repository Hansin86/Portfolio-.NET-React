using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using PortfolioApp.Application.Common.Models;
using PortfolioApp.Application.Features.Transactions.Common;
using PortfolioApp.IntegrationTests.Infrastructure;

namespace PortfolioApp.IntegrationTests.Transactions;

/// <summary>
/// End-to-end coverage of the Transactions CRUD slice through the real HTTP pipeline and a
/// PostgreSQL container: the full lifecycle, per-user isolation (FR-03), and JWT enforcement
/// (NFR-04).
/// </summary>
public class TransactionsEndpointsTests : IClassFixture<PortfolioApiFactory>
{
    private readonly PortfolioApiFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public TransactionsEndpointsTests(PortfolioApiFactory factory)
    {
        _factory = factory;
    }

    private static object BuyAaplPayload(decimal quantity = 10m) => new
    {
        ticker = "AAPL",
        type = "Buy",
        quantity,
        pricePerUnit = 150m,
        currency = "USD",
        transactionDate = "2026-05-01",
    };

    [Fact]
    public async Task FullLifecycle_Create_Read_Update_Delete()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync();

        // Create
        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/transactions", BuyAaplPayload());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.Headers.Location.Should().NotBeNull();

        TransactionDto created = (await createResponse.Content.ReadFromJsonAsync<TransactionDto>(JsonOptions))!;
        created.Id.Should().NotBe(Guid.Empty);
        created.Ticker.Should().Be("AAPL");
        created.Currency.Should().Be("USD");
        created.Quantity.Should().Be(10m);

        // Read (list)
        PagedResult<TransactionDto> page =
            (await client.GetFromJsonAsync<PagedResult<TransactionDto>>("/transactions", JsonOptions))!;
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle(t => t.Id == created.Id);

        // Read (by id)
        HttpResponseMessage byIdResponse = await client.GetAsync($"/transactions/{created.Id}");
        byIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Update
        var update = new
        {
            type = "Buy",
            quantity = 12m,
            pricePerUnit = 155m,
            currency = "EUR",
            transactionDate = "2026-05-02",
        };
        HttpResponseMessage updateResponse = await client.PutAsJsonAsync($"/transactions/{created.Id}", update);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        TransactionDto updated = (await updateResponse.Content.ReadFromJsonAsync<TransactionDto>(JsonOptions))!;
        updated.Quantity.Should().Be(12m);
        updated.Currency.Should().Be("EUR");

        // Delete
        HttpResponseMessage deleteResponse = await client.DeleteAsync($"/transactions/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Gone
        HttpResponseMessage goneResponse = await client.GetAsync($"/transactions/{created.Id}");
        goneResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SellWithoutHoldings_ReturnsUnprocessableEntity()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync();

        var sell = new
        {
            ticker = "TSLA",
            type = "Sell",
            quantity = 5m,
            pricePerUnit = 200m,
            currency = "USD",
            transactionDate = "2026-05-01",
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/transactions", sell);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Endpoints_RequireAuthentication()
    {
        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage listResponse = await anonymous.GetAsync("/transactions");
        listResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        HttpResponseMessage createResponse = await anonymous.PostAsJsonAsync("/transactions", BuyAaplPayload());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UserCannotAccessAnotherUsersTransaction()
    {
        HttpClient userA = await _factory.CreateAuthenticatedClientAsync();
        HttpClient userB = await _factory.CreateAuthenticatedClientAsync();

        HttpResponseMessage createResponse = await userA.PostAsJsonAsync("/transactions", BuyAaplPayload());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        TransactionDto created = (await createResponse.Content.ReadFromJsonAsync<TransactionDto>(JsonOptions))!;

        // User B must not see, edit, or delete user A's transaction — all surface as 404 so
        // existence isn't leaked (FR-03).
        HttpResponseMessage bReads = await userB.GetAsync($"/transactions/{created.Id}");
        bReads.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var update = new
        {
            type = "Buy",
            quantity = 99m,
            pricePerUnit = 1m,
            currency = "USD",
            transactionDate = "2026-05-01",
        };
        HttpResponseMessage bEdits = await userB.PutAsJsonAsync($"/transactions/{created.Id}", update);
        bEdits.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage bDeletes = await userB.DeleteAsync($"/transactions/{created.Id}");
        bDeletes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // User A's transaction is untouched.
        PagedResult<TransactionDto> aPage =
            (await userA.GetFromJsonAsync<PagedResult<TransactionDto>>("/transactions", JsonOptions))!;
        aPage.TotalCount.Should().Be(1);
    }
}
