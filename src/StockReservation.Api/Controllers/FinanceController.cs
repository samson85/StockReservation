using Microsoft.AspNetCore.Mvc;
using StockReservation.Application;

namespace StockReservation.Api.Controllers;

[ApiController]
[Route("api/finance")]
public class FinanceController(IFinanceQueries queries) : ControllerBase
{
    [HttpGet("committed-stock-value")]
    public async Task<ActionResult<IReadOnlyList<WarehouseCommittedValueDto>>> Get(CancellationToken cancellationToken)
    {
        var result = await queries.GetCommittedValuesAsync(cancellationToken);
        return Ok(result);
    }
}
