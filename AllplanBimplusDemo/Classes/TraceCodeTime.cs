using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllplanBimplusDemo.Classes
{
    /// <summary>
    /// <summary>
    /// Class for simple runtime measurements on the code.
    /// </summary>
    public class TraceCodeTime : IDisposable
    {
        #region private fields

        private string _message;

        private string _category;

        private TracingLevel _tracingLevel;

        private Stopwatch _stopwatch = new Stopwatch();

        #endregion private fields

        #region constructor

        /// <summary>
        /// The constructor starts the measurement. This is to be used in a using block.
        /// </summary>
        /// <param name="message">Tracing Message.</param>
        /// <param name="category">Tracing Category.</param>
        /// <param name="tracingLevel">Der Level der Nachricht.</param>
        public TraceCodeTime(string message, string category, TracingLevel tracingLevel = TracingLevel.Info)
        {
            _message = message;
            _category = category;
            _tracingLevel = tracingLevel;

            string msg = string.Format("{0} - {1} - {2}", message, category, tracingLevel.ToString());

#if DEBUG
            Trace.TraceInformation(msg);
#endif

            _stopwatch.Start();
        }

        #endregion constructor

        #region IDisposable
        // Dispose triggers the end of the measurement.
        public void Dispose()
        {
            _stopwatch.Stop();

            TimeSpan ts = _stopwatch.Elapsed;

            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);

            string infoMsg = string.Format("{0} - {1} - {2}", _message, _category, _tracingLevel.ToString());
            string msg = string.Format("~{0} Elapsed Time: {1} [hh:mm:ss.ffff] {2} - {3}", _message, elapsedTime, _category, _tracingLevel);

#if DEBUG
            Trace.TraceInformation(msg);
#endif
        }
        #endregion
    }

    public enum TracingLevel
    {
        Off,
        Error,
        Warning,
        Info,
        Methods
    }
}
