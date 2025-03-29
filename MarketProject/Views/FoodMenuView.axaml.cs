using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using MarketProject.Controllers;
using MarketProject.Models;
using MarketProject.ViewModels;

namespace MarketProject.Views;

public partial class FoodMenuView : UserControl
{
    private OrderHomeViewModel _vm => DataContext as OrderHomeViewModel;

    private static readonly StyledProperty<ObservableCollection<Foods>> FoodsProperty =
        AvaloniaProperty.Register<FoodMenuView, ObservableCollection<Foods>>(nameof(FoodMenuController));

    public delegate void FoodAddedDelegate(Foods? food);

    public event FoodAddedDelegate FoodAdded;

    public FoodMenuView()
    {
        InitializeComponent();
        UpdateFood();
        FoodsProperty.Changed.AddClassHandler<FoodMenuView>((_, _) => UpdateFood());
        InitMethods();
        Database.DatabaseRestored += () =>
        {
            Dispatcher.UIThread.Post(InitMethods);
            UpdateFood();
        };    
    }

    private void InitMethods()
    {
        Database.FoodsMenuList.CollectionChanged += ((_, _) => { UpdateFood(); });
    }

    private async void UpdateFood()
    {
        Dispatcher.UIThread.Post(() =>
        {
            FoodMenuCardsPanel.Children.Clear();
            var foodList = FoodMenuController.FindFoodMenu();
            if (foodList is null) return;
            foreach (Foods food in foodList)
                FoodMenuCardsPanel.Children.Add(_vm.FoodToCard(food));
        });
    }

    private async void UpdateFood(FoodTypesEnum? foodtype)
    {
        if (foodtype is null) return;

        var searchFood = await FoodMenuController.FindFoodMenu((FoodTypesEnum)foodtype);
        FoodMenuCardsPanel.Children.Clear();
        Dispatcher.UIThread.Post(() =>
        {
            foreach (Foods food in searchFood)
                FoodMenuCardsPanel.Children.Add(_vm.FoodToCard(food));
        });
    }

    private void UpdateFood(IEnumerable<Foods> searchedList)
    {
        FoodMenuCardsPanel.Children.Clear();
        Dispatcher.UIThread.Post(() =>
        {
            foreach (Foods food in searchedList)
                FoodMenuCardsPanel.Children.Add(_vm.FoodToCard(food));
        });
    }

    private async void AddFoodButton_OnClick(object sender, RoutedEventArgs e)
    {
        ManageFoodView manageFoodView = new ManageFoodView { Title = "Cadastro de Pratos" };
        manageFoodView.ShowDialog((Window)Parent!.Parent!.Parent!.Parent!.Parent!.Parent!.Parent);
        var newFood = await manageFoodView.GetFood();
        FoodMenuController.AddNewFoodMenu(newFood);
        manageFoodView.FoodMenuAdded += (food) =>
        {
            if (food is null)
                FoodAdded?.Invoke(null);

            FoodAdded?.Invoke(food);
        };
    }

    private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = SearchTextBox.Text;
        if (keyword.Length < 1)
        {
            UpdateFood();
            return;
        }

        var searchedList = Database.FoodsMenuList.Where(f => f.FoodName.Contains(keyword));
        UpdateFood(searchedList);
    }

    private void TopicsComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FoodMenuCardsPanel is null) return;

        if (TopicsComboBox.SelectedIndex == 0)
        {
            UpdateFood();
            return;
        }

        GetFoodType(((TopicsComboBox.SelectedItem as ComboBoxItem)!).Content!.ToString(),
            out FoodTypesEnum? foodType);
        UpdateFood(foodType);
    }

    private void GetFoodType(string item, out FoodTypesEnum? foodType)
    {
        foodType = item switch
        {
            "Entrada" => FoodTypesEnum.Appetizer,
            "Principal" => FoodTypesEnum.Main,
            "Kids" => FoodTypesEnum.Kids,
            "Vegano" => FoodTypesEnum.Vegan,
            "Salada" => FoodTypesEnum.Salad,
            "Massa" => FoodTypesEnum.Pasta,
            "Peixes" => FoodTypesEnum.Fish,
            "Sanduíche" => FoodTypesEnum.Sandwich,
            "Carnes" => FoodTypesEnum.Meat,
            "Hambúrgueres" => FoodTypesEnum.Meat,
            "Sobremesas" => FoodTypesEnum.Desserts,
            "Bebidas" => FoodTypesEnum.Drink,
            _ => null
        };
    }
}