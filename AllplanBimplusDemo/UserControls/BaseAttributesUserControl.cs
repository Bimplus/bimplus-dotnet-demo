using AllplanBimplusDemo.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllplanBimplusDemo.UserControls
{
    public class BaseAttributesUserControl : NotifyPropertyChangedUserControl
    {
        #region properties

        public bool HasObjects
        {
            get { return _hasObjects; }

            set { _hasObjects = value; NotifyPropertyChanged(); }
        }

        private bool _hasObjects = false;

        public bool InternalValuesCheckBoxIsChecked
        {
            get { return _internalValuesCheckBoxIsChecked; }

            set { _internalValuesCheckBoxIsChecked = value; NotifyPropertyChanged(); }
        }

        private bool _internalValuesCheckBoxIsChecked = false;

        public bool AttributeDefinitionIsChecked
        {
            get { return _attributeDefinitionIsChecked; }

            set { _attributeDefinitionIsChecked = value; NotifyPropertyChanged(); }
        }

        private bool _attributeDefinitionIsChecked = true;

        public bool PsetCheckBoxIsChecked
        {
            get { return _psetCheckBoxIsChecked; }

            set { _psetCheckBoxIsChecked = value; NotifyPropertyChanged(); }
        }

        private bool _psetCheckBoxIsChecked = true;

        #endregion properties

        #region protected methods

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
    }
}
