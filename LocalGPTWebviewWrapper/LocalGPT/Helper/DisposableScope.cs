namespace LocalGPT.Helper
{
    public class DisposableScope : IDisposable
    {
        private readonly string _scopeInfo;

        public DisposableScope(string scopeInfo)
        {
            _scopeInfo = scopeInfo;
        }


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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public string ScopeInfo => _scopeInfo;
    }
}
