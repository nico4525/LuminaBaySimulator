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
