namespace mprCopyElementsToOpenDocuments
{
    using System;
    using System.Collections.Generic;
    using ModPlusAPI.Abstractions;
    using ModPlusAPI.Enums;

    /// <inheritdoc />
    public class ModPlusConnector : IModPlusPlugin
    {
        private static ModPlusConnector _instance;

        /// <summary>
        /// Singleton
        /// </summary>
        public static ModPlusConnector Instance => _instance ?? (_instance = new ModPlusConnector());

        /// <inheritdoc />
        public SupportedProduct SupportedProduct => SupportedProduct.Revit;

        /// <inheritdoc />
        public string Name => "mprCopyElementsToOpenDocuments";

#if R2017
        /// <inheritdoc />
        public string AvailProductExternalVersion => "2017";
#elif R2018
        /// <inheritdoc />
        public string AvailProductExternalVersion => "2018";
#elif R2019
        /// <inheritdoc />
        public string AvailProductExternalVersion => "2019";
#elif R2020
        /// <inheritdoc />
        public string AvailProductExternalVersion => "2020";
#elif R2021
        /// <inheritdoc />
        public string AvailProductExternalVersion => "2021";
#endif

        /// <inheritdoc />
        public string FullClassName => "mprCopyElementsToOpenDocuments.Command";

        /// <inheritdoc />
        public string AppFullClassName => string.Empty;

        /// <inheritdoc />
        public Guid AddInId => Guid.Empty;

        /// <inheritdoc />
        public string LName => "Копировать элементы в документы";

        /// <inheritdoc />
        public string Description => "Пакетное копирование элементов в открытые документы";

        /// <inheritdoc />
        public string Author => "Ахмедов Самиль";

        /// <inheritdoc />
        public string Price => "0";

        /// <inheritdoc />
        public bool CanAddToRibbon => true;

        /// <inheritdoc />
        public string FullDescription => "Пакетное копирование выбранных элементов из текущего документа в указанные открытые документы";

        /// <inheritdoc />
        public string ToolTipHelpImage => string.Empty;

        /// <inheritdoc />
        public List<string> SubPluginsNames => new List<string>();

        /// <inheritdoc />
        public List<string> SubPluginsLNames => new List<string>();
        
        /// <inheritdoc />
        public List<string> SubDescriptions => new List<string>();

        /// <inheritdoc />
        public List<string> SubFullDescriptions => new List<string>();

        /// <inheritdoc />
        public List<string> SubHelpImages => new List<string>();

        /// <inheritdoc />
        public List<string> SubClassNames => new List<string>();
    }
}