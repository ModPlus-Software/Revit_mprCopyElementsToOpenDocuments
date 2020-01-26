﻿namespace mprCopyElementsToOpenDocuments.Models
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Interfaces;
    using ModPlusAPI.Mvvm;

    /// <summary>
    /// Группа элементов в браузере
    /// </summary>
    public class BrowserItemGroup : VmBase, IBrowserItem
    {
        private bool? _checked = false;
        private bool _isExpanded;
        private ObservableCollection<BrowserItem> _items = new ObservableCollection<BrowserItem>();

        /// <summary>
        /// Создает экземпляр класса <see cref="BrowserItemGroup"/>
        /// </summary>
        /// <param name="name">Имя группы</param>
        /// <param name="items">Список элементов группы</param>
        public BrowserItemGroup(string name, List<BrowserItem> items)
        {
            Name = name;

            items.ForEach(item =>
            {
                item.SelectionChanged += OnItemSelectionChanged;
                _items.Add(item);
            });
        }

        /// <summary>
        /// Событие выделения группы
        /// </summary>
        public event EventHandler SelectionChanged;

        /// <inheritdoc/>
        public bool? Checked
        {
            get => _checked;
            set
            {
                _checked = value;

                foreach (var item in _items)
                {
                    item.Checked = value;
                }

                OnPropertyChanged();
                OnSelectionChanged();
            }
        }

        /// <summary>
        /// Имя группы элементов
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Показывает, развернута ли группа
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Список элементов группы
        /// </summary>
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
    }
}
