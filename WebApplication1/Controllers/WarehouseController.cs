using System.Data;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Dto;
using WebApplication1.Exceptions;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[ApiController]
[Route("/api/Warehouse")]
public class WarehouseController : ControllerBase
{

    private IWarehouseService _warehouseService;
    
    public WarehouseController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }
    
    
    [HttpPost]
    public async Task<IActionResult> RegisterProductInWarehouse([FromBody] WarehouseDto dto)
    {
        try
        {
            var id = await _warehouseService.RegisterProductInWarehouseAsync(dto);
            return Ok(id);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (ConflictException e)
        {
            return Conflict(e.Message);
        }
        catch (DataException e)
        {
            return NotFound(e.Message);
        }
    }
    
}