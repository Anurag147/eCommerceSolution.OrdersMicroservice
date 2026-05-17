using eCommerce.OrdersMicroService.DataAccessLayer.Entities;
using eCommerce.OrdersMicroService.DataAccessLayer.RepositoryContracts;
using MongoDB.Driver;

namespace eCommerce.OrdersMicroService.DataAccessLayer.Repositories
{
    public class OrdersRepository : IOrdersRepository
    {
        private readonly IMongoCollection<Order> _ordersCollection;

        public OrdersRepository(IMongoDatabase database)
        {
            _ordersCollection = database.GetCollection<Order>("orders");
        }

        public async Task<IEnumerable<Order>> GetAllOrders()
        {
            return await _ordersCollection.Find(_ => true).ToListAsync();
        }

        public async Task<IEnumerable<Order?>> GetOrdersByCondition(FilterDefinition<Order> filter)
        {
            return await _ordersCollection.Find(filter).ToListAsync();
        }

        public async Task<Order?> GetOrderByCondition(FilterDefinition<Order> filter)
        {
            return await _ordersCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<Order?> AddOrder(Order order)
        {
            order._id = Guid.NewGuid();
            order.OrderID = order._id; // Ensure OrderID is set to the same value as _id
            foreach (var item in order.OrderItems)
            {
                item._id = Guid.NewGuid();
            }
            await _ordersCollection.InsertOneAsync(order);
            return order;
        }

        public async Task<Order?> UpdateOrder(Order order)
        {
            var result = await _ordersCollection.ReplaceOneAsync(o => o._id == order._id, order);
            return result.IsAcknowledged && result.ModifiedCount > 0 ? order : null;
        }

        public async Task<bool> DeleteOrder(Guid orderId)
        {
            var result = await _ordersCollection.DeleteOneAsync(o => o._id == orderId);
            return result.IsAcknowledged && result.DeletedCount > 0;
        }
    }
}