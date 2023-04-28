namespace Monte_Carlo_based_Pi_Calculator_Form
{
    public partial class SlucajneVrijednosti : Form
    {
        public SlucajneVrijednosti()
        {
            InitializeComponent();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !string.IsNullOrEmpty(textBox1.Text))
            {
                int numVals = (int)int.Parse(textBox1.Text);
                double pi = Monte_Carlo_based_Pi_Calculator.Monte_Carlo_based_Pi_Calculator.EstimatePiFromNumValues(numVals);
                label2.Text = "PI: " + pi.ToString();
                label3.Text = "PI(tabelarno): " + Monte_Carlo_based_Pi_Calculator.Monte_Carlo_based_Pi_Calculator.getTPI().ToString("F15");

            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            UnosiDecimale unosiDecimale = new UnosiDecimale();
            this.Hide();
            unosiDecimale.Show();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
