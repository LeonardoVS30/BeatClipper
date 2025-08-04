// COPIA Y PEGA TODO ESTE CÓDIGO EN TU ARCHIVO Form1.cs. BORRA TODO LO ANTERIOR.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using YoutubeExplode;
using System.Net.Http;
using YoutubeExplode.Videos.Streams;

namespace BeatClipper // Asegúrate que este sea el nombre de tu proyecto
{
    public partial class Form1 : Form
    {
        private readonly string _ffmpegPath = "ffmpeg.exe"; // Asumimos que está junto a nuestro .exe

        public Form1()
        {
            InitializeComponent();
        }

        public class MomentoClave
        {
            public TimeSpan Inicio { get; set; }
            public double Puntuacion { get; set; }
        }

        private async void btnAnalizar_Click(object sender, EventArgs e)
        {
            btnAnalizar.Enabled = false;
            txtUrl.Enabled = false;

            try
            {
                // --- PASO 0: VERIFICAR QUE FFMPEG EXISTE ---
                UpdateStatus("Verificando entorno...", 1);
                if (!File.Exists(_ffmpegPath))
                {
                    throw new FileNotFoundException("ffmpeg.exe no se encontró. Por favor, asegúrate de que esté en la misma carpeta que la aplicación. (Sigue los pasos manuales de nuevo).");
                }

                // --- PASO 1: VALIDACIÓN Y PREPARACIÓN ---
                UpdateStatus("Validando URL...", 5);
                string videoUrl = txtUrl.Text;
                if (string.IsNullOrWhiteSpace(videoUrl)) { throw new ArgumentException("La URL no puede estar vacía."); }

                string workingDirectory = Path.Combine(Path.GetTempPath(), "BeatClipper_Temp");
                if (Directory.Exists(workingDirectory)) Directory.Delete(workingDirectory, true);
                Directory.CreateDirectory(workingDirectory);

                // --- PASO 2: DESCARGA DEL VIDEO ---
                UpdateStatus("Conectando con YouTube...", 10);
                var youtube = new YoutubeClient();
                var video = await youtube.Videos.GetAsync(videoUrl);
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
                var muxedStreamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
                if (muxedStreamInfo == null) { throw new InvalidOperationException("No se encontró un stream de video compatible."); }

                string videoFilePath = Path.Combine(workingDirectory, $"video_original.{muxedStreamInfo.Container.Name}");
                UpdateStatus($"Descargando video: {video.Title}...", 20);
                await youtube.Videos.Streams.DownloadAsync(muxedStreamInfo, videoFilePath);

                // --- PASO 3: EXTRACCIÓN DEL AUDIO (LLAMADA DIRECTA) ---
                UpdateStatus("Extrayendo audio para análisis...", 40);
                string audioFilePath = Path.Combine(workingDirectory, "audio_analisis.wav");
                string audioArgs = $"-i \"{videoFilePath}\" -vn -y \"{audioFilePath}\"";
                await RunFfmpegCommand(audioArgs);

                // --- PASO 4: ANÁLISIS DEL AUDIO ---
                UpdateStatus("Analizando la energía del beat...", 60);
                List<MomentoClave> momentosClave = AnalizarAudioParaPicos(audioFilePath);
                if (momentosClave.Count == 0)
                {
                    MessageBox.Show("No se encontraron momentos de alta energía (drops) en el beat.", "Análisis Completo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // --- PASO 5: CREACIÓN DE LOS CLIPS DE VIDEO ---
                UpdateStatus($"Creando {momentosClave.Count} clips...", 80);
                await CrearClipsDeVideo(videoFilePath, momentosClave);

                // --- PASO 6: PROCESO FINALIZADO ---
                UpdateStatus("¡Proceso completado!", 100);
                MessageBox.Show($"¡Éxito! Se han creado {momentosClave.Count} clips en tu carpeta de 'Videos'.", "Proceso Finalizado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ha ocurrido un error inesperado: {ex.Message}", "Error Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Error. Listo para un nuevo intento.", 0);
            }
            finally
            {
                btnAnalizar.Enabled = true;
                txtUrl.Enabled = true;
            }
        }

        private Task<int> RunFfmpegCommand(string arguments)
        {
            var tcs = new TaskCompletionSource<int>();
            var process = new Process { StartInfo = { FileName = _ffmpegPath, Arguments = arguments, UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true }, EnableRaisingEvents = true };
            process.Exited += (sender, args) =>
            {
                if (process.ExitCode == 0) { tcs.SetResult(0); } else { var errorMessage = process.StandardError.ReadToEnd(); tcs.SetException(new InvalidOperationException($"FFmpeg falló con el código {process.ExitCode}:\n{errorMessage}")); }
                process.Dispose();
            };
            process.Start(); return tcs.Task;
        }

        private async Task CrearClipsDeVideo(string videoOriginalPath, List<MomentoClave> momentos)
        {
            string outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "BeatClipper_Clips"); Directory.CreateDirectory(outputDirectory); int clipCount = 1; int duracionClipSegundos = 15;
            foreach (var momento in momentos)
            {
                string outputPath = Path.Combine(outputDirectory, $"clip_{DateTime.Now:yyyyMMdd}_{clipCount++}.mp4"); string startTime = momento.Inicio.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string clipArgs = $"-y -ss {startTime} -i \"{videoOriginalPath}\" -t {duracionClipSegundos} -c copy \"{outputPath}\""; await RunFfmpegCommand(clipArgs);
            }
        }

        private List<MomentoClave> AnalizarAudioParaPicos(string audioFilePath)
        {
            var momentosCandidatos = new List<MomentoClave>(); float umbralEnergiaBase = 0.25f; float factorAumentoDrop = 1.9f; int duracionClipSegundos = 15; int maxClips = 3; var energiasPorSegundo = new List<float>();
            using (var reader = new AudioFileReader(audioFilePath)) { int sampleRate = reader.WaveFormat.SampleRate; int channels = reader.WaveFormat.Channels; int bufferSize = sampleRate * channels; float[] buffer = new float[bufferSize]; int bytesRead; while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0) { float sumaCuadrados = 0; for (int i = 0; i < bytesRead / channels; i++) { sumaCuadrados += buffer[i] * buffer[i]; } float energia = (float)Math.Sqrt(sumaCuadrados / (bytesRead / channels)); energiasPorSegundo.Add(energia); } }
            for (int i = 5; i < energiasPorSegundo.Count; i++) { float energiaActual = energiasPorSegundo[i]; float energiaAnterior = energiasPorSegundo[i - 1]; if (energiaActual > umbralEnergiaBase && energiaAnterior > 0 && (energiaActual / energiaAnterior) > factorAumentoDrop) { momentosCandidatos.Add(new MomentoClave { Inicio = TimeSpan.FromSeconds(i), Puntuacion = energiaActual * (energiaActual / energiaAnterior) }); } }
            if (momentosCandidatos.Count == 0) { for (int i = 5; i < energiasPorSegundo.Count; i++) { if (energiasPorSegundo[i] > umbralEnergiaBase * 1.5) { momentosCandidatos.Add(new MomentoClave { Inicio = TimeSpan.FromSeconds(i), Puntuacion = energiasPorSegundo[i] }); } } }
            var momentosFinales = new List<MomentoClave>(); var momentosOrdenados = momentosCandidatos.OrderByDescending(m => m.Puntuacion).ToList();
            foreach (var momento in momentosOrdenados) { bool seSolapa = momentosFinales.Any(m => Math.Abs((m.Inicio - momento.Inicio).TotalSeconds) < duracionClipSegundos); if (!seSolapa && momentosFinales.Count < maxClips) { momentosFinales.Add(momento); } }
            return momentosFinales.OrderBy(m => m.Inicio).ToList();
        }
        private void UpdateStatus(string message, int progress)
        {
            lblStatus.Text = message; progressBar.Value = progress; Application.DoEvents();
        }
    }
}