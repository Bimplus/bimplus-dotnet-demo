using System;
using System.Collections.Generic;
using System.Linq;

using System.IO;
using System.Net;
using System.Windows.Threading;

using IntegrationBase = BimPlus.Client.Integration.IntegrationBase;
using BimPlus.Client.WebControls.WPF;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Microsoft.Office.Interop.Excel;
using BimPlus.Client;
// ReSharper disable InconsistentNaming
// ReSharper disable RedundantDelegateCreation
// ReSharper disable RedundantArgumentDefaultValue

namespace BimPlusExcelAddIn
{
    public partial class ThisAddIn
    {
        #region private and internal member
        private IntegrationBase _integrationBase;
        private readonly StreamWriter _streamWriter = StreamWriter.Null;
        private SecurityProtocolType _originalSecurityProtocol;
        private WebViewer _webViewer;
        private Microsoft.Office.Tools.CustomTaskPane _taskPane;

        /// Application ID for the BimPlus API
        private static readonly Guid TestApplicationId = new Guid("7B8E7315-7F69-40DB-B340-03782B8BCB12");

        #endregion

        public IntegrationBase IntBase => _integrationBase;
        public IntPtr Handle => new IntPtr(Application.Hwnd);
        public BimPlusRibbon RibbonUI { get; set; } 


        private void ThisAddIn_Startup(object sender, EventArgs e)
        {
            _originalSecurityProtocol = ServicePointManager.SecurityProtocol;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            //_taskPane = null;
            _integrationBase =
                new IntegrationBase(TestApplicationId, _streamWriter, clientName: "BimPlusExcelAddon")
                {
                    OwnerWindowHandle = Handle,
                    UseSignalRCore = true,
                    SignalRAppCode = $"BimPlusClient-{TestApplicationId}.SignalRCore",
                    SignalRLogFileName = $"BimPlusExcelAddon-{TestApplicationId}",
                    SignalREnableUIMessages = true
                };

            Application.SheetSelectionChange += Application_SheetSelectionChange;
        }

        /// <summary>
        /// Highlight the selected objects from sheet in the WebViewer.
        /// </summary>
        /// <param name="sh"></param>
        /// <param name="target"></param>
        private void Application_SheetSelectionChange(object sh, Range target)
        {
            if (_integrationBase?.CurrentProject == null)
                return;
            if (_webViewer == null)
                return;

            var sheet = (Worksheet)sh;
            if (sheet == null) return;  

            var selectedIds = GetSelectedIds(target);
            if (selectedIds.Count == 0)
                return;

            _webViewer.HighlightObjectsByID(selectedIds, false, true, false);
        }

        internal List<Guid> GetSelectedIds(Range target, int column = 1)
        {
            List<Guid> objectIds = new List<Guid>(target.Rows.Count);
            foreach (var id in from Range cell in target.Cells
                     where cell.Column == column
                     where cell.Value2 != null
                     select cell.Value2.ToString())
            {
                if (Guid.TryParse(id, out Guid id2))
                {
                    objectIds.Add(id2);
                }
            }
            return objectIds;
        }

        private void ThisAddIn_Shutdown(object sender, EventArgs e)
        {
            ServicePointManager.SecurityProtocol = _originalSecurityProtocol;
            Application.SheetSelectionChange -= Application_SheetSelectionChange;
        }

        public void ShowBimPlusViewer(bool enable = true)
        {
            var taskPane = CustomTaskPanes.FirstOrDefault(x => x.Title == "BimExplorer");

            if (taskPane == null && _integrationBase.CurrentProject != null)
            {
                var uc = new UserControl();
                if (_webViewer == null)
                    _webViewer = new WebViewer(_integrationBase);
                _webViewer.NavigateToControl(_integrationBase.CurrentProject.Id);
                ElementHost host = new ElementHost { Child = _webViewer, Dock = DockStyle.Fill };
                uc.Controls.Add(host);
                _taskPane = Globals.ThisAddIn.CustomTaskPanes.Add(uc, "BimExplorer");

                uc.Disposed += (sender, args) =>
                {
                    if (_webViewer != null)
                    {
                        _webViewer.Dispose();
                        _webViewer = null;
                    }

                };
                _taskPane.VisibleChanged += (sender, args) =>
                {
                    RibbonUI.ShowViewerButton.Checked = _taskPane.Visible;
                };
            }

            _taskPane.Width = 600;
            _taskPane.Visible = enable;

            if (enable)
                IntBase.EventHandlerCore.ObjectSelected += EventHandlerCore_ObjectSelected;
            else
                IntBase.EventHandlerCore.ObjectSelected -= EventHandlerCore_ObjectSelected;
        }

        /// <summary>
        /// receive the selected object from the WebExplorer.
        /// activate the Excel sheet and select the row with the object.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventHandlerCore_ObjectSelected(object sender, BimPlusEventArgs e)
        {

            if (e == null || e.Id == Guid.Empty)
                return;

            var selectedObject = IntBase.ApiCore.DtObjects.GetObject(e.Id);
            var sheet = Application.ActiveWorkbook.Sheets.Cast<Worksheet>().FirstOrDefault(ws => ws.Name == selectedObject.Type);
            if (sheet == null)
                return;

            Globals.ThisAddIn.Application.SheetSelectionChange -= Application_SheetSelectionChange;
            sheet.Select();
            var range = sheet.Columns[1].Find(selectedObject.Id.ToString(), Type.Missing, Type.Missing, Type.Missing, Type.Missing, XlSearchDirection.xlNext, Type.Missing, Type.Missing, Type.Missing);
            if (range != null)
                range.EntireRow.Select(); // Select the entire row of the found range
            Globals.ThisAddIn.Application.SheetSelectionChange += Application_SheetSelectionChange;
        }

        /// <summary>
        /// Select BimPlus project by using WebView ProjectSelection.
        /// </summary>
        /// <returns></returns>
        public bool ShowProjectSelection()
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(new System.Action(() =>
            {
                var projectSelection = new ProjectSelection(IntBase, 800, 1200, "ProjectSelection", null);
                var id = projectSelection.SelectBimPlusProject(Guid.Empty);

                if (id != Guid.Empty)
                {
                    ShowBimPlusViewer();
                    RibbonUI.ShowViewerButton.Enabled = true;
                    RibbonUI.ShowViewerButton.Checked = true;

                }
            }));
            return IntBase.CurrentProject != null;
        }


        #region VSTO generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new EventHandler(ThisAddIn_Startup);
            this.Shutdown += new EventHandler(ThisAddIn_Shutdown);
        }
        
        #endregion
    }
}
