namespace MvcApi //System.Web.Mvc.Async
{
    using System.Threading;

    internal sealed class SingleEntryGate
    {
        // Fields
        private int _status;
        private const int ENTERED = 1;
        private const int NOT_ENTERED = 0;

        // Methods
        public SingleEntryGate()
        {
        }

        public bool TryEnter()
        {
            return (Interlocked.Exchange(ref this._status, 1) == 0);
        }

    }
}
