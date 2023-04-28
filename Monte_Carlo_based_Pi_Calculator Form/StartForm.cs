namespace Monte_Carlo_based_Pi_Calculator_Form
{
    public partial class StartForm : Form
    {
        public StartForm()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            UnosiDecimale unosiDecimale = new UnosiDecimale();
            this.Hide();
            unosiDecimale.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SlucajneVrijednosti slucajneVrijednosti = new SlucajneVrijednosti();
            this.Hide();
            slucajneVrijednosti.Show();
        }
    }
}
