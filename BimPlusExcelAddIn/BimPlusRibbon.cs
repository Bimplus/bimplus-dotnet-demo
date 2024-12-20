
using Microsoft.Office.Tools.Ribbon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using BimPlus.Client;
using BimPlus.Client.Integration;
using BimPlus.Sdk.Data.DbCore;
using Microsoft.Office.Interop.Excel;
// ReSharper disable LocalizableElement

namespace BimPlusExcelAddIn
{
    public partial class BimPlusRibbon
    {
        private IntegrationBase IntBase => Globals.ThisAddIn.IntBase;
        private static Workbook Workbook => Globals.ThisAddIn.Application.ActiveWorkbook;


        private bool IsLoggedIn =>
            !string.IsNullOrEmpty(Globals.ThisAddIn.IntBase?.ClientConnection.AuthorizationAccessToken);
        private void BimPlusRibbon_Load(object sender, RibbonUIEventArgs e)
        {
        }

        private async Task Login(object sender, RibbonControlEventArgs e)
        {
            if (await Globals.ThisAddIn.IntBase.ConnectAsync() == HttpStatusCode.OK)
            {
                Username.Label = $"User: {IntBase.UserName}";
                Username.Visible = true;
                ShowProjectSelection.Enabled = true;

                LoginButton.Visible = false;
                LogoutButton.Visible = true;

                IntBase.EventHandlerCore.ProjectChanged += EventHandlerCoreOnProjectChanged;
            }
            else
            {
                if (IntBase.ExceptionList.Count > 0)
                    MessageBox.Show(IntBase.ExceptionList[0].Exception.Message, "LoginError");
                else
                    MessageBox.Show("login failed!", "BimPlus Login");
            }

            Globals.ThisAddIn.RibbonUI = this;
        }


        /// <summary>
        /// Logout from BimPlus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogoutButton_Click(object sender, RibbonControlEventArgs e)
        {
            if (IntBase == null || IsLoggedIn == false)
                return;

            if (IntBase.Disconnect() != HttpStatusCode.OK) 
                return;
            Username.Visible = false;
            ShowProjectSelection.Enabled = false;
            ShowViewerButton.Enabled = false;
            Projekt.Visible = false;
            IntBase.EventHandlerCore.ProjectChanged -= EventHandlerCoreOnProjectChanged;

        }

        /// <summary>
        /// receive ProjectChanged event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventHandlerCoreOnProjectChanged(object sender, BimPlusEventArgs e)
        {
            if (IntBase.CurrentProject == null)
                return;

            Projekt.Label = $"Project: {IntBase.CurrentProject.Name}";

            var elements = IntBase.ApiCore.Projects.GetProjectElementTypes(IntBase.CurrentProject.Id);
            gallery1.Enabled = true;
            gallery1.Items.Clear(); // Clear existing items

            foreach (var element in elements)
            {
                var item = this.Factory.CreateRibbonDropDownItem();

                item.Label = element.Type;
                item.Tag = element.Id;
                gallery1.Items.Add(item);
            }
        }

        private void ShowProjectSelection_Click(object sender, RibbonControlEventArgs e)
        {
            Globals.ThisAddIn.ShowProjectSelection();
        }

        private void showViewer_Click(object sender, RibbonControlEventArgs e)
        {
            Globals.ThisAddIn.ShowBimPlusViewer(ShowViewerButton.Checked);
        }

        private async void LoginButton_Click(object sender, RibbonControlEventArgs e)
        {
            await Login(sender, e);
        }

        private void gallery1_Click(object sender, RibbonControlEventArgs e)
        {
            if (!(sender is RibbonGallery g)) return;
            if (g.SelectedItem.Tag is Guid elementType)
            {
                var elements =
                    IntBase.ApiCore.DtObjects.GetObjects(IntBase.CurrentProject.Id, DbObjectList.GetType(elementType));
                ExportElementsToSheet(g.SelectedItem.Label, elements);

            }
        }

        /// <summary>
        /// Export elements to Excel sheet.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="elements"></param>
        private void ExportElementsToSheet(string type, List<DtObject> elements)
        {
            if (elements == null || !elements.Any())
            {
                MessageBox.Show("No elements found to export.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            else if (elements.Count > 500)
            {
                MessageBox.Show("I reduce output to 500 objects", "Export comment", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                elements = elements.Take(500).ToList();
            }

            var s0 = Workbook.Sheets.Cast<Worksheet>().FirstOrDefault();
            var sheet = Workbook.Sheets.Cast<Worksheet>().FirstOrDefault(ws => ws.Name == type);
            if (sheet != null)
            {
                if (MessageBox.Show("Sheet with the same name already exists.\n Do you like to reload it?", "Reload", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly) == DialogResult.Yes)
                    sheet.Cells.Clear(); // Clear all cells in the sheet
                else
                    return;
            }
            else if (s0?.Name == "Tabelle1")
            {
                sheet = s0;
                sheet.Name = type;
            }
            else
            {
                sheet = Workbook.Sheets.Add();
                sheet.Name = type;
            }
            var attributes = elements.First().AttributeGroups;
            var ranges = new Dictionary<string, List<string>>(attributes.Count);
            foreach (var a in elements.SelectMany(element => element.AttributeGroups))
            {
                if (ranges.TryGetValue(a.Key, out var pSet) == false)
                {
                    pSet = new List<string>();
                    ranges.Add(a.Key, pSet);
                }

                foreach (var property in a.Value.Where(property => !pSet.Contains(property.Key)))
                    pSet.Add(property.Key);
            }

            Dictionary<string, int> attributePositions = new Dictionary<string, int>();
            sheet.Cells[1, 1] = "BimPlusID";
            int idx = 2;
            foreach (var range in ranges)
            {
                foreach (var pSet in range.Value)
                {
                    attributePositions.Add($"{range.Key}.{pSet}", idx);
                    sheet.Cells[1, idx++] = pSet;
                }
            }

            int row = 2;
            foreach (var element in elements)
            {
                sheet.Cells[row, 1] = element.Id.ToString();
                foreach (var a in element.AttributeGroups)
                {
                    foreach (var group in a.Value)
                    {
                        sheet.Cells[row, attributePositions[$"{a.Key}.{group.Key}"]] = group.Value;
                    }
                }
                row++;
            }
        }
    }
}
