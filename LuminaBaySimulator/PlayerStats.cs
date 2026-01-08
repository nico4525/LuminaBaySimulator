using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace LuminaBaySimulator
{
    public partial class PlayerStats : ObservableObject
    {
        [ObservableProperty]
        private int _intelligence;

        [ObservableProperty]
        private int _money;

        [ObservableProperty]
        private int _energy;

        [ObservableProperty]
        private int _stress;

        public const int MaxEnergy = 100;
        public const int MaxStress = 100;

        public Dictionary<string, bool> StoryFlags { get; set; } = new Dictionary<string, bool>();

        public ObservableCollection<GameItem> Inventory { get; } = new ObservableCollection<GameItem>();

        public PlayerStats()
        {
            Energy = 100;
            Stress = 0;
            Money = 50; 
        }

        partial void OnEnergyChanged(int value)
        {
            if (value < 0) _energy = 0;
            else if (value > MaxEnergy) _energy = MaxEnergy;

            if (_energy != value) 
                OnPropertyChanged(nameof(Energy));
        }

        partial void OnStressChanged(int value)
        {
            if (value < 0) _stress = 0;
            else if (value > MaxStress) _stress = MaxStress;

            if (_stress != value)
                OnPropertyChanged(nameof(Stress));
        }

        public bool HasEnergy(int amount)
        {
            return Energy >= amount;
        }

        public void AddItem(GameItem item)
        {
            Inventory.Add(item);
            OnPropertyChanged(nameof(Inventory));
        }

        public bool HasItem(string itemId)
        {
            return Inventory.Any(i => i.Id == itemId);
        }

        /// <summary>
        /// Imposta un flag narrativo. Se esiste lo aggiorna, se no lo crea.
        /// </summary>
        public void SetFlag(string key, bool value)
        {
            if (StoryFlags.ContainsKey(key))
            {
                StoryFlags[key] = value;
            }
            else
            {
                StoryFlags.Add(key, value);
            }

            System.Diagnostics.Debug.WriteLine($"[MEMORY] Flag '{key}' impostato a {value}");
        }

        /// <summary>
        /// Verifica il valore di un flag. Di default è false se non esiste.
        /// </summary>
        public bool CheckFlag(string key)
        {
            if (StoryFlags.TryGetValue(key, out bool value))
            {
                return value;
            }
            return false;
        }
    }
}
