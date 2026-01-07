using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LuminaBaySimulator
{
    public partial class MainViewModel : ObservableObject
    {
        public TimeManager WorldTime => GameManager.Instance.WorldTime;

        [ObservableProperty]
        private NpcData? _currentNpc;

        public MainViewModel()
        {
            GameManager.Instance.LoadAllNpcs();

            CurrentNpc = GameManager.Instance.AllNpcs.FirstOrDefault();

            if (CurrentNpc == null)
            {
                MessageBox.Show("Nessun NPC trovato! Controlla che i file JSON siano copiati nella cartella di output.",
                                "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private void AdvanceTime()
        {
            GameManager.Instance.WorldTime.AdvanceTime();

            System.Diagnostics.Debug.WriteLine($"Tempo Avanzato: {WorldTime.LocalizedPhase}");
        }
    }
}
