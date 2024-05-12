using System.Data;
using System.Data.SqlClient;
using Microsoft.VisualBasic.CompilerServices;

namespace WebApplication1.Repositories;

public interface IWarehouseRepository
{
    bool ExistsProduct(int idProduct);
    bool ExistsWarehouse(int idWarehouse);
    int? GetOrder(int idProduct, int amount, DateTime time);
    bool OrderIsRealized(int idOrder);

    int? RegisterProduct(int idProduct, int idWarehouse, int idOrder, int productAmount, double productPrice, DateTime createdAt);
    public double GetPrice(int idProduct);
    int? RegisterProductByProcedure(int idWarehouse, int idProduct, DateTime createdAt, int amount);
}

public class WarehouseRepository : IWarehouseRepository
{

    private readonly IConfiguration _configuration;

    public WarehouseRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool ExistsProduct(int idProduct)
    {
        using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        connection.Open();

        using var command = new SqlCommand("Select 1 FROM Product WHERE IdProduct = @id", connection);
        command.Parameters.AddWithValue("@id", idProduct);
        
        return  command.ExecuteScalar() != null;
    }

    public bool ExistsWarehouse(int idWarehouse)
    {
        using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        connection.Open();

        using var command = new SqlCommand("Select 1 FROM Warehouse WHERE IdWarehouse = @id", connection);
        command.Parameters.AddWithValue("@id", idWarehouse);
        
        return  command.ExecuteScalar() != null;
    }

    public int? GetOrder(int idProduct, int amount, DateTime dateTime)
    {
        using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        connection.Open();

        using var command = new SqlCommand("SELECT IdOrder FROM [Order] WHERE IdProduct = @id AND Amount = @amount AND CreatedAt < @date", connection);
        command.Parameters.AddWithValue("@id", idProduct);
        command.Parameters.AddWithValue("@amount", amount);
        command.Parameters.AddWithValue("@date", dateTime);

        int? id = (int?)command.ExecuteScalar();
        
        return id;
    }

    public double GetPrice(int idProduct)
    {
        using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        connection.Open();
        
        using var command = new SqlCommand("SELECT Price FROM Product WHERE IdProduct = @id", connection);
        command.Parameters.AddWithValue("@id", idProduct);
        
        return Double.Parse(command.ExecuteScalar().ToString());
    }
    
    public bool OrderIsRealized(int idOrder)
    {
        using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        connection.Open();

        using var command = new SqlCommand("SELECT * FROM Product_Warehouse where IdOrder = @id", connection);
        command.Parameters.AddWithValue("@id", idOrder);
        
        return  command.ExecuteScalar() != null;
    }
    
    
    
    public int? RegisterProduct(int idProduct, int idWarehouse, int idOrder, int productAmount, double productPrice,  DateTime createdAt)
    {
        using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        connection.Open();
    
        using var transaction = connection.BeginTransaction();

        try
        {
           using var command = new SqlCommand("UPDATE [Order] SET FulfilledAt = @date WHERE IdOrder = @idOrder", connection); 
           command.Transaction = transaction;
           command.Parameters.AddWithValue("@date", DateTime.UtcNow);
           command.Parameters.AddWithValue("@idOrder", idOrder);
           command.ExecuteNonQuery();
           
           
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
           
           var idProductWarehouse = (int)command.ExecuteScalar();
           
           transaction.Commit();
           return idProductWarehouse;
        }
        catch
        {
            transaction.Rollback();
            return null;
        }
    }
    
    public int? RegisterProductByProcedure(int idWarehouse, int idProduct, DateTime createdAt, int amount)
    {
        try
        { 
            using var connection = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
            connection.Open();
            using var command = new SqlCommand("AddProductToWarehouse", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("IdProduct", idProduct);
            command.Parameters.AddWithValue("IdWarehouse",idWarehouse);
            command.Parameters.AddWithValue("Amount", amount);
            command.Parameters.AddWithValue("CreatedAt", createdAt);
            return (int)command.ExecuteScalar();  
        } catch (Exception e)
        {
            return null;
        }
    }
    
}