using System;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using System.Media;

using NetMQ;
using NetMQ.Sockets;

using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Interop.Format;
using Microsoft.Psi.Interop.Transport;
using Newtonsoft.Json;

namespace SurvivalExperimental
{
    public class robotBehavior
    {
        public string robotSpeechContent { get; set; }
        public string wordCount { get; set; }
        public string duration { get; set; }
        public string robotFacialExpression { get; set; }
        public string robotHeadOrientation { get; set; }
        public string changeItemList { get; set; }
        public string itemList { get; set; }
    }

    class SurvivalExperimental
    {
        public static string storeName = "experimental-desert-survival-study19";
        public static string storeAddress = @"C:\Users\scao1\OneDrive\Desktop\socialRobotPersonalityTailoring\data\";

        // wakeWords (all lowercase) 
        public static List<string> wakeWords = new List<string> { "luna" };

        // Robot Head Movements Bank
        public static List<string> possibleHeadMovements = new List<string> { "lookAtUser", "lookAtScreen", "lookAway", "nod", "doubleNod", "startGazeAversion", "thinking" };

        // Robot Facial Expressions Bank
        public static List<string> possibleFaces = new List<string> { "neutral", "satisfied", "happy", "surprised", "interested", "excited", "thinking", "startTalking", "stopTalking", "reset" };

        // LLM Processing
        public static bool isLLMProcessing = false;

        // Robot speech synthesized 
        public static bool isReady = false;

        // Robot speech interrupted
        public static bool isRobotInterrupted = false;

        // Robot is talking
        public static bool isRobotTalking = false;
        public static string currRobotSpeechContent = "";
        public static string currRobotBehaviorString = "";

        public static double currRobotSpeechDurationLeft = 0.0;
        public static double currRobotSpeechDurationCompleted = 0.0;

        // Robot turn duration 
        public static double robotTurnDuration = 0.0;

        // Robot face state
        public static string robotFaceState = "";

        // Robot head orientation state
        public static string robotHeadState = "";
        public static bool isGazeAversion = false;

        // Sound Player
        public static SoundPlayer heardAckPlayer = new SoundPlayer();

        public static CancellationTokenSource cts = new CancellationTokenSource();

        // Event 1: self-introduction 
        // Event 2: funny story 
        public static bool isEvent = false;
        public static int eventNum = 0;
        public static int eventTime1 = 0;
        public static int eventTime2 = 210; //210 is right

        public static string itemList = "";
        public static string userUpdatedItemList = "";

        public static bool isStarted = false;
        public static bool isSubmitted = false;
        public static bool isDone = false;

        public static readonly HttpClient client = new HttpClient();

        // Stopwatch to measure elapsed time since the first message is received
        public static Stopwatch stopwatch = new Stopwatch();

        public static Stopwatch stopwatchRobotSpeech = new Stopwatch();

        public static string newestUserSpeechContent = "";

        public static List<(string Time, string Role, string Content)> systemLog = new List<(string Time, string Role, string Content)>();

        public static PublisherSocket headSocket;

