using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using MarketProject.ViewModels;
using System.Diagnostics;
using System.Globalization;
using db = MarketProject.Models.Database;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DynamicData;
using ExCSS;
using MarketProject.Controllers;
using MarketProject.Controls;
using MarketProject.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using Newtonsoft.Json;
using HorizontalAlignment = Avalonia.Layout.HorizontalAlignment;
using Timer = System.Timers.Timer;
using VerticalAlignment = Avalonia.Layout.VerticalAlignment;


namespace MarketProject.Views;

public partial class OptionsView : UserControl
{
    // Acrescentar no caminho da backup, a data atual e uma pasta respectiva.
    private const string BackupPath = @"C:\ranGO\Backup";
    
    private RestoringData_MsgBox loading = new();
    private Timer _timer = new();
    private int _time = 80;

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

        if (folder is null) return;
        
        ShowLoadingScreen(folder.Name);
        
        _timer.Start();

        foreach (var file in _backupFilesName)
        {
            string path = folder!.Path.AbsolutePath;
            using StreamReader sr = new($"{path}{file}");
            var contentJson = await sr.ReadToEndAsync().ConfigureAwait(false);
            
            try
            {
                switch (file)
                {
                    case "supplys.json":
                        var supplies = JsonConvert.DeserializeObject<List<Supply>>(contentJson);
                        db.SupplyList.Clear();
                        await db.DropDatabase(DbType.Supply);
                        await db.CreateNewCollectionIntoDatabase(DbType.Supply);
                        db.SupplyList = new ObservableCollection<Supply>(supplies);
                        await db.AddDataIntoDatabase(supplies);
                        break;
                    case "products.json":
                        var products = JsonConvert.DeserializeObject<List<Product>>(contentJson);
                        db.ProductsList.Clear();
                        await db.DropDatabase(DbType.Products);
                        await db.CreateNewCollectionIntoDatabase(DbType.Products);
                        db.ProductsList = new ObservableCollection<Product>(products);
                        await db.AddDataIntoDatabase(products);
                        break;
                    case "orders.json":
                        var ordersList = JsonConvert.DeserializeObject<List<Orders>>(contentJson);
                        db.OrdersList.Clear();
                        await db.DropDatabase(DbType.Orders);
                        await db.CreateNewCollectionIntoDatabase(DbType.Orders);
                        db.OrdersList = new ObservableCollection<Orders>(ordersList);
                        await db.AddDataIntoDatabase(ordersList);
                        break;
                    case "foodMenu.json":
                        var foodsList = JsonConvert.DeserializeObject<List<Foods>>(contentJson);
                        db.FoodsMenuList.Clear();
                        await db.DropDatabase(DbType.FoodMenu);
                        await db.CreateNewCollectionIntoDatabase(DbType.FoodMenu);
                        db.FoodsMenuList = new ObservableCollection<Foods>(foodsList);
                        await db.AddDataIntoDatabase(foodsList);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\n");
                Console.WriteLine(ex.StackTrace);
            }
        }

        db.DatabaseRestoredNotify();
        _timer.Stop();
        _time = 10;
    }

    private void ShowLoadingScreen(string folder)
    {
        loading.DateLabel.Content = $"Importando dados de {folder!.Replace('.', '/')}";
        loading.ShowDialog((Window)Parent!.Parent!.Parent!.Parent!);
        worker_Declaring();
    }

    private void worker_Declaring()
    {
        BackgroundWorker worker = new();
        worker.WorkerReportsProgress = true;
        worker.DoWork += worker_DoWork;
        worker.ProgressChanged += worker_ProgressChanged;
        worker.RunWorkerAsync();
    }

    private void worker_DoWork(object sender, DoWorkEventArgs e)
    {
        for (int i = 0; i <= 100; i++)
        {
            (sender as BackgroundWorker)?.ReportProgress(i);
            Thread.Sleep(_time);
        }
    }

    private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        loading.RestoringProgressBar.Value = e.ProgressPercentage;

        if (loading.RestoringProgressBar.Value != 100) return;
        loading.Hide();
        loading.RestoringProgressBar.Value = 0;

    }
    
}