using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LuminaBaySimulator
{
    public enum GameViewMode
    {
        Map,           
        LocationInside, 
        Dialogue,
        Shop
    }

    public class FloatingTextItem
    {
        public string Text { get; set; } = "";
        public string Color { get; set; } = "White";
        public double StartX { get; set; }
        public double StartY { get; set; }
    }

    public partial class MainViewModel : ObservableObject
    {
        public TimeManager WorldTime => GameManager.Instance.WorldTime;
        public PlayerStats Player => GameManager.Instance.Player;

        [ObservableProperty]
        private GameViewMode _currentViewMode = GameViewMode.Map;

        public ObservableCollection<GameItem> CurrentShopInventory { get; } = new ObservableCollection<GameItem>();

        public ObservableCollection<GameLocation> AvailableLocations { get; } = new ObservableCollection<GameLocation>(GameManager.Instance.Locations);

        [ObservableProperty]
        private GameLocation? _currentLocation;

        [ObservableProperty]
        private ObservableCollection<NpcData> _npcsInLocation = new ObservableCollection<NpcData>();

        [ObservableProperty]
        private NpcData? _currentNpc;

        [ObservableProperty]
        private bool _isDialogueActive;

        [ObservableProperty]
        private DialogueNode? _currentDialogueNode;

        [ObservableProperty]
        private string _statusMessage = "";

        [ObservableProperty]
        private bool _isStatusVisible;

        public ObservableCollection<FloatingTextItem> FloatingEffects { get; } = new ObservableCollection<FloatingTextItem>();

        private int _lastMoney;
        private int _lastEnergy;
        private int _lastStress;

        public MainViewModel()
        {
            GameManager.Instance.LoadAllNpcs();

            _lastMoney = Player.Money;
            _lastEnergy = Player.Energy;
            _lastStress = Player.Stress;

            GameManager.Instance.WorldTime.NewDayStarted += (s, e) => ShowStatusMessage("🌅 È sorto un nuovo giorno!", 4000);

            GameManager.Instance.WorldTime.PropertyChanged += (s, e) => RefreshCommandStates();
            GameManager.Instance.Player.PropertyChanged += OnPlayerStatsChanged;

            CurrentNpc = GameManager.Instance.AllNpcs.FirstOrDefault();
            if (CurrentNpc != null) CurrentNpc.RefreshLocation();
        }

        private void OnPlayerStatsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RefreshCommandStates(); 

            if (e.PropertyName == nameof(PlayerStats.Money))
            {
                int diff = Player.Money - _lastMoney;
                if (diff != 0) SpawnFloatingText(diff > 0 ? $"+{diff}€" : $"{diff}€", diff > 0 ? "#FFD700" : "#E57373");
                _lastMoney = Player.Money;
            }
            else if (e.PropertyName == nameof(PlayerStats.Energy))
            {
                int diff = Player.Energy - _lastEnergy;
                if (diff != 0) SpawnFloatingText(diff > 0 ? $"+{diff}⚡" : $"{diff}⚡", diff > 0 ? "#76FF03" : "#BDBDBD");
                _lastEnergy = Player.Energy;
            }
            else if (e.PropertyName == nameof(PlayerStats.Stress))
            {
                int diff = Player.Stress - _lastStress;
                if (diff != 0) SpawnFloatingText(diff > 0 ? $"+{diff} Stress" : $"{diff} Stress", diff > 0 ? "#FF3D00" : "#00E5FF");
                _lastStress = Player.Stress;
            }
        }

        private async void SpawnFloatingText(string text, string color)
        {
            // Creiamo l'oggetto visuale
            var floatingItem = new FloatingTextItem
            {
                Text = text,
                Color = color,
                StartX = 400 + new Random().Next(-50, 50),
                StartY = 300
            };

            FloatingEffects.Add(floatingItem);

            await Task.Delay(2000);
            if (FloatingEffects.Contains(floatingItem))
            {
                FloatingEffects.Remove(floatingItem);
            }
        }

        /// <summary>
        /// Mostra un messaggio temporaneo che sparisce automaticamente (Async/Await)
        /// </summary>
        private async void ShowStatusMessage(string message, int delayMs = 3000)
        {
            StatusMessage = message;
            IsStatusVisible = true;

            await Task.Delay(delayMs);

            if (StatusMessage == message)
            {
                IsStatusVisible = false;
                StatusMessage = "";
            }
        }



        private void OnNewDayStarted(object? sender, EventArgs e)
        {
            Player.Energy = PlayerStats.MaxEnergy;

            ShowStatusMessage("È sorto un nuovo giorno! L'energia è stata ripristinata.");
        }

        private void RefreshCommandStates()
        {
            GoToSchoolCommand.NotifyCanExecuteChanged();
            StudyCommand.NotifyCanExecuteChanged();
            RelaxCommand.NotifyCanExecuteChanged();
            SleepCommand.NotifyCanExecuteChanged();
            AdvanceTimeCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanGoToSchool))]
        private void GoToSchool()
        {
            Player.Energy -= 30;
            Player.Stress += 10;
            Player.Money += 5;

            ShowStatusMessage("Sei andato a scuola. Hai imparato qualcosa, ma che fatica!");

            WorldTime.AdvanceTime();
        }
        private bool CanGoToSchool()
        {
            return WorldTime.CurrentPhase == DayPhase.Morning && Player.HasEnergy(30);
        }

        [RelayCommand(CanExecute = nameof(CanStudy))]
        private void Study()
        {
            Player.Energy -= 20;
            Player.Stress += 15; 

            ShowStatusMessage("Hai studiato intensamente.");
            WorldTime.AdvanceTime();
        }
        private bool CanStudy()
        {
            return WorldTime.CurrentPhase != DayPhase.Night && Player.HasEnergy(20);
        }

        [RelayCommand(CanExecute = nameof(CanRelax))]
        private void Relax()
        {
            Player.Energy -= 5;
            Player.Stress -= 20; 

            ShowStatusMessage("Ti sei preso un momento per respirare. Lo stress diminuisce.");
            WorldTime.AdvanceTime();
        }
        private bool CanRelax()
        {
            return Player.HasEnergy(5);
        }

        [RelayCommand]
        private void Sleep()
        {
            ShowStatusMessage("Vai a dormire...");

            while (WorldTime.CurrentPhase != DayPhase.Night)
            {
                WorldTime.AdvanceTime();
            }
            WorldTime.AdvanceTime();
        }

        [RelayCommand]
        private void AdvanceTime()
        {
            GameManager.Instance.WorldTime.AdvanceTime();

            System.Diagnostics.Debug.WriteLine($"Tempo Avanzato: {WorldTime.LocalizedPhase}");
        }

        [RelayCommand]
        private void TravelToLocation(GameLocation location)
        {
            if (location == null) return;
            CurrentLocation = location;
            CurrentViewMode = GameViewMode.LocationInside;
            RefreshNpcsInLocation();
            ShowStatusMessage($"Sei arrivato a: {location.Name}");
        }

        [RelayCommand]
        private void BackToMap()
        {
            CurrentViewMode = GameViewMode.Map;
            CurrentLocation = null;
            CurrentNpc = null;
            IsDialogueActive = false;
            CurrentDialogueNode = null;
            ShowStatusMessage("Sei tornato alla mappa della città.");
        }

        private void RefreshNpcsInLocation()
        {
            if (CurrentLocation == null) return;

            var npcs = GameManager.Instance.GetNpcsAtLocation(CurrentLocation.Id);
            NpcsInLocation.Clear();
            foreach (var npc in npcs)
            {
                npc.RefreshLocation();
                NpcsInLocation.Add(npc);
            }

            if (NpcsInLocation.Count == 0)
            {
                ShowStatusMessage("Non sembra esserci nessuno qui al momento.");
            }
        }

        [RelayCommand]
        private void OpenShop()
        {
            CurrentShopInventory.Clear();
            foreach (var item in GameManager.Instance.ShopItems) CurrentShopInventory.Add(item);
            CurrentViewMode = GameViewMode.Shop;
        }

        [RelayCommand]
        private void CloseShop()
        {
            CurrentViewMode = GameViewMode.LocationInside;
            ShowStatusMessage("Hai lasciato il negozio.");
        }

        [RelayCommand]
        private void BuyItem(GameItem item)
        {
            if (item == null) return;

            if (Player.Money >= item.Cost)
            {
                Player.Money -= item.Cost;
                Player.AddItem(item);
                ShowStatusMessage($"Acquistato: {item.Name}!", 2000);
            }
            else
            {
                ShowStatusMessage("❌ Non hai abbastanza soldi!", 2000);
            }
        }

        [RelayCommand]
        private void InteractWithNpc(NpcData npc)
        {
            if (npc == null) return;
            CurrentNpc = npc;

            if (CurrentNpc.Dialogues == null || !CurrentNpc.Dialogues.ContainsKey("root"))
            {
                ShowStatusMessage($"{npc.Name} sembra impegnato/a.", 2000);
                return;
            }

            CurrentViewMode = GameViewMode.Dialogue;
            IsDialogueActive = true; 
            LoadNode("root");
        }

        [RelayCommand]
        private void StartDialogue()
        {
            if (CurrentNpc?.Dialogues == null || !CurrentNpc.Dialogues.ContainsKey("root"))
            {
                MessageBox.Show("Questo personaggio non ha dialoghi disponibili (manca il nodo 'root').", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsDialogueActive = true;
            
            LoadNode("root");
        }

        [RelayCommand(CanExecute = nameof(CanSelectChoice))]
        private void SelectChoice(DialogueChoice choice)
        {
            if (CurrentNpc == null || CurrentNpc.Stats == null) return;

            if (choice.Impact != null)
            {
                CurrentNpc.Stats.CurrentAffection += choice.Impact.Affection;
                if (CurrentNpc.Stats.CurrentAffection < 0) CurrentNpc.Stats.CurrentAffection = 0;
                if (CurrentNpc.Stats.CurrentAffection > CurrentNpc.Stats.MaxAffection) CurrentNpc.Stats.CurrentAffection = CurrentNpc.Stats.MaxAffection;

                CurrentNpc.Stats.CurrentPatience += choice.Impact.Patience;
                if (CurrentNpc.Stats.CurrentPatience < 0) CurrentNpc.Stats.CurrentPatience = 0;
                if (CurrentNpc.Stats.CurrentPatience > 100) CurrentNpc.Stats.CurrentPatience = 100;

                if (choice.Impact.Affection != 0 || choice.Impact.Patience != 0)
                {
                    ShowStatusMessage($"Effetto: Affetto {choice.Impact.Affection:+0;-0}, Pazienza {choice.Impact.Patience:+0;-0}");
                }

                if (choice.Impact.SetStoryFlags != null)
                {
                    foreach (var flag in choice.Impact.SetStoryFlags)
                    {
                        GameManager.Instance.Player.SetFlag(flag.Key, flag.Value);
                    }
                }
            }

            if (string.IsNullOrEmpty(choice.NextNodeId) || choice.NextNodeId.ToUpper() == "END")
            {
                CurrentViewMode = GameViewMode.LocationInside;
                CurrentDialogueNode = null;
                ShowStatusMessage("Conversazione terminata.");
            }
            else
            {
                LoadNode(choice.NextNodeId);
            }
        }

        private bool CanSelectChoice(DialogueChoice choice)
        {
            return GameManager.Instance.CheckRequirements(choice.Requirements);
        }

        private void LoadNode(string nodeId)
        {
            if (CurrentNpc != null && CurrentNpc.Dialogues != null && CurrentNpc.Dialogues.TryGetValue(nodeId, out var node))
            {
                CurrentDialogueNode = node;
                SelectChoiceCommand.NotifyCanExecuteChanged();
            }
            else
            {
                IsDialogueActive = false;
            }
        }

        [RelayCommand]
        private void SaveGame()
        {
            GameManager.Instance.SaveGame();
            ShowStatusMessage("💾 Partita Salvata!", 2000);
        }

        [RelayCommand]
        private void LoadGame()
        {
            bool success = GameManager.Instance.LoadGame();
            if (success)
            {
                BackToMap();
                RefreshCommandStates();
                ShowStatusMessage("📂 Partita Caricata!", 2000);
            }
            else
            {
                ShowStatusMessage("⚠ Nessun salvataggio trovato.", 2000);
            }
        }


        [RelayCommand]
        private void DebugAddMoney()
        {
            Player.Money += 100;
            ShowStatusMessage("💰 DEBUG: +100€ Aggiunti");
        }

        [RelayCommand]
        private void DebugSkipTime()
        {
            WorldTime.AdvanceTime();
            ShowStatusMessage($"⏩ DEBUG: Tempo avanzato ({WorldTime.LocalizedPhase})");
        }

        [RelayCommand]
        private void DebugRestoreStats()
        {
            Player.Energy = 100;
            Player.Stress = 0;
            ShowStatusMessage("⚡ DEBUG: Statistiche Ripristinate");
        }
    }
}
