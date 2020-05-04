namespace mprCopyElementsToOpenDocuments.Helpers
{
    using System.Text;

    /// <summary>
    /// Журнал работы приложения
    /// </summary>
    public class Logger
    {
        private static Logger _instance;
        private StringBuilder _logger;
        private int _errorsCount;
        private static readonly object Mutex = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        protected Logger()
        {
            _logger = new StringBuilder();
            _errorsCount = 0;
        }

        /// <summary>
        /// Экземпляр списка для ведения журнала работы приложения
        /// </summary>
        public static Logger Instance
        {
            get
            {
                lock (Mutex)
                {
                    return _instance ?? (_instance = new Logger());
                }
            }
        }

        /// <summary>
        /// Clear log
        /// </summary>
        public void Clear()
        {
            _logger = new StringBuilder();
            _errorsCount = 0;
        }

        /// <summary>
        /// Add info message
        /// </summary>
        /// <param name="message">Message</param>
        public void AddInfo(string message)
        {
            _logger.AppendLine(message);
        }

        /// <summary>
        /// Add error message
        /// </summary>
        /// <param name="message">Message</param>
        public void AddError(string message)
        {
            _logger.AppendLine(message);
            _errorsCount++;
        }

        /// <summary>
        /// Get log as one string instance
        /// </summary>
        public string GetLogString()
        {
            return _logger.ToString();
        }

        /// <summary>
        /// Get errors count
        /// </summary>
        public int GetErrorsCount()
        {
            return _errorsCount;
        }
    }
}
