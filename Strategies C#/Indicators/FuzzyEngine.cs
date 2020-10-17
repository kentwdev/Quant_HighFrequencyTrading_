using Accord.Fuzzy;

namespace QuantConnect
{

    public class FuzzyEngine
    {
        private InferenceSystem IS;

        public FuzzyEngine()
        {
            // Linguistic labels (fuzzy sets) for Momentum
            FuzzySet momDown = new FuzzySet("Down", new TrapezoidalFunction(-20, 5, 5, 5));
            FuzzySet momNeutral = new FuzzySet("Neutral", new TrapezoidalFunction(-20, 0, 0, 20));
            FuzzySet momUp = new FuzzySet("Up", new TrapezoidalFunction(5, 20, 20, 20));


            // Linguistic labels (fuzzy sets) for RSI
            FuzzySet rsiLow = new FuzzySet("Low", new TrapezoidalFunction(0, 30, 30, 30));
            FuzzySet rsiMedium = new FuzzySet("Medium", new TrapezoidalFunction(0, 50, 50, 100));
            FuzzySet rsiHigh = new FuzzySet("High", new TrapezoidalFunction(70, 100, 100, 100));

            // MOM (Input)
            LinguisticVariable lvMom = new LinguisticVariable("MOM", -20, 20);
            lvMom.AddLabel(momDown);
            lvMom.AddLabel(momNeutral);
            lvMom.AddLabel(momUp);

            // RSI (Input)
            LinguisticVariable lvRsi = new LinguisticVariable("RSI", 0, 100);
            lvRsi.AddLabel(rsiLow);
            lvRsi.AddLabel(rsiMedium);
            lvRsi.AddLabel(rsiHigh);

            // Linguistic labels (fuzzy sets) that compose the Signal
            FuzzySet fsShort = new FuzzySet("Sell", new TrapezoidalFunction(-100, 0, 0, 00));
            FuzzySet fsHold = new FuzzySet("Hold", new TrapezoidalFunction(-50, 0, 0, 50));
            FuzzySet fsLong = new FuzzySet("Buy", new TrapezoidalFunction(0, 100, 100, 100));

            // Output
            LinguisticVariable lvSignal = new LinguisticVariable("Signal", -100, 100);
            lvSignal.AddLabel(fsShort);
            lvSignal.AddLabel(fsHold);
            lvSignal.AddLabel(fsLong);

            // The database
            Database fuzzyDB = new Database();
            fuzzyDB.AddVariable(lvMom);
            fuzzyDB.AddVariable(lvRsi);
            fuzzyDB.AddVariable(lvSignal);

            // Creating the inference system
            IS = new InferenceSystem(fuzzyDB, new CentroidDefuzzifier(1000));

            // Rules
            IS.NewRule("Rule 1", "IF RSI IS Low AND MOM IS Down THEN Signal IS Buy");
            IS.NewRule("Rule 2", "IF RSI IS Medium AND MOM IS Down THEN Signal IS Buy");
            IS.NewRule("Rule 3", "IF RSI IS High AND MOM IS Down THEN Signal IS Hold");

            IS.NewRule("Rule 4", "IF RSI IS Low AND MOM IS Neutral THEN Signal IS Buy");
            IS.NewRule("Rule 5", "IF RSI IS Medium AND MOM IS Neutral THEN Signal IS Hold");
            IS.NewRule("Rule 6", "IF RSI IS High AND MOM IS Neutral THEN Signal IS Sell");

            IS.NewRule("Rule 7", "IF RSI IS Low AND MOM IS Up THEN Signal IS Hold");
            IS.NewRule("Rule 8", "IF RSI IS Medium AND MOM IS Up THEN Signal IS Sell");
            IS.NewRule("Rule 9", "IF RSI IS High AND MOM IS Up THEN Signal IS Sell");



        }

        public double DoInference(float mom, float rsi)
        {
            // Setting inputs
            IS.SetInput("MOM", mom);
            IS.SetInput("RSI", rsi);

            // Setting outputs
            double signal = IS.Evaluate("Signal");

            return signal;
        }
    }
}