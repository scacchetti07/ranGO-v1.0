using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using DynamicData;
using MarketProject.Controllers;
using MarketProject.Models;
using MarketProject.ViewModels;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using Supplyctrl = MarketProject.Controllers.SupplyController;

namespace MarketProject.Views;

public partial class SupplyView : UserControl
{
    public SupplyView()
    {
        InitializeComponent();
        SupplyDataGrid.ItemsSource = Database.SupplyList.Select(SupplyViewModel.SuppliesToDataGrid);

        InitMethods();
        Database.DatabaseRestored += () =>
        {
            Dispatcher.UIThread.Post(InitMethods);
            UpdateSupplyDatagrid(null,null);
        };
        
        if (Database.SupplyList.Select(s => s.InDeliver).FirstOrDefault())
            SendSupplyDeliverButton.Classes.Add("DefineSupplyDeliverButtonToolTip");
        else
            SendSupplyDeliverButton.Classes.Remove("DefineSupplyDeliverButtonToolTip");
    }

    private void InitMethods()
    {
        // Resolução do Erro: Call Invalid Thread
        Database.SupplyList.CollectionChanged += UpdateSupplyDatagrid;
        Database.ProductsList.CollectionChanged += UpdateSupplyDatagrid;
    }

    private void UpdateSupplyDatagrid(object sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            SupplyDataGrid.ItemsSource = new List<SupplyDataGrid>();
            IEnumerable<Supply> list = (sender as ObservableCollection<Supply>) ?? (IEnumerable<Supply>)Database.SupplyList.ToList();
            SupplyDataGrid.ItemsSource = list.Select(SupplyViewModel.SuppliesToDataGrid);
        }, DispatcherPriority.Background);
    }

    private async void AddSupply_OnClick(object sender, RoutedEventArgs e)
    {
        SupplyAddView addNewSupplyView = new()
        {
            Title = "Cadastro de Fornecedores",
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome,
            ExtendClientAreaToDecorationsHint = true,
            CanResize = false,
            ShowInTaskbar = false,
            SizeToContent = SizeToContent.WidthAndHeight
        };
        addNewSupplyView.SupplyAdded += (supply) =>
        {
            AddPopup.IsOpen = true;
            AddProdLabel.Content = "Novo Fornecedor Adicionado!";
            ContentAddTextBlock.Text = $"O Fornecedor '{supply.Name}' foi adicionado com sucesso!";
        };
        await addNewSupplyView.ShowDialog((Window)this.Parent!.Parent!.Parent!.Parent!).ConfigureAwait(false);
    }

    private async void DeleteSupply_OnClick(object sender, RoutedEventArgs e)
    {
        var supplies = SupplyDataGrid.SelectedItems.Cast<SupplyDataGrid>().FirstOrDefault();
        var selectedSupply = Supplyctrl.FindSupplyByCnpj(supplies.Cnpj);

        var msgBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        {
            ContentHeader = "Excluir produto do estoque",
            ContentMessage = $"Você realmente deseja excluir \"{selectedSupply.Name}\" do estoque em definitivo?",
            ButtonDefinitions = ButtonEnum.YesNo,
            Icon = Icon.Warning,
            CanResize = false,
            ShowInCenter = true,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            SystemDecorations = SystemDecorations.BorderOnly
        });
        var result = await msgBox.ShowAsync().ConfigureAwait(false);
        if (result == ButtonResult.No) return;
        Supplyctrl.DeleteSupply(selectedSupply);
        
        Dispatcher.UIThread.Post(() =>
        {
            DeletePopup.IsOpen = true;
            DeleteProdLabel.Content = "Fornecedor Removido!";
            ContentDeleteTextBlock.Text = $"O '{selectedSupply.Name}' foi removido do estoque com sucesso!"; 
        },DispatcherPriority.Background);
    }

    private async void EditSupply_OnClick(object sender, RoutedEventArgs e)
    {
        var supplies = SupplyDataGrid.SelectedItems.Cast<SupplyDataGrid>().FirstOrDefault();
        if (supplies is null) return;
        var selectedSupply = Supplyctrl.FindSupplyByCnpj(supplies.Cnpj);

        SupplyAddView editSupply = new(selectedSupply)
        {
            Title = "Cadastro de Fornecedores",
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome,
            ExtendClientAreaToDecorationsHint = true,
            CanResize = false,
            ShowInTaskbar = false,
            SizeToContent = SizeToContent.WidthAndHeight
        };
        editSupply.SupplyAdded += (supply) =>
        {
            if (supply is null) return;
            AddPopup.IsOpen = true;
            AddProdLabel.Content = "Fornecedor Editado!";
            ContentAddTextBlock.Text = $"O Fornecedor '{supply.Name}' foi editado com sucesso!";
        };
        await editSupply.ShowDialog((Window)Parent!.Parent!.Parent!.Parent!).ConfigureAwait(false);
    }

    private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = SearchTextBox.Text;
        if (keyword.Length < 1)
        {
            SupplyDataGrid.ItemsSource = Database.SupplyList!
                .Select(SupplyViewModel.SuppliesToDataGrid);
            return;
        }

        var regexPattern = new Regex("@[./-]|\\d");

        IEnumerable<Supply> searchedList;
        if (regexPattern.IsMatch(keyword))
            searchedList = Database.SupplyList.Where(p => p.Cnpj.Contains(keyword));
        else
            searchedList =
                Database.SupplyList.Where(p => p.Name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase));

        SupplyDataGrid.ItemsSource = searchedList!.Select(SupplyViewModel.SuppliesToDataGrid);
    }

    private async void SendSupplyDeliverButton_OnClick(object sender, RoutedEventArgs e)
    {
        var deliverSupply = SupplyController.SupplyList.Where(s => s.InDeliver).FirstOrDefault();
        if (deliverSupply is not null)
        {
            var msgBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentHeader = "Já foi definido uma entrega!",
                ContentMessage = $"Uma entrega realizada pelo fornecedor {deliverSupply.Name} já está em andamento. Você quer removê-lo?",
                ButtonDefinitions = ButtonEnum.YesNo,
                Icon = Icon.Warning,
                CanResize = false,
                ShowInCenter = true,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                SystemDecorations = SystemDecorations.BorderOnly
            });
            var result = await msgBox.ShowAsync();
            if (result == ButtonResult.No) return;
            deliverSupply.InDeliver = false;
            Supplyctrl.UpdateSupply(deliverSupply);
        }

        SendSupplyDeliverView sendSupplyDeliverView = new();
        sendSupplyDeliverView.ShowDialog((Window)Parent!.Parent!.Parent!.Parent!);
        sendSupplyDeliverView.SendSupply += (supply) =>
        {
            if (supply is null) return;
            AddPopup.IsOpen = true;
            AddProdLabel.Content = "Fornecedor adicionado como entrega!!";
            ContentAddTextBlock.Text = $"O Fornecedor '{supply.Name}' foi adicionado na entrega!";
        };
    }
}