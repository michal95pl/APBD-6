using System.Data;
using System.Data.SqlClient;
using Microsoft.VisualBasic.CompilerServices;

namespace WebApplication1.Repositories;

public interface IWarehouseRepository
{
    Task<bool> ExistsProductAsync(int idProduct);
    Task<bool> ExistsWarehouseAsync(int idWarehouse);
    Task<int?> GetOrderAsync(int idProduct, int amount, DateTime time);
    Task<bool> OrderIsRealizedAsync(int idOrder);

    Task<int?> RegisterProductAsync(int idProduct, int idWarehouse, int idOrder, int productAmount, double productPrice,
        DateTime createdAt);
    Task<double> GetPriceAsync(int idProduct);
    Task<int?> RegisterProductByProcedureAsync(int idWarehouse, int idProduct, DateTime createdAt, int amount);
}

public class WarehouseRepository : IWarehouseRepository
{

    private readonly IConfiguration _configuration;

    public WarehouseRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> ExistsProductAsync(int idProduct)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();

        await using var command = new SqlCommand("Select 1 FROM Product WHERE IdProduct = @id", connection);
        command.Parameters.AddWithValue("@id", idProduct);

        var ex = await command.ExecuteScalarAsync();
       
        return ex != null;
    }

    public async Task<bool> ExistsWarehouseAsync(int idWarehouse)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();

        await using var command = new SqlCommand("Select 1 FROM Warehouse WHERE IdWarehouse = @id", connection);
        command.Parameters.AddWithValue("@id", idWarehouse);
        
        return await command.ExecuteScalarAsync() != null;
    }

    public async Task<int?> GetOrderAsync(int idProduct, int amount, DateTime dateTime)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();

        await using var command = new SqlCommand("SELECT IdOrder FROM [Order] WHERE IdProduct = @id AND Amount = @amount AND CreatedAt < @date", connection);
        command.Parameters.AddWithValue("@id", idProduct);
        command.Parameters.AddWithValue("@amount", amount);
        command.Parameters.AddWithValue("@date", dateTime);

        int? id = (int?) await command.ExecuteScalarAsync();
        
        return id;
    }

    public async Task<double> GetPriceAsync(int idProduct)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        connection.Open();
        
        await using var command = new SqlCommand("SELECT Price FROM Product WHERE IdProduct = @id", connection);
        command.Parameters.AddWithValue("@id", idProduct);
        
        return Double.Parse((await command.ExecuteScalarAsync()).ToString());
    }
    
    public async Task<bool> OrderIsRealizedAsync(int idOrder)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();

        await using var command = new SqlCommand("SELECT * FROM Product_Warehouse where IdOrder = @id", connection);
        command.Parameters.AddWithValue("@id", idOrder);
        
        return await command.ExecuteScalarAsync() != null;
    }
    
    
    
    public async Task<int?> RegisterProductAsync(int idProduct, int idWarehouse, int idOrder, int productAmount, double productPrice,  DateTime createdAt)
    {
        await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        await connection.OpenAsync();
    
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
           await using var command = new SqlCommand("UPDATE [Order] SET FulfilledAt = @date WHERE IdOrder = @idOrder", connection); 
           command.Transaction = (SqlTransaction) transaction;
           command.Parameters.AddWithValue("@date", DateTime.UtcNow);
           command.Parameters.AddWithValue("@idOrder", idOrder);
           await command.ExecuteNonQueryAsync();
           
           
           command.CommandText = @"
                      INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, CreatedAt, Amount, Price)
                      OUTPUT Inserted.IdProductWarehouse
                      VALUES (@IdWarehouse, @IdProduct, @IdOrder, @CreatedAt, @Amount, @Price);";
           command.Parameters.Clear();
           command.Parameters.AddWithValue("@IdWarehouse", idWarehouse);
           command.Parameters.AddWithValue("@IdProduct", idProduct);
           command.Parameters.AddWithValue("@IdOrder", idOrder);
           command.Parameters.AddWithValue("@CreatedAt", createdAt);
           command.Parameters.AddWithValue("@Price", productPrice * productAmount);
           command.Parameters.AddWithValue("@Amount", productAmount);
           
           var idProductWarehouse = (int) await command.ExecuteScalarAsync();
           
           await transaction.CommitAsync();
           return idProductWarehouse;
        }
        catch
        {
            await transaction.RollbackAsync();
            return null;
        }
    }
    
    public async Task<int?> RegisterProductByProcedureAsync(int idWarehouse, int idProduct, DateTime createdAt, int amount)
    {
        try
        { 
            await using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
            await connection.OpenAsync();
            await using var command = new SqlCommand("AddProductToWarehouse", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("IdProduct", idProduct);
            command.Parameters.AddWithValue("IdWarehouse",idWarehouse);
            command.Parameters.AddWithValue("Amount", amount);
            command.Parameters.AddWithValue("CreatedAt", createdAt);
            var id = await command.ExecuteScalarAsync();
            return (int)id;
        } catch (Exception e)
        {
            return null;
        }
    }
    
}