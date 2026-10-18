using Newtonsoft.Json;
using PricingService.Api.Commands;
using PricingService.Api.Commands.Dto;
using Xunit;

namespace PricingService.Test.Commands;

public class QuestionAnswerSerializationTests
{
    [Fact]
    public void BothJsonSerializersPreserveThePolymorphicAnswers()
    {
        var command = new CalculatePriceCommand { ProductCode = "TRI", Answers = new()
        {
            new NumericQuestionAnswer { QuestionCode = "ADULTS", Answer = 2M },
            new TextQuestionAnswer { QuestionCode = "DESTINATION", Answer = "EUR" },
            new ChoiceQuestionAnswer { QuestionCode = "FLOOD", Answer = "NO" }
        } };
        var systemTextJson = System.Text.Json.JsonSerializer.Serialize(command);
        var fromNewtonsoft = JsonConvert.DeserializeObject<CalculatePriceCommand>(systemTextJson)!;
        AssertAnswers(fromNewtonsoft);
        var newtonsoftJson = JsonConvert.SerializeObject(command);
        var fromSystemText = System.Text.Json.JsonSerializer.Deserialize<CalculatePriceCommand>(newtonsoftJson)!;
        AssertAnswers(fromSystemText);
    }

    private static void AssertAnswers(CalculatePriceCommand command)
    {
        Assert.Equal(2M, Assert.IsType<NumericQuestionAnswer>(command.Answers![0]).Answer);
        Assert.Equal("EUR", Assert.IsType<TextQuestionAnswer>(command.Answers[1]).Answer);
        Assert.Equal("NO", Assert.IsType<ChoiceQuestionAnswer>(command.Answers[2]).Answer);
    }
}
