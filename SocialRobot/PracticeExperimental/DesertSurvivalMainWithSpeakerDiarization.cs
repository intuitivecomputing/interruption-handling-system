using System;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Speech;
using Microsoft.Psi.Interop.Format;
using Microsoft.Psi.Interop.Transport;
using Microsoft.Psi.CognitiveServices.Speech;
using Newtonsoft.Json;


namespace SocialRobot
{
    public class ResponseSegment
    {
        public string responseSegment { get; set; }
        public string receiver { get; set; }
    }

    class DesertSurvivalMain
    {
        static string speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY", EnvironmentVariableTarget.User);
        static string speechRegion = Environment.GetEnvironmentVariable("SPEECH_REGION", EnvironmentVariableTarget.User);

        public static string folderName = ConfigurationManager.AppSettings["Folder"];

        // Flags
        public static string robotFaceState = "";
        public static bool isUpdateRobotFace = false;

        public static bool isLLMProcessing = false;

        public static bool isRobotTalking = false;
        public static bool robotWantToTalk = false; 
        public static bool isRobotInterrupted = false;

        public static bool isLLMOutputReady = false;

        public static bool isRobotSpeechSynthesized = false;

        public static List<byte> aggregatedAudio = new List<byte>();
        public static List<string> robotSpeechSegments = new List<string>();
        public static List<string> robotSpeechReceivers = new List<string>();

