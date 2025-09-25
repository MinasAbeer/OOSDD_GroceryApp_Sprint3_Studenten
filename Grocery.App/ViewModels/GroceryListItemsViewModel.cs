using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grocery.App.Views;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.ObjectModel;
using System.Text.Json;


namespace Grocery.App.ViewModels
{
    [QueryProperty(nameof(GroceryList), nameof(GroceryList))]
    public partial class GroceryListItemsViewModel : BaseViewModel
    {
        private readonly IGroceryListItemsService _groceryListItemsService;
        private readonly IProductService _productService;
        private readonly IFileSaverService _fileSaverService;

        public ObservableCollection<GroceryListItem> MyGroceryListItems { get; set; } = new ObservableCollection<GroceryListItem>();
        public ObservableCollection<Product> AvailableProducts { get; set; } = new ObservableCollection<Product>();

        private List<Product> _allAvailableProducts = new List<Product>();

        [ObservableProperty]
        GroceryList groceryList = new(0, "None", DateOnly.MinValue, "", 0);

        [ObservableProperty]
        string myMessage = string.Empty;

        [ObservableProperty]
        string query = string.Empty;

        public GroceryListItemsViewModel(IGroceryListItemsService groceryListItemsService, IProductService productService, IFileSaverService fileSaverService)
        {
            _groceryListItemsService = groceryListItemsService;
            _productService = productService;
            _fileSaverService = fileSaverService;

            if (groceryList != null && groceryList.Id != 0)
                Load(groceryList.Id);
        }

        private void Load(int id)
        {
            MyGroceryListItems.Clear();
            foreach (var item in _groceryListItemsService.GetAllOnGroceryListId(id))
                MyGroceryListItems.Add(item);

            GetAvailableProducts();
            MyMessage = string.Empty;
        }

        private void GetAvailableProducts()
        {
            AvailableProducts.Clear();
            _allAvailableProducts.Clear();

            var products = _productService.GetAll() ?? Enumerable.Empty<Product>();

            foreach (Product p in products)
            {
                if (MyGroceryListItems.FirstOrDefault(g => g.ProductId == p.Id) == null && p.Stock > 0)
                {
                    AvailableProducts.Add(p);
                    _allAvailableProducts.Add(p);
                }
            }

        }

        partial void OnGroceryListChanged(GroceryList value)
        {
            if (value == null) return;
            Load(value.Id);
        }

        [RelayCommand]
        public async Task ChangeColor()
        {
            Dictionary<string, object> paramater = new() { { nameof(GroceryList), GroceryList } };
            await Shell.Current.GoToAsync($"{nameof(ChangeColorView)}?Name={GroceryList.Name}", true, paramater);
        }

        [RelayCommand]
        public void AddProduct(Product product)
        {
            if (product == null) return;

            GroceryListItem item = new(0, GroceryList.Id, product.Id, 1);
            _groceryListItemsService.Add(item);

            product.Stock--;
            _productService.Update(product);

            Load(GroceryList.Id);
        }

        [RelayCommand]
        public async Task ShareGroceryList(CancellationToken cancellationToken)
        {
            if (GroceryList == null || MyGroceryListItems == null) return;
            string jsonString = JsonSerializer.Serialize(MyGroceryListItems);
            try
            {
                await _fileSaverService.SaveFileAsync("Boodschappen.json", jsonString, cancellationToken);
                await Toast.Make("Boodschappenlijst is opgeslagen.").Show(cancellationToken);
            }
            catch (Exception ex)
            {
                await Toast.Make($"Opslaan mislukt: {ex.Message}").Show(cancellationToken);
            }
        }

        [RelayCommand]
        private void Search(string searchTerm)
        {
            var term = (searchTerm ?? Query ?? string.Empty).Trim();

            if (_allAvailableProducts == null || _allAvailableProducts.Count == 0)
            {
                AvailableProducts.Clear();
                return;
            }

            if (string.IsNullOrWhiteSpace(term))
            {
                AvailableProducts.Clear();
                foreach (var p in _allAvailableProducts)
                    AvailableProducts.Add(p);
            }

            var filtered = _allAvailableProducts
                .Where(p => !string.IsNullOrEmpty(p.Name) && p.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();

            AvailableProducts.Clear();
            foreach (var p in filtered)
                AvailableProducts.Add(p);

            if (filtered.Count == 0)
            {
                MyMessage = $"Er zijn geen producten gevonden voor '{term}'.";
            }
            else
            {
                MyMessage = string.Empty;
            }
        }
    }
}
