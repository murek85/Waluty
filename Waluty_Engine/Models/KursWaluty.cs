namespace Waluty.Engine.Models
{
    /// <summary>
    /// Dane podstawowe odnośnie kursu waluty.
    /// </summary>
    public class KursWaluty
    {
        /// <summary>
        /// Nazwa.
        /// </summary>
        public string Nazwa
        {
            get; set;
        }

        /// <summary>
        /// Przelicznik.
        /// </summary>
        public string Przelicznik
        {
            get; set;
        }

        /// <summary>
        /// Kod.
        /// </summary>
        public string Kod
        {
            get; set;
        }

        /// <summary>
        /// Średni kurs.
        /// </summary>
        public string KursSredni
        {
            get; set;
        }
    }
}
