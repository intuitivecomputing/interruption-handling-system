using System;
using System.Net.Http;
using System.Text;
using System.Linq;
using System.Configuration;
using Microsoft.Psi;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PracticeExperimental
{
    public class LLMInterruptionHandlingComponent
    {
        private static string ApiKey;
        private static string OpenAIUrl;

        private static string task;

        public static List<string> wakeWords = new List<string> {"luna", "stop"};
        public LLMInterruptionHandlingComponent(Pipeline p)
        {
            In = p.CreateReceiver<(bool, string, string, double, double)>(this, LLMAPI, nameof(In));
            Out = p.CreateEmitter<string>(this, nameof(Out));
            p.PipelineRun += initializeLLM;
            p.PipelineCompleted += OnPipelineCompleted;
        }

        public Receiver<(bool, string, string, double, double)> In { get; private set; }
        public Emitter<string> Out { get; private set; }

        private void initializeLLM(object sender, PipelineRunEventArgs e)
        {
            ApiKey = Environment.GetEnvironmentVariable("OPENAI_KEY", EnvironmentVariableTarget.User);
            OpenAIUrl = "https://api.openai.com/v1/chat/completions";
        }

        private void LLMAPI((bool, string, string, double, double) componentInput, Envelope envelope)
        {
            bool isRobotInterrupted = componentInput.Item1;
            string robotSpeechContent = componentInput.Item2;
            string userSpeechContent = componentInput.Item3;
            double robotSpeechDurationLeft = componentInput.Item4;
            double robotSpeechDurationCompleted = componentInput.Item5;

            var output = "";
            var interruptionType = "";
            var systemPrompt = @"
            You are designed to categorize user interruptions in a two-person conversation between a user and a voice assistant. You will be provided with a conversation transcript and the time in seconds when the user interruption occurs relative to the start of the voice assistant's speech. The voice assistant’s text-to-speech rate is 185 words per minute. Using this information, you must determine the nature of any interruptions.
            There are five main types of interruptions:
            1. Disruptive Interruption: The user seeks to take the turn from the speaker. If the user speech content contains ""Luna"" or ""stop"", it must be considered disruptive. Subtypes of disruptive interruption include:
            - Disagreement: The user disagrees and immediately expresses their own opinion.
            - Floor Taking: The user takes over the conversation, continuing or shifting the topic.
            -Topic Change: The user introduces an entirely new topic, breaking away from the current one.
            - Tangentialization: The user summarizes the speaker’s point to conclude the topic, typically to prevent further information.
            2.Cooperative clarification: The user asks for clarification or requests additional information to better understand the speaker’s message.
            3.Cooperative agreement: The user expresses agreement, support, understanding, or compliance with the speaker.
            4.Cooperative assistance: The user offers help by providing a word, phrase, or idea to complete the speaker’s turn.If the user's interruption has similar contextual meaning with the robot speech, it must be considered cooperative assistance.
            Your task is to classify each user interruption into one of these 5 categories based on the transcript and conversation context.Your output must be one of the following: ""disruptive""; ""cooperative-clarification""; ""cooperative-agreement""; ""cooperative-assistance"". Do not provide justifications.
            ";
            string modelToUse = "gpt-4o-mini-2024-07-18";
            double temperatureToUse = 0.5;

            if (isRobotInterrupted == false)
            {
                Console.WriteLine("Robot is not interrupted");

                Dictionary<string, string> llmOutput = new Dictionary<string, string>();

                llmOutput.Add("interruptionType", "normal-speech");
                llmOutput.Add("isRobotInterrupted", isRobotInterrupted.ToString());
                llmOutput.Add("userSpeechContent", userSpeechContent);
                llmOutput.Add("robotSpeechContent", "");

                output = JsonConvert.SerializeObject(llmOutput);
            }
            else
            {
                Console.WriteLine("Interruption Detected");

                bool containsWakeword = wakeWords.Any(w => userSpeechContent.ToLower().Contains(w.ToLower())); 
                if (robotSpeechDurationLeft <= 2 && !containsWakeword)
                {
                    Console.WriteLine("Interruption but only less than 2 seconds of robot speech left");
                    Dictionary<string, string> llmOutput = new Dictionary<string, string>();

                    llmOutput.Add("interruptionType", "ignore");
                    llmOutput.Add("isRobotInterrupted", isRobotInterrupted.ToString());
                    llmOutput.Add("userSpeechContent", "");
                    llmOutput.Add("robotSpeechContent", "");

                    output = JsonConvert.SerializeObject(llmOutput);
                }
                else if (robotSpeechDurationCompleted < 1)
                {
                    Console.WriteLine("Interruption but robot only spoke for less than 1 second");
                    Dictionary<string, string> llmOutput = new Dictionary<string, string>();

                    llmOutput.Add("interruptionType", "ignore");
                    llmOutput.Add("isRobotInterrupted", isRobotInterrupted.ToString());
                    llmOutput.Add("userSpeechContent", "");
                    llmOutput.Add("robotSpeechContent", "");

                    output = JsonConvert.SerializeObject(llmOutput);
                }
                else
                {
                    var userStatement = @"""assistant"": """ + robotSpeechContent + @"""\n" + @"""user"": """ + userSpeechContent + @"""\n" + @"""interruption-time"":" + robotSpeechDurationCompleted;
                    interruptionType = CallOpenAIAsync(systemPrompt, userStatement, modelToUse, temperatureToUse).GetAwaiter().GetResult();

                    Dictionary<string, string> llmOutput = new Dictionary<string, string>();

                    llmOutput.Add("interruptionType", interruptionType);
                    llmOutput.Add("isRobotInterrupted", isRobotInterrupted.ToString());
                    llmOutput.Add("userSpeechContent", userSpeechContent);
                    llmOutput.Add("robotSpeechContent", "");

                    output = JsonConvert.SerializeObject(llmOutput);
                }
                
            }

            // Output the response
            Out.Post(output, envelope.OriginatingTime);
        }

        private static async Task<string> CallOpenAIAsync(string systemPrompt, string userStatement, string modelName, double temperature = 0.5)
        {
            using (var client = new HttpClient())
            {
                if (userStatement == "")
                {
                    return "";
                }

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");

                var messages = new List<dynamic>();
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    messages.Add(new { role = "system", content = systemPrompt });
                }
                if (!string.IsNullOrEmpty(userStatement))
                {
                    messages.Add(new { role = "user", content = userStatement });
                }

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

                    return answer;
                }
                else
                {
                    return $"Error: {responseContent}";
                }
            }
        }

        private void OnPipelineCompleted(object sender, PipelineCompletedEventArgs e)
        {
           
        }
    }
}