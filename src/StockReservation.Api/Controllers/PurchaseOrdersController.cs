using Microsoft.AspNetCore.Mvc;
using StockReservation.Application;

namespace StockReservation.Api.Controllers;

[ApiController]
[Route("api/purchase-orders")]
public class PurchaseOrdersController(IPurchaseOrderQueries queries) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderDto>>> Get([FromQuery] long warehouseId, CancellationToken cancellationToken)
    {
        var result = await queries.GetOutstandingAsync(warehouseId, cancellationToken);
        return Ok(result);
    }
}
