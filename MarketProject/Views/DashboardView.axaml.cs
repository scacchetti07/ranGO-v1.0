using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DynamicData;
using MarketProject.Controllers;
using MarketProject.Controls;
using MarketProject.Models;
using MarketProject.ViewModels;

namespace MarketProject.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        InitMethods();
        Database.DatabaseRestored += () =>
        {
            Dispatcher.UIThread.Post(InitMethods);
            DeliverCurrentSupply(Database.SupplyList,null);
            UpdateFoodCards(null,null);
        };
    }

    private void InitMethods()
    {
        Database.SupplyList.CollectionChanged += DeliverCurrentSupply;

        Database.FoodsMenuList.CollectionChanged += UpdateFoodCards;
        
        if (Database.FoodsMenuList.Count == 0) return;

        foreach (var foods in Database.FoodsMenuList.TakeLast(3))
            FoodCardsStackPanel.Children.Add(new FoodDashboardCards { CurrentFood = foods});
    }

    private void DeliverCurrentSupply(object sender,NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var newList = sender as ObservableCollection<Supply>;
            CardSupplyDashboard.CurrentSupply = newList!.FirstOrDefault(s => s.InDeliver);
        }, DispatcherPriority.Background);
    }

    private void UpdateFoodCards(object sender,NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            FoodCardsStackPanel.Children.Clear();
            foreach (var foods in Database.FoodsMenuList)
                FoodCardsStackPanel.Children.Add(new FoodDashboardCards { CurrentFood = foods});
        });
    }
}