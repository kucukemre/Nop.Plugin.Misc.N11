using Nop.Services.Tasks;

namespace Nop.Plugin.Misc.N11.Services
{
    /// <summary>
    /// Represents a schedule task to synchronize contacts
    /// </summary>
    public class SynchronizationTask : IScheduleTask
    {
        #region Fields

        private readonly N11Manager _n11Manager;

        #endregion

        #region Ctor

        public SynchronizationTask(N11Manager n11Manager)
        {
            _n11Manager = n11Manager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Execute task
        /// </summary>
        public void Execute()
        {
            _n11Manager.Synchronize();
        }

        #endregion
    }
}