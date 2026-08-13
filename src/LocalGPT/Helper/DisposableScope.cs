namespace LocalGPT.Helper
{
    /// <summary>
    /// Represents a disposable scope application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    public class DisposableScope : IDisposable
    {
        /// <summary>
        /// Stores the internal scope info state used by <see cref="DisposableScope"/> while executing its surrounding workflow.
        /// </summary>
        private readonly string _scopeInfo;

        /// <summary>
        /// Initializes a new <see cref="DisposableScope"/> instance and captures the dependencies or initial state required by its disposable scope workflow.
        /// </summary>
        /// <param name="scopeInfo">Scope info value supplied to the disposable scope operation and used when producing its result.</param>
        public DisposableScope(string scopeInfo)
        {
            _scopeInfo = scopeInfo;
        }


        /// <summary>
        /// Releases resources owned by <see cref="DisposableScope"/> and leaves the disposable scope workflow in a safely disposed state.
        /// </summary>
        /// <param name="disposing">Value indicating whether disposing should apply to this operation.</param>
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
        /// <summary>
        /// Stores the internal disposed state used by <see cref="DisposableScope"/> while executing its surrounding workflow.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// Releases resources owned by <see cref="DisposableScope"/> and leaves the disposable scope workflow in a safely disposed state.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the scope info value that forms part of the disposable scope state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The scope info value exposed by <see cref="DisposableScope"/>.</value>
        public string ScopeInfo => _scopeInfo;
    }
}
