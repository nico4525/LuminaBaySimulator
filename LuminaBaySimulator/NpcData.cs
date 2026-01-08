using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace LuminaBaySimulator
{
    public partial class NpcData : ObservableObject
    {
        [ObservableProperty]
        [property: JsonProperty("name")]
        private string? _name;

        [ObservableProperty]
        [property: JsonProperty("hex_color")]
        private string? _hexColor;

        [ObservableProperty]
        [property: JsonProperty("sprite_path")]
        private string? _spritePath;

        [ObservableProperty]
        [property: JsonProperty("stats")]
        private NpcStats? _stats;

        [JsonProperty("npc_id")]
        public string? Id { get; set; }

        [JsonProperty("age")]
        public int Age { get; set; }

        [JsonProperty("date_of_birth")]
        public DateTime DateOfBirth { get; set; }

        [JsonProperty("min_player_age")]
        public int MinPlayerAge { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("social_groups")]
        public List<string>? SocialGroups { get; set; }

        [JsonProperty("preferences")]
        public NpcPreferences? Preferences { get; set; }

        [JsonProperty("schedule")]
        public Dictionary<string, DaySchedule>? Schedule { get; set; }

        [JsonProperty("dialogues")]
        public Dictionary<string, DialogueNode>? Dialogues { get; set; }

        public NpcData()
        {
            if (GameManager.Instance?.WorldTime != null)
            {
                GameManager.Instance.WorldTime.PropertyChanged += WorldTime_PropertyChanged;
            }
        }

        private void WorldTime_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TimeManager.CurrentPhase) ||
                e.PropertyName == nameof(TimeManager.CurrentDay))
            {
                OnPropertyChanged(nameof(CurrentLocation));
            }
        }

        public string CurrentLocation
        {
            get
            {
                if (Schedule == null || Schedule.Count == 0)
                    return "Nessuna Schedule";

                if (GameManager.Instance == null) return "GameManager Error";

                var currentDayEnum = GameManager.Instance.WorldTime.CurrentDayOfWeek;

                string currentDayKey = currentDayEnum.ToString().ToLowerInvariant();

                if (!Schedule.TryGetValue(currentDayKey, out var daySchedule))
                {
                    if (!Schedule.TryGetValue("default", out daySchedule) &&
                        !Schedule.TryGetValue("monday", out daySchedule))
                    {
                        return "Riposo (No Schedule)";
                    }
                }

                var phase = GameManager.Instance.WorldTime.CurrentPhase;

                string? rawLocation = phase switch
                {
                    DayPhase.Morning => daySchedule.morning,
                    DayPhase.Afternoon => daySchedule.afternoon,
                    DayPhase.Evening => daySchedule.evening,
                    DayPhase.Night => daySchedule.night,
                    _ => "Sconosciuto"
                };

                return FormatLocationName(rawLocation);
            }
        }

        private string FormatLocationName(string? raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Non specificato";
            string formatted = raw.Replace("_", " ");
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(formatted);
        }

        public void RefreshLocation()
        {
            OnPropertyChanged(nameof(CurrentLocation));
        }
    }

    public partial class NpcStats : ObservableObject
    {
        [ObservableProperty]
        [property: JsonProperty("base_patience")]
        private int _basePatience;

        [ObservableProperty]
        [property: JsonIgnore] 
        private int _currentPatience;

        [ObservableProperty]
        [property: JsonProperty("affection_max")]
        private int _maxAffection;

        [ObservableProperty]
        [property: JsonProperty("affection_current")]
        private int _currentAffection;

        [JsonProperty("jealousy_factor")]
        public double JealousyFactor { get; set; }

        partial void OnBasePatienceChanged(int value)
        {
            if (CurrentPatience == 0) CurrentPatience = value;
        }
    }

    public class NpcPreferences
    {
        [JsonProperty("favorite_foods")]
        public List<string>? FavoriteFoods { get; set; }

        [JsonProperty("hobbies")]
        public List<string>? Hobbies { get; set; }

        [JsonProperty("gifts_loved")]
        public List<string>? LovedGifts { get; set; }

        [JsonProperty("gifts_hated")]
        public List<string>? HatedGifts { get; set; }
    }

    public class DaySchedule
    {
        public string? morning { get; set; }
        public string? afternoon { get; set; }
        public string? evening { get; set; }
        public string? night { get; set; }
    }

    public class DialogueNode
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;

        [JsonProperty("choices")]
        public List<DialogueChoice> Choices { get; set; } = new List<DialogueChoice>();
    }

    public class DialogueChoice : ObservableObject
    {
        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;

        [JsonProperty("next_node_id")]
        public string NextNodeId { get; set; } = "END"; 

        [JsonProperty("impact")]
        public DialogueImpact? Impact { get; set; }

        [JsonProperty("requirements")]
        public DialogueRequirements? Requirements { get; set; }
    }

    public class DialogueRequirements
    {
        [JsonProperty("intelligence")]
        public int? Intelligence { get; set; }

        [JsonProperty("money")]
        public int? Money { get; set; }

        [JsonProperty("item_id")]
        public string? ItemId { get; set; }
    }

    public class DialogueImpact
    {
        [JsonProperty("affection")]
        public int Affection { get; set; }

        [JsonProperty("patience")]
        public int Patience { get; set; }

    }
}
