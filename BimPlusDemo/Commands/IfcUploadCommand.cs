using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using BimPlusDemo.UserControls;

namespace BimPlusDemo.Commands
{
    public class IfcUploadCommand : ICommand
    {
        #region Fields

        // Member variables
        private readonly ProgressWindow _mProgress;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public IfcUploadCommand(ProgressWindow progress)
        {
            _mProgress = progress;
        }

        #endregion

        #region ICommand Members

        /// <summary>
        /// Whether the DoDemoWorkCommand is enabled.
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
        /// Executes the DoDemoWorkCommand
        /// </summary>
        public void Execute(object? parameter)
        {
            if (parameter is double v)
            {
                var assignProgressCounter = new Action<int>(_mProgress.AssignProgressCounter);
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, assignProgressCounter, (int)v);
                return;
            }

            if (parameter is string command)
            {
                var assignMessage = new Action<string>(_mProgress.AssignMessage);
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, assignMessage, command);
                return;
            }

            // Initialize
            _mProgress.ClearViewModel();

            // Set the view model's token source
            _mProgress.TokenSource = new CancellationTokenSource();

            // Set the maximum progress value 
            _mProgress.ProgressMax = 100;

            // Announce that work is starting
            _mProgress.RaiseWorkStartedEvent();

            // Launch first background task
            Task.Factory.StartNew(() =>
            {
                try
                {
                    int processState = 0;
                    var incrementProgressCounter = new Action<int>(_mProgress.IncrementProgressCounter);
                    while (processState < 100)
                    {
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, incrementProgressCounter, 1);
                        Thread.Sleep(200);
                        ++processState;
                    }
                }
                catch (OperationCanceledException)
                {
                    var showCancellationMessage = new Action(_mProgress.ShowCancellationMessage);
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, showCancellationMessage);
                }
            });
        }
        #endregion
    }
}
