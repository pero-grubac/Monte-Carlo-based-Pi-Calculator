namespace Monte_Carlo_based_Pi_Calculator_Form
{
    public partial class UnosiDecimale : Form
    {
        public UnosiDecimale()
        {
            InitializeComponent();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !string.IsNullOrEmpty(textBox1.Text))
            {
                int decimalPlaces = int.Parse(textBox1.Text);
                (double, int) piTuple = Monte_Carlo_based_Pi_Calculator.Monte_Carlo_based_Pi_Calculator.EstimatePiFromDecimalPlaces(decimalPlaces);
                double pi = piTuple.Item1;
                int iteracije = piTuple.Item2;
                label2.Text = "PI: " + pi.ToString("F15");
                label3.Text = "PI(tabelarno): " + Monte_Carlo_based_Pi_Calculator.Monte_Carlo_based_Pi_Calculator.getTPI().ToString("F15");
                label4.Text = "Broj iteracija: " + iteracije.ToString();

            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SlucajneVrijednosti slucajneVrijednosti = new SlucajneVrijednosti();
            this.Hide();
            slucajneVrijednosti.Show();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
