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

        private GameManager()
        {
            AllNpcs = new List<NpcData>();
            WorldTime = new TimeManager();
        }

        public void LoadAllNpcs()
        {
            AllNpcs.Clear();

            string folderPath = AppDomain.CurrentDomain.BaseDirectory;

            string[] jsonFiles = Directory.GetFiles(folderPath, "*.json");

            Debug.WriteLine($"[GameManager] Trovati {jsonFiles.Length} file JSON in {folderPath}");

            foreach (string file in jsonFiles)
            {
                try
                {
                    string jsonContent = File.ReadAllText(file);
                    var npc = JsonConvert.DeserializeObject<NpcData>(jsonContent);

                    if (npc != null && !string.IsNullOrEmpty(npc.Id) && !string.IsNullOrEmpty(npc.Name))
                    {
                        if (npc.Stats != null)
                        {
                            npc.Stats.CurrentPatience = npc.Stats.BasePatience;
                        }

                        AllNpcs.Add(npc);
                        Debug.WriteLine($"[GameManager] Caricato NPC: {npc.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[GameManager] ERRORE caricamento {Path.GetFileName(file)}: {ex.Message}");
                }
            }
        }
    }
}
