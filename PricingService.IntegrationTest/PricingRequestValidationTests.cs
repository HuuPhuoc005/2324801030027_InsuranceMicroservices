using System.Net;
using PricingService.Api.Commands;
using PricingService.Api.Commands.Dto;
using Xunit;

namespace PricingService.IntegrationTest;

[Collection("PricingControllerFixtureCollection")]
public class PricingRequestValidationTests(PricingControllerFixture fixture)
{
    [Fact]
    public async Task UnknownProductReturnsNotFound()
    {
        var command = Travel();
        command.ProductCode = "UNKNOWN_PRODUCT";
        await fixture.SystemUnderTest.Scenario(s =>
        {
            s.Post.Json(command).ToUrl("/api/Pricing");
            s.StatusCodeShouldBe(HttpStatusCode.NotFound);
        });
    }

    [Fact]
    public async Task MissingTariffAnswersReturnBadRequest()
    {
        var command = Travel();
        command.Answers = new();
        await fixture.SystemUnderTest.Scenario(s =>
        {
            s.Post.Json(command).ToUrl("/api/Pricing");
            s.StatusCodeShouldBe(HttpStatusCode.BadRequest);
        });
    }

    [Fact]
    public async Task RepeatedAnswersReturnBadRequest()
    {
        var command = Travel();
        command.Answers!.Add(new NumericQuestionAnswer { QuestionCode = "NUM_OF_ADULTS", Answer = 5M });
        await fixture.SystemUnderTest.Scenario(s =>
        {
            s.Post.Json(command).ToUrl("/api/Pricing");
            s.StatusCodeShouldBe(HttpStatusCode.BadRequest);
        });
    }

    private static CalculatePriceCommand Travel() => new()
    {
        ProductCode = "TRI",
        PolicyFrom = DateTimeOffset.UtcNow.AddDays(5),
        PolicyTo = DateTimeOffset.UtcNow.AddDays(10),
        SelectedCovers = new() { "C1", "C2", "C3" },
        Answers = new()
        {
            new NumericQuestionAnswer { QuestionCode = "NUM_OF_ADULTS", Answer = 1M },
            new NumericQuestionAnswer { QuestionCode = "NUM_OF_CHILDREN", Answer = 1M },
            new TextQuestionAnswer { QuestionCode = "DESTINATION", Answer = "EUR" }
        }
    };
}
