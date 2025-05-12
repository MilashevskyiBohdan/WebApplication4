using WebApplication4.Models;

namespace WebApplication4.Services
{
    public interface IWarehouseService
    {
        Task<int> AddProductToWarehouseAsync(WarehouseRequest request);
    }
}
