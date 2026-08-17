using Microsoft.AspNetCore.Mvc;
using StockReservation.Api.Contracts.Reservations;
using StockReservation.Application;

namespace StockReservation.Api.Controllers;

[ApiController]
[Route("api/reservations")]
public class ReservationsController(IReservationService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReservationResult>> Reserve( [FromBody] ReserveRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ReserveAsync( request.PurchaseOrderLineId, request.Quantity, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{reservationId:long}/release")]
    public async Task<ActionResult<ReservationResult>> Release(long reservationId, [FromBody] ReleaseRequest request, CancellationToken cancellationToken)
    {
        var result = await service.ReleaseAsync( reservationId, request.Quantity, cancellationToken);
        return Ok(result);
    }
}
