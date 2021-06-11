namespace mprCopyElementsToOpenDocuments.Models
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using Interfaces;
    using ModPlusAPI.Mvvm;

    /// <summary>
    /// Элемент в браузере
    /// </summary>
    public class BrowserItem : VmBase, IBrowserItem, IRevitElement
    {
        private bool? _checked = false;
        private bool _isExpanded;
        private ObservableCollection<BrowserItem> _items = new ObservableCollection<BrowserItem>();
        private bool _isVisible = true;

        /// <summary>
        /// Создает экземпляр класса <see cref="BrowserItem"/>
        /// </summary>
        /// <param name="id">Идентификатор элемента</param>
        /// <param name="categoryName">Имя категории</param>
        /// <param name="familyName">Имя семейства</param>
        /// <param name="name">Имя элемента</param>
        public BrowserItem(int id, string categoryName, string familyName, string name)
        {
            Id = id;
            CategoryName = categoryName;
            FamilyName = familyName;
            Name = name;
            NameUpperInvariant = name.ToUpperInvariant();
        }

        /// <summary>
        /// Создает экземпляр класса <see cref="BrowserItem"/>
        /// </summary>
        /// <param name="name">Имя группы</param>
        /// <param name="items">Список элементов группы</param>
        public BrowserItem(string name, List<BrowserItem> items)
        {
            Name = name;
            NameUpperInvariant = name.ToUpperInvariant();
            
            ProcessSubItems(items, this);

            foreach (var item in items)
            {
                _items.Add(item);
            }
        }

        /// <summary>
        /// Событие выделения элемента
        /// </summary>
        public event EventHandler SelectionChanged;
        
        /// <summary>
        /// Родительский элемент дерева
        /// </summary>
        public IBrowserItem ParentBrowserItem { get; set; }

        /// <inheritdoc/>
        public bool? Checked
        {
            get => _checked;
            set
            {
                _checked = value;

                if (Items.Any())
                {
                    foreach (var item in Items)
                    {
                        if (item.IsVisible)
                            item.Checked = value;
                    }
                }

                OnPropertyChanged();
                OnSelectionChanged();
            }
        }

        /// <inheritdoc />
        public bool ShowSecondRow { get; set; }

        /// <inheritdoc/>
        public string SecondRowValue { get; set; }

        /// <inheritdoc/>
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                OnPropertyChanged();
                if (value && ParentBrowserItem != null)
                    ParentBrowserItem.IsVisible = true;
            }
        }

        /// <inheritdoc />
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc/>
        public int Id { get; }

        /// <inheritdoc/>
        public string CategoryName { get; }

        /// <inheritdoc/>
        public string FamilyName { get; }

        /// <inheritdoc/>
        public string Name { get; }
        
        /// <inheritdoc/>
        public string NameUpperInvariant { get; }

        /// <summary>
        /// True указывает, что свойство <see cref="Id"/> содержит числовое представление типа
        /// <see cref="Autodesk.Revit.DB.WorksetId"/> вместо <see cref="Autodesk.Revit.DB.ElementId"/>
        /// </summary>
        public bool IsWorksetId { get; set; }

        /// <inheritdoc/>
        public ObservableCollection<BrowserItem> Items
        {
            get => _items;
            set
            {
                _items = value;
                OnPropertyChanged();
            }
        }
        
        /// <summary>
        /// Метод обработки выделения элементов в браузере
        /// </summary>
        private void OnItemSelectionChanged(object sender, EventArgs e)
        {
            OnSelectionChanged();
        }

        /// <summary>
        /// Метод вызова события
        /// </summary>
        protected virtual void OnSelectionChanged()
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
        
        private void ProcessSubItems(IEnumerable<BrowserItem> items, IBrowserItem parent)
        {
            foreach (var item in items)
            {
                item.SelectionChanged += OnItemSelectionChanged;
                item.ParentBrowserItem = parent;
                
                if (item.Items.Any())
                    ProcessSubItems(item.Items, item);
            }
        }
    }
}
