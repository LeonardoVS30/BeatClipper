# BeatClipper 🎵✂️

**BeatClipper** es una aplicación de escritorio para Windows, creada en C#, que automatiza la creación de clips cortos y virales (formato 9:16) a partir de videos de type beats de YouTube. La herramienta analiza el audio para detectar los "drops" o momentos de mayor energía, generando clips perfectos para YouTube Shorts, TikTok o Instagram Reels.


<img src="https://github.com/LeonardoVS30/BeatClipper/blob/29ccdfab78d08fa6bf3af4f8db36fefe7b2afb77/Beatclipper_Demo.png" align="center" width="80%">

---

## ¿Qué es BeatClipper?

Como productor de type beats, sabes que promocionar tu música en redes sociales es clave. Crear clips cortos y atractivos de tus mejores beats puede llevar mucho tiempo: tienes que escuchar el beat completo, encontrar el mejor fragmento, abrir un editor de video, cortar, formatear...

**BeatClipper resuelve este problema.** Simplemente pega la URL de tu video de YouTube, haz clic en un botón, y la aplicación hará todo el trabajo por ti:

1.  **Descarga** el video en la mejor calidad disponible.
2.  **Analiza** la onda de audio para identificar los picos de energía (el "drop").
3.  **Crea** automáticamente varios clips de 15 segundos centrados en esos momentos.
4.  **Formatea** los clips a una proporción de 9:16 con un efecto visual de "mirilla", listos para subir.

## ✨ Características Principales

-   **Análisis Automático:** No más búsquedas manuales. El algoritmo heurístico encuentra las partes más potentes de tu beat.
-   **Descarga Directa desde YouTube:** Utiliza `yt-dlp` para una descarga robusta y fiable.
-   **Procesamiento con FFmpeg:** Se apoya en el estándar de la industria para el procesamiento de video, garantizando alta calidad.
-   **Formato Vertical (9:16):** Genera clips con el efecto de "mirilla" circular, optimizados para plataformas móviles.
-   **Interfaz Sencilla:** Diseñado para ser intuitivo y fácil de usar. ¡Pega, haz clic y listo!

## 🛠️ Tecnologías Utilizadas

-   **Lenguaje:** C#
-   **Framework:** .NET Framework 4.8
-   **Interfaz:** Windows Forms
-   **Librerías Clave:**
    -   `NAudio`: Para el análisis de la forma de onda del audio.
-   **Herramientas Externas:**
    -   `yt-dlp.exe`: Para la descarga de videos de YouTube.
    -   `ffmpeg.exe`: Para la extracción de audio y la creación/formateo de los clips de video.

## 🚀 Cómo Empezar

Para ejecutar este proyecto en tu propia máquina, sigue estos pasos:

### Prerrequisitos

-   Visual Studio 2022 con la carga de trabajo de ".NET desktop development".
-   .NET Framework 4.8 (generalmente incluido con Visual Studio).

### Instalación

1.  **Clona el repositorio:**
    ```bash
    git clone https://github.com/TU_USUARIO/BeatClipper.git
    ```

2.  **Abre la solución** (`BeatClipper.sln`) en Visual Studio.

3.  **Descarga las herramientas externas:**
    -   **FFmpeg:** Descarga `ffmpeg.exe` desde [gyan.dev](https://www.gyan.dev/ffmpeg/builds/) (la versión "essentials" es suficiente).
    -   **yt-dlp:** Descarga `yt-dlp.exe` desde la [página oficial de releases en GitHub](https://github.com/yt-dlp/yt-dlp/releases/latest).

4.  **Añade las herramientas al proyecto (¡Paso Crucial!):**
    -   En el Explorador de Soluciones de Visual Studio, haz clic derecho en el proyecto `BeatClipper` > `Agregar` > `Elemento existente...` y selecciona los archivos `ffmpeg.exe` y `yt-dlp.exe`.
    -   Para cada uno de los dos archivos (`.exe`) que acabas de añadir:
        -   Haz clic sobre el archivo en el Explorador de Soluciones.
        -   En la ventana de **Propiedades**, cambia la opción **"Copiar en el directorio de resultados"** a **"Copiar si es posterior"**.

5.  **Restaura los Paquetes NuGet:** Visual Studio debería hacerlo automáticamente. Si no, haz clic derecho en la solución y selecciona "Restaurar paquetes NuGet".

6.  **¡Ejecuta el proyecto!** Presiona `F5` o el botón de "Iniciar".

## 📋 Uso

1.  Ejecuta la aplicación.
2.  Busca un video de un type beat en YouTube y copia su URL.
3.  Pega la URL en el campo de texto.
4.  Haz clic en "Analizar y Crear Clips".
5.  Espera a que el proceso termine.
6.  ¡Encuentra tus clips listos para subir en la carpeta `Videos\BeatClipper_Clips`!

## 💡 Futuras Mejoras

Este proyecto es una base sólida con mucho potencial de crecimiento. Algunas ideas para el futuro:

-   [ ] Permitir al usuario seleccionar la carpeta de salida.
-   [ ] Previsualizar los momentos detectados antes de generar los clips.
-   [ ] Ofrecer diferentes estilos visuales (zoom, barras negras, etc.).
-   [ ] Añadir una marca de agua personalizada.
-   [ ] Permitir el procesamiento de archivos de video locales.

## 📄 Licencia

Este proyecto está bajo la Licencia MIT. Consulta el archivo `LICENSE` para más detalles.

## 🙏 Agradecimientos

-   A los equipos detrás de **FFmpeg** y **yt-dlp** por crear herramientas tan increíbles y versátiles.
-   Al proyecto **NAudio** por facilitar el procesamiento de audio en .NET.
