using System.Data;
using WebApplication1.Dto;
using WebApplication1.Exceptions;
using WebApplication1.Repositories;

namespace WebApplication1.Services;

public interface IWarehouseService
{
    Task<int> RegisterProductInWarehouseAsync(WarehouseDto dto);
}

public class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _warehouseRepository;
    
    public WarehouseService(IWarehouseRepository warehouseRepository)
    {
        _warehouseRepository = warehouseRepository;
    }
    
    public async Task<int> RegisterProductInWarehouseAsync(WarehouseDto dto)
    {
        if (dto.Amount <= 0)
            throw new DataException("Incorrect amount value");
        
        if (!await _warehouseRepository.ExistsProductAsync(dto.IdProduct!.Value))
            throw new NotFoundException("Product not found");
        
        if (!await _warehouseRepository.ExistsWarehouseAsync(dto.IdWarehouse!.Value))
            throw new NotFoundException("Warehouse not found");
        
        var orderId = await _warehouseRepository.GetOrderAsync(dto.IdProduct!.Value, dto.Amount!.Value, dto.CreatedAt!.Value);
        
        if (orderId is null)
            throw new NotFoundException("Order not found");
        
        if (await _warehouseRepository.OrderIsRealizedAsync(orderId.Value))
            throw new ConflictException("Order was realized");
        
        var productPrice = await _warehouseRepository.GetPriceAsync(dto.IdProduct!.Value);
        
        var idProductWarehouse = await _warehouseRepository.RegisterProductAsync(
            idWarehouse: dto.IdWarehouse!.Value,
            idProduct: dto.IdProduct!.Value,
            idOrder: orderId.Value,
            productPrice: productPrice,
            productAmount: dto.Amount!.Value,
            createdAt: DateTime.UtcNow);
        
        if (!idProductWarehouse.HasValue)
            throw new ConflictException("Failed to register product in warehouse");
        
        return idProductWarehouse.Value;
        
        // var id = await _warehouseRepository.RegisterProductByProcedure(dto.IdWarehouse!.Value, dto.IdProduct!.Value, dto.CreatedAt!.Value, dto.Amount!.Value);
        //
        // if (id is null)
        //     throw new ConflictException("Failed to register product in warehouse");
        //
        // return id.Value;
    }
}