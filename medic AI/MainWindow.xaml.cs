using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.CognitiveServices.Speech;
using System.Windows.Media;

namespace medic_AI
{
    public partial class MainWindow : Window
    {
        private bool isListening = false;
        bool Check_IA_Sound = false;
        public MainWindow()
        {
            InitializeComponent();

            // Charger l'image du microphone
            BackgroundVideo.Source = new Uri("C:\\Users\\a\\Desktop\\artificial medical\\medic AI\\medic AI\\videos\\videoback.mp4");
            IMG_IA_Voice.Source = new BitmapImage(new Uri("image/Sound_Off.png", UriKind.Relative));
            IMG_Microphone.Source = new BitmapImage(new Uri("image/Mic_Off.png", UriKind.Relative));
            SymptomInput.Text = "Entrez vos symptomes";
        }

        // Gestion du bouton VoiceInputButton
        private async void VoiceInputButton_Click(object sender, RoutedEventArgs e)
        {
            if (isListening)
            {
                IMG_Microphone.Source = new BitmapImage(new Uri("image/mic3.png", UriKind.Relative));
                isListening = false;
            }
            else
            {
                IMG_Microphone.Source = new BitmapImage(new Uri("image/mic1.png", UriKind.Relative));
                isListening = true;

                await RecognizeSpeechAsync();
                IMG_Microphone.Source = new BitmapImage(new Uri("image/Mic_Off.png", UriKind.Relative));
                isListening = false;
            }
        }

