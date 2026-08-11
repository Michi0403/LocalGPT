namespace LocalGPT.Helper
{
    /// <summary>
    /// Represents a disposable scope.
    /// </summary>
    public class DisposableScope : IDisposable
    {
        private readonly string _scopeInfo;

        /// <summary>
        /// Runs the disposable scope operation.
        /// </summary>
        public DisposableScope(string scopeInfo)
        {
            _scopeInfo = scopeInfo;
        }


        /// <summary>
        /// Runs the dispose operation.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {

                }


                disposed = true;
            }
        }
        private bool disposed;
        /// <summary>
        /// Runs the dispose operation.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets or sets scope info.
        /// </summary>
        public string ScopeInfo => _scopeInfo;
    }
}
