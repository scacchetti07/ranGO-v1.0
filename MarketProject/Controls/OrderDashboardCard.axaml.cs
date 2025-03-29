using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MarketProject.Controllers;
using MarketProject.Models;

namespace MarketProject.Controls;

public partial class OrderDashboardCard : UserControl
{
    public OrderDashboardCard()
    {
        InitializeComponent();
        UpdateDashboardCard();
        InitMethods();
        Database.DatabaseRestored += () =>
        {
            InitMethods();
            UpdateDashboardCard();
        };
    }

    private void InitMethods()
    {
        Database.OrdersList.CollectionChanged += (_, _) => { UpdateDashboardCard(); };
    }

    private void UpdateDashboardCard()
    {
        Dispatcher.UIThread.Post(() =>
        {
            int preparingCount = Database.OrdersList.Where(o => o.OrderStatus == OrderStatusEnum.Preparing).Count();
            ProductsProgressBar.Maximum = Database.OrdersList.Count;
            
            double percentegeValue = preparingCount * 100 / ProductsProgressBar.Maximum;
            DashboardCardMainContent.Text = $"{preparingCount} abertos";
            ProductsProgressBar.Value = preparingCount;
            ProgressBarPercentage.Content = $"{Math.Round(percentegeValue, 0)}%";
            
        }, DispatcherPriority.Background);
    }
    
}