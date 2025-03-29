using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using MarketProject.ViewModels;
using System.Diagnostics;
using db = MarketProject.Models.Database;
using System.IO;
using System.Linq;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DynamicData;
using MarketProject.Controllers;
using MarketProject.Controls;
using MarketProject.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using Newtonsoft.Json;


namespace MarketProject.Views;

public partial class OptionsView : UserControl
{
    // Acrescentar no caminho da backup, a data atual e uma pasta respectiva.
    private const string BackupPath = @"C:\ranGO\Backup";

    private readonly List<string> _backupFilesName = new()
    {
        "supplys.json", "products.json",
        "orders.json", "foodMenu.json"
    };

    private readonly string Today = DateTime.Now.ToShortDateString().Replace('/', '.');

    public OptionsView()
    {
        InitializeComponent();
    }

    private async void RestoreButton_OnClick(object sender, RoutedEventArgs e)
    {
        bool isRestored = true;
        var startLocation = await TopLevel.GetTopLevel(this)!.StorageProvider.TryGetFolderFromPathAsync(BackupPath)
            .ConfigureAwait(false);
        FolderPickerOpenOptions folderOption = new()
        {
            AllowMultiple = false,
            Title = "Selecione um arquivo para importar",
            SuggestedStartLocation = startLocation
        };
        var folder = TopLevel.GetTopLevel(this)!.StorageProvider.OpenFolderPickerAsync(folderOption).Result
            .FirstOrDefault();

        foreach (var f in _backupFilesName)
        {
            if (folder is null) return;
            string path = folder!.Path.AbsolutePath;
            using StreamReader sr = new($"{path}{f}");
            var contentJson = await sr.ReadToEndAsync().ConfigureAwait(false);

            try
            {
                switch (f)
                {
                    case "supplys.json":
                        var supplies = JsonConvert.DeserializeObject<List<Supply>>(contentJson);
                        db.SupplyList.Clear();
                        await db.DropDatabase(DbType.Supply);
                        await db.CreateNewCollectionIntoDatabase(DbType.Supply);
                        // db.SupplyList.AddRange(supplies);
                        db.SupplyList = new ObservableCollection<Supply>(supplies);
                        await db.AddDataIntoDatabase(supplies);
                        break;
                    case "products.json":
                        var products = JsonConvert.DeserializeObject<List<Product>>(contentJson);
                        db.ProductsList.Clear();
                        await db.DropDatabase(DbType.Products);
                        await db.CreateNewCollectionIntoDatabase(DbType.Products);
                        // db.ProductsList.AddRange(products);
                        db.ProductsList = new ObservableCollection<Product>(products);
                        await db.AddDataIntoDatabase(products);
                        // foreach (var product in products)
                        // {
                        //     string supplyName = SupplyController.GetSupplyNameByProduct(product);
                        //     SupplyController.AddProductToSupply(product, supplyName);
                        // }
                        break;
                    case "orders.json":
                        var ordersList = JsonConvert.DeserializeObject<List<Orders>>(contentJson);
                        db.OrdersList.Clear();
                        await db.DropDatabase(DbType.Orders);
                        await db.CreateNewCollectionIntoDatabase(DbType.Orders);
                        // db.OrdersList.AddRange(ordersList);
                        db.OrdersList = new ObservableCollection<Orders>(ordersList);
                        await db.AddDataIntoDatabase(ordersList);
                        break;
                    case "foodMenu.json":
                        var foodsList = JsonConvert.DeserializeObject<List<Foods>>(contentJson);
                        db.FoodsMenuList.Clear();
                        await db.DropDatabase(DbType.FoodMenu);
                        await db.CreateNewCollectionIntoDatabase(DbType.FoodMenu);
                        // db.FoodsMenuList.AddRange(foodsList);
                        db.FoodsMenuList = new ObservableCollection<Foods>(foodsList);
                        await db.AddDataIntoDatabase(foodsList);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message+"\n");
                Console.WriteLine(ex.StackTrace);
                isRestored = false;
            }
        }
        db.DatabaseRestoredNotify();
        if (isRestored is false) return;
        
        // Dispatcher.UIThread.Post(() =>
        // {
        //     AddPopup.IsOpen = true;
        //     AddProdLabel.Content = "Backup restaurado!";
        //     ContentAddTextBlock.Text = $"O Backup do dia '{folder.Name.Replace('.', '/')}' foi restaurado!";   
        // });
        
    }
}