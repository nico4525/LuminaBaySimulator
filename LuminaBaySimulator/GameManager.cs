using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LuminaBaySimulator
{
    public class GameSaveData
    {
        public int Money { get; set; }
        public int Energy { get; set; }
        public int Stress { get; set; }
        public int Intelligence { get; set; }
        public List<string> InventoryItemIds { get; set; } = new List<string>();

        public Dictionary<string, bool> StoryFlags { get; set; } = new Dictionary<string, bool>();

        public int CurrentDay { get; set; }
        public DayPhase CurrentPhase { get; set; }
    }

    public class GameManager
    {
        private static readonly Lazy<GameManager> _lazy = new Lazy<GameManager>(() => new GameManager());
        public static GameManager Instance => _lazy.Value;

        public List<NpcData> AllNpcs { get; private set; }
        public TimeManager WorldTime { get; private set; }

        public PlayerStats Player { get; private set; }

        public List<GameLocation> Locations { get; private set; }

        public List<GameItem> ShopItems { get; private set; }

        public string CurrentWeather { get; set; } = "Sunny";
        private string SaveFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "savegame.json");

        private GameManager()
        {
            AllNpcs = new List<NpcData>();
            WorldTime = new TimeManager();
            Player = new PlayerStats();
            ShopItems = new List<GameItem>();

            InitializeLocations();
            LoadShopItems();

            LoadAllNpcs();

            ValidateDate();
        }

        /// <summary>
        /// Verifica se il giocatore soddisfa tutti i requisiti per una scelta di dialogo.
        /// </summary>
        public bool CheckRequirements(DialogueRequirements? reqs)
        {
            if (reqs == null) return true;

            if (reqs.Money.HasValue && Player.Money < reqs.Money.Value) return false;
            if (reqs.Intelligence.HasValue && Player.Intelligence < reqs.Intelligence.Value) return false;
            if (reqs.Energy.HasValue && Player.Energy < reqs.Energy.Value) return false;

            if (!string.IsNullOrEmpty(reqs.ItemId))
            {
                if (!Player.HasItem(reqs.ItemId)) return false;
            }

            if (reqs.StoryFlagsCondition != null)
            {
                foreach (var kvp in reqs.StoryFlagsCondition)
                {
                    string flagKey = kvp.Key;
                    bool requiredValue = kvp.Value;

                    if (Player.CheckFlag(flagKey) != requiredValue)
                        return false;
                }
            }

            return true;
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
            List<NpcData> presentNpcs = new List<NpcData>();

            string weather = CurrentWeather;
            int dayNumber = WorldTime.CurrentDay;
            DayPhase phase = WorldTime.CurrentPhase;
            DayOfWeek dayOfWeek = WorldTime.CurrentDayOfWeek;

            foreach (var npc in AllNpcs)
            {
                string currentLocationOfNpc = npc.GetCurrentLocationId(weather, dayNumber, phase, dayOfWeek);

                if (!string.IsNullOrEmpty(currentLocationOfNpc) &&
                    currentLocationOfNpc.Equals(locationId, StringComparison.InvariantCultureIgnoreCase))
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

        public void SaveGame()
        {
            try
            {
                var saveData = new GameSaveData
                {
                    Money = Player.Money,
                    Energy = Player.Energy,
                    Stress = Player.Stress,
                    Intelligence = Player.Intelligence,
                    InventoryItemIds = Player.Inventory.Select(x => x.Id).ToList(),
                    CurrentDay = WorldTime.CurrentDay,
                    CurrentPhase = WorldTime.CurrentPhase
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = System.Text.Json.JsonSerializer.Serialize(saveData, options);
                File.WriteAllText(SaveFilePath, jsonString);

                System.Diagnostics.Debug.WriteLine("Partita salvata con successo.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Errore durante il salvataggio: {ex.Message}");
            }
        }

        public bool LoadGame()
        {
            if (!File.Exists(SaveFilePath)) return false;

            try
            {
                string jsonString = File.ReadAllText(SaveFilePath);
                var loadedData = System.Text.Json.JsonSerializer.Deserialize<GameSaveData>(jsonString);

                if (loadedData == null) return false;

                Player.Money = loadedData.Money;
                Player.Energy = loadedData.Energy;
                Player.Stress = loadedData.Stress;
                Player.Intelligence = loadedData.Intelligence;

                Player.Inventory.Clear();
                foreach (var itemId in loadedData.InventoryItemIds)
                {
                    var itemRef = ShopItems.FirstOrDefault(x => x.Id == itemId);
                    if (itemRef != null)
                    {
                        Player.Inventory.Add(itemRef);
                    }
                }

                WorldTime.CurrentDay = loadedData.CurrentDay;
                WorldTime.CurrentPhase = loadedData.CurrentPhase;

                WorldTime.RefreshTimeDisplay();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Errore durante il caricamento: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validazione dell'integrità referenziale dei dati.
        /// Controlla che tutti i "NextNodeId" nei dialoghi puntino a nodi esistenti.
        /// </summary>
        public void ValidateDate()
        {
            System.Diagnostics.Debug.WriteLine("--- INIZIO VALIDAZIONE DATI ---");
            int errorsFound = 0;

            foreach(var npc in AllNpcs)
            {
                if (npc.Dialogues == null || npc.Dialogues.Count == 0)
                {
                    continue;
                }

                foreach(var nodePair in npc.Dialogues)
                {
                    string nodeId = nodePair.Key;
                    DialogueNode node = nodePair.Value;

                    if (node.Choices == null) continue;

                    foreach(var choice in node.Choices)
                    {
                        if (string.Equals(choice.NextNodeId, "END", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (string.IsNullOrEmpty(choice.NextNodeId))
                        {
                            System.Diagnostics.Debug.WriteLine($"[ERRORE DATI] NPC: {npc.Name} ({npc.Id}) | Nodo: {nodeId} | Scelta: '{choice.Text}' ha un NextNodeId VUOTO.");
                            errorsFound++;
                            continue;
                        }

                        if (!npc.Dialogues.ContainsKey(choice.NextNodeId))
                        {
                            string msg = $"[CRITICO] DEAD LINK TROVATO!\n" +
                                         $"NPC: {npc.Name}\n" +
                                         $"Nodo Origine: {nodeId}\n" +
                                         $"Testo Scelta: '{choice.Text}'\n" +
                                         $"Punta a: '{choice.NextNodeId}' (NON ESISTE)";

                            System.Diagnostics.Debug.WriteLine(msg);

                            #if DEBUG
                            System.Windows.MessageBox.Show(msg, "Errore Integrità Dati", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                            #endif

                            errorsFound++;
                        }
                    }
                }
            }

            if (errorsFound == 0)
            {
                System.Diagnostics.Debug.WriteLine("--- VALIDAZIONE COMPLETATA: NESSUN ERRORE ---");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"--- VALIDAZIONE COMPLETATA: TROVATI {errorsFound} ERRORI ---");
            }
        }
    }
}

