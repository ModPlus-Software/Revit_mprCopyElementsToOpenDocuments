namespace mprCopyElementsToOpenDocuments.Helpers
{
    using ModPlusAPI.Enums;
    using ModPlusAPI.Services;

    /// <summary>
    /// Журнал работы приложения
    /// </summary>
    public sealed class Logger
    {
        private static Logger _instance;
        private readonly ResultService _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        private Logger()
        {
            _logger = new ResultService();
        }

        /// <summary>
        /// Экземпляр списка для ведения журнала работы приложения
        /// </summary>
        public static Logger Instance => _instance ?? (_instance = new Logger());

        /// <summary>
        /// Clear log
        /// </summary>
        public void Clear()
        {
            _logger.Clear();
        }

        /// <summary>
        /// Add info message
        /// </summary>
        /// <param name="message">Message</param>
        public void AddInfo(string message)
        {
            _logger.Add(message, null);
        }

        /// <summary>
        /// Add error message
        /// </summary>
        /// <param name="message">Message</param>
        public void AddError(string message)
        {
            _logger.Add(message, null, ResultItemType.Error);
        }

        /// <summary>
        /// Show logger window
        /// </summary>
        public void Show()
        {
            _logger.ShowByType();
        }

        /// <summary>
        /// Get errors count
        /// </summary>
        public int GetErrorsCount()
        {
            return _logger.Count(ResultItemType.Error);
        }
    }
}
