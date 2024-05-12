using System.Data;
using WebApplication1.Dto;
using WebApplication1.Exceptions;
using WebApplication1.Repositories;

namespace WebApplication1.Services;

public interface IWarehouseService
{
    int RegisterProductInWarehouse(WarehouseDto dto);
}

public class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _warehouseRepository;
    
    public WarehouseService(IWarehouseRepository warehouseRepository)
    {
        _warehouseRepository = warehouseRepository;
    }
    
    public int RegisterProductInWarehouse(WarehouseDto dto)
    {
        if (dto.Amount <= 0)
            throw new DataException("Incorrect amount value");
        
        if (!_warehouseRepository.ExistsProduct(dto.IdProduct!.Value))
            throw new NotFoundException("Product not found");
        
        if (!_warehouseRepository.ExistsWarehouse(dto.IdWarehouse!.Value))
            throw new NotFoundException("Warehouse not found");
        
        var orderId = _warehouseRepository.GetOrder(dto.IdProduct!.Value, dto.Amount!.Value, dto.CreatedAt!.Value);
        
        if (orderId is null)
            throw new NotFoundException("Order not found");
        
        if (_warehouseRepository.OrderIsRealized(orderId.Value))
            throw new ConflictException("Order was realized");
        
        var productPrice = _warehouseRepository.GetPrice(dto.IdProduct!.Value);
        
        var idProductWarehouse = _warehouseRepository.RegisterProduct(
            idWarehouse: dto.IdWarehouse!.Value,
            idProduct: dto.IdProduct!.Value,
            idOrder: orderId!.Value,
            productPrice: productPrice,
            productAmount: dto.Amount!.Value,
            createdAt: DateTime.UtcNow);
        
        if (!idProductWarehouse.HasValue)
            throw new ConflictException("Failed to register product in warehouse");
        
        return idProductWarehouse.Value;
        
        // var id = _warehouseRepository.RegisterProductByProcedure(dto.IdWarehouse!.Value, dto.IdProduct!.Value, dto.CreatedAt!.Value, dto.Amount!.Value);
        //
        // if (id is null)
        //     throw new ConflictException("Failed to register product in warehouse");
        //
        // return id.Value;
    }
}