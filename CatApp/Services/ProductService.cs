using CatApp.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;

namespace CatApp.Services
{
    public class ProductService
    {

        private readonly IMongoCollection<Product> _products;

        public ProductService(ICatalogoDBDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _products = database.GetCollection<Product>(settings.ProductsCollectionName);
        }

        public List<Product> Get() =>
            _products.Find(Product => true).ToList();

       
    

        public Product Get(string id) =>
            _products.Find<Product>(Product => Product.Id == id).FirstOrDefault();

        public Product Create(Product Product)
        {
            _products.InsertOne(Product);
            return Product;
        }

         public void Update(string id, Product ProductIn) =>
            _products.ReplaceOne(Product => Product.Id == id, ProductIn);

        public void Remove(Product ProductIn) =>
            _products.DeleteOne(Product => Product.Id == ProductIn.Id);

        public void Remove(string id) =>
            _products.DeleteOne(Product => Product.Id == id);
    } 
}
    
