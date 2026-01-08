using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaBaySimulator
{
    public enum DayPhase
    {
        Morning,
        Afternoon,
        Evening,
        Night
    }

    public partial class TimeManager : ObservableObject
    {
        [ObservableProperty]
        private int _currentDay = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LocalizedDayName))]
        [NotifyPropertyChangedFor(nameof(FullDateString))]
        private DayOfWeek _currentDayOfWeek = DayOfWeek.Monday;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LocalizedPhase))]
        private DayPhase _currentPhase = DayPhase.Morning;

        public event EventHandler? NewDayStarted;

        public void AdvanceTime()
        {
            switch (CurrentPhase)
            {
                case DayPhase.Morning:
                    CurrentPhase = DayPhase.Afternoon;
                    break;
                case DayPhase.Afternoon:
                    CurrentPhase = DayPhase.Evening;
                    break;
                case DayPhase.Evening:
                    CurrentPhase = DayPhase.Night;
                    break;
                case DayPhase.Night:
                    CurrentPhase = DayPhase.Morning;
                    AdvanceDay();
                    break;
            }
        }

        private void AdvanceDay()
        {
            CurrentPhase = DayPhase.Morning;
            CurrentDay++;

            CurrentDayOfWeek = (DayOfWeek)(((int)CurrentDayOfWeek + 1) % 7);

            NewDayStarted?.Invoke(this, EventArgs.Empty);
        }

        public string LocalizedPhase
        {
            get
            {
                return CurrentPhase switch
                {
                    DayPhase.Morning => "Mattina",
                    DayPhase.Afternoon => "Pomeriggio",
                    DayPhase.Evening => "Sera",
                    DayPhase.Night => "Notte",
                    _ => CurrentPhase.ToString()
                };
            }
        }

        public string LocalizedDayName => CultureInfo.GetCultureInfo("it-IT").DateTimeFormat.GetDayName(CurrentDayOfWeek);

        public string FullDateString => $"GIORNO {CurrentDay} - {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(LocalizedDayName)}";

        /// <summary>
        /// Forza l'aggiornamento della UI per tutte le proprietà temporali.
        /// Da chiamare dopo aver caricato una partita salvata.
        /// </summary>
        public void RefreshTimeDisplay()
        {
            CurrentDayOfWeek = (DayOfWeek)(CurrentDay % 7);

            OnPropertyChanged(nameof(CurrentDay));
            OnPropertyChanged(nameof(CurrentPhase));
            OnPropertyChanged(nameof(CurrentDayOfWeek));

            OnPropertyChanged(nameof(LocalizedPhase));
            OnPropertyChanged(nameof(LocalizedDayName));
            OnPropertyChanged(nameof(FullDateString));
        }
    }
}
