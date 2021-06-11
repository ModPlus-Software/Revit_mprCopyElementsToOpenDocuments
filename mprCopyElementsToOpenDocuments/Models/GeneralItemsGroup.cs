namespace mprCopyElementsToOpenDocuments.Models
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Interfaces;
    using ModPlusAPI.Mvvm;

    /// <summary>
    /// Общая группа элементов в браузере
    /// </summary>
    public class GeneralItemsGroup : VmBase, IBrowserItem
    {
        private bool? _checked = false;
        private bool _isExpanded = true;
        private bool _isVisible = true;

        /// <summary>
        /// Создает экземпляр класса <see cref="GeneralItemsGroup"/>
        /// </summary>
        /// <param name="name">Имя группы</param>
        /// <param name="groups">Список групп элементов</param>
        public GeneralItemsGroup(string name, IEnumerable<BrowserItem> groups)
        {
            Name = name;
            NameUpperInvariant = name.ToUpperInvariant();
            Items = new ObservableCollection<BrowserItem>();

            foreach (var g in groups)
            {
                g.ParentBrowserItem = this;
                g.SelectionChanged += OnGroupSelectionChanged;
                Items.Add(g);
            }
        }

        /// <summary>
        /// Событие изменения количества выделенных элементов
        /// </summary>
        public event EventHandler SelectionChanged;

        /// <inheritdoc/>
        public IBrowserItem ParentBrowserItem { get; set; } = null;

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc/>
        public string NameUpperInvariant { get; }

        /// <inheritdoc />
        public bool? Checked
        {
            get => _checked;
            set
            {
                _checked = value;

                foreach (var group in Items)
                {
                    if (group.IsVisible)
                        group.Checked = value;
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
                if (_isVisible == value)
                    return;
                _isVisible = value;
                OnPropertyChanged();
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
        public ObservableCollection<BrowserItem> Items { get; set; }

        /// <summary>
        /// Метод обработки выделения элементов в браузере
        /// </summary>
        private void OnGroupSelectionChanged(object sender, EventArgs e)
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