        // Méthode pour reconnaître la parole
        private async Task RecognizeSpeechAsync()
        {
            try
            {
                var config = SpeechConfig.FromSubscription("BUBj7c7pZXJtZRNmJc57zfq3AskR3KQ1QGd24pAWxhSHpaB36QvIJQQJ99BAAC5T7U2XJ3w3AAAYACOGVOqY", "francecentral");
                config.SpeechRecognitionLanguage = "fr-FR"; // Langue configurée en français

                using (var recognizer = new SpeechRecognizer(config))
                {
                    ResultOutput.Text = "Listening for symptoms...";
                    var result = await recognizer.RecognizeOnceAsync();

                    if (result.Reason == ResultReason.RecognizedSpeech)
                    {
                        SymptomInput.Text = result.Text;
                        ResultOutput.Text = "Speech recognized successfully!";
                    }
                    else if (result.Reason == ResultReason.NoMatch)
                    {
                        SymptomInput.Text = "No speech recognized. Please try again.";
                    }
                    else if (result.Reason == ResultReason.Canceled)
                    {
                        var cancellation = CancellationDetails.FromResult(result);
                        SymptomInput.Text = $"Speech recognition canceled: {cancellation.ErrorDetails}";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private async Task SynthesizeTextAsync(string text)
        {
            var config = SpeechConfig.FromSubscription("8wDi561dGZDepjPRwxpAy1qMJjDKB9nxjfHiN0BOmT1booMoIyDBJQQJ99BAAC5T7U2XJ3w3AAAYACOGYdG0", "francecentral");
            config.SpeechSynthesisVoiceName = "fr-FR-DeniseNeural"; // Modifier selon vos préférences

            using (var synthesizer = new SpeechSynthesizer(config))
            {
                var result = await synthesizer.SpeakTextAsync(text);
                if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                {
                    Console.WriteLine("Synthèse vocale terminée.");
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                    Console.WriteLine($"Synthèse vocale annulée : {cancellation.ErrorDetails}");
                }
            }
        }

        // Soumettre les symptômes
        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            string symptoms = SymptomInput.Text;

            if (!string.IsNullOrWhiteSpace(symptoms))
            {
                ResultOutput.Text = "Processing your symptoms...";

                // Définir les chemins exacts
                string projectDirectory = @"C:\Users\a\PycharmProjects\AI_Project\AI_Project";
                string questionFilePath = Path.Combine(projectDirectory, "Question.txt");
                string pythonScriptPath = Path.Combine(projectDirectory, "Disease_Prediction_SaMo.py");
                string dataFilePath = Path.Combine(projectDirectory, "data.json");

                // Écriture des symptômes dans le fichier Question.txt
                try
                {
                    // Écrire les symptômes dans le fichier Question.txt
                    File.WriteAllText(questionFilePath, symptoms);

                    // Exécution du script Python avec un délai d'attente maximal de 3 minutes
                    bool scriptCompleted = await RunPythonScriptWithTimeout(pythonScriptPath, TimeSpan.FromMinutes(3));

                    if (scriptCompleted && File.Exists(dataFilePath))
                    {
                        string predictedDisease = File.ReadAllText(dataFilePath);
                        ResultOutput.Text = $"Diagnostic result: {predictedDisease}";
                    }
                    else
                    {
                        ResultOutput.Text = "The diagnostic result could not be found or the operation timed out. Please try again.";
                    }
                }
                catch (Exception ex)
                {
                    ResultOutput.Text = $"An error occurred: {ex.Message}";
                }
            }
            else
            {
                ResultOutput.Text = "Please enter your symptoms.";
            }
        }

        // Fonction pour exécuter le script Python avec un délai d'attente
        private async Task<bool> RunPythonScriptWithTimeout(string scriptPath, TimeSpan timeout)
        {
            try
            {
                // Définir le chemin vers le fichier data.json
                string dataFilePath = @"C:\Users\a\PycharmProjects\AI_Project\AI_Project\data.json";

                // Supprimer l'ancien fichier data.json s'il existe
                if (File.Exists(dataFilePath))
                {
                    File.Delete(dataFilePath);
                }

                // Définir le chemin complet vers l'exécutable Python
                string pythonExecutable = @"C:\Users\a\PycharmProjects\AI_Project\.venv\Scripts\python.exe"; // Remplacez par le chemin complet vers python.exe

                // Définir le processus pour exécuter le script Python
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pythonExecutable, // Utilisez python.exe
                    Arguments = $"\"{scriptPath}\"", // Passer le chemin complet du script
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false, // Important pour rediriger les flux
                    CreateNoWindow = true,
                };

                using (var process = new System.Diagnostics.Process { StartInfo = psi })
                {
                    // Démarrer le processus
                    process.Start();

                    // Attendre que le processus se termine ou dépasse le délai
                    bool processCompleted = await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds));

                    // Lire les sorties si nécessaire
                    if (processCompleted)
                    {
                        string output = await process.StandardOutput.ReadToEndAsync();
                        string error = await process.StandardError.ReadToEndAsync();
                        Console.WriteLine($"Output: {output}");
                        Console.WriteLine($"Error: {error}");
                    }
                    else
                    {
                        // Forcer l'arrêt du processus en cas de dépassement de délai
                        process.Kill();
                    }

                    return processCompleted;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }


        private void BackgroundVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            BackgroundVideo.Position = TimeSpan.Zero;
            BackgroundVideo.Play();
        }

        private void SymptomInput_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SymptomInput.Text == "Entrez vos symptômes")
            {
                SymptomInput.Text = string.Empty;
                SymptomInput.Foreground = Brushes.White; // La couleur du texte
            }
        }

        private void SymptomInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SymptomInput.Text))
            {
                SymptomInput.Text = "Entrez vos symptômes";
                SymptomInput.Foreground = Brushes.Gray; // La couleur du placeholder
            }
        }


        private async void BTN_IA_Voice_Activation(object sender, RoutedEventArgs e)
        {
            if (Check_IA_Sound)
            {
                IMG_IA_Voice.Source = new BitmapImage(new Uri("image/Sound_Off.png", UriKind.Relative));
                Check_IA_Sound = false;
            }
            else
            {
                IMG_IA_Voice.Source = new BitmapImage(new Uri("image/Sound_On.png", UriKind.Relative));
                Check_IA_Sound = true;

                string textToSpeak = ResultOutput.Text;
                await SynthesizeTextAsync(textToSpeak);
            }
        }
    }
}