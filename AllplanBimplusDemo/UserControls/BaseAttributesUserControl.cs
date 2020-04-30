using AllplanBimplusDemo.Classes;
using System.Windows;

namespace AllplanBimplusDemo.UserControls
{
    /// <summary>
    /// Class BaseAttributesUserControl.
    /// </summary>
    public class BaseAttributesUserControl : NotifyPropertyChangedUserControl
    {
        #region properties

        private bool _buttonsEnabled = false;

        /// <summary>
        /// Property ButtonsEnabled.
        /// </summary>
        public bool ButtonsEnabled
        {
            get { return _buttonsEnabled; }
            set { _buttonsEnabled = value; NotifyPropertyChanged(); }
        }

        /// <summary>
        /// Property HasObjects.
        /// </summary>
        public bool HasObjects
        {
            get { return _hasObjects; }

            set { _hasObjects = value; NotifyPropertyChanged(); }
        }

        private bool _hasObjects = false;

        /// <summary>
        /// Property InternalValuesCheckBoxIsChecked.
        /// </summary>
        public bool InternalValuesCheckBoxIsChecked
        {
            get { return _internalValuesCheckBoxIsChecked; }

            set { _internalValuesCheckBoxIsChecked = value; NotifyPropertyChanged(); }
        }

        private bool _internalValuesCheckBoxIsChecked = false;

        /// <summary>
        /// Property AttributeDefinitionIsChecked.
        /// </summary>
        public bool AttributeDefinitionIsChecked
        {
            get { return _attributeDefinitionIsChecked; }

            set { _attributeDefinitionIsChecked = value; NotifyPropertyChanged(); }
        }

        private bool _attributeDefinitionIsChecked = true;

        /// <summary>
        ///  Property PsetCheckBoxIsChecked.
        /// </summary>
        public bool PsetCheckBoxIsChecked
        {
            get { return _psetCheckBoxIsChecked; }

            set { _psetCheckBoxIsChecked = value; NotifyPropertyChanged(); }
        }

        private bool _psetCheckBoxIsChecked = true;

        #endregion properties

        #region protected methods

        /// <summary>
        /// Function HasAllowedFlags.
        /// </summary>
        /// <returns></returns>
        protected bool HasAllowedFlags()
        {
            if (InternalValuesCheckBoxIsChecked && AttributeDefinitionIsChecked)
            {
                string message = "The combination of these flags is not supported.";
                MessageBoxHelper.ShowInformation(message);
                return false;
            }
            else
                return true;
        }

        #endregion protected methods

        private void BaseAttributesUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ButtonsEnabled = true;
        }
    }
}
