using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MarketProject.Controllers;
using MarketProject.Controls;
using MarketProject.Extensions;
using MarketProject.Models;
using MarketProject.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;

namespace MarketProject.Views;

public partial class ManageOrdersView : Window
{
    public async Task<Orders> GetOrder() => await _task.Task;

    private TaskCompletionSource<Orders> _task = new();
    public List<Foods> AutoCompleteSelectedFoodsList { get; } = [];

    private string _editUserId;
    private OrderHomeViewModel _vm => DataContext as OrderHomeViewModel;

    public delegate void NewOrderAdded(Orders? orders);

    public event NewOrderAdded OrderAdded;

    public ManageOrdersView()
    {
        InitializeComponent();
        TableNumberTextBox.AddHandler(TextInputEvent, PreviewTextChanged, RoutingStrategies.Tunnel);
        this.ResponsiveWindow();
        
        FoodsAutoCompleteBox.AddHandler(KeyDownEvent, (sender, e) =>
        {
            if (sender is not AutoCompleteBox autoComplete)
                return;
            if (e.Key != Key.Tab || autoComplete.Text is null || autoComplete.Text.Trim() == "")
                return;

            string item = autoComplete.ItemsSource!
                .Cast<string>()
                .FirstOrDefault(item =>
                    autoComplete.TextFilter?.Invoke(autoComplete.Text, item) ?? true);
            if (item is null)
                return;

            autoComplete.Text = item;
            autoComplete.CaretIndex = autoComplete.Text.Length;
            e.Handled = true;
        }, RoutingStrategies.Tunnel);

        FoodsAutoCompleteBox.ItemsSource = Database.FoodsMenuList.Select(f => f.FoodName);
    }

    public ManageOrdersView(string id) : this()
    {
        _editUserId = id;
        _ = EditOrdersAsync(id);
    }
    private void PreviewTextChanged(object sender, TextInputEventArgs e)
    {
        Regex regex = new(@"^[0-9]+$");
        e.Handled = !regex.IsMatch(e.Text!);
    }


    private async Task EditOrdersAsync(string id)
    {
        var selectedOrders = OrderController.FindOrders(id);
        AddNewOrderButton.Content = "Editar";
        TableNumberTextBox.Text = selectedOrders.TableNumber.ToString();
        WaiterNameTextBox.Text = selectedOrders.WaiterName;
        _vm.OrderStatus = selectedOrders.OrderStatus;
        List<Foods> foods = (await FoodMenuController.FindFoodsByOrders(selectedOrders.FoodsOrder)).ToList();
        foreach (var food in foods)
        {
            AutoCompleteSelectedFoodsList.Add(food);
            TagContentStackPanel.Children.Add(GenereteAutoCompleteTag(food));

            var itemSource = FoodsAutoCompleteBox.ItemsSource.Cast<string>().ToList();
            itemSource.Remove(food.FoodName);
            FoodsAutoCompleteBox.ItemsSource = itemSource;
        }
        FoodDescriptionTextBox.Text = selectedOrders.FoodDescription;
    }
    private async void AddNewOrder_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            List<string> textBoxes = GetTextBoxes();
            if (textBoxes.TrueForAll(txt => !string.IsNullOrEmpty(txt)) && AutoCompleteSelectedFoodsList.Count > 0)
                throw new Exception("Existem campos incompletos no cadastro de pedidos!");
            
            if (int.Parse(TableNumberTextBox.Text) < 1)
                throw new Exception("O número da mesa deve ser superior a 0.");
            
            var newOrder = new Orders(int.Parse(TableNumberTextBox.Text!), WaiterNameTextBox.Text,
                AutoCompleteSelectedFoodsList.Select(f => f.Id).ToList(), OrderStatusEnum.New, FoodDescriptionTextBox.Text);
            
            if (_editUserId is not null)
            {
                newOrder.Id = _editUserId;
                newOrder.OrderStatus = _vm.OrderStatus;
                OrderController.EditOrder(newOrder);
                OrderHomeViewModel.IsEditable = true;
                Close();
                //OrderAdded?.Invoke(newOrder);
                return;
            }

            OrderHomeViewModel.IsEditable = false;
            _task.TrySetResult(newOrder);
            Close();
        }
        catch (Exception ex)
        {
            var msgBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentHeader = "Erro ao salvar no pedido",
                ContentMessage = ex.Message,
                ButtonDefinitions = ButtonEnum.Ok,
                Icon = MsBox.Avalonia.Enums.Icon.Error,
                CanResize = false,
                ShowInCenter = true,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                SystemDecorations = SystemDecorations.BorderOnly
            });
            await msgBox.ShowAsync().ConfigureAwait(false);
        }
    }

    private void ReturnButton_OnClick(object sender, RoutedEventArgs e)
    {
        List<string> textBoxes = GetTextBoxes();
        if (textBoxes.TrueForAll(txt => string.IsNullOrEmpty(txt)) && AutoCompleteSelectedFoodsList.Count == 0)
        {
            Close();
            return;
        }

        Dispatcher.UIThread.Post(async () =>
        {
            var checkMsgBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentHeader = "Dados ainda digitados.",
                ContentMessage =
                    "Ainda existem dados escritos nos campos de cadastro,\nQuer realmente sair do cadastro?",
                ButtonDefinitions = ButtonEnum.YesNo,
                Icon = MsBox.Avalonia.Enums.Icon.Info,
                CanResize = false,
                ShowInCenter = true,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                SystemDecorations = SystemDecorations.BorderOnly
            });
            var result = await checkMsgBox.ShowAsync();
            if (result == ButtonResult.Yes) Close();
        }, DispatcherPriority.Background);
    }

    private List<string> GetTextBoxes()
        =>
        [
            TableNumberTextBox.Text,
            WaiterNameTextBox.Text,
            FoodDescriptionTextBox.Text,
            FoodsAutoCompleteBox.Text
        ];

    private async void FoodsAutoCompleteBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return; 

        Foods food = await FoodMenuController.FindFoodMenuByNameAsync(FoodsAutoCompleteBox.Text);
        if (food is null || AutoCompleteSelectedFoodsList.Any(p => p.Id == food.Id)) return;

        AutoCompleteSelectedFoodsList.Add(food);
        var itemSource = FoodsAutoCompleteBox.ItemsSource.Cast<string>().ToList();
        itemSource.Remove(food.FoodName);
        FoodsAutoCompleteBox.ItemsSource = itemSource;

        FoodsAutoCompleteBox.Text = "";
        TagContentStackPanel.Children.Add(GenereteAutoCompleteTag(food));
    }

    public Border GenereteAutoCompleteTag(Foods food)
    {
        Label label = new() { Content = food.FoodName };
        Image image = new();
        StackPanel stackPanel = new() { Children = { label, image } };

        var border = new Border
        {
            Child = stackPanel,
            Classes = { "AutoCompleteTag" }
        };
        border.PointerPressed += (_, _) =>
        {
            AutoCompleteSelectedFoodsList.Remove(food);
            TagContentStackPanel.Children.Remove(border);

            var itemSource = FoodsAutoCompleteBox.ItemsSource.Cast<string>().ToList();
            itemSource.Add(food.FoodName);
            FoodsAutoCompleteBox.ItemsSource = itemSource;
        };
        return border;
    }
}