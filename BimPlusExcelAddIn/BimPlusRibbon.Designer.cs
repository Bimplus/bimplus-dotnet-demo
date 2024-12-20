namespace BimPlusExcelAddIn
{
    partial class BimPlusRibbon : Microsoft.Office.Tools.Ribbon.RibbonBase
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        public BimPlusRibbon()
            : base(Globals.Factory.GetRibbonFactory())
        {
            InitializeComponent();
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.bimPlus = this.Factory.CreateRibbonTab();
            this.LoginGroup = this.Factory.CreateRibbonGroup();
            this.LoginButton = this.Factory.CreateRibbonButton();
            this.LogoutButton = this.Factory.CreateRibbonButton();
            this.ShowProjectSelection = this.Factory.CreateRibbonButton();
            this.ShowViewerButton = this.Factory.CreateRibbonToggleButton();
            this.Username = this.Factory.CreateRibbonLabel();
            this.Projekt = this.Factory.CreateRibbonLabel();
            this.Elements = this.Factory.CreateRibbonGroup();
            this.gallery1 = this.Factory.CreateRibbonGallery();
            this.bimPlus.SuspendLayout();
            this.LoginGroup.SuspendLayout();
            this.Elements.SuspendLayout();
            this.SuspendLayout();
            // 
            // bimPlus
            // 
            this.bimPlus.ControlId.ControlIdType = Microsoft.Office.Tools.Ribbon.RibbonControlIdType.Office;
            this.bimPlus.Groups.Add(this.LoginGroup);
            this.bimPlus.Groups.Add(this.Elements);
            this.bimPlus.Label = "bimPlus";
            this.bimPlus.Name = "bimPlus";
            // 
            // LoginGroup
            // 
            this.LoginGroup.Items.Add(this.LoginButton);
            this.LoginGroup.Items.Add(this.LogoutButton);
            this.LoginGroup.Items.Add(this.ShowProjectSelection);
            this.LoginGroup.Items.Add(this.ShowViewerButton);
            this.LoginGroup.Items.Add(this.Username);
            this.LoginGroup.Items.Add(this.Projekt);
            this.LoginGroup.Label = "Login/logout";
            this.LoginGroup.Name = "LoginGroup";
            // 
            // LoginButton
            // 
            this.LoginButton.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.LoginButton.Label = "Login";
            this.LoginButton.Name = "LoginButton";
            this.LoginButton.OfficeImageId = "FileDocumentEncrypt";
            this.LoginButton.ScreenTip = "Login";
            this.LoginButton.ShowImage = true;
            this.LoginButton.SuperTip = "Log in to Allplan Bimplus.";
            this.LoginButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.LoginButton_Click);
            // 
            // LogoutButton
            // 
            this.LogoutButton.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.LogoutButton.Label = "Logout";
            this.LogoutButton.Name = "LogoutButton";
            this.LogoutButton.OfficeImageId = "FileDocumentEncrypt";
            this.LogoutButton.ScreenTip = "Logout";
            this.LogoutButton.ShowImage = true;
            this.LogoutButton.SuperTip = "Log out from Allplan Bimplus.";
            this.LogoutButton.Visible = false;
            this.LogoutButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.LogoutButton_Click);
            // 
            // ShowProjectSelection
            // 
            this.ShowProjectSelection.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.ShowProjectSelection.Enabled = false;
            this.ShowProjectSelection.Label = "Project selection";
            this.ShowProjectSelection.Name = "ShowProjectSelection";
            this.ShowProjectSelection.OfficeImageId = "FileOpenDatabase";
            this.ShowProjectSelection.ScreenTip = "Project selection";
            this.ShowProjectSelection.ShowImage = true;
            this.ShowProjectSelection.SuperTip = "Show the project selection.";
            this.ShowProjectSelection.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.ShowProjectSelection_Click);
            // 
            // ShowViewerButton
            // 
            this.ShowViewerButton.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.ShowViewerButton.Enabled = false;
            this.ShowViewerButton.Label = "BIM Explorer";
            this.ShowViewerButton.Name = "ShowViewerButton";
            this.ShowViewerButton.OfficeImageId = "AutoSigInsertPictureFromFile";
            this.ShowViewerButton.ScreenTip = "BIM Explorer";
            this.ShowViewerButton.ShowImage = true;
            this.ShowViewerButton.SuperTip = "Show the BIM Explorer.";
            this.ShowViewerButton.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.showViewer_Click);
            // 
            // Username
            // 
            this.Username.Label = "User:";
            this.Username.Name = "Username";
            // 
            // Projekt
            // 
            this.Projekt.Label = "Project:";
            this.Projekt.Name = "Projekt";
            // 
            // Elements
            // 
            this.Elements.Items.Add(this.gallery1);
            this.Elements.Label = "Fill ObjectProperties";
            this.Elements.Name = "Elements";
            // 
            // gallery1
            // 
            this.gallery1.Enabled = false;
            this.gallery1.Label = "ObjectList";
            this.gallery1.Name = "gallery1";
            this.gallery1.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.gallery1_Click);
            // 
            // BimPlusRibbon
            // 
            this.Name = "BimPlusRibbon";
            this.RibbonType = "Microsoft.Excel.Workbook";
            this.Tabs.Add(this.bimPlus);
            this.Load += new Microsoft.Office.Tools.Ribbon.RibbonUIEventHandler(this.BimPlusRibbon_Load);
            this.bimPlus.ResumeLayout(false);
            this.bimPlus.PerformLayout();
            this.LoginGroup.ResumeLayout(false);
            this.LoginGroup.PerformLayout();
            this.Elements.ResumeLayout(false);
            this.Elements.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        internal Microsoft.Office.Tools.Ribbon.RibbonTab bimPlus;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup LoginGroup;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton LoginButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton LogoutButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonLabel Username;
        internal Microsoft.Office.Tools.Ribbon.RibbonLabel Projekt;
        internal Microsoft.Office.Tools.Ribbon.RibbonToggleButton ShowViewerButton;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton ShowProjectSelection;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup Elements;
        internal Microsoft.Office.Tools.Ribbon.RibbonGallery gallery1;
    }

    partial class ThisRibbonCollection
    {
        internal BimPlusRibbon BimPlusRibbon
        {
            get { return this.GetRibbon<BimPlusRibbon>(); }
        }
    }
}
