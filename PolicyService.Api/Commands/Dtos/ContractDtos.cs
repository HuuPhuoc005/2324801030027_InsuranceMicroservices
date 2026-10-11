namespace PolicyService.Api.Commands.Dtos;
public enum QuestionType { Numeric, Choice }
public abstract class QuestionAnswer
{
    public string QuestionCode { get; set; } = "";
    public abstract QuestionType QuestionType { get; }
}
public class NumericQuestionAnswer : QuestionAnswer
{
    public override QuestionType QuestionType => QuestionType.Numeric;
    public decimal Answer { get; set; }
}
public class ChoiceQuestionAnswer : QuestionAnswer
{
    public override QuestionType QuestionType => QuestionType.Choice;
    public string Answer { get; set; } = "";
}
public class PersonDto
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string TaxId { get; set; } = "";
}
public class AddressDto
{
    public string Country { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string City { get; set; } = "";
    public string Street { get; set; } = "";
}
