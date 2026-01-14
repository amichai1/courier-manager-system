using System.Threading.Tasks;

namespace PL.Helpers
{
    /// <summary>
    /// Non-blocking mutual exclusion helper for UI observer updates.
    /// Prevents multiple simultaneous updates to the same UI element while allowing new requests to be queued.
    /// </summary>
    internal class ObserverMutex
    {
        private volatile bool _isLoadInProgress = false;
        private volatile bool _restartRequired = false;

        /// <summary>
        /// Checks if a load operation is in progress. If not, marks it as in progress.
        /// Returns true if already in progress (caller should return early).
        /// </summary>
        public bool CheckAndSetLoadInProgressOrRestartRequired()
        {
            if (_isLoadInProgress)
            {
                _restartRequired = true;
                return true;
            }

            _isLoadInProgress = true;
            _restartRequired = false;
            return false;
        }

        /// <summary>
        /// Marks the load operation as complete and checks if a restart was requested.
        /// Includes a small delay (100ms) to prevent UI flooding.
        /// </summary>
        public async Task<bool> UnsetLoadInProgressAndCheckRestartRequested()
        {
            await Task.Delay(100); // Prevent UI flooding
            _isLoadInProgress = false;
            return _restartRequired;
        }
    }
}
