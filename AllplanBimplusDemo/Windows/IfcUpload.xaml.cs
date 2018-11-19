using AllplanBimplusDemo.Classes;
using BimPlus.Client.Integration;
using BimPlus.Sdk.Data.TenantDto;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AllplanBimplusDemo.Windows
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class IfcUpload : Window
    {
        public ObservableCollection<DtoDivision> ViewDivs { get; set; }
        public DtoDivision SelectedDivision { get; set; }
        private IntegrationBase IntBase { get; set; }

        public static bool HasIfcFileName { get; private set; }

        public IfcUpload()
        {
            HasIfcFileName = false;

            ContextMenu menu = new ContextMenu();
            menu.Opened += Menu_Opened;

            ViewDivs = new ObservableCollection<DtoDivision>();
            DataContext = this;
            InitializeComponent();
            ModelView.ItemsSource = ViewDivs;
        }

        private void Menu_Opened(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = sender as ContextMenu;
            if (menu != null)
            {
                foreach (MenuItem item in menu.Items)
                {
                    item.IsEnabled = false;

                    switch (item.Name)
                    {
                        case "Upload":
                            if (SelectedDivision != null)
                                item.IsEnabled = SelectedDivision.Id == Guid.Empty && IfcUpload.HasIfcFileName == true;
                            break;
                        case "UploadAndRevision":
                            if (SelectedDivision != null)
                                item.IsEnabled = SelectedDivision.Id != Guid.Empty && IfcUpload.HasIfcFileName == true;
                            break;
                        case "DownLoad":
                            if (SelectedDivision != null)
                                item.IsEnabled = SelectedDivision.Id != Guid.Empty;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public bool LoadContent(IntegrationBase intBase)
        {
            IntBase = intBase;
            if (intBase.CurrentProject == null || intBase.CurrentProject.Id == Guid.Empty)
                return false;
            List<DtoDivision> divisions = intBase.ApiCore.Divisions.GetProjectDivisions(intBase.CurrentProject.Id);

            // Test
            //List<DtoDivision> toDelete = divisions.Where(d => d.Name.Contains("create new Division")).ToList();

            //for (int i = 0; i < toDelete.Count; i++)
            //{
            //    bool deleted = intBase.ApiCore.Divisions.DeleteDtoDivision(toDelete[i].Id);
            //}

            foreach (DtoDivision division in divisions)
            {
                ViewDivs.Add(division);
            }

            DtoDivision newDivision = new DtoDivision { Name = "<create new Division>" };
            ViewDivs.Add(newDivision);

            ModelView.SelectedItem = newDivision;

            return true;
        }

        private void StartImport_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedDivision == null)
            {
                MessageBoxHelper.ShowInformation("No valid model selected.");
                return;
            }

            string ifcFile = IfcFile.Content.ToString();
            if (String.IsNullOrEmpty(ifcFile))
            {
                MessageBoxHelper.ShowInformation("No valid Ifc-File selected.");
                return;
            }

            // Create model if it doesn't exist.
            if (SelectedDivision.Id == Guid.Empty)
            {
                SelectedDivision.Id = Guid.NewGuid();
                IntBase.ApiCore.Divisions.UploadProjectDivision(IntBase.CurrentProject.Id, SelectedDivision);
            }
            // Create new Revision
            else
            {
                IntBase.ApiCore.Divisions.CreateRevision(SelectedDivision.Id, "new Revision", "");
            }

            // Upload
            IntBase.ApiCore.Divisions.UploadIfcModel(IntBase.CurrentProject.Id, SelectedDivision.Id, ifcFile, ifcFile);

            MessageBoxHelper.ShowInformation("The model was exported to Allplan Bimplus.");
            IfcFile.Content = "";
            _adorner.InvalidateVisual();
            HasIfcFileName = false;

            ViewDivs.Clear();
            LoadContent(IntBase);
        }

        private void IfcFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                DefaultExt = ".ifc",
                Filter = "Ifc files (.ifc)|*.ifc"
            };
            if (dlg.ShowDialog() == true)
            {
                IfcFile.Content = dlg.FileName;
                if (!string.IsNullOrEmpty(IfcFile.Content as string))
                {
                    HasIfcFileName = true;
                }

                _adorner.InvalidateVisual();
            }
        }

        private void DownLoad_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedDivision == null)
            {
                MessageBoxHelper.ShowInformation("No valid model selected.");
                return;
            }
            byte[] result = IntBase.ApiCore.Divisions.DownloadIfcModel(SelectedDivision.Id);
            MessageBoxHelper.ShowInformation(result == null
                ? "No valid IfcFile available."
                : Encoding.UTF8.GetString(result, 0, 100));
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Thickness margin = ModelView.Margin;

            Grid grid = ModelView.Parent as Grid;

            if (grid != null)
            {
                RowDefinitionCollection defs = grid.RowDefinitions;
                RowDefinition def = defs[0];
                ModelView.Height = def.ActualHeight - margin.Top - margin.Bottom;

                ModelView.Width = grid.ActualWidth - margin.Left - margin.Right;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IfcFile.Focus();
        }

        #region Adorner

        private UploadButtonAdorner _adorner;

        private void IfcFile_Loaded(object sender, RoutedEventArgs e)
        {
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(sender as Button);
            if (layer != null)
            {
                Adorner[] adorner = layer.GetAdorners(this);
                if (adorner == null || adorner.FirstOrDefault(a => a is UploadButtonAdorner) == null)
                {
                    _adorner = new UploadButtonAdorner(sender as Button);
                    layer.Add(_adorner);
                }
            }
        }

        #endregion Adorner
    }

    #region Adorner

    public class UploadButtonAdorner : Adorner
    {
        public UploadButtonAdorner(Button adornedElement)
            : base(adornedElement)
        {
            IsHitTestVisible = false;
            SnapsToDevicePixels = true;

            _button = adornedElement;
        }

        private Button _button;

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            string content = _button.Content as string;

            if (string.IsNullOrEmpty(content))
            {
                Size elementSize = AdornedElement.RenderSize;
                Rect rect = new Rect(0, 0, elementSize.Width, elementSize.Height);

                Typeface typeFace = _button.FontFamily.GetTypefaces().FirstOrDefault();
                if (typeFace != null)
                {
#pragma warning disable 0618
                    // DpiScale dpiScale = VisualTreeHelper.GetDpi(this);
                    // Use it with framework 4.71!
                    FormattedText formattedText = new FormattedText("Select ifc file...", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        typeFace, _button.FontSize, Brushes.Red/*, dpiScale.PixelsPerDip*/);
#pragma warning restore 0618

                    double margin = 5.0;

                    formattedText.MaxTextWidth = elementSize.Width - 2 * margin;
                    formattedText.Trimming = TextTrimming.WordEllipsis;
                    formattedText.TextAlignment = TextAlignment.Center;
                    formattedText.SetFontWeight(FontWeight.FromOpenTypeWeight(400));

                    drawingContext.DrawText(formattedText, new Point(margin, (elementSize.Height - formattedText.Height) / 2));
                }
            }
        }
    }

    #endregion Adorner
}
