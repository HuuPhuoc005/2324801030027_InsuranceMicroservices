using MediatR;
using Microsoft.AspNetCore.Mvc;
using PricingService.Api.Commands;

namespace PricingService.Controllers;

[ApiController]
[Route("api/Pricing")]
public sealed class PricingController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CalculatePriceCommand command)
        => new JsonResult(await mediator.Send(command, HttpContext?.RequestAborted ?? default));
}
