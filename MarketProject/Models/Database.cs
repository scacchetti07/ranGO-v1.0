using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using MarketProject.Views;
using MongoDB.Driver;
using MongoDB.Bson;
using System.IO;
using System.Threading.Tasks;

namespace MarketProject.Models;

public class Database
{
    public static event Action DatabaseRestored;
    public static ObservableCollection<Product> ProductsList { get; set; } = new();
    public static ObservableCollection<Supply> SupplyList { get; set; } = new();
    public static ObservableCollection<Orders> OrdersList { get; set; } = new();
    public static ObservableCollection<Foods> FoodsMenuList { get; set; } = new();

    private static MongoClient _client =
        new("mongodb+srv://luiscacchetti07:c2qb5VFjcVBA18PH@rango.3fwol.mongodb.net/");

    protected static IMongoDatabase GetDatabase(string dbName)
        => _client.GetDatabase(dbName);

    protected static IMongoCollection<T> GetCollection<T>(string dbName, string dbCollection)
        => GetDatabase(dbName).GetCollection<T>(dbCollection);

    public Database()
    {
        _client.StartSession();
        Console.WriteLine("Conexão Estabelecida!");
    }

    public static void DatabaseRestoredNotify()
    {
        DatabaseRestored?.Invoke();
    }

    public async void StartStorage()
    {
        var collectionProducts = GetDatabase("storage").GetCollection<Product>("products");
        var prodDocs = await collectionProducts.Find(Builders<Product>.Filter.Empty).ToListAsync();

        var collectionSupply = GetDatabase("storage").GetCollection<Supply>("supplys");
        var supplyDocs = await collectionSupply.Find(Builders<Supply>.Filter.Empty).ToListAsync();

        var collectionOrder = GetDatabase("storage").GetCollection<Orders>("orders");
        var orderDocs = await collectionOrder.Find(Builders<Orders>.Filter.Empty).ToListAsync();

        var collectionFoodMenu = GetDatabase("storage").GetCollection<Foods>("foodMenu");
        var foodsMenuDocs = await collectionFoodMenu.Find(Builders<Foods>.Filter.Empty).ToListAsync();

        ProductsList = new ObservableCollection<Product>(prodDocs);
        SupplyList = new ObservableCollection<Supply>(supplyDocs);
        OrdersList = new ObservableCollection<Orders>(orderDocs);
        FoodsMenuList = new ObservableCollection<Foods>(foodsMenuDocs);

        Directory.CreateDirectory(@"C:/ranGO/Backup");
        Directory.CreateDirectory(@"C:/ranGO/GaleriaDosPratos");
        Console.WriteLine("Lista de produtos e fornecedores Iniciada!");
        Console.WriteLine("Listas de Pedidos e Cardápio iniciadas!");
    }

    public static async Task DropDatabase(DbType collection)
    {
        switch (collection)
        {
            case DbType.Products:
                var prodCollection = GetCollection<Product>("storage", "products");
                await prodCollection.DeleteManyAsync(p => true).ConfigureAwait(false);
                break;
            case DbType.Supply:
                var supCollection = GetCollection<Supply>("storage", "supplys");
                await supCollection.DeleteManyAsync(s => true).ConfigureAwait(false);
                break;
            case DbType.Orders:
                var orderCollection = GetCollection<Orders>("storage", "orders");
                await orderCollection.DeleteManyAsync(o => true).ConfigureAwait(false);
                break;
            case DbType.FoodMenu:
                var foodCollection = GetCollection<Foods>("storage", "foodMenu");
                await foodCollection.DeleteManyAsync(f => true).ConfigureAwait(false);
                break;
        }
    }
    public static async Task CreateNewCollectionIntoDatabase(DbType collection)
    {
        switch (collection)
        {
            case DbType.Products:
                await GetDatabase("storage").CreateCollectionAsync("products");
                break;
            case DbType.Supply:
                await GetDatabase("storage").CreateCollectionAsync("supplys");
                break;
            case DbType.Orders:
                await GetDatabase("storage").CreateCollectionAsync("orders");
                break;
            case DbType.FoodMenu:
                await GetDatabase("storage").CreateCollectionAsync("foodMenu");
                break;
        }
    }

    public static async Task AddDataIntoDatabase(IEnumerable<Product> prods)
    {
        var prodCollection = GetDatabase("storage").GetCollection<Product>("products");
        if (!prods.Any()) return;
        await prodCollection.InsertManyAsync(prods);
    }
    public static async Task AddDataIntoDatabase(IEnumerable<Supply> supplies)
    {
        var prodCollection = GetDatabase("storage").GetCollection<Supply>("supplys");
        if (!supplies.Any()) return;
        await prodCollection.InsertManyAsync(supplies);
    }
    public static async Task AddDataIntoDatabase(IEnumerable<Orders> orders)    
    {
        var prodCollection = GetDatabase("storage").GetCollection<Orders>("orders");
        if (!orders.Any()) return;
        await prodCollection.InsertManyAsync(orders);
    }
    public static async Task AddDataIntoDatabase(IEnumerable<Foods> foods)
    {
        var prodCollection = GetDatabase("storage").GetCollection<Foods>("foodMenu");
        if (!foods.Any()) return;
        await prodCollection.InsertManyAsync(foods);
    }
}

public enum DbType
{
    Products,
    Supply,
    Orders,
    FoodMenu
}