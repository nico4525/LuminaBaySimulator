using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaBaySimulator
{
    public class GameItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Cost { get; set; }
        public string? IconPath { get; set; }
    }
}
