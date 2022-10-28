using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BimPlusDemo.Commands;

namespace BimPlusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : INotifyPropertyChanging, INotifyPropertyChanged
    {
        #region Fields

        // Property variables
        private int _pProgress;
        private string _pProgressMessage;
        private int _pProgressMax;

        //Member variables
        private string _mProgressMessageTemplate;
        private readonly string _mCancellationMessage;

        #endregion

        public ProgressWindow(Window parent)
        {
            InitializeComponent();
            DataContext = this;
            _mProgressMessageTemplate = "Upload Ifc-File {0}% complete";
            _mCancellationMessage = "Ifc Upload cancelled";
            Cancel = new CancelCommand(this);
            ClearViewModel();

            WorkStarted += (sender, args) =>
            {
                this.Owner = parent;
                this.Show();
            };
            WorkEnded += (sender, args) => { this.Close(); };
        }


        #region Admin Properties

        /// <summary>
        /// A cancellation token source for the background operations.
        /// </summary>
        internal CancellationTokenSource TokenSource { get; set; }

        /// <summary>
        /// Whether the operation in progress has been cancelled.
        /// </summary>
        /// <remarks> 
        /// The Cancel command is invoked by the Cancel button, and on the window
        /// close (in case the user clicks the close box to cancel. The Cancel 
        /// command sets this property and checks it to make sure that the command 
        /// isn't run twice when the user clicks the Cancel button (once for the 
        /// button-click, and once for the window-close.
        /// </remarks>
        public bool IsCancelled { get; set; }

        public virtual bool IgnorePropertyChangeEvents { get; set; }

        #endregion

        #region Command Properties

        /// <summary>
        /// The Cancel command.
        /// </summary>
        public ICommand Cancel { get; set; }

        #endregion

        #region Data Properties

        /// <summary>
        /// The progress of an image processing job.
        /// </summary>
        /// <remarks>
        /// The setter for this property also sets the ProgressMessage property.
        /// </remarks>
        public int Progress
        {
            get => _pProgress;

            set
            {
                RaisePropertyChangingEvent("Progress");
                _pProgress = value;
                RaisePropertyChangedEvent("Progress");
            }
        }

        /// <summary>
        /// The maximum progress value.
        /// </summary>
        /// <remarks>
        /// The 
        /// </remarks>
        public int ProgressMax
        {
            get => _pProgressMax;

            set
            {
                RaisePropertyChangingEvent("ProgressMax");
                _pProgressMax = value;
                RaisePropertyChangedEvent("ProgressMax");
            }
        }

        /// <summary>
        /// The status message to be displayed in the View.
        /// </summary>
        public string ProgressMessage
        {
            get => _pProgressMessage;

            set
            {
                RaisePropertyChangingEvent("ProgressMessage");
                _pProgressMessage = value;
                RaisePropertyChangedEvent("ProgressMessage");
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Clears the view model.
        /// </summary>
        internal void ClearViewModel()
        {
            _pProgress = 0;
            _pProgressMax = 0;
            _pProgressMessage = "Preparing to perform simulated work.";
            this.IsCancelled = false;
        }

        /// <summary>
        /// Advances the progress counter for the Progress dialog.
        /// </summary>
        /// <param name="incrementClicks">The number of 'clicks' to advance the counter.</param>
        internal void IncrementProgressCounter(int incrementClicks)
        {
            if (Progress + incrementClicks > _pProgressMax)
                return;

            // Increment counter
            this.Progress += incrementClicks;

            // Update progress message
            var progress = Convert.ToSingle(_pProgress);
            var progressMax = Convert.ToSingle(_pProgressMax);
            var f = (progress / progressMax) * 100;
            var percentComplete = Single.IsNaN(f) ? 0 : Convert.ToInt32(f);
            ProgressMessage = string.Format(_mProgressMessageTemplate, percentComplete);
        }

        internal void AssignProgressCounter(int value)
        {
            _mProgressMessageTemplate = "processing Ifc-Import {0}% complete";
            Progress = value;
            ProgressMessage = string.Format(_mProgressMessageTemplate, value);

        }

        internal void AssignMessage(string message)
        {
            ProgressMessage = message;
        }

        /// <summary>
        /// Sets the progress message to show that processing was cancelled.
        /// </summary>
        internal void ShowCancellationMessage()
        {
            ProgressMessage = _mCancellationMessage;
        }

        #endregion

        #region Private Methods


        #endregion

        #region Public Methods

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the changed property.</param>
        public virtual void RaisePropertyChangedEvent(string propertyName)
        {
            // Exit if changes ignored
            if (IgnorePropertyChangeEvents) return;

            // Exit if no subscribers
            if (PropertyChanged == null) return;

            // Raise event
            var e = new PropertyChangedEventArgs(propertyName);
            PropertyChanged(this, e);
        }

        /// <summary>
        /// Raises the PropertyChanging event.
        /// </summary>
        /// <param name="propertyName">The name of the changing property.</param>
        public virtual void RaisePropertyChangingEvent(string propertyName)
        {
            // Exit if changes ignored
            if (IgnorePropertyChangeEvents) return;

            // Exit if no subscribers
            if (PropertyChanging == null) return;

            // Raise event
            var e = new PropertyChangingEventArgs(propertyName);
            PropertyChanging(this, e);
        }

        /// <summary>
        /// Raises the WorkStarting event.
        /// </summary>
        internal void RaiseWorkStartedEvent()
        {
            // Raise event
            WorkStarted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the WorkEnding event.
        /// </summary>
        internal void RaiseWorkEndedEvent()
        {
            // Raise event
            WorkEnded?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        public event PropertyChangingEventHandler PropertyChanging;
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler WorkStarted;
        public event EventHandler WorkEnded;

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            Cancel.Execute(null);
        }
    }
}