        static void Main(string[] args)
        {
            // Attach the event handler for process exit
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            Console.CancelKeyPress += new ConsoleCancelEventHandler(OnCancelKeyPress);


            using (var p = Pipeline.Create(enableDiagnostics: true))
            {
                // Initialize the headSocket
                headSocket = new PublisherSocket();
                headSocket.Bind("tcp://127.0.0.1:12348");

                var store = PsiStore.Create(p, storeName, storeAddress);

                // Create netMQWriter for communicating with robot face 
                var faceWriter = new NetMQWriter<string>(p, "facialExpressions", "tcp://127.0.0.1:3000", MessagePackFormat.Instance);

                // Create stream for robot face state
                var robotFaceStateStream = Generators.Repeat(p, 0, TimeSpan.FromTicks(50000)).Select(m => { return robotFaceState; });
                robotFaceStateStream.Write("robotFaceState", store);

                // Create stream for robot head state
                var robotHeadStateStream = Generators.Repeat(p, 0, TimeSpan.FromTicks(50000)).Select(m => { return robotHeadState; });
                robotHeadStateStream.Write("robotHeadState", store);

                // Create stream for item list 
                var itemListStream = Generators.Repeat(p, 0, TimeSpan.FromTicks(50000)).Select(m => { return itemList; });
                itemListStream.Write("itemList", store);

                // Create stream with isLLMProcessing Tag
                var isLLMProcessingStream = Generators.Repeat(p, 0, TimeSpan.FromTicks(50000)).Select(m => { return isLLMProcessing; });
                isLLMProcessingStream.Write("isLLMProcessing", store);

                // Create stream with isRobotTalking Tag
                var isRobotTalkingStream = Generators.Repeat(p, 0, TimeSpan.FromTicks(50000)).Select(m => { return isRobotTalking; });
                isRobotTalkingStream.Write("isRobotTalking", store);

                // Create stream with isRobotInterrupted Tag
                var isRobotInterruptedStream = Generators.Repeat(p, 0, TimeSpan.FromTicks(50000)).Select(m => { return isRobotInterrupted; });
                isRobotInterruptedStream.Write("isRobotInterrupted", store);

                var transcribedAudioStream = new NetMQSource<string>(p, "transcribedAudio", "tcp://127.0.0.1:12345", MessagePackFormat.Instance);
                transcribedAudioStream.Write("transcribedAudio", store);
                transcribedAudioStream.Do(x => {
                    Console.WriteLine(stopwatch.Elapsed.ToString() + ": " + x);
                    systemLog.Add((stopwatch.Elapsed.ToString(), "[server-transcribedAudio]", x));
                });

                var wakewordDetectionStream = new NetMQSource<string>(p, "wakeWordDetected", "tcp://127.0.0.1:12346", MessagePackFormat.Instance);
                wakewordDetectionStream.Write("wakewordDetected", store);
                wakewordDetectionStream.Do(x => {
                    Console.WriteLine(x);
                    systemLog.Add((stopwatch.Elapsed.ToString(), "[server-wakewordDetected]", x));
                });

                var speechDetectionStream = new NetMQSource<string>(p, "speechDetected", "tcp://127.0.0.1:12346", MessagePackFormat.Instance);
                speechDetectionStream.Write("speechDetected", store);
                speechDetectionStream.Do(x => {
                    Console.WriteLine(x);
                    
                    // always looks at user once speech is detected
                    moveRobotHead("lookAtUser");

                    systemLog.Add((stopwatch.Elapsed.ToString(), "[server-speechDetected]", x));
                });

                var itemListRecieverStream = new NetMQSource<string>(p, "updateItems", "tcp://localhost:12347", JsonFormat.Instance);
                itemListRecieverStream.Do(m =>
                {
                    systemLog.Add((stopwatch.Elapsed.ToString(), "[server-updateItems]", m));

                    if (m != itemList)
                    {
                        Console.WriteLine("new items: " + m);
                        itemList = m;
                        userUpdatedItemList = m;
                    }
                }
                );

                var isStartedStream = new NetMQSource<string>(p, "isStarted", "tcp://localhost:12347", JsonFormat.Instance);
                isStartedStream.Do(x =>
                {
                    systemLog.Add((stopwatch.Elapsed.ToString(), "[server-isStarted]", "Task Started"));

                    if (!stopwatch.IsRunning)
                    {
                        stopwatch.Start();
                    }

                    Console.WriteLine(x);
                    isStarted = true;

                    if (eventNum == 0)
                    {
                        eventNum = 1;
                        isEvent = true;
                    }
                }
                );

                var isSubmittedStream = new NetMQSource<string>(p, "isFinished", "tcp://localhost:12347", JsonFormat.Instance);
                isSubmittedStream.Do(x =>
                {
                    systemLog.Add((stopwatch.Elapsed.ToString(), "[server-isSubmitted]", "Task Submitted"));

                    if (!stopwatch.IsRunning)
                    {
                        stopwatch.Stop();
                    }

                    Console.WriteLine(x);
                    isSubmitted = true;
                }
                );

                var wakewordDetection = wakewordDetectionStream.Join(isRobotTalkingStream, RelativeTimeInterval.Past());
                wakewordDetection.Where(m => isRobotTalking == true).Where(m => m.Item1.Length > 0 && eventNum != 0).Select(m => m.Item2).Do(m =>
                {
                    if (m)
                    {
                        // Pause robot speech
                        updateRobotFace("reset");
                        moveRobotHead("lookAtUser");

                        Console.WriteLine("wakeword detected");
                        heardAckPlayer.Stop();

                        if (cts != null && !cts.IsCancellationRequested)
                        {
                            cts.Cancel();
                        }

                        if (!stopwatchRobotSpeech.IsRunning)
                        {
                            stopwatchRobotSpeech.Stop();
                        }

                        systemLog.Add((stopwatch.Elapsed.ToString(), "[system]", "wakeword detected; robot stopped talking"));

                        isRobotInterrupted = true;
                    }
                });

                // Overlap detection 
                var speechDetection = speechDetectionStream.Join(isRobotTalkingStream, RelativeTimeInterval.Past());
                speechDetection.Where(m => isRobotTalking == true && isStarted == true).Where(m => m.Item1.Length > 1 && eventNum != 0).Do(m =>
                {
                    if (isRobotTalking)
                    {
                        Console.WriteLine("Overlap Detected");

                        currRobotSpeechDurationCompleted = stopwatchRobotSpeech.Elapsed.TotalSeconds;
                        currRobotSpeechDurationLeft = robotTurnDuration - currRobotSpeechDurationCompleted;
                        if (currRobotSpeechDurationCompleted < 1)
                        {
                            Console.WriteLine("interruption too early");
                            systemLog.Add((stopwatch.Elapsed.ToString(), "[system]", "interrupted less than 1 second into robot speech"));
                        }
                        else if (currRobotSpeechDurationLeft < 2)
                        {
                            Console.WriteLine("Let me finish. Turn almost done!");
                            systemLog.Add((stopwatch.Elapsed.ToString(), "[system]", "less than 2 seconds of robot speech left"));
                        }
                        else
                        {
                            Console.WriteLine("Robot speech paused");

                            if (!stopwatchRobotSpeech.IsRunning)
                            {
                                stopwatchRobotSpeech.Stop();
                            }

                            // Pause robot speech
                            systemLog.Add((stopwatch.Elapsed.ToString(), "[system]", "overlapping speech detected; robot stopped"));

                            updateRobotFace("reset");
                            moveRobotHead("lookAtUser");

                            heardAckPlayer.Stop();

                            if (cts != null && !cts.IsCancellationRequested)
                            {
                                cts.Cancel();
                            }

                            isRobotTalking = false;
                            isRobotInterrupted = true;
                            isReady = false;
                        }
                    }
                });

                // Interruption Detection
                // Interruption Types: "notInterruption", "disruptive", "cooperative-clarification", "cooperative-agreement", "cooperative-assistance", "incomplete-interruption"
                var interruptionHandler = new LLMInterruptionHandlingComponent(p);
                var transcribedAudio = transcribedAudioStream.Join(isRobotInterruptedStream, RelativeTimeInterval.Past());
                transcribedAudio.Where(m => isLLMProcessing == false && isStarted == true && isDone == false).Where(m => m.Item1.Length > 0).Select(m => (m.Item2, currRobotSpeechContent, m.Item1, currRobotSpeechDurationLeft, currRobotSpeechDurationCompleted)).Do(m =>
                {
                    if (m.Item1 == false)
                    {
                        if (isEvent == false && eventNum == 1)
                        {
                            if (stopwatch.Elapsed.TotalSeconds > eventTime2)
                            {
                                eventNum = 2;
                                isEvent = true;
                            }
                        }
                    }

                    Console.WriteLine("Sent to interruptionHandler");
                    isLLMProcessing = true;
                }
                ).PipeTo(interruptionHandler.In);

                var interruptionHandlerOutputString = interruptionHandler.Out.Where(m => m != "").Do(m =>
                {
                    Console.WriteLine("Interruption handler response received.");
                    isLLMProcessing = false;
                });
                interruptionHandlerOutputString.Write("interruptionHandlerResponse", store);

                var interruptionHandlerOutput = interruptionHandlerOutputString.Select(m => JsonConvert.DeserializeObject<Dictionary<string, string>>(m)).Select(m =>
                {
                    newestUserSpeechContent = m["userSpeechContent"];

                    Console.WriteLine(m["isRobotInterrupted"]);
                    Console.WriteLine(m["interruptionType"]);
                    Console.WriteLine(m["userSpeechContent"]);

                    isRobotTalking = false;

                    systemLog.Add((stopwatch.Elapsed.ToString(), "[system]", "[" + m["interruptionType"] + "interruption] " + m["userSpeechContent"]));

                    return (m["userSpeechContent"], m["interruptionType"], currRobotSpeechDurationCompleted, isSubmitted, stopwatch.Elapsed.TotalSeconds);
                });

                var isStartedLLMInput = isStartedStream.Select(m => (m, "", 0.0, false, stopwatch.Elapsed.TotalSeconds));
                var isSubmittedLLMInput = isSubmittedStream.Select(m => (m, "", 0.0, true, stopwatch.Elapsed.TotalSeconds));
                var userUpdatedItemLLMInput = itemListRecieverStream.Select(m => (m, "", 0.0, isSubmitted, stopwatch.Elapsed.TotalSeconds));

                var templlmResponseInput1 = interruptionHandlerOutput.Merge(isStartedLLMInput);
                var templlmResponseInput2 = templlmResponseInput1.Select(m => m.Data).Merge(isSubmittedLLMInput);
                var llmResponseInput = templlmResponseInput2.Select(m => m.Data).Merge(userUpdatedItemLLMInput);

                var llmResponseComponent = new LLMResponseComponent(p);
                llmResponseInput.Where(m => isRobotTalking == false && isLLMProcessing == false && isStarted == true && isDone == false).Where(m => m.Data.Item1.Length > 0).Select(m => (m.Data.Item1, m.Data.Item2, m.Data.Item3, m.Data.Item4, m.Data.Item5, currRobotBehaviorString, isEvent, eventNum, userUpdatedItemList, itemList)).Do(m =>
                {
                    isLLMProcessing = true;
                    isEvent = false;
                    Console.WriteLine("LLM input received.");

                    if (isSubmitted == true)
                    {
                        isDone = true;
                    }

                    // Set robot thinking head orientation
                    moveRobotHead("thinking");
                    updateRobotFace("thinking");

                }).PipeTo(llmResponseComponent.In);
                llmResponseInput.Where(m => isRobotTalking == false && isLLMProcessing == false).Write("llmInput", store);


                var llmResponseOutputString = llmResponseComponent.Out.Do(m =>
                {
                    moveRobotHead("lookAtUser");
                    updateRobotFace("reset");

                    systemLog.Add((stopwatch.Elapsed.ToString(), "[system-llm-planned-robot-behavior]", m));
                    
                    isLLMProcessing = false;
                    isRobotInterrupted = false;
                    Console.WriteLine("LLM response received.");

                    userUpdatedItemList = "";
                });
                llmResponseOutputString.Write("robotResponserobotResponse", store);

                var synthesizer = new GoogleTextToSpeech(p);
                var llmResponseOutput = llmResponseOutputString.Where(m => m != "" && m != "none").Select(m => JsonConvert.DeserializeObject<Dictionary<string, string>>(m));
                
                llmResponseOutput.Where(m => m["robotTalk"] == "TRUE").Select(m => m["robotFullSpeechContent"]).Do(m =>
                {
                    Console.WriteLine("Robot Speech Synthesized.");

                    currRobotSpeechContent = m;
                    isRobotTalking = true;
                    isReady = true;
                }).PipeTo(synthesizer.In);

                // Robot behavior
                llmResponseOutput.Do(m =>
                {
                    // Parse robot behavior
                    robotTurnDuration = 0.0;
                    if (m["robotBehavior"] != null && m["robotBehavior"] != "[]")
                    {
                        currRobotBehaviorString = m["robotBehavior"];
                        List<robotBehavior> robotBehaviorList = JsonConvert.DeserializeObject<List<robotBehavior>>(m["robotBehavior"]);

                        Console.WriteLine("robot behavior parsed");
                        foreach (var robotBehavior in robotBehaviorList)
                        {
                            robotTurnDuration = robotTurnDuration + double.Parse(robotBehavior.duration);
                            //Console.WriteLine(robotBehavior.robotSpeechContent);
                            //Console.WriteLine(robotBehavior.wordCount);
                            //Console.WriteLine(robotBehavior.duration);
                            //Console.WriteLine(robotBehavior.robotFacialExpression);
                            //Console.WriteLine(robotBehavior.robotHeadOrientation);
                            //Console.WriteLine(robotBehavior.changeItemList);
                            //Console.WriteLine(robotBehavior.itemList);
                        }

                        if (m["robotTalk"] == "TRUE")
                        {
                            var counter = 0;
                            var robotBehaviorQuantity = robotBehaviorList.Count;

                            Console.WriteLine("Timer starts.");
                            if (!stopwatchRobotSpeech.IsRunning)
                            {
                                stopwatchRobotSpeech.Start();
                            }

                            while (counter < robotBehaviorQuantity && isRobotInterrupted == false)
                            {
                                Console.WriteLine("[speech]" + robotBehaviorList[counter].robotSpeechContent);
                                systemLog.Add((stopwatch.Elapsed.ToString(), "[speech]", robotBehaviorList[counter].robotSpeechContent));

                                updateRobotFace(robotBehaviorList[counter].robotFacialExpression);
                                updateRobotFace("startTalking");

                                // Estimate speech duration (seconds)
                                float estimatedDurationSeconds = float.Parse(robotBehaviorList[counter].duration);

                                if (estimatedDurationSeconds > 2)
                                {
                                    isGazeAversion = true;
                                    if (robotBehaviorList[counter].robotHeadOrientation != "lookAtScreen")
                                    {
                                        moveRobotHead("startGazeAversion");
                                    }
                                    else
                                    {
                                        moveRobotHead("lookAtScreen");
                                    }

                                    estimatedDurationSeconds = estimatedDurationSeconds - 2;

                                    cts = new CancellationTokenSource();
                                    Task.Run(async () =>
                                    {

                                        var newChangeOrderedList = robotBehaviorList[counter].changeItemList;
                                        if (newChangeOrderedList == "TRUE")
                                        {
                                            updateTaskInterface(robotBehaviorList[counter].itemList);
                                        }

                                        counter = counter + 1;

                                        // Create a timer to keep track of speech time
                                        var timerComponent = new TimerSecondsComponent();
                                        try
                                        {
                                            await timerComponent.StartTimer(estimatedDurationSeconds, () =>
                                            {
                                                Console.WriteLine("Timer 1 is done.");
                                                if (counter < robotBehaviorQuantity)
                                                {
                                                    moveRobotHead("lookAway");
                                                }
                                                else
                                                {
                                                    moveRobotHead("lookAtUser");
                                                }
                                            }, cts.Token);

                                            await timerComponent.StartTimer(2, () =>
                                            {
                                                Console.WriteLine("Timer 2 is done.");
                                                updateRobotFace("reset");
                                            }, cts.Token);
                                        }
                                        catch (OperationCanceledException)
                                        {
                                            systemLog.Add((stopwatch.Elapsed.ToString(), "[system]", "robot behavior interrupted"));

                                            stopwatchRobotSpeech.Stop();
                                            stopwatchRobotSpeech.Reset();

                                            isRobotInterrupted = true;
                                            Console.WriteLine("Robot speech timer cancelled.");
                                        }
                                        finally
                                        {
                                            Console.WriteLine("Robot speech timer finally.");

                                            cts.Dispose();
                                            cts = new CancellationTokenSource();
                                        }
                                    }).Wait();
                                }
                                else
                                {
                                    if (robotBehaviorList[counter].robotHeadOrientation != "lookAtScreen")
                                    {
                                        moveRobotHead("lookAtScreen");
                                    }
                                    else if (counter < robotBehaviorQuantity - 1)
                                    {
                                        moveRobotHead("lookAway");
                                    }
                                    else
                                    {
                                        moveRobotHead("lookAtUser");
                                    }

                                    cts = new CancellationTokenSource();
                                    Task.Run(async () =>
                                    {
                                        Console.WriteLine("Timer starts.");

                                        var newChangeOrderedList = robotBehaviorList[counter].changeItemList;
                                        if (newChangeOrderedList == "TRUE")
                                        {
                                            updateTaskInterface(robotBehaviorList[counter].itemList);
                                        }

                                        counter = counter + 1;

                                        // Create a timer to keep track of speech time
                                        var timerComponent = new TimerSecondsComponent();
                                        try
                                        {
                                            await timerComponent.StartTimer(estimatedDurationSeconds, () =>
                                            {
                                                Console.WriteLine("Timer is done.");
                                                updateRobotFace("reset");
                                            }, cts.Token);
                                        }
                                        catch (OperationCanceledException)
                                        {
                                            systemLog.Add((stopwatch.Elapsed.ToString(), "[system]", "robot behavior interrupted"));

                                            stopwatchRobotSpeech.Stop();
                                            stopwatchRobotSpeech.Reset();

                                            isRobotInterrupted = true;
                                            Console.WriteLine("Robot speech timer cancelled.");
                                        }
                                        finally
                                        {
                                            Console.WriteLine("Robot speech timer finally.");

                                            cts.Dispose();
                                            cts = new CancellationTokenSource();
                                        }
                                    }).Wait();
                                }
                            }
                            Console.WriteLine("Robot Behavior Loop Done.");

                            isRobotTalking = false;
                            isReady = false;

                            stopwatchRobotSpeech.Stop();
                            stopwatchRobotSpeech.Reset();
                        }
                        else
                        {
                            // Robot did not speak
                            moveRobotHead("lookAtUser");
                            updateRobotFace(robotBehaviorList[0].robotFacialExpression);
                        }
                    }
                });

                var synthesizerOutput = synthesizer.Out;
                synthesizerOutput.Write("text2Speech", store);

                synthesizerOutput.Where(m => isReady = true).Do(m =>
                {
                    isRobotTalking = true;
                    Console.WriteLine("Robot is talking.");

                    using (MemoryStream memoryStream = new MemoryStream(m))
                    {
                        memoryStream.Position = 0; // Reset the stream position
                        heardAckPlayer.Stream = memoryStream;
                        heardAckPlayer.Play(); // Play the audio synchronously
                    }
                });

                p.Diagnostics.Write("Diagnostics", store);

                // Perform an action when the pipeline completes
                p.PipelineCompleted += (s, e) =>
                {
                    Console.WriteLine($"Pipeline execution completed at {e.CompletedOriginatingTime}");
                    WriteConversationHistoryToFile();

                    moveRobotHead("lookAtUser");
                    updateRobotFace("reset");
                };

                p.RunAsync();

                // Wait for any key to quit
                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true).Key;
                        if (key == ConsoleKey.W || key != ConsoleKey.W)  // This allows quitting on any key, including 'w'
                        {
                            Console.WriteLine($"Key pressed: {key}. Quitting the program...");
                            break;
                        }
                    }
                }

