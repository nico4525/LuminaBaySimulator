using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LuminaBaySimulator
{
    /// <summary>
    /// Gestisce il caricamento e la cache delle immagini per ottimizzare la memoria e le performance di rendering.
    /// </summary>
    public static class ImageLoader
    {
        private static readonly Dictionary<string, BitmapImage> _imageCache = new Dictionary<string, BitmapImage>();

        private static BitmapImage? _placeholderImage;

        /// <summary>
        /// Carica un'immagine dal disco, la mette in cache e la restituisce.
        /// Se l'immagine è già in cache, la restituisce immediatamente.
        /// </summary>
        public static BitmapImage LoadImage(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return GetPlaceholder();

            string fullPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath.TrimStart('/', '\\')));

            if (_imageCache.ContainsKey(fullPath))
            {
                return _imageCache[fullPath];
            }

            if (File.Exists(fullPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);

                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    bitmap.Freeze();

                    _imageCache[fullPath] = bitmap;
                    return bitmap;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ImageLoader] Errore caricamento {fullPath}: {ex.Message}");
                    return GetPlaceholder();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ImageLoader] File non trovato: {fullPath}");
                return GetPlaceholder();
            }
        }

        private static BitmapImage GetPlaceholder()
        {
            if (_placeholderImage != null) return _placeholderImage;

            return null;
        }

        /// <summary>
        /// Pulisce la cache se la memoria diventa critica (da chiamare manualmente se necessario).
        /// </summary>
        public static void ClearCache()
        {
            _imageCache.Clear();
        }
    }
}
