using Microsoft.Data.SqlClient;
using WebApplication4.Models;

namespace WebApplication4.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IConfiguration _config;

        public WarehouseService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<int> AddProductToWarehouseAsync(WarehouseRequest request)
        {
            using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                if (request.Amount <= 0)
                    throw new ArgumentException("Amount must be greater than 0");

                var productCmd = new SqlCommand("SELECT 1 FROM Product WHERE IdProduct = @id", connection, transaction);
                productCmd.Parameters.AddWithValue("@id", request.ProductId);
                if ((await productCmd.ExecuteScalarAsync()) is null)
                    throw new KeyNotFoundException("Product not found");

                var warehouseCmd = new SqlCommand("SELECT 1 FROM Warehouse WHERE IdWarehouse = @id", connection, transaction);
                warehouseCmd.Parameters.AddWithValue("@id", request.WarehouseId);
                if ((await warehouseCmd.ExecuteScalarAsync()) is null)
                    throw new KeyNotFoundException("Warehouse not found");

                var orderCmd = new SqlCommand(@"
                    SELECT IdOrder FROM [Order]
                    WHERE IdProduct = @pid AND Amount = @amount AND CreatedAt < @createdAt
                ", connection, transaction);
                orderCmd.Parameters.AddWithValue("@pid", request.ProductId);
                orderCmd.Parameters.AddWithValue("@amount", request.Amount);
                orderCmd.Parameters.AddWithValue("@createdAt", request.CreatedAt);
                var orderId = await orderCmd.ExecuteScalarAsync();
                if (orderId is null)
                    throw new InvalidOperationException("Matching order not found");

                var orderCheckCmd = new SqlCommand("SELECT 1 FROM Product_Warehouse WHERE IdOrder = @id", connection, transaction);
                orderCheckCmd.Parameters.AddWithValue("@id", orderId);
                if ((await orderCheckCmd.ExecuteScalarAsync()) != null)
                    throw new InvalidOperationException("Order already fulfilled");

                var updateOrder = new SqlCommand("UPDATE [Order] SET FulfilledAt = @date WHERE IdOrder = @id", connection, transaction);
                updateOrder.Parameters.AddWithValue("@date", DateTime.Now);
                updateOrder.Parameters.AddWithValue("@id", orderId);
                await updateOrder.ExecuteNonQueryAsync();

                var priceCmd = new SqlCommand("SELECT Price FROM Product WHERE IdProduct = @id", connection, transaction);
                priceCmd.Parameters.AddWithValue("@id", request.ProductId);
                decimal price = (decimal)(await priceCmd.ExecuteScalarAsync());
                decimal total = price * request.Amount;

                var insertCmd = new SqlCommand(@"
                    INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                    OUTPUT INSERTED.IdProductWarehouse
                    VALUES (@wid, @pid, @oid, @amount, @price, @created)
                ", connection, transaction);
                insertCmd.Parameters.AddWithValue("@wid", request.WarehouseId);
                insertCmd.Parameters.AddWithValue("@pid", request.ProductId);
                insertCmd.Parameters.AddWithValue("@oid", orderId);
                insertCmd.Parameters.AddWithValue("@amount", request.Amount);
                insertCmd.Parameters.AddWithValue("@price", total);
                insertCmd.Parameters.AddWithValue("@created", DateTime.Now);
                int newId = (int)(await insertCmd.ExecuteScalarAsync());

                await transaction.CommitAsync();
                return newId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        
    }
}
