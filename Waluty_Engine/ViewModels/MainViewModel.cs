using Caliburn.Micro;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Waluty.Engine.Interfaces;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using GalaSoft.MvvmLight.Command;
using Waluty.Engine.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Waluty.Engine.ViewModels
{
    [Export(typeof(IMain))]
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive, IMain, IDataErrorInfo
    {
        protected bool _canOperOblicz = true;

        #region Private Variables

        /// <summary>
        /// Menadżer okien biblioteki Mahapps.
        /// </summary>
        private readonly IWindowManager _windowManager;

        /// <summary>
        /// Kontener zdarzeń.
        /// </summary>
        private readonly IEventAggregator _eventAggregator;
        
        /// <summary>
        /// Informacja o wybranej walucie.
        /// </summary>
        private String _listaWalutRemember;

        /// <summary>
        /// Lista nazw plików z kursami walut z nbp.pl
        /// </summary>
        private IList<String> _dirs;

        private DateTime? _dataKursuPobrana;

        #endregion

        #region Constructor

        [ImportingConstructor]
        public MainViewModel(IWindowManager piWindowManager, IEventAggregator piEventAggregator)
        {
            _windowManager = piWindowManager;
            _eventAggregator = piEventAggregator;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Inicjalizacja okna MainView.
        /// </summary>
        protected override void OnInitialize()
        {
            try
            {
                base.OnInitialize();

                DisplayName = String.Format("{0} wersja {1}", "Waluty", Assembly.GetExecutingAssembly().GetName().Version);

                RegisterCommands();
            }
            catch (Exception)
            {
            
            }
        }

        /// <summary>
        /// Deaktywowanie okna MainView.
        /// </summary>
        /// <param name="close"></param>
        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Pobranie danych odnośnie kursu walut za dany dzień na podstawie danych z nbp.pl.
        /// </summary>
        /// <param name="piDir">Nazwa pliku, na podstawie której pobierane są kursy walutowe za danych dzień.</param>
        /// <returns>Zwraca dane w postaci xml'a.</returns>
        private String GetExchangeRate(String piDir)
        {
            String data = null;
            using (var client = new WebClient())
            {
                data = client.DownloadString(String.Format("http://www.nbp.pl/kursy/xml/{0}.xml", piDir));
            }

            return data;
        }

        /// <summary>
        /// Sprawdzenie czy podana data z kalendarza jest dniem z zakresu Pn, Wt, Śr, Czw, Pt.
        /// </summary>
        /// <param name="piDateTime">Data z kalendarze.</param>
        /// <returns>Zwraca odpowiednią datę.</returns>
        private DateTime GetDateNonDayOfWeek(DateTime piDateTime)
        {
            if (piDateTime.DayOfYear == 1)
            {
                piDateTime = piDateTime.AddDays(-1);
            }

            switch (piDateTime.DayOfWeek)
            {
                case DayOfWeek.Saturday:
                case DayOfWeek.Sunday:
                    {
                        return GetDateNonDayOfWeek(piDateTime.AddDays(-1));
                    }

                default:
                    return piDateTime;
            }
        }

        /// <summary>
        /// Sprawdzenie czy podana data zawiera się w katalogu z kursami walut z nbp.pl.
        /// </summary>
        /// <param name="piDateTime">Szukana data.</param>
        /// <param name="piDateFormat">Format daty.</param>
        /// <returns>Zwraca odpowiednią nazwę pliku dla wskazanej daty.</returns>
        private String GetDir(IList<string> piDirs, bool piDateEnd, DateTime piDateTime, String piDateFormat)
        {
            String date = piDateTime.Date.ToString(piDateFormat);
            String fd = piDirs.FirstOrDefault(t => t.Contains(date));

            if (!piDateEnd)
            {
                if (String.IsNullOrEmpty(fd))
                    return GetDir(piDirs, false, piDateTime.AddDays(-1), piDateFormat);
            }

            _dataKursuPobrana = piDateTime;

            return fd;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="piDateTime"></param>
        /// <returns></returns>
        private IList<string> GetDir(DateTime piDateTime)
        {
            IList<string> temp = null;
            using (var client = new WebClient())
            {
                String url = String.Format("http://www.nbp.pl/kursy/xml/dir{0}.txt",
                    piDateTime.Year != DateTime.Now.Year ? Convert.ToString(piDateTime.Year) : String.Empty);
                //String file = client.DownloadString(url);
                String file = file = client.DownloadString(url);

                String[] stringSeparators = new String[] { "\r\n" };
                String[] lines = file.Split(stringSeparators, StringSplitOptions.None);
                temp = lines.ToList().FindAll(x => x.Contains("a"));
            }

            return temp;
        }

        /// <summary>
        /// Rejestrowanie komend operacji wykonywanych w całej aplikacji.
        /// </summary>
        private void RegisterCommands()
        {
            //CmdOperOblicz = new RelayCommand(() => OperationOblicz(), () => _canOperOblicz);
            CmdOperOblicz = new RelayCommand(() => OperationOblicz());
        }

        /// <summary>
        /// Operacja przeliczania kursów.
        /// </summary>
        private void OperationOblicz()
        {
            try
            {
                if (!ValidateFiltersToAccept())
                {
                    return;
                }

                if (!String.IsNullOrEmpty(KursSredni) && !String.IsNullOrEmpty(Kwota))
                {
                    KwotaPrzelicz = Convert.ToString(Convert.ToDecimal(KursSredni) * Convert.ToDecimal(Kwota));
                    KwotaPrzeliczKoniec = Convert.ToString((Convert.ToDecimal(KursSredniKoniec) * Convert.ToDecimal(Kwota)));
                    KwotaRoznica = Convert.ToString(Convert.ToDecimal(KwotaPrzeliczKoniec) - Convert.ToDecimal(KwotaPrzelicz));
                }
            }
            catch (Exception)
            {
            
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="piDataKursu"></param>
        /// <returns></returns>
        private IList<KursWaluty> GetKursyWalut(DateTime piDataKursu)
        {
            IList<KursWaluty> temp = null;
            //await Task.Factory.StartNew(() =>
            //{
                _dirs = GetDir(piDataKursu);
                if (_dirs != null && _dirs.Count > 0)
                {
                    String fd = GetDir(_dirs, false, piDataKursu, "yyMMdd");
                    String data = GetExchangeRate(fd);

                    XmlSerializer x = new XmlSerializer(typeof(tabela_kursow));
                    tabela_kursow tabelaKursow = (tabela_kursow)x.Deserialize(new StringReader(data));

                    IList<KursWaluty> listaWalut = new List<KursWaluty>();
                    foreach (var item in tabelaKursow.pozycja)
                    {
                        KursWaluty waluta = new KursWaluty
                        {
                            Nazwa = String.Format("[{0}] {1}", item.kod_waluty, item.nazwa_waluty),
                            Kod = item.kod_waluty,
                            Przelicznik = item.przelicznik,
                            KursSredni = item.kurs_sredni,
                        };

                        listaWalut.Add(waluta);
                    }

                    temp = listaWalut;
                }
            //});

            return temp;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// RelayCommand operacji obliczania kursów i wartości.
        /// </summary>
        public RelayCommand CmdOperOblicz
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="piRokKoniec"></param>
        public void SelectedDateChangedRokKoniec(DateTime piRokKoniec)
        {
            try
            {
                KursSredniKoniec = String.Empty;
                KwotaPrzeliczKoniec = String.Empty;
                KwotaRoznica = String.Empty;

                //RokKoniec = new DateTime(piRokKoniec.Year, 12, 31);
                RokKoniec = piRokKoniec;

                IList<string> dirs = GetDir(RokKoniec);
                if (dirs != null && dirs.Count > 0)
                {
                    String fd = GetDir(dirs, false, RokKoniec, "yyMMdd");
                    String data = GetExchangeRate(fd);

                    XmlSerializer x = new XmlSerializer(typeof(tabela_kursow));
                    tabela_kursow tabelaKursow = (tabela_kursow)x.Deserialize(new StringReader(data));

                    if (!String.IsNullOrEmpty(_listaWalutRemember))
                    {
                        tabela_kursowPozycja pozycja = tabelaKursow.pozycja.FirstOrDefault(t => t.kod_waluty == _listaWalutRemember);
                        KursSredniKoniec = pozycja.kurs_sredni;
                    }
                }
            }
            catch (Exception)
            {
            
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="piKursWaluty">Dane odnośnie kursu waluty.</param>
        public void SelectionChangedWaluta(KursWaluty piKursWaluty)
        {
            try
            {
                if (piKursWaluty != null)
                {
                    KursSredni = String.Empty;
                    KwotaPrzelicz = String.Empty;
                    KwotaRoznica = String.Empty;
                    
                    KursSredni = piKursWaluty.KursSredni;
                    _listaWalutRemember = piKursWaluty.Kod;

                    KursSredniKoniec = String.Empty;
                    KwotaPrzeliczKoniec = String.Empty;

                    IList<string> dirs = GetDir(RokKoniec);
                    if (dirs != null && dirs.Count > 0)
                    {
                        String fd = GetDir(dirs, false, RokKoniec, "yy1231");
                        String data = GetExchangeRate(fd);

                        XmlSerializer x = new XmlSerializer(typeof(tabela_kursow));
                        tabela_kursow tabelaKursow = (tabela_kursow)x.Deserialize(new StringReader(data));

                        if (!String.IsNullOrEmpty(_listaWalutRemember))
                        {
                            tabela_kursowPozycja pozycja = tabelaKursow.pozycja.FirstOrDefault(t => t.kod_waluty == _listaWalutRemember);
                            KursSredniKoniec = pozycja.kurs_sredni;
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Zdarzenie zmiany daty kalendarza dotyczącego daty faktury.
        /// </summary>
        /// <param name="piDataFaktury">Data faktury.</param>
        public void SelectedDateChangedDataFaktury(DateTime? piDataFaktury)
        {
            try
            {
                if (!piDataFaktury.HasValue)
                    return;

                DataKursu = GetDateNonDayOfWeek(piDataFaktury.Value.AddDays(-1));
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Zdarzenie zmiany daty kalendarza dotyczącego daty kursu waluty.
        /// Pobranie odpowiedniego pliku z nbp.pl z kursami walut za dany dzień, przeszukanie talicy z nazwami plików
        /// w poszukiwaniu Tablicy A (kursy walut aktualizowane codziennie 11:45 - 12:15),
        /// pobranie i zdeserializowanie odpowiedniego pliku z kursami walut.
        /// </summary>
        /// <param name="piDataKursu">Data kursu waluty.</param>
        public void SelectedDateChangedDataKursu(DateTime? piDataKursu)
        {
            try
            {
                if (!piDataKursu.HasValue)
                    return;

                _listaWalut.Clear();

                KursSredni = String.Empty;
                KwotaPrzelicz = String.Empty;
                KwotaRoznica = String.Empty;
                DataKursu = piDataKursu;

                IList<KursWaluty> kursyWalut = GetKursyWalut(DataKursu.Value);
                ListaWalut = new ObservableCollection<KursWaluty>(kursyWalut);

                DataKursu = _dataKursuPobrana;
                ListaWalutValue = _listaWalutRemember;
            }
            catch (Exception)
            {

            }
        }
        
        #endregion

        #region Public Properties

        private DateTime? _dataFaktury;
        /// <summary>
        /// Data faktury.
        /// </summary>
        public DateTime? DataFaktury
        {
            get
            {
                return _dataFaktury;
            }
            set
            {
                _dataFaktury = value;
                NotifyOfPropertyChange(() => DataFaktury);
            }
        }

        private DateTime? _dataKursu;
        /// <summary>
        /// Data kursu waluty za dany dzień.
        /// </summary>
        public DateTime? DataKursu
        {
            get
            {
                return _dataKursu;
            }
            set
            {
                _dataKursu = value;
                NotifyOfPropertyChange(() => DataKursu);
            }
        }

        private ObservableCollection<KursWaluty> _listaWalut = new ObservableCollection<KursWaluty>();
        /// <summary>
        /// Lista pobranych walut z kursami z nbp.pl.
        /// </summary>
        public ObservableCollection<KursWaluty> ListaWalut
        {
            get
            {
                return _listaWalut;
            } 
            set
            {
                _listaWalut = value;
                NotifyOfPropertyChange(() => ListaWalut);
            }
        }

        private String _kursSredni = String.Empty;
        /// <summary>
        /// Średni kurs waluty.
        /// </summary>
        public String KursSredni
        {
            get
            {
                return _kursSredni;
            }
            set
            {
                _kursSredni = value;
                NotifyOfPropertyChange(() => KursSredni);
            }
        }

        private String _kursSredniKoniec = String.Empty;
        /// <summary>
        /// Średni kurs waluty na dzień 31 grudzień.
        /// </summary>
        public String KursSredniKoniec
        {
            get
            {
                return _kursSredniKoniec;
            }
            set
            {
                _kursSredniKoniec = value;
                NotifyOfPropertyChange(() => KursSredniKoniec);
            }
        }
        
        private String _kwota = String.Empty;
        /// <summary>
        /// Kwota jaką należy przewalutować.
        /// </summary>
        public String Kwota
        {
            get
            {
                return _kwota;
            }
            set
            {
                _kwota = value;
                NotifyOfPropertyChange(() => Kwota);
            }
        }

        private String _kwotaPrzelicz = String.Empty;
        /// <summary>
        /// Kwota po kursie.
        /// </summary>
        public String KwotaPrzelicz
        {
            get
            {
                return _kwotaPrzelicz;
            }
            set
            {
                _kwotaPrzelicz = value;
                NotifyOfPropertyChange(() => KwotaPrzelicz);
            }
        }

        private String _kwotaPrzeliczKoniec = String.Empty;
        /// <summary>
        /// Kwota po kursie na dzień 31 grudzień.
        /// </summary>
        public String KwotaPrzeliczKoniec
        {
            get
            {
                return _kwotaPrzeliczKoniec;
            }
            set
            {
                _kwotaPrzeliczKoniec = value;
                NotifyOfPropertyChange(() => KwotaPrzeliczKoniec);
            }
        }

        private String _kwotaRoznica = String.Empty;
        /// <summary>
        /// Różnica pomiędzy kwotą za dzień 31 grudzień a kwotą po kursie ze wskazanego dnia.
        /// </summary>
        public String KwotaRoznica
        {
            get
            {
                return _kwotaRoznica;
            }
            set
            {
                _kwotaRoznica = value;
                NotifyOfPropertyChange(() => KwotaRoznica);
            }
        }

        private String _listaWalutValue = String.Empty;
        /// <summary>
        /// Słownik walut ze średnimi kursami.
        /// </summary>
        public String ListaWalutValue
        {
            get
            {
                return _listaWalutValue;
            }
            set
            {
                _listaWalutValue = value;
                NotifyOfPropertyChange(() => ListaWalutValue);
            }
        }

        private DateTime _rokKoniec = new DateTime(DateTime.Now.AddYears(-1).Year, 12, 31);
        /// <summary>
        /// 
        /// </summary>
        public DateTime RokKoniec
        {
            get
            {
                return _rokKoniec;
            }
            set
            {
                _rokKoniec = value;
                NotifyOfPropertyChange(() => RokKoniec);
            }
        }

        #endregion

        #region Validation Error Data

        private readonly Dictionary<string, bool?> _validityFields = new Dictionary<string, bool?>()
        {
            { KwotaPropertyName, null },
        };

        private const string KwotaPropertyName = "Kwota";

        private bool ValidateFiltersToAccept()
        {
            return (_validityFields.All(pair => pair.Value == true));
        }

        private bool? ValidateInput(ref string prMessage, string piColumnName)
        {
            bool isValid = true;
            switch (piColumnName)
            {
                case KwotaPropertyName:
                    {
                        if (String.IsNullOrEmpty(Kwota))
                        {
                            isValid = false;
                            prMessage = "To pole jest wymagane do przewalutowania!";
                            break;
                        }
                        break;
                    }
            }

            return isValid;
        }

        public string Error
        {
            get
            {
                return String.Empty;
            }
        }

        public string this[string columnName]
        {
            get
            {
                string str = string.Empty;
                bool? isValid = ValidateInput(ref str, columnName);

                if (_validityFields.ContainsKey(columnName))
                {
                    _validityFields[columnName] = isValid;
                }
                _canOperOblicz = ValidateFiltersToAccept();
                return str;
            }
        }

        #endregion
    }
}
