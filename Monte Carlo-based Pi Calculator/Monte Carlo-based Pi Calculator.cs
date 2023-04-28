namespace Monte_Carlo_based_Pi_Calculator
{
    public class Monte_Carlo_based_Pi_Calculator
    {
        private static double tabelarnoPI = 3.14159265358979323846;
        public static double getTPI() { return tabelarnoPI; }

        public static (double, int) EstimatePiFromDecimalPlaces(int decimalPlaces)
        {
            double d = (double)Math.Pow(0.1, decimalPlaces + 1);
            Random rand = new Random();
            double x, y, pi = 0;
            int brojac = 0, iteracije = 0;

            while (Math.Abs(1 - (tabelarnoPI / pi)) > d)
            {
                x = rand.NextDouble();
                y = rand.NextDouble();
                if (x * x + y * y <= 1)
                    brojac++;
                iteracije++;
                pi = (double)4 * brojac / iteracije;
            }
            return (pi, iteracije);
        }
        public static double EstimatePiFromNumValues(double numValues)
        {
            Random rand = new Random();
            double count = 0;
            for (int i = 0; i < numValues; i++)
            {
                double x = rand.NextDouble();
                double y = rand.NextDouble();
                if (x * x + y * y <= 1)
                    count++;
            }
            double pi = 4.0 * count / numValues;
            return pi;
        }


    }
}
