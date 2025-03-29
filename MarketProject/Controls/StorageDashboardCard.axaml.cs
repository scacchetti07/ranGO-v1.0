using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MarketProject.Models;

namespace MarketProject.Controls;

public partial class StorageDashboardCard : UserControl
{
    public StorageDashboardCard()
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
        Database.ProductsList.CollectionChanged += (_, _) => { UpdateDashboardCard(); };

    }

    private void UpdateDashboardCard()
    {
        Dispatcher.UIThread.Post(() =>
        {
            int counting = Database.ProductsList.Count;
            double percentegeValue = counting * 100 / ProductsProgressBar.Maximum;
            DashboardCardMainContent.Text = $"{counting} itens";
            ProductsProgressBar.Value = counting;
            ProgressBarPercentage.Content = $"{Math.Round(percentegeValue, 0)}%";
        }, DispatcherPriority.Background);
    }
}