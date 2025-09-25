using Grocery.Core.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Grocery.App.ViewModels
{
    public class ProductViewModel : BaseViewModel
    {
        private List<Product> _allProducts = new List<Product>();
        public ObservableCollection<Product> Products { get; } = new ObservableCollection<Product>();
        public ICommand SearchCommand { get; }

        // lege ctor: geen voorbeelddata hier
        public ProductViewModel()
        {
            SearchCommand = new Command<object>(param =>
            {
                var query = param as string;
                ApplyFilter(query);
            });
        }

        // alternatief: ctor die meteen initial data accepteert
        public ProductViewModel(IEnumerable<Product> initialProducts) : this()
        {
            SetAllProducts(initialProducts);
        }

        public void SetAllProducts(IEnumerable<Product> products)
        {
            _allProducts = products?.ToList() ?? new List<Product>();
            UpdateProducts(_allProducts);
        }

        // optioneel: async loader als jouw service async is
        public async System.Threading.Tasks.Task LoadProductsAsync(Func<System.Threading.Tasks.Task<IEnumerable<Product>>> loader)
        {
            var list = await loader();
            SetAllProducts(list);
        }

        private void ApplyFilter(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                UpdateProducts(_allProducts);
                return;
            }

            var q = query.Trim();
            var filtered = _allProducts
                .Where(p => !string.IsNullOrEmpty(p.Name) &&
                            p.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            UpdateProducts(filtered);
        }

        private void UpdateProducts(IEnumerable<Product> items)
        {
            Products.Clear();
            foreach (var p in items)
                Products.Add(p);
        }
    }
}
