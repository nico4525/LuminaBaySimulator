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
    public class NpcCoordinates
    {
        [JsonProperty("x")]
        public double X { get; set; }
        [JsonProperty("y")]
        public double Y { get; set; }
    }

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

        [JsonProperty("special_events")]
        public Dictionary<int, DaySchedule>? SpecialEvents { get; set; }

        [ObservableProperty]
        private double _currentX;

        [ObservableProperty]
        private double _currentY;

        [JsonProperty("location_coordinates")]
        public Dictionary<string, NpcCoordinates> LocationCoordinates { get; set; } = new Dictionary<string, NpcCoordinates>();

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
                OnPropertyChanged(nameof(CurrentLocationUiString));
            }
        }

        public string CurrentLocationUiString
        {
            get
            {
                if (GameManager.Instance == null) return "Loading...";

                string locationId = GetCurrentLocationId(
                    GameManager.Instance.CurrentWeather,
                    GameManager.Instance.WorldTime.CurrentDay,
                    GameManager.Instance.WorldTime.CurrentPhase,
                    GameManager.Instance.WorldTime.CurrentDayOfWeek
                );

                return FormatLocationName(locationId);
            }
        }

        /// <summary>
        /// Logica Master per determinare dove si trova l'NPC (Task 3)
        /// </summary>
        public string GetCurrentLocationId(string weather, int currentDay, DayPhase phase, DayOfWeek dayOfWeek)
        {
            if (Schedule == null) return "";

            if (SpecialEvents != null && SpecialEvents.ContainsKey(currentDay))
            {
                string? eventLoc = GetLocationFromSchedule(SpecialEvents[currentDay], phase);
                if (!string.IsNullOrEmpty(eventLoc)) return eventLoc;
            }

            bool isRainy = weather.ToLower().Contains("rain");
            if (isRainy && Schedule.ContainsKey("rainy"))
            {
                string? rainyLoc = GetLocationFromSchedule(Schedule["rainy"], phase);
                if (!string.IsNullOrEmpty(rainyLoc)) return rainyLoc;
            }

            string dayKey = dayOfWeek.ToString().ToLowerInvariant();
            DaySchedule? todaySchedule = null;

            if (Schedule.ContainsKey(dayKey))
                todaySchedule = Schedule[dayKey];
            else if (Schedule.ContainsKey("default"))
                todaySchedule = Schedule["default"];
            else if (Schedule.ContainsKey("monday")) 
                todaySchedule = Schedule["monday"];

            if (todaySchedule == null) return "";

            return GetLocationFromSchedule(todaySchedule, phase) ?? "";
        }

        private string? GetLocationFromSchedule(DaySchedule schedule, DayPhase phase)
        {
            return phase switch
            {
                DayPhase.Morning => schedule.Morning,
                DayPhase.Afternoon => schedule.Afternoon,
                DayPhase.Evening => schedule.Evening,
                DayPhase.Night => schedule.Night,
                _ => null
            };
        }

        private string FormatLocationName(string? rawId)
        {
            if (string.IsNullOrEmpty(rawId)) return "Sconosciuto";
            var locObj = GameManager.Instance.Locations.Find(l => l.Id == rawId);
            return locObj != null ? locObj.Name : rawId;
        }

        public void RefreshLocation()
        {
            OnPropertyChanged(nameof(CurrentLocationUiString));
            UpdateCoordinatesForCurrentLocation();
        }

        private void UpdateCoordinatesForCurrentLocation()
        {
            if (GameManager.Instance == null) return;

            string locId = GetCurrentLocationId(
                 GameManager.Instance.CurrentWeather,
                 GameManager.Instance.WorldTime.CurrentDay,
                 GameManager.Instance.WorldTime.CurrentPhase,
                 GameManager.Instance.WorldTime.CurrentDayOfWeek
            );

            if (LocationCoordinates != null && LocationCoordinates.ContainsKey(locId))
            {
                CurrentX = LocationCoordinates[locId].X;
                CurrentY = LocationCoordinates[locId].Y;
            }
            else
            {

                var rand = new Random(this.GetHashCode());
                CurrentX = rand.Next(50, 600);
                CurrentY = rand.Next(150, 400);
            }
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
        [JsonProperty("morning")]  
        public string? Morning { get; set; }

        [JsonProperty("afternoon")] 
        public string? Afternoon { get; set; }

        [JsonProperty("evening")] 
        public string? Evening { get; set; }

        [JsonProperty("night")] 
        public string? Night { get; set; }
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

        [JsonProperty("energy")]
        public int? Energy { get; set; }

        [JsonProperty("item_id")]
        public string? ItemId { get; set; }

        [JsonProperty("story_flags")]
        public Dictionary<string, bool>? StoryFlagsCondition { get; set; }
    }

    public class DialogueImpact
    {
        [JsonProperty("affection")]
        public int Affection { get; set; }

        [JsonProperty("patience")]
        public int Patience { get; set; }

        [JsonProperty("set_story_flags")]
        public Dictionary<string, bool>? SetStoryFlags { get; set; }

    }
}
