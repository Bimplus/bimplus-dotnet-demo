
using System.Windows.Input;
using BimPlusDemo.UserControls;

namespace BimPlusDemo.Commands
{
    internal class CancelCommand : ICommand
    {
        #region Fields

        // Member variables
        private readonly ProgressWindow _mProgress;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CancelCommand(ProgressWindow progress)
        {
            _mProgress = progress;
        }

        #endregion

        #region ICommand Members

        /// <summary>
        /// Whether the CancelCommand is enabled.
        /// </summary>
        public bool CanExecute(object? parameter)
        {
            return true;
        }

        /// <summary>
        /// Actions to take when CanExecute() changes.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        /// <summary>
        /// Executes the CancelCommand
        /// </summary>
        public void Execute(object? parameter)
        {
            /* The Cancel command is invoked by the Cancel button, and on the window
             * close (in case the user clicks the close box to cancel. The Cancel 
             * command sets this property and checks it to make sure that the command 
             * isn't run twice when the user clicks the Cancel button (once for the 
             * button-click, and once for the window-close. */

            // Exit if dialog has already been cancelled
            if (_mProgress.IsCancelled) return;

            /* The DoDemoWorkCommand.Execute() method defines a cancellation token source and
             * passes it to the Progress Dialog view model. The token itself is passed to the 
             * parallel image processing loop defined in the GoCommand.DoWork()  method. We 
             * cancel the loop by calling the TokenSource.Cancel() method. */

            // Validate TokenSource object
            if (_mProgress.TokenSource == null)
            {
                throw new ApplicationException("ProgressDialogViewModel.TokenSource property is null");
            }

            // Cancel all pending background tasks
            _mProgress.TokenSource.Cancel();
        }

        #endregion
    }
}
