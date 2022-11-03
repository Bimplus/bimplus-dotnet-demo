using System;
using System.Windows;
using BimPlus.Client.Integration;

namespace BimPlusDemo.UserControls
{
    /// <summary>
    /// Interaction logic for AttributeView.xaml
    /// </summary>
    public partial class AttributeView
    {
        private readonly GetAttributeValues _propertyViewer;
        private readonly UpdateAttributeValues _propertyEditor;

        public Guid SelectedObject => _propertyViewer.SelectedObject?.Id ?? Guid.Empty;

        public AttributeView(IntegrationBase integrationBase)
        {
            InitializeComponent();

            _propertyEditor = new UpdateAttributeValues(integrationBase);
            EditView.Content = _propertyEditor;

            _propertyViewer = new GetAttributeValues(integrationBase);
            PropertyView.Content = _propertyViewer;
            
        }

        /// <summary>
        /// assign selected object.
        /// </summary>
        /// <param name="id"></param>
        public void AssignObject(Guid id)
        {
            if (PropertyView.Visibility == Visibility.Visible)
                _propertyViewer.UpdateSelectedObject(id);
        }

        /// <summary>
        /// switch to edit sheet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditProperties_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedObject == Guid.Empty)
                return;
            TaskLabel.Content = "Attribute properties";
            PropertyView.Visibility = Visibility.Hidden;
            EditButton.Visibility = Visibility.Hidden;
            BackButton.Visibility = Visibility.Visible;
            SaveButton.Visibility = Visibility.Visible;
            EditView.Visibility = Visibility.Visible;
            _propertyEditor.InitTreeView(SelectedObject);
        }

        /// <summary>
        /// save properties
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedObject != Guid.Empty)
            {
                _propertyEditor.SaveProperties(sender, e);
                _propertyViewer.UpdateSelectedObject(SelectedObject);
            }
            BackToList_OnClick(sender, e);
        }

        /// <summary>
        /// switch to viewer sheet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackToList_OnClick(object sender, RoutedEventArgs e)
        {
            TaskLabel.Content = "Properties";
            PropertyView.Visibility = Visibility.Visible;
            EditButton.Visibility = Visibility.Visible;
            BackButton.Visibility = Visibility.Hidden;
            SaveButton.Visibility = Visibility.Hidden;
            EditView.Visibility = Visibility.Hidden;
        }

    }
}
