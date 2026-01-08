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
        public PlayerStats Player => GameManager.Instance.Player;

        [ObservableProperty]
        private NpcData? _currentNpc;

        [ObservableProperty]
        private bool _isDialogueActive;

        [ObservableProperty]
        private DialogueNode? _currentDialogueNode;

        [ObservableProperty]
        private string _lastActionFeedback = "";

        public MainViewModel()
        {
            GameManager.Instance.LoadAllNpcs();

            CurrentNpc = GameManager.Instance.AllNpcs.FirstOrDefault();
            if (CurrentNpc != null)
            {
                CurrentNpc.RefreshLocation();

                System.Diagnostics.Debug.WriteLine($"[DEBUG UI] Location calcolata: {CurrentNpc.CurrentLocation}");
            }

            GameManager.Instance.WorldTime.PropertyChanged += (s, e) => RefreshCommandStates();
            GameManager.Instance.Player.PropertyChanged += (s, e) => RefreshCommandStates();
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

            LastActionFeedback = "Sei andato a scuola. Hai imparato qualcosa, ma che fatica!";

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

            LastActionFeedback = "Hai studiato intensamente.";
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

            LastActionFeedback = "Ti sei preso un momento per respirare. Lo stress diminuisce.";
            WorldTime.AdvanceTime();
        }
        private bool CanRelax()
        {
            return Player.HasEnergy(5);
        }

        [RelayCommand]
        private void Sleep()
        {
            Player.Energy = 100;
            Player.Stress = 0;

            LastActionFeedback = "Hai dormito profondamente. Sei pronto per un nuovo giorno!";

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
        private void StartDialogue()
        {
            if (CurrentNpc?.Dialogues == null || !CurrentNpc.Dialogues.ContainsKey("root"))
            {
                MessageBox.Show("Questo personaggio non ha dialoghi disponibili (manca il nodo 'root').", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsDialogueActive = true;
            LastActionFeedback = "";
            
            LoadNode("root");
        }

        [RelayCommand]
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

                LastActionFeedback = $"Effetto: Affetto {choice.Impact.Affection:+0;-0}, Pazienza {choice.Impact.Patience:+0;-0}";
            }

            if (string.IsNullOrEmpty(choice.NextNodeId) || choice.NextNodeId.ToUpper() == "END")
            {
                IsDialogueActive = false;
                CurrentDialogueNode = null;
            }
            else
            {
                LoadNode(choice.NextNodeId);
            }
        }

        private void LoadNode(string nodeId)
        {
            if (CurrentNpc != null && CurrentNpc.Dialogues != null && CurrentNpc.Dialogues.TryGetValue(nodeId, out var node))
            {
                CurrentDialogueNode = node;
            }
            else
            {
                IsDialogueActive = false;
                MessageBox.Show($"Errore: Nodo dialogo '{nodeId}' non trovato.", "Errore Dialogo");
            }
        }
    }
}
