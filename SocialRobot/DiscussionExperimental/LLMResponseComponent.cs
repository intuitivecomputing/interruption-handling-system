using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Linq;
using System.Configuration;
using Microsoft.Psi;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DiscussionExperimental
{

    public class LLMResponseComponent
    {
        private static string promptAddress = @"C:\Users\scao1\OneDrive\Desktop\socialRobotPersonalityTailoring\SocialRobot\prompts.json";
        private static string defaultPromptName = "behavior-generator-discussion-luna";
        private static string event1PromptName = "event1-death-penalty";
        private static string event2PromptName = "event2-death-penalty";

        public static List<string> cooperativeAgreementAcknowledgements = new List<string> { "sure", "yes", "ya", "uhhum", "en" };
        public static List<string> cooperativeAssistanceAcknowledgements = new List<string> { "yes", "ya", "thanks"};
        public static List<string> wakeWords = new List<string> { "luna", "stop" };

        private static string ApiKey;
        private static string OpenAIUrl;
        private static Dictionary<string, string> prompts;  // Dictionary to hold the prompts

        private static string task;

        private List<(string Role, string Content)> conversationHistory;

        public LLMResponseComponent(Pipeline p)
        {
            In = p.CreateReceiver<(string, string, double, bool, double, string, bool, int)> (this, LLMAPI, nameof(In));
            Out = p.CreateEmitter<string>(this, nameof(Out));
            p.PipelineRun += initializeLLM;
            p.PipelineCompleted += OnPipelineCompleted;

            // Load prompts from JSON file
            LoadPrompts(promptAddress);
        }

        public Receiver<(string, string, double, bool, double, string, bool, int)> In { get; private set; }
        public Emitter<string> Out { get; private set; }

        private void initializeLLM(object sender, PipelineRunEventArgs e)
        {
            ApiKey = Environment.GetEnvironmentVariable("OPENAI_KEY", EnvironmentVariableTarget.User);
            OpenAIUrl = "https://api.openai.com/v1/chat/completions";
            //task = ConfigurationManager.AppSettings["Task"];
            conversationHistory = new List<(string Role, string Content)>();
        }

        private void LoadPrompts(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                prompts = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading prompts config: " + ex.Message);
            }
        }

        private void LLMAPI((string, string, double, bool, double, string, bool, int) componentInput, Envelope envelope)
        {
            string userSpeechContent = componentInput.Item1;
            string interruptionType = componentInput.Item2;
            double prevRobotSpeechDurationCompleted = componentInput.Item3;
            bool isSubmitted = componentInput.Item4;
            double taskTime = componentInput.Item5; 
            string prevRobotBehaviorString = componentInput.Item6;
            bool isEvent = componentInput.Item7;
            int eventNum = componentInput.Item8;

            var output = "";
            
            var systemPrompt = "";
            string modelToUse = "gpt-4o-2024-05-13";
            double temperatureToUse = 0.5;

            bool containsWakeword = wakeWords.Any(w => userSpeechContent.ToLower().Contains(w.ToLower()));

            if (interruptionType == "notInterruption" || interruptionType == "ignore")
            {
                Console.WriteLine("Continue robot behavior");
                output = continueRobotBehavior(prevRobotBehaviorString, prevRobotSpeechDurationCompleted);
            }
            else if (interruptionType == "cooperative-agreement" || interruptionType == "notInterruption" || interruptionType == "ignore")
            {
                Console.WriteLine("Continue robot behavior");
                output = interruptionHandlingAgreement(userSpeechContent, prevRobotBehaviorString, prevRobotSpeechDurationCompleted);
            }
            else if (interruptionType == "cooperative-assistance")
            {
                Console.WriteLine("Continue robot behavior");
                output = interruptionHandlingAssistance(prevRobotBehaviorString, prevRobotSpeechDurationCompleted);
            }
            else if (interruptionType == "cooperative-clarification")
            {
                var remainingRobotBehavior = continueRobotBehavior(prevRobotBehaviorString, prevRobotSpeechDurationCompleted);
                userSpeechContent = "[" + interruptionType + " interruption after robot talked for " + prevRobotSpeechDurationCompleted + " seconds]: " + userSpeechContent + "\n" + "[remaining robot behavior]:" + remainingRobotBehavior;

                Console.WriteLine("In cooperative clarification LLM");
                modelToUse = "gpt-4o-2024-05-13";
                systemPrompt = prompts.ContainsKey("cooperative-clarification-response") ? prompts["cooperative-clarification-response"] : "";

                Console.WriteLine("Respond to: " + userSpeechContent);

                output = CallOpenAIAsync(systemPrompt, userSpeechContent, conversationHistory, modelToUse, temperatureToUse).GetAwaiter().GetResult();
            }
            else if (interruptionType == "disruptive" && prevRobotSpeechDurationCompleted < 5.0 && !containsWakeword)
            {
                var remainingRobotBehavior = continueRobotBehavior(prevRobotBehaviorString, prevRobotSpeechDurationCompleted);
                userSpeechContent = "[remaining robot behavior]:" + remainingRobotBehavior;

                Console.WriteLine("In early disruptive LLM");
                modelToUse = "gpt-4o-2024-05-13";
                systemPrompt = prompts.ContainsKey("early-disruptive-response") ? prompts["early-disruptive-response"] : "";

                Console.WriteLine("Respond to: " + userSpeechContent);

                output = CallOpenAIAsync(systemPrompt, userSpeechContent, conversationHistory, modelToUse, temperatureToUse).GetAwaiter().GetResult();
            }
            else
            {
                Console.WriteLine("generating robot behavior");
                if (taskTime > 280)
                {
                    var timeLeft = 300 - taskTime;
                    userSpeechContent = " [less than " + timeLeft.ToString() + " seconds left in discussion]";
                }
                else if (isSubmitted == true)
                {
                    userSpeechContent = " [user submitted]";
                }

                if (interruptionType == "disruptive")
                {
                    Console.WriteLine("Respond to disruptive interruption");
                    userSpeechContent = "[" + interruptionType + " interruption after robot talked for " + prevRobotSpeechDurationCompleted + " seconds]: " + userSpeechContent;
                }

                if (isEvent == false)
                {
                    Console.WriteLine("In Default LLM");
                    modelToUse = "gpt-4o-2024-05-13";
                    systemPrompt = prompts.ContainsKey(defaultPromptName) ? prompts[defaultPromptName] : "";

                    Console.WriteLine("Respond to: " + userSpeechContent);

                    output = CallOpenAIAsync(systemPrompt, userSpeechContent, conversationHistory, modelToUse, temperatureToUse).GetAwaiter().GetResult();
                }
                else
                {
                    if (eventNum == 1)
                    {
                        Console.WriteLine("In Event 1 LLM");
                        modelToUse = "gpt-4o-2024-05-13";
                        //modelToUse = "gpt-4o-mini-2024-07-18";
                        systemPrompt = prompts.ContainsKey(event1PromptName) ? prompts[event1PromptName] : "";
                        output = ParseResponse(systemPrompt);
                    }
                    else if (eventNum == 2)
                    {
                        Console.WriteLine("In Event 2 LLM");
                        if (event2PromptName == "event2-1" || event2PromptName == "event2-4")
                        {
                            modelToUse = "gpt-4o-2024-05-13";
                            systemPrompt = prompts.ContainsKey(event2PromptName) ? prompts[event2PromptName] : "";
                            Console.WriteLine("Respond to: " + userSpeechContent);
                            output = CallOpenAIAsync(systemPrompt, userSpeechContent, conversationHistory, modelToUse, temperatureToUse).GetAwaiter().GetResult();
                        }
                        else
                        {
                            //modelToUse = "gpt-4o-2024-05-13";
                            systemPrompt = prompts.ContainsKey(event2PromptName) ? prompts[event2PromptName] : "";
                            output = ParseResponse(systemPrompt);
                        }
                    }
                }
                
            }

            if (!string.IsNullOrEmpty(userSpeechContent))
            {
                conversationHistory.Add(("user", userSpeechContent));
            }
            if (!string.IsNullOrEmpty(output))
            {
                conversationHistory.Add(("assistant", output));
            }
            TrimConversationHistory();

            // Output the response
            Out.Post(output, envelope.OriginatingTime);
        }

        // Adjust the signature to accept the conversation history
        private static async Task<string> CallOpenAIAsync(string systemPrompt, string userStatement, List<(string Role, string Content)> conversationHistory, string modelName, double temperature = 0.5)
        {
            using (var client = new HttpClient())
            {
                if (userStatement == "")
                {
                    return "";
                }

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");

                // Prepare the messages array including the conversation history
                var messages = new List<dynamic>();
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    messages.Add(new { role = "system", content = systemPrompt });
                }
                // Add the current exchange to the history
                foreach (var entry in conversationHistory)
                {
                    messages.Add(new { role = entry.Role, content = entry.Content });
                }
                if (!string.IsNullOrEmpty(userStatement))
                {
                    messages.Add(new { role = "user", content = userStatement });
                }

                // TODO: Add what assistant said to conversation history 

                var requestBody = new
                {
                    model = modelName,
                    temperature = temperature,
                    top_p = 1,
                    messages = messages.ToArray()
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(OpenAIUrl, data);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    var answer = result.choices[0].message.content.ToString();

                    var parsedAnswer = ParseResponse(answer);
                    return parsedAnswer;
                }
                else
                {
                    return $"Error: {responseContent}";
                }
            }
        }

        private void OnPipelineCompleted(object sender, PipelineCompletedEventArgs e)
        {
            // Clear the conversation history if needed
            conversationHistory.Clear();
        }

        private void TrimConversationHistory()
        {
            // Ensure the conversation history does not exceed 20 entries (10 back and forth exchanges)
            while (conversationHistory.Count > 20)
            {
                // Remove the oldest entry
                conversationHistory.RemoveAt(0);
            }
        }


        static string interruptionHandlingAgreement(string userSpeechContent, string currRobotBehavior, double speechDurationCompleted)
        {
            List<robotBehavior> oldRobotBehaviorList = JsonConvert.DeserializeObject<List<robotBehavior>>(currRobotBehavior);
            List<robotBehavior> newRobotBehaviorList = new List<robotBehavior>();

            Console.WriteLine("Preparing robot to continue talking");

            int robotBehaviorCount = 0;
            double speechDurationCounted = 0.0;
            double speechDurationAfterSpeechSegment = double.Parse(oldRobotBehaviorList[robotBehaviorCount].duration);

            while (speechDurationAfterSpeechSegment < speechDurationCompleted && robotBehaviorCount < (oldRobotBehaviorList.Count - 1))
            {
                robotBehaviorCount = robotBehaviorCount + 1;

                speechDurationCounted = speechDurationAfterSpeechSegment;
                speechDurationAfterSpeechSegment = speechDurationAfterSpeechSegment + double.Parse(oldRobotBehaviorList[robotBehaviorCount].duration);
            }

            double speechDurationLeft = speechDurationCompleted - speechDurationCounted;

            int speakingRate = 180;

            // repeat last few words
            int wordsSpoken = (int)Math.Floor(speakingRate * speechDurationLeft);

            // Get the words that have not been said
            string[] wordsArray = oldRobotBehaviorList[robotBehaviorCount].robotSpeechContent.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            // Ensure wordsSpoken is not negative or out of bounds
            if (wordsSpoken < 0)
            {
                wordsSpoken = 0;
            }
            if (wordsSpoken >= wordsArray.Length)
            {
                wordsSpoken = wordsArray.Length - 1;
            }

            // Get the remaining speech content
            //string remainingSpeechContent = string.Join(" ", wordsArray.Skip(wordsSpoken));
            //string newRobotFullSpeechContent = remainingSpeechContent;

            // Get the spoken speech content up to the current point
            string spokenSpeechContent = string.Join(" ", wordsArray.Take(wordsSpoken));

            // Find the last punctuation mark in the spoken speech content
            int lastPunctuationIndex = spokenSpeechContent.LastIndexOfAny(new char[] { '.', ',', '!', '?', ';', ':' });

            // If a punctuation mark is found, include content up to it
            if (lastPunctuationIndex != -1)
            {
                spokenSpeechContent = spokenSpeechContent.Substring(lastPunctuationIndex + 1).Trim();
            }

            // Remaining speech content to be spoken
            string remainingSpeechContent = string.Join(" ", wordsArray.Skip(wordsSpoken));

            int userWordCount = userSpeechContent.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

            string cooperativeAcknowledgement = "";

            if (userWordCount > 2)
            {
                Random random = new Random();
                int index = random.Next(cooperativeAgreementAcknowledgements.Count);
                cooperativeAcknowledgement = cooperativeAgreementAcknowledgements[index] + ". ";

                robotBehavior newBehaviorAck = new robotBehavior
                {
                    robotSpeechContent = cooperativeAcknowledgement,
                    wordCount = "1",
                    duration = "1",
                    robotFacialExpression = "neutral",
                    robotHeadOrientation = "doubleNod",
                };
                newRobotBehaviorList.Add(newBehaviorAck);
            }

            string newRobotFullSpeechContent = cooperativeAcknowledgement + spokenSpeechContent + " " + remainingSpeechContent;

            // Output the remaining speech content
            Console.WriteLine("new continue speech content: " + newRobotFullSpeechContent);

            int averageSpeakingRate = 180;
            int wordCount = spokenSpeechContent.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

            // Estimate speech duration
            double estimatedDurationMinutes = (double)wordCount / averageSpeakingRate;

            // Convert minutes to seconds
            int estimatedDurationSeconds = (int)(estimatedDurationMinutes * 60);

            // Create a new robotBehavior with the remaining speech content and add it to the new list
            robotBehavior newBehavior = new robotBehavior
            {
                robotSpeechContent = spokenSpeechContent,
                wordCount = wordCount.ToString(),
                duration = estimatedDurationSeconds.ToString(),
                robotFacialExpression = oldRobotBehaviorList[robotBehaviorCount].robotFacialExpression,
                robotHeadOrientation = oldRobotBehaviorList[robotBehaviorCount].robotHeadOrientation,
            };
            newRobotBehaviorList.Add(newBehavior);

            // Add all subsequent behaviors from the old list to the new list
            for (int i = robotBehaviorCount + 1; i < oldRobotBehaviorList.Count; i++)
            {
                newRobotBehaviorList.Add(oldRobotBehaviorList[i]);
                newRobotFullSpeechContent = newRobotFullSpeechContent + oldRobotBehaviorList[i].robotSpeechContent;
            }

            Dictionary<string, string> llmOutput = new Dictionary<string, string>();

            llmOutput.Add("robotTalk", "TRUE");
            llmOutput.Add("robotBehavior", JsonConvert.SerializeObject(newRobotBehaviorList));
            llmOutput.Add("robotFullSpeechContent", newRobotFullSpeechContent);

            return JsonConvert.SerializeObject(llmOutput);
        }

        static string interruptionHandlingAssistance(string currRobotBehavior, double speechDurationCompleted)
        {
            List<robotBehavior> oldRobotBehaviorList = JsonConvert.DeserializeObject<List<robotBehavior>>(currRobotBehavior);
            List<robotBehavior> newRobotBehaviorList = new List<robotBehavior>();

            Console.WriteLine("Preparing robot to continue talking");

            int robotBehaviorCount = 0;
            double speechDurationCounted = 0.0;
            double speechDurationAfterSpeechSegment = double.Parse(oldRobotBehaviorList[robotBehaviorCount].duration);

            while (speechDurationAfterSpeechSegment < speechDurationCompleted && robotBehaviorCount < (oldRobotBehaviorList.Count - 1))
            {
                robotBehaviorCount = robotBehaviorCount + 1;

                speechDurationCounted = speechDurationAfterSpeechSegment;
                speechDurationAfterSpeechSegment = speechDurationAfterSpeechSegment + double.Parse(oldRobotBehaviorList[robotBehaviorCount].duration);
            }

            double speechDurationLeft = speechDurationCompleted - speechDurationCounted;

            int speakingRate = 180;

            // repeat last few words
            int wordsSpoken = (int)Math.Floor(speakingRate * speechDurationLeft);

            // Get the words that have not been said
            string[] wordsArray = oldRobotBehaviorList[robotBehaviorCount].robotSpeechContent.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            // Ensure wordsSpoken is not negative or out of bounds
            if (wordsSpoken < 0)
            {
                wordsSpoken = 0;
            }
            if (wordsSpoken >= wordsArray.Length)
            {
                wordsSpoken = wordsArray.Length - 1;
            }

            // Get the spoken speech content up to the current point
            string spokenSpeechContent = string.Join(" ", wordsArray.Take(wordsSpoken));

            // Find the last punctuation mark in the spoken speech content
            int lastPunctuationIndex = spokenSpeechContent.LastIndexOfAny(new char[] { '.', ',', '!', '?', ';', ':' });

            // If a punctuation mark is found, include content up to it
            if (lastPunctuationIndex != -1)
            {
                spokenSpeechContent = spokenSpeechContent.Substring(lastPunctuationIndex + 1).Trim();
            }

            // Remaining speech content to be spoken
            string remainingSpeechContent = string.Join(" ", wordsArray.Skip(wordsSpoken));

            Random random = new Random();
            int index = random.Next(cooperativeAssistanceAcknowledgements.Count);
            string cooperativeAcknowledgement = cooperativeAssistanceAcknowledgements[index] + ". ";

            robotBehavior newBehaviorAck = new robotBehavior
            {
                robotSpeechContent = cooperativeAcknowledgement,
                wordCount = "1",
                duration = "1",
                robotFacialExpression = "neutral",
                robotHeadOrientation = "doubleNod",
            };
            newRobotBehaviorList.Add(newBehaviorAck);
            
            string newRobotFullSpeechContent = cooperativeAcknowledgement + spokenSpeechContent + " " + remainingSpeechContent;

            // Output the remaining speech content
            Console.WriteLine("new continue speech content: " + newRobotFullSpeechContent);

            int averageSpeakingRate = 180;
            int wordCount = spokenSpeechContent.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

            // Estimate speech duration
            double estimatedDurationMinutes = (double)wordCount / averageSpeakingRate;

            // Convert minutes to seconds
            int estimatedDurationSeconds = (int)(estimatedDurationMinutes * 60);

            // Create a new robotBehavior with the remaining speech content and add it to the new list
            robotBehavior newBehavior = new robotBehavior
            {
                robotSpeechContent = spokenSpeechContent,
                wordCount = wordCount.ToString(),
                duration = estimatedDurationSeconds.ToString(),
                robotFacialExpression = oldRobotBehaviorList[robotBehaviorCount].robotFacialExpression,
                robotHeadOrientation = oldRobotBehaviorList[robotBehaviorCount].robotHeadOrientation,
            };
            newRobotBehaviorList.Add(newBehavior);

            // Add all subsequent behaviors from the old list to the new list
            for (int i = robotBehaviorCount + 1; i < oldRobotBehaviorList.Count; i++)
            {
                newRobotBehaviorList.Add(oldRobotBehaviorList[i]);
                newRobotFullSpeechContent = newRobotFullSpeechContent + oldRobotBehaviorList[i].robotSpeechContent;
            }

            Dictionary<string, string> llmOutput = new Dictionary<string, string>();

            llmOutput.Add("robotTalk", "TRUE");
            llmOutput.Add("robotBehavior", JsonConvert.SerializeObject(newRobotBehaviorList));
            llmOutput.Add("robotFullSpeechContent", newRobotFullSpeechContent);

            return JsonConvert.SerializeObject(llmOutput);
        }

        static string continueRobotBehavior(string currRobotBehavior, double speechDurationCompleted)
        {
            List<robotBehavior> oldRobotBehaviorList = JsonConvert.DeserializeObject<List<robotBehavior>>(currRobotBehavior);
            List<robotBehavior> newRobotBehaviorList = new List<robotBehavior>();

            Console.WriteLine("Preparing robot to continue talking");

            int robotBehaviorCount = 0;
            double speechDurationCounted = 0.0;
            double speechDurationAfterSpeechSegment = double.Parse(oldRobotBehaviorList[robotBehaviorCount].duration);

            // Account for lag; it's ok to repeat a bit
            speechDurationCompleted = speechDurationCompleted - 1;

            while (speechDurationAfterSpeechSegment < speechDurationCompleted && robotBehaviorCount < (oldRobotBehaviorList.Count - 1))
            {
                robotBehaviorCount = robotBehaviorCount + 1;

                speechDurationCounted = speechDurationAfterSpeechSegment;
                speechDurationAfterSpeechSegment = speechDurationAfterSpeechSegment + double.Parse(oldRobotBehaviorList[robotBehaviorCount].duration);
            }

            double speechDurationLeft = speechDurationCompleted - speechDurationCounted;

            int speakingRate = 180;

            // repeat last few words
            int wordsSpoken = (int)Math.Floor(speakingRate * speechDurationLeft);

            // Get the words that have not been said
            string[] wordsArray = oldRobotBehaviorList[robotBehaviorCount].robotSpeechContent.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            // Ensure wordsSpoken is not negative or out of bounds
            if (wordsSpoken < 0)
            {
                wordsSpoken = 0;
            }
            if (wordsSpoken >= wordsArray.Length)
            {
                wordsSpoken = wordsArray.Length - 1;
            }

            // Get the remaining speech content
            //string remainingSpeechContent = string.Join(" ", wordsArray.Skip(wordsSpoken));
            //string newRobotFullSpeechContent = remainingSpeechContent;

            // Get the spoken speech content up to the current point
            string spokenSpeechContent = string.Join(" ", wordsArray.Take(wordsSpoken));

            // Find the last punctuation mark in the spoken speech content
            int lastPunctuationIndex = spokenSpeechContent.LastIndexOfAny(new char[] { '.', ',', '!', '?', ';', ':' });

            // If a punctuation mark is found, include content up to it
            if (lastPunctuationIndex != -1)
            {
                spokenSpeechContent = spokenSpeechContent.Substring(lastPunctuationIndex + 1).Trim();
            }

            // Remaining speech content to be spoken
            string remainingSpeechContent = string.Join(" ", wordsArray.Skip(wordsSpoken));

            string newRobotFullSpeechContent = spokenSpeechContent + " " + remainingSpeechContent;

            // Output the remaining speech content
            Console.WriteLine("remaining speech content: " + remainingSpeechContent);
            Console.WriteLine("new continue speech content: " + newRobotFullSpeechContent);

            int averageSpeakingRate = 180;
            int wordCount = spokenSpeechContent.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

            // Estimate speech duration
            double estimatedDurationMinutes = (double)wordCount / averageSpeakingRate;

            // Convert minutes to seconds
            int estimatedDurationSeconds = (int)(estimatedDurationMinutes * 60);

            // Create a new robotBehavior with the remaining speech content and add it to the new list
            robotBehavior newBehavior = new robotBehavior
            {
                robotSpeechContent = spokenSpeechContent,
                wordCount = (int.Parse(oldRobotBehaviorList[robotBehaviorCount].wordCount) - wordsSpoken).ToString(),
                duration = estimatedDurationSeconds.ToString(),
                robotFacialExpression = oldRobotBehaviorList[robotBehaviorCount].robotFacialExpression,
                robotHeadOrientation = oldRobotBehaviorList[robotBehaviorCount].robotHeadOrientation,
            };
            newRobotBehaviorList.Add(newBehavior);

            // Add all subsequent behaviors from the old list to the new list
            for (int i = robotBehaviorCount + 1; i < oldRobotBehaviorList.Count; i++)
            {
                newRobotBehaviorList.Add(oldRobotBehaviorList[i]);
                newRobotFullSpeechContent = newRobotFullSpeechContent + oldRobotBehaviorList[i].robotSpeechContent;
            }

            Dictionary<string, string> llmOutput = new Dictionary<string, string>();

            llmOutput.Add("robotTalk", "TRUE");
            llmOutput.Add("robotBehavior", JsonConvert.SerializeObject(newRobotBehaviorList));
            llmOutput.Add("robotFullSpeechContent", newRobotFullSpeechContent);

            return JsonConvert.SerializeObject(llmOutput);
        }


        static string errorHandling()
        {
            List<robotBehavior> newRobotBehaviorList = new List<robotBehavior>();

            Console.WriteLine("Preparing robot to continue talking");

            string newRobotFullSpeechContent = "Sorry, I didn't get that. Can you repeat that please?";

            robotBehavior newBehavior = new robotBehavior
            {
                robotSpeechContent = newRobotFullSpeechContent,
                wordCount = "10",
                duration = "3.33",
                robotFacialExpression = "neutral",
                robotHeadOrientation = "lookAtUser",
            };
            newRobotBehaviorList.Add(newBehavior);

            Dictionary<string, string> llmOutput = new Dictionary<string, string>();

            llmOutput.Add("robotTalk", "TRUE");
            llmOutput.Add("robotBehavior", JsonConvert.SerializeObject(newRobotBehaviorList));
            llmOutput.Add("robotFullSpeechContent", newRobotFullSpeechContent);

            return JsonConvert.SerializeObject(llmOutput);
        }


        private static string ParseResponse(string answer)
        {
            try
            {
                Console.WriteLine(answer);
                answer = answer.Replace(@"\""", @"""").Replace(@"""[", @"[").Replace(@"]""", @"]");
                Console.WriteLine(answer);

                var parsed_answer = JsonConvert.DeserializeObject<dynamic>(answer);

                Console.WriteLine("robotTalk = " + parsed_answer.robotTalk);
                Console.WriteLine("robotBehavior = " + parsed_answer.robotBehavior);
                Console.WriteLine("robotFullSpeechContent = " + parsed_answer.robotFullSpeechContent);

                string robotTalk = parsed_answer.robotTalk;

                string robotFullSpeechContent = "";
                string robotBehavior = "[]";
                if (robotTalk == "TRUE")
                {
                    robotFullSpeechContent = parsed_answer.robotFullSpeechContent;
                    robotBehavior = JsonConvert.SerializeObject(parsed_answer.robotBehavior);
                }

                Dictionary<string, string> llmOutput = new Dictionary<string, string>();

                llmOutput.Add("robotTalk", robotTalk);
                llmOutput.Add("robotBehavior", robotBehavior);
                llmOutput.Add("robotFullSpeechContent", robotFullSpeechContent);

                return JsonConvert.SerializeObject(llmOutput);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error parsing response" + ex.Message);

                return errorHandling();
            }
        }
    }
}