using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

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
    }
}