                // Dispose the pipeline
                p.Dispose();
            }
        }


        static void updateRobotFace(string newRobotFaceState)
        {
            Console.WriteLine("[Face]:" + newRobotFaceState);
            systemLog.Add((stopwatch.Elapsed.ToString(), "[Face]", newRobotFaceState));

            if (newRobotFaceState != null && newRobotFaceState != "")
            {
                if (possibleFaces.Contains(newRobotFaceState) && newRobotFaceState != robotFaceState)
                {
                    var content = new StringContent(JsonConvert.SerializeObject(new { expression = newRobotFaceState }), Encoding.UTF8, "application/json");
                    client.PostAsync("http://localhost:3000/express", content).Wait();

                    robotFaceState = newRobotFaceState;
                }
            }
        }

        static void updateTaskInterface(string newItemList)
        {
            Console.WriteLine("[Task Interface]:" + newItemList);
            systemLog.Add((stopwatch.Elapsed.ToString(), "[Task Interface]", newItemList));

            if (newItemList != itemList)
            {
                var content = new StringContent(JsonConvert.SerializeObject(new { message = newItemList }), Encoding.UTF8, "application/json");
                client.PostAsync("http://localhost:4000/update_item_list", content).Wait();

                itemList = newItemList;
            }
        }

        static void moveRobotHead(string newRobotHeadState)
        {
            if (newRobotHeadState != null && newRobotHeadState != "")
            {
                if (possibleHeadMovements.Contains(newRobotHeadState) && newRobotHeadState != robotHeadState)
                {
                    Console.WriteLine("[Head]:" + newRobotHeadState);
                    systemLog.Add((stopwatch.Elapsed.ToString(), "[Head]", newRobotHeadState));

                    var message = new
                    {
                        originatingTime = DateTime.UtcNow.ToString("o"),
                        message = newRobotHeadState
                    };

                    string jsonMessage = JsonConvert.SerializeObject(message);
                    headSocket.SendFrame(jsonMessage);

                    robotHeadState = newRobotHeadState;
                }
            }
        }


        static void WriteConversationHistoryToFile()
        {
            try
            {
                string filePath = Path.Combine(storeAddress, "logs", "log_" + storeName + "_" + DateTime.Now.ToString("MMddHHmm") + ".txt");

                using (StreamWriter writer = new StreamWriter(filePath, false))
                {
                    foreach (var entry in systemLog)
                    {
                        writer.WriteLine($"{entry.Time} {entry.Role}: {entry.Content}");
                    }
                }
                Console.WriteLine("Full Conversation history written to file successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while writing the conversation history to file: " + ex.Message);
            }
        }

        static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            // Handle Ctrl+C gracefully
            moveRobotHead("lookAtUser");
            updateRobotFace("reset");

            WriteConversationHistoryToFile();

            Console.WriteLine("Ctrl+C pressed. Quitting the program...");
            e.Cancel = true;  // Prevent immediate termination
            Environment.Exit(0);  // Exit the program
        }


        // Event handler for process exit
        static void OnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("Process exit event triggered.");

            moveRobotHead("lookAtUser");
            updateRobotFace("reset");

            WriteConversationHistoryToFile();
        }
    }
}