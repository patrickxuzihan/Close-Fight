using CloseFight.Core.Sim;

namespace CloseFight.Core.Units
{
    /// <summary>
    /// Generates soldier names from common names of the period (D46), using the
    /// battle's seeded random so a scenario always produces the same soldiers.
    /// Soviet lists include Ukrainian, Kazakh, Georgian and other names: the Red
    /// Army at Stalingrad was multinational.
    /// </summary>
    public static class NameGenerator
    {
        static readonly string[] GermanFirst =
        {
            "Hans", "Karl", "Heinrich", "Wilhelm", "Friedrich", "Otto", "Walter", "Paul", "Ernst", "Hermann",
            "Kurt", "Fritz", "Willi", "Josef", "Georg", "Erich", "Werner", "Herbert", "Rudolf", "Gerhard",
            "Günther", "Helmut", "Alfred", "Max", "Johann", "Heinz", "Franz", "August", "Albert", "Emil",
        };

        static readonly string[] GermanLast =
        {
            "Müller", "Schmidt", "Schneider", "Fischer", "Weber", "Meyer", "Wagner", "Becker", "Schulz", "Hoffmann",
            "Koch", "Bauer", "Richter", "Klein", "Wolf", "Schröder", "Neumann", "Schwarz", "Zimmermann", "Braun",
            "Krüger", "Hartmann", "Lange", "Schmitt", "Werner", "Krause", "Lehmann", "Köhler", "Maier", "Huber",
            "Kaiser", "Fuchs", "Peters", "Lang", "Scholz", "Möller", "Weiß", "Jung", "Hahn", "Vogel",
        };

        static readonly string[] SovietFirst =
        {
            "Ivan", "Pyotr", "Nikolai", "Aleksei", "Mikhail", "Sergei", "Vasily", "Dmitri", "Andrei", "Fyodor",
            "Grigory", "Pavel", "Yakov", "Semyon", "Konstantin", "Viktor", "Boris", "Leonid", "Stepan", "Timofei",
            "Ilya", "Vladimir", "Yegor", "Matvei", "Taras", "Mykola", "Bakhyt", "Nurlan", "Giorgi", "Rustam",
        };

        static readonly string[] SovietLast =
        {
            "Ivanov", "Smirnov", "Kuznetsov", "Popov", "Vasiliev", "Petrov", "Sokolov", "Mikhailov", "Novikov", "Fyodorov",
            "Morozov", "Volkov", "Alekseev", "Lebedev", "Semyonov", "Yegorov", "Pavlov", "Kozlov", "Stepanov", "Nikolaev",
            "Orlov", "Andreev", "Makarov", "Nikitin", "Zakharov", "Zaitsev", "Solovyov", "Borisov", "Yakovlev", "Grigoriev",
            "Shevchenko", "Bondarenko", "Kovalenko", "Tkachenko", "Nurpeisov", "Abdulov", "Beridze", "Gabunia", "Kasymov", "Aliyev",
        };

        public static (string first, string last) Next(Side side, SimRandom random)
        {
            if (side == Side.German)
            {
                return (GermanFirst[random.Range(0, GermanFirst.Length)], GermanLast[random.Range(0, GermanLast.Length)]);
            }

            return (SovietFirst[random.Range(0, SovietFirst.Length)], SovietLast[random.Range(0, SovietLast.Length)]);
        }
    }
}
