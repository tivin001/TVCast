namespace TVCast
{
    public partial class frmCloseDialog :AntdUI.Window
    {
        public frmCloseDialog()
        {
            InitializeComponent();
        }

        private void btnExitApp_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Yes;
        }

        private void btnMinimize_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.No;
        }
    }
}
