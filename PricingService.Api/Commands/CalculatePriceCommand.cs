using System;
using System.Collections.Generic;
using FluentValidation;
using MediatR;
using PricingService.Api.Commands.Dto;

namespace PricingService.Api.Commands;

public class CalculatePriceCommand : IRequest<CalculatePriceResult>
{
    public string ProductCode { get; set; } = "";
    public DateTimeOffset PolicyFrom { get; set; }
    public DateTimeOffset PolicyTo { get; set; }
    public List<string>? SelectedCovers { get; set; }
    public List<QuestionAnswer>? Answers { get; set; }
}

public class CalculatePriceCommandValidator : AbstractValidator<CalculatePriceCommand>
{
    public CalculatePriceCommandValidator()
    {
        RuleFor(m => m.ProductCode).NotEmpty();
        RuleFor(m => m.SelectedCovers).NotNull();
        RuleFor(m => m.Answers).NotNull();
        RuleFor(m => m.PolicyTo).GreaterThan(m => m.PolicyFrom)
            .When(m => m.PolicyFrom != default && m.PolicyTo != default);
        RuleForEach(m => m.SelectedCovers).NotEmpty();
        RuleForEach(m => m.Answers).Must(answer => answer is not null &&
            !string.IsNullOrWhiteSpace(answer.QuestionCode) &&
            System.Text.RegularExpressions.Regex.IsMatch(answer.QuestionCode, @"^[A-Za-z_][A-Za-z0-9_]*$") &&
            (answer is not NumericQuestionAnswer number || number.Answer >= 0))
            .WithMessage("Answers must have a question code and non-negative numeric values.");
    }
}