        static void Main(string[] args)
        {
            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                var storeName = "trial";
                var storeAddress = @"C:\Users\scao1\OneDrive\Desktop\socialRobotPersonalityTailoring\data\" + folderName;
                var store = PsiStore.Create(p, storeName, storeAddress);

                // Port to connect to face 
                var faceWriter = new NetMQWriter<string>(p, "facialExpressionsBlinking", "tcp://127.0.0.1:12345", MessagePackFormat.Instance);

                // Create stream with robotFaceState
                var robotFaceStream = Generators.Repeat(p, 0, TimeSpan.FromTicks(500000)).Select(m => { return robotFaceState; });
                robotFaceStream.Write("robotFaceState", store);

                // Create stream with isUpdateRobotFace Tag
                var tempUpdateRobotFaceStream = Generators.Repeat(p, 0, TimeSpan.FromTicks(500000)).Select(m => { return isUpdateRobotFace; });
                var tempUpdateRobotFaceSyncedStream = tempUpdateRobotFaceStream.Join(robotFaceStream, RelativeTimeInterval.Past());
                var updateRobotFaceStream = tempUpdateRobotFaceSyncedStream.Select(m => { var (l, robotFace) = m; return l; });
                updateRobotFaceStream.Write("updateRobotFace", store);

                // Create stream with isLLMProcessing Tag
                var tempLLMProcessingStream = Generators.Repeat(p, 0, TimeSpan.FromTicks(500000)).Select(m => { return isLLMProcessing; });
                var tempLLMProcessingSyncedStream = tempLLMProcessingStream.Join(robotFaceStream, RelativeTimeInterval.Past());
                var isLLMProcessingStream = tempLLMProcessingSyncedStream.Select(m => { var (l, robotFace) = m; return l; });
                isLLMProcessingStream.Write("isLLMProcessing", store);

                // Create stream with isRobotTalking Tag
                var tempIsRobotTalkingStream = Generators.Repeat(p, 0, TimeSpan.FromTicks(500000)).Select(m => { return isRobotTalking; });
                var tempIsRobotTalkingSyncedStream = tempIsRobotTalkingStream.Join(robotFaceStream, RelativeTimeInterval.Past());
                var isRobotTalkingStream = tempIsRobotTalkingSyncedStream.Select(m => { var (l, robotFace) = m; return l; });
                isRobotTalkingStream.Write("isRobotTalking", store);

                // Create stream with robotWantToTalk Tag
                var tempRobotWantToTalkStream = Generators.Repeat(p, 0, TimeSpan.FromTicks(500000)).Select(m => { return robotWantToTalk; });
                var tempRobotWantToTalkSyncedStream = tempRobotWantToTalkStream.Join(robotFaceStream, RelativeTimeInterval.Past());
                var robotWantToTalkStream = tempRobotWantToTalkSyncedStream.Select(m => { var (l, robotFace) = m; return l; });
                robotWantToTalkStream.Write("robotWantToTalk", store);

                // Create stream of robot speech segments 
                var tempRobotSpeechSegmentsStream = Generators.Repeat(p, 0, TimeSpan.FromTicks(500000)).Select(m => { return robotSpeechSegments; });
                var tempRobotSpeechSegmentsStreamSyncedStream = tempRobotSpeechSegmentsStream.Join(robotFaceStream, RelativeTimeInterval.Past());
                var robotSpeechSegmentsStream = tempRobotSpeechSegmentsStreamSyncedStream.Select(m => { var (l, robotFace) = m; return l; });
                robotSpeechSegmentsStream.Write("robotSpeechSegments", store);

                // Create stream of robot speech receivers 
                var tempRobotSpeechReceiversStream = Generators.Repeat(p, 0, TimeSpan.FromTicks(500000)).Select(m => { return robotSpeechReceivers; });
                var tempRobotSpeechReceiversStreamSyncedStream = tempRobotSpeechReceiversStream.Join(robotFaceStream, RelativeTimeInterval.Past());
                var robotSpeechReceiversStream = tempRobotSpeechReceiversStreamSyncedStream.Select(m => { var (l, robotFace) = m; return l; });
                robotSpeechReceiversStream.Write("robotSpeechReceivers", store);

                // Create stream of isRobotSpeechSynthesized Tag
                var tempIsRobotSpeechSynthesizedStream = Generators.Repeat(p, 0, TimeSpan.FromTicks(500000)).Select(m => { return isRobotSpeechSynthesized; });
                var tempIsRobotSpeechSynthesizedSyncedStream = tempIsRobotSpeechSynthesizedStream.Join(robotFaceStream, RelativeTimeInterval.Past());
                var isRobotSpeechSynthesizedStream = tempIsRobotSpeechSynthesizedSyncedStream.Select(m => { var (l, robotFace) = m; return l; });
                isRobotSpeechSynthesizedStream.Write("isRobotSpeechSynthesized", store);


                // TODO: Port to connect to task web app 
                // TODO: Port to connect to head motorc

                var audioSource = new AudioCapture(p, new AudioCaptureConfiguration()
                {
                    //DeviceName = "Microphone (USB PnP Audio Device)",
                    //DeviceName = "Microphone (2- USBAudio1.0)",
                    DeviceName = "Microphone (TKGOU PnP USB Microphone)",
                    OptimizeForSpeech = true,
                    Format = WaveFormat.Create16kHz1Channel16BitPcm()
                });
                audioSource.Write("audio", store);

                // Sync Audio Stream 
                var tempAudioSyncedStream = audioSource.Join(robotFaceStream, RelativeTimeInterval.Past());
                var audioStream = tempAudioSyncedStream.Select(m => { var (l, robotFace) = m; return l; });

                // Create aggregated audio stream for Speech2Text
                var tempAggregatedAudioStream = Generators.Repeat(p, 0, TimeSpan.FromTicks(80000)).Select(m => { return aggregatedAudio; });
                var tempAggregatedAudioSyncedStream = tempAggregatedAudioStream.Join(robotFaceStream, RelativeTimeInterval.Past());
                var aggregatedAudioStream = tempAggregatedAudioSyncedStream.Select(m => { var (aggA, a) = m; return aggA; });
                aggregatedAudioStream.Write("AggregatedAudio", store);

                // Acoustic Features Extracor for Voice Activity Detection
                var acousticFeaturesExtractor = new AcousticFeaturesExtractor(p);
                audioSource.PipeTo(acousticFeaturesExtractor);

                // Log Energy
                var audioLogEnergy = acousticFeaturesExtractor.LogEnergy;
                audioLogEnergy.Write("audioLogEnergy", store);

                // Create filtered signal by aggregating over historical buffers
                var vadWithHistory = acousticFeaturesExtractor.LogEnergy
                    .Window(RelativeTimeInterval.Future(TimeSpan.FromMilliseconds(100)))
                    .Aggregate(false, (previous, buffer) => (!previous && buffer.All(v => v > 7)) || (previous && !buffer.All(v => v < 7)));


                //vadWithHistory.Where(v => v == true).Do(m =>
                //{
                //    Console.WriteLine("Speaker Talking");
                //});

                // Create the stream that detects transitions from True to False
                var vadStartStream = vadWithHistory.Window(-1, 0).Select(vadPair => !vadPair[0] && vadPair[1]);
                var vadEndStream = vadWithHistory.Window(-1, 0).Select(vadPair => vadPair[0] && !vadPair[1]);

                //vadEndStream.Where(v => v == true).Do(m =>
                //{
                //    Console.WriteLine("Speaker Done Talking");
                //});
                //vadWithHistory.Write("VADFiltered", store);

                var audioWithVAD = audioStream.Join(vadWithHistory, RelativeTimeInterval.Past());

                audioWithVAD.Where(m => m.Item2 == true).Do(a => 
                {
                    if (a.Item1.Data != null)
                    {
                        aggregatedAudio.AddRange(a.Item1.Data);
                    }
                });

                var azureSpeech2TextComponent = new AzureSpeech2Text(p);
                vadEndStream.Where(v => v == true).Select(a => {
                    //Add 1 second of silence to end of aggregatedAudio
                    aggregatedAudio.AddRange(new byte[64000]);
                    var audioArray = aggregatedAudio.ToArray();
                    aggregatedAudio.Clear();
                    //aggregatedAudio = new List<byte>();
                    return audioArray;
                }
                ).PipeTo(azureSpeech2TextComponent.In);
                vadStartStream.Write("VADStartStream", store);
                vadEndStream.Write("VADEndStream", store);

                var voiceOutput = azureSpeech2TextComponent.Out;
                voiceOutput.Write("speech2Text", store);
                voiceOutput.Do(m =>
                {
                    Console.WriteLine("Transcribed Output:");
                    Console.WriteLine(m);
                });

                var llmResponseComponent = new LLMResponseComponent(p);
                var findResponseStream = voiceOutput.Join(isLLMProcessingStream, RelativeTimeInterval.Past());

                // Guest-2 is robot
                findResponseStream.Where(m => m.Item2 == false).Where(m => m.Item1.StartsWith("Guest-2") == false).Select(text => {return text.Item1;}).Do(m => { isLLMProcessing = true; }).PipeTo(llmResponseComponent.In);

                //var llmResponseOutput = llmResponseComponent.Out.Where(m => m != "");
                //llmResponseOutput.Do(m => Console.WriteLine("LLM Output"));
                //llmResponseOutput.Do(m => Console.WriteLine(m));
                //llmResponseOutput.Write("Response", store);

                //llmResponseOutput.Do(m => { isLLMProcessing = false; });


                var llmResponseOutputString = llmResponseComponent.Out.Where(m => m != "");
                llmResponseOutputString.Write("Response", store);
                llmResponseOutputString.Do(m => { isLLMProcessing = false; });

                var llmResponseOutput = llmResponseOutputString.Select(m => JsonConvert.DeserializeObject<Dictionary<string, string>>(m));
                llmResponseOutput.Do(m =>
                {
                    Console.WriteLine(m["answer"]);
                    Console.WriteLine(m["interrupt"]);

                    string jsonString = m["responseList"];
                    List<ResponseSegment> responseSegmentsList = JsonConvert.DeserializeObject<List<ResponseSegment>>(jsonString);
                    if (responseSegmentsList != null)
                    {
                        if (responseSegmentsList.Count > 0)
                        {
                            robotWantToTalk = true;

                            foreach (var segment in responseSegmentsList)
                            {
                                robotSpeechSegments.Add(segment.responseSegment);
                                robotSpeechReceivers.Add(segment.receiver);
                            }

                            Console.WriteLine("Response Segments:");
                            foreach (var segment in robotSpeechSegments)
                            {
                                Console.WriteLine(segment);
                            }

                            Console.WriteLine("\nReceivers:");
                            foreach (var receiver in robotSpeechReceivers)
                            {
                                Console.WriteLine(receiver);
                            }
                        }
                    }
                    else
                    {
                        // no response returned 
                        Console.WriteLine("Robot have nothing to say.");
                    }

                });
                llmResponseOutput.Write("llmOutput", store);

                var tempRobotSpeechTags = robotWantToTalkStream.Join(isRobotTalkingStream, RelativeTimeInterval.Past());
                var tempRobotSpeechSegments = tempRobotSpeechTags.Join(robotSpeechSegmentsStream, RelativeTimeInterval.Past());
                var tempRobotSpeechReceiver = tempRobotSpeechSegments.Join(robotSpeechReceiversStream, RelativeTimeInterval.Past());
                var robotSpeech = tempRobotSpeechReceiver.Join(isRobotSpeechSynthesizedStream, RelativeTimeInterval.Past());

                var synthesizer = new SystemSpeechSynthesizer(p);
                //var synthesizer = new SystemSpeechSynthesizer(
                //p,
                //new SystemSpeechSynthesizerConfiguration
                //{
                //    Voice = "Microsoft Zira Desktop"
                //});

                var player = new AudioPlayer(p);
                
                robotSpeech.Where(m => m.Item1 == true && m.Item2 == false && m.Item3.Count > 0 && m.Item5 == false)
                    .Select(text => { return robotSpeechSegments[0]; })
                    .Do(m => { isRobotSpeechSynthesized = true; })
                    .PipeTo(synthesizer);

                robotSpeech.Where(m => m.Item1 == true && m.Item2 == false && m.Item3.Count > 0 && m.Item5 == true).Do(m =>
                {
                    isRobotTalking = true;

                    var robotSpeechContent = robotSpeechSegments[0];
                    robotSpeechSegments.RemoveAt(0);

                    Console.WriteLine("Response Segments:");
                    foreach (var segment in robotSpeechSegments)
                    {
                        Console.WriteLine(segment);
                    }

                    if (robotSpeechSegments.Count > 0)
                    {
                        robotWantToTalk = true;
                    }

                    Console.WriteLine("Generated.");
                    int averageSpeakingRate = 140;
                    int wordCount = robotSpeechContent.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

                    // Estimate speech duration
                    double estimatedDurationMinutes = (double)wordCount / averageSpeakingRate;

                    // Convert minutes to seconds
                    int estimatedDurationSeconds = (int)(estimatedDurationMinutes * 60);

                    Task.Run(async () =>
                    {
                        // Create an instance of the TimerComponent
                        var timerComponent = new TimerComponent();

                        Console.WriteLine("Timer starts.");

                        // Start the timer and define the completion callback
                        await timerComponent.StartTimer(estimatedDurationSeconds, () =>
                        {
                            // This code block will be executed when the timer completes
                            Console.WriteLine("Timer is done.");
                            isRobotTalking = false; 
                        });
                    }).Wait();
                });

                synthesizer.Where(m => isRobotTalking = true).PipeTo(player);

                p.Diagnostics.Write("Diagnostics", store);

                p.RunAsync();

                // Wait for the user to hit a key before closing the pipeline
                Console.ReadKey();

                // Close the pipeline
                p.Dispose();
            }
        }
    }
}