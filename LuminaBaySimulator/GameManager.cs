using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;

namespace LuminaBaySimulator
{
    public class GameManager
    {
        private static readonly Lazy<GameManager> _lazy = new Lazy<GameManager>(() => new GameManager());
        public static GameManager Instance => _lazy.Value;

        public List<NpcData> AllNpcs { get; private set; }
        public TimeManager WorldTime { get; private set; }

        public PlayerStats Player { get; private set; }

        public List<GameLocation> Locations { get; private set; }

        public List<GameItem> ShopItems { get; private set; }

        private GameManager()
        {
            AllNpcs = new List<NpcData>();
            WorldTime = new TimeManager();
            Player = new PlayerStats();
            ShopItems = new List<GameItem>();

            InitializeLocations();
            LoadShopItems();
        }

        public void LoadShopItems()
        {
            ShopItems.Clear();
            string targetDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string shopFile = Path.Combine(targetDirectory, "shop_data.json");

            #if DEBUG
            string debugPath = Path.GetFullPath(Path.Combine(targetDirectory, @"..\..\..\"));
            if (File.Exists(Path.Combine(debugPath, "shop_data.json")))
            {
                shopFile = Path.Combine(debugPath, "shop_data.json");
            }
            #endif

            if (File.Exists(shopFile))
            {
                try
                {
                    string jsonContent = File.ReadAllText(shopFile);
                    var items = JsonConvert.DeserializeObject<List<GameItem>>(jsonContent);
                    if (items != null)
                    {
                        ShopItems.AddRange(items);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Errore caricamento negozio: {ex.Message}");
                }
            }
        }

        private void InitializeLocations()
        {
            
            Locations = new List<GameLocation>
            {
                new GameLocation { Id = "liceo_newton_classe_2B", Name = "Liceo Newton", Description = "Il luogo dove studi (e soffri).", ImagePath = "/Assets/Images/Locations/school.png" },
                new GameLocation { Id = "centro_commerciale", Name = "Centro Commerciale", Description = "Negozi, fast food e posti dove spendere soldi.", ImagePath = "/Assets/Images/Locations/mall.png" },
                new GameLocation { Id = "parco", Name = "Parco Cittadino", Description = "Aria fresca e relax.", ImagePath = "/Assets/Images/Locations/park.png" },
                new GameLocation { Id = "casa", Name = "Casa Tua", Description = "Dolce casa.", ImagePath = "/Assets/Images/Locations/home.png" }
            };
        }

        /// <summary>
        /// Restituisce la lista degli NPC che si trovano in un dato luogo in questo momento.
        /// </summary>
        public List<NpcData> GetNpcsAtLocation(string locationId)
        {
            var currentDayOfWeek = WorldTime.CurrentDayOfWeek.ToString().ToLower(); 
            var currentPhase = WorldTime.CurrentPhase;

            List<NpcData> presentNpcs = new List<NpcData>();

            foreach (var npc in AllNpcs)
            {
                if (npc.Schedule == null) continue;

                DaySchedule? todaySchedule = null;

                if (npc.Schedule.ContainsKey(currentDayOfWeek))
                {
                    todaySchedule = npc.Schedule[currentDayOfWeek];
                }
                else if (npc.Schedule.ContainsKey("default"))
                {
                    todaySchedule = npc.Schedule["default"];
                }

                if (todaySchedule == null) continue;

                string? scheduledLocationId = currentPhase switch
                {
                    DayPhase.Morning => todaySchedule.morning,
                    DayPhase.Afternoon => todaySchedule.afternoon,
                    DayPhase.Evening => todaySchedule.evening,
                    DayPhase.Night => todaySchedule.night,
                    _ => null
                };

                if (!string.IsNullOrEmpty(scheduledLocationId) && scheduledLocationId.Equals(locationId, StringComparison.InvariantCultureIgnoreCase))
                {
                    presentNpcs.Add(npc);
                }
            }

            return presentNpcs;
        }

        public void LoadAllNpcs()
        {
            AllNpcs.Clear();

            string targetDirectory = AppDomain.CurrentDomain.BaseDirectory;

            #if DEBUG
            
            string debugPath = Path.GetFullPath(Path.Combine(targetDirectory, @"..\..\..\"));
            if (Directory.Exists(debugPath))
            {
                targetDirectory = debugPath;
                System.Diagnostics.Debug.WriteLine($"[DEBUG MODE] Leggo i file dalla cartella sorgente: {targetDirectory}");
            }
            #endif

            string[] jsonFiles = Directory.GetFiles(targetDirectory, "char_*.json");

            if (jsonFiles.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[ERRORE] Nessun file JSON trovato in: {targetDirectory}");
                return;
            }

            foreach (string file in jsonFiles)
            {
                try
                {
                    string jsonContent = File.ReadAllText(file);

                    if (!jsonContent.Contains("dialogues"))
                    {
                        System.Diagnostics.Debug.WriteLine($"[ATTENZIONE CRITICA] Il file {Path.GetFileName(file)} letto da {targetDirectory} NON ha i dialoghi!");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[OK] Trovata sezione dialoghi in {Path.GetFileName(file)}");
                    }

                    var npc = JsonConvert.DeserializeObject<NpcData>(jsonContent);

                    if (npc != null && !string.IsNullOrEmpty(npc.Id))
                    {
                        if (npc.Stats != null && npc.Stats.CurrentPatience == 0)
                            npc.Stats.CurrentPatience = npc.Stats.BasePatience;

                        AllNpcs.Add(npc);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading NPC {file}: {ex.Message}");
                }
            }
        }
    }
}
