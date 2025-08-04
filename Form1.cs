// COPIA Y PEGA TODO ESTE CÓDIGO EN TU ARCHIVO Form1.cs. BORRA TODO LO ANTERIOR.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace BeatClipper // Asegúrate que este sea el nombre de tu proyecto
{
    public partial class Form1 : Form
    {
        private readonly string _ffmpegPath = "ffmpeg.exe";
        private readonly string _ytDlpPath = "yt-dlp.exe";

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
                // PASO 0: VERIFICAR HERRAMIENTAS
                UpdateStatus("Verificando entorno...", 1);
                if (!File.Exists(_ffmpegPath)) { throw new FileNotFoundException("ffmpeg.exe no se encontró. Asegúrate de que esté configurado como 'Copiar si es posterior'."); }
                if (!File.Exists(_ytDlpPath)) { throw new FileNotFoundException("yt-dlp.exe no se encontró. Asegúrate de que esté configurado como 'Copiar si es posterior'."); }

                // PASO 1: VALIDACIÓN Y PREPARACIÓN
                UpdateStatus("Validando URL...", 5);
                string videoUrl = txtUrl.Text;
                if (string.IsNullOrWhiteSpace(videoUrl)) { throw new ArgumentException("La URL no puede estar vacía."); }

                string workingDirectory = Path.Combine(Path.GetTempPath(), "BeatClipper_Temp");
                if (Directory.Exists(workingDirectory)) Directory.Delete(workingDirectory, true);
                Directory.CreateDirectory(workingDirectory);

                // PASO 2: DESCARGA DEL VIDEO CON YT-DLP
                UpdateStatus("Descargando video con yt-dlp...", 10);
                string videoFilePath = Path.Combine(workingDirectory, "downloaded_video.mp4");
                string downloadArgs = $"-f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best\" -o \"{videoFilePath}\" \"{videoUrl}\"";
                await RunExternalProcessAsync(_ytDlpPath, downloadArgs);

                if (!File.Exists(videoFilePath)) { throw new FileNotFoundException("yt-dlp no pudo descargar el video. Revisa la URL o tu conexión."); }

                // PASO 3: EXTRACCIÓN DEL AUDIO
                UpdateStatus("Extrayendo audio para análisis...", 40);
                string audioFilePath = Path.Combine(workingDirectory, "audio_analisis.wav");
                string audioArgs = $"-i \"{videoFilePath}\" -vn -y \"{audioFilePath}\"";
                await RunExternalProcessAsync(_ffmpegPath, audioArgs);

                // PASO 4: ANÁLISIS DEL AUDIO
                UpdateStatus("Analizando la energía del beat...", 60);
                List<MomentoClave> momentosClave = AnalizarAudioParaPicos(audioFilePath);
                if (momentosClave.Count == 0)
                {
                    MessageBox.Show("No se encontraron momentos de alta energía (drops) en el beat.", "Análisis Completo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // PASO 5: CREACIÓN DE LOS CLIPS DE VIDEO
                UpdateStatus($"Creando {momentosClave.Count} clips...", 80);
                await CrearClipsDeVideo(videoFilePath, momentosClave);

                // PASO 6: PROCESO FINALIZADO
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

        private Task<int> RunExternalProcessAsync(string executablePath, string arguments)
        {
            var tcs = new TaskCompletionSource<int>();
            var errorOutput = new StringBuilder();
            var process = new Process { StartInfo = { FileName = executablePath, Arguments = arguments, UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true }, EnableRaisingEvents = true };
            process.OutputDataReceived += (sender, args) => { /* No hacemos nada con la salida estándar */ };
            process.ErrorDataReceived += (sender, args) => { if (args.Data != null) errorOutput.AppendLine(args.Data); };
            process.Exited += (sender, args) => { Task.Delay(100).ContinueWith(_ => { if (process.ExitCode == 0) { tcs.TrySetResult(0); } else { tcs.TrySetException(new InvalidOperationException($"{Path.GetFileName(executablePath)} falló con el código {process.ExitCode}:\n{errorOutput.ToString()}")); } process.Dispose(); }); };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            return tcs.Task;
        }

        private async Task CrearClipsDeVideo(string videoOriginalPath, List<MomentoClave> momentos)
        {
            string outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "BeatClipper_Clips");
            Directory.CreateDirectory(outputDirectory);
            int clipCount = 1;
            int duracionClipSegundos = 15;

            foreach (var momento in momentos)
            {
                string outputPath = Path.Combine(outputDirectory, $"clip_{DateTime.Now:yyyyMMdd}_{clipCount++}.mp4");
                string startTime = momento.Inicio.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);

                // --- ESTA ES LA CADENA DE FILTROS CORRECTA Y DEFINITIVA ---
                // 1. crop=ih:ih -> Recorta un cuadrado del centro del video (alto x alto).
                // 2. vignette -> Aplica el efecto circular a ese cuadrado.
                // 3. pad=1080:1920 -> Coloca el resultado en un lienzo vertical de 1080p.
                string clipArgs = $"-y -ss {startTime} -i \"{videoOriginalPath}\" -t {duracionClipSegundos} -vf \"crop=ih:ih,vignette='PI/4',pad=1080:1920:(ow-iw)/2:(oh-ih)/2:color=black\" -preset veryfast -c:a copy \"{outputPath}\"";

                await RunExternalProcessAsync(_ffmpegPath, clipArgs);
            }
        }

        private List<MomentoClave> AnalizarAudioParaPicos(string audioFilePath)
        {
            var momentosCandidatos = new List<MomentoClave>(); float umbralEnergiaBase = 0.25f; float factorAumentoDrop = 1.9f; int duracionClipSegundos = 15; int maxClips = 3; double retrasoInicioMs = 300;
            var energiasPorSegundo = new List<float>();
            using (var reader = new AudioFileReader(audioFilePath)) { int sampleRate = reader.WaveFormat.SampleRate; int channels = reader.WaveFormat.Channels; int bufferSize = sampleRate * channels; float[] buffer = new float[bufferSize]; int bytesRead; while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0) { float sumaCuadrados = 0; for (int i = 0; i < bytesRead / channels; i++) { sumaCuadrados += buffer[i] * buffer[i]; } float energia = (float)Math.Sqrt(sumaCuadrados / (bytesRead / channels)); energiasPorSegundo.Add(energia); } }
            for (int i = 5; i < energiasPorSegundo.Count; i++) { float energiaActual = energiasPorSegundo[i]; float energiaAnterior = energiasPorSegundo[i - 1]; if (energiaActual > umbralEnergiaBase && energiaAnterior > 0 && (energiaActual / energiaAnterior) > factorAumentoDrop) { TimeSpan inicioConRetraso = TimeSpan.FromSeconds(i).Add(TimeSpan.FromMilliseconds(retrasoInicioMs)); momentosCandidatos.Add(new MomentoClave { Inicio = inicioConRetraso, Puntuacion = energiaActual * (energiaActual / energiaAnterior) }); } }
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