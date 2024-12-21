using System;
using System.Net.Http;
using System.Text;
using System.Configuration;
using Microsoft.Psi;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SocialRobot
{
    public class LLMResponseComponent
    {
        private static string ApiKey;
        private static string OpenAIUrl;
        private static bool isActiveDetection;
        private static string task;

        private static string responseList = string.Empty;
        private static string orderedList = string.Empty; 

        // Define a list to store the conversation history
        private List<(string Role, string Content)> conversationHistory;

        public LLMResponseComponent(Pipeline p)
        {
            In = p.CreateReceiver<string>(this, LLMAPI, nameof(In));
            Out = p.CreateEmitter<string>(this, nameof(Out));
            p.PipelineRun += initializeLLM;
            p.PipelineCompleted += OnPipelineCompleted;
        }

        public Receiver<string> In { get; private set; }
        public Emitter<string> Out { get; private set; }

        private void initializeLLM(object sender, PipelineRunEventArgs e)
        {
            ApiKey = Environment.GetEnvironmentVariable("OPENAI_KEY", EnvironmentVariableTarget.User);
            OpenAIUrl = "https://api.openai.com/v1/chat/completions";
            task = ConfigurationManager.AppSettings["Task"];
            conversationHistory =  new List<(string Role, string Content)>();
        }

        private void LLMAPI(string text, Envelope envelope)
        {
            var systemPrompt = "";
            if (task == "DesertSurvivalSort")
            {
                systemPrompt = @"Your Role: Your name is Jay. You need to solve a desert survival problem by communicating with your three teammates. Your team is in a desert survival simulation and is tasked to rank 15 items in order of importance as quickly and accurately as possible given the survival nature of the task. The 15 items are: flashlight, jackknife, air map of the area, plastic raincoat, magnetic compass, compress kit with gauze, 45 - caliber pistol, 1 parachute per person, bottle of salt tablets, 1 quart of water per person, animals book, 1 pair of sunglasses per person, 2 quarts of vodka, 1 topcoat per person, cosmetic mirror.
Your personality: Your personality is characterized based on the Big Five Personality traits. Your personality is:
    - Higher Openness: You are very creative, open to trying new things, focused on tackling new challenges, and happy to think about abstract concepts.
    - Higher Conscientiousness: You finish important tasks right away. You like organizing a detailed list. You have a set schedule. You pay attention to detail.
    - Higher Extraversion: You enjoy making small talk. You enjoy getting to know other people.You enjoy being the center of attention. You like to start conversations.
    - Higher Agreeableness: You care about how other people feel. You enjoy helping and contributing to the happiness of other people. You are trustworthy and cooperative.
    - Lower Neuroticism: You are emotionally stable, deal well with stress, rarely feel sad or depressed, and are very relaxed.
You need to behave and respond according to your personality at all times. Keep each of your responses under 100 words. Replace signs such as &, <, >, = to words: and, less than, greater than and equal to respectively. Consider that you are using your voice to talk to your teammates. Use pauses and natural verbal and vocal cues as needed since the mode of communication is speech. Do not include emojis in the response. Always remember that this is NOT a 1:1 discussion. This is a group discussion of 4 individuals (including you). Based on your personality, listen and give other people a chance to respond to ensure balanced participation. Consider everyone’s opinion.
Your task is to create a sorted list of items in order of importance with your teammates. Create the list of items into a list format in ""orderedList"". The list should contain the list of items that everyone has agreed upon. ""orderedList"" is empty at first. As your team makes decisions, the list will be filled or changed. Every member of the team can change the list of items. Based on your personality, if you think that the order of the list should be changed or filled, you can choose to change it. If there is a change in the list (change in order or list is being filled up), append the variable ""ChangeOrder"": ""TRUE"". ONLY append a variable ""Done"": ""TRUE"" when you and your teammates agree on the finalized list that no further changes are needed.
Based on your personality, if you think that no response is necessary, append the variable ""Talk"": ""FALSE"" to indicate that you will stay silent. You do not have to respond every time. If your teammate repeats what they have said, do not respond to your teammate for the same context, append the variable that you will stay silent. If you have something to say, append ""Talk"": ""TRUE"".
When ""Talk"": ""TRUE"", attach a json object
               {
                    ""response"": ""Your response here.""
                                ""Talk"": TRUE
                 }
            where:
                Don’t refer to your teammate by their speaker number.The speaker number is only for you to keep track of.
               Provide your response in json format.Remove ""```json"" from front and ""```"" from end.
              

              ";
                           }
            else if (task == "DesertSurvivalPick")
            {
                systemPrompt = "";
            }

            var userStatement = text;

            // Include conversation history in the call to OpenAI
            var output = CallOpenAIAsync(systemPrompt, userStatement, conversationHistory).GetAwaiter().GetResult();
             
            if (!string.IsNullOrEmpty(userStatement))
            {
                conversationHistory.Add(("user", userStatement));
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
        private static async Task<string> CallOpenAIAsync(string systemPrompt, string userStatement, List<(string Role, string Content)> conversationHistory, double temperature = 1)
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
                    model = "gpt-4o-2024-05-13",
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
                    //var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    //return result.choices[0].message.content.ToString();

                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    var answer = result.choices[0].message.content.ToString();

                    //Console.WriteLine(responseContent);
                    //Console.WriteLine(answer);

                    //Search for Done 
                    var done = "FALSE";
                    var donePattern = @"Done:\s*TRUE";
                    var doneMatch = Regex.IsMatch(answer, donePattern, RegexOptions.IgnoreCase);

                    if (doneMatch)
                    {
                        done = "TRUE";
                        // Remove matched patterns from the input for the answer
                        answer = Regex.Replace(answer, donePattern.ToString(), "").Trim();
                    }

                    //Search for ChangeOrder
                    var changeOrder = "FALSE";
                    var changeOrderPattern = @"\""ChangeOrder\"":\s*TRUE";
                    var changeOrderMatch = Regex.IsMatch(answer, changeOrderPattern, RegexOptions.IgnoreCase);

                    if (changeOrderMatch)
                    {
                        changeOrder = "TRUE";
                        // Remove matched patterns from the input for the answer
                        answer = Regex.Replace(answer, changeOrderPattern.ToString(), "").Trim();

                        orderedList = "[]"; // Default value, assuming you want an empty JSON array if not found

                        //@"orderedList=\[(.*)\]"
                        var pattern = @"\""orderedList\"":\s*\[(.*?)\]";
                        var orderedListMatch = Regex.Match(answer, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

                        if (orderedListMatch.Success)
                        {
                            orderedList = orderedListMatch.Value;
                            pattern = @"\""orderedList\"":\s*\[(.*?)\]";
                            answer = Regex.Replace(answer, pattern, "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                        }
                    }

                    // Search for Talk
                    var talk = "FALSE";
                    var talkPattern = @"\""Talk\"":\s*TRUE";
                    var talkMatch = Regex.IsMatch(answer, talkPattern, RegexOptions.IgnoreCase);

                    if (talkMatch)
                    {
                        talk = "TRUE";
                        // Remove matched patterns from the input for the answer
                        answer = Regex.Replace(answer, talkPattern.ToString(), "").Trim();

                        responseList = "[]"; // Default value, assuming you want an empty JSON array if not found

                        //@"responseList=\[(.*)\]"
                        var pattern = @"\""responseSegments\"":\s*\[(.*?)\]";
                        var responseListMatch = Regex.Match(answer, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

                        if (responseListMatch.Success)
                        {
                            responseList = responseListMatch.Value;
                            pattern = @"\""responseSegments\"":\s*\[(.*?)\]";
                            answer = Regex.Replace(answer, pattern, "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

                            pattern = @"\""responseSegments\"":\s*";
                            responseList = Regex.Replace(responseList, pattern, "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                        }
                    }

                    // Search for interrupt
                    var interrupt = "FALSE";
                    var interruptPattern = @"Interrupt\:\s*TRUE";
                    var interruptMatch = Regex.IsMatch(answer, interruptPattern, RegexOptions.IgnoreCase);

                    if (interruptMatch)
                    {
                        interrupt = "TRUE";
                        // Remove matched patterns from the input for the answer
                        answer = Regex.Replace(answer, interruptPattern.ToString(), "").Trim();
                    }

                    Dictionary<string, string> llmOutput = new Dictionary<string, string>();
                    llmOutput.Add("done", done);
                    llmOutput.Add("changeOrder", changeOrder);
                    llmOutput.Add("orderedList", orderedList);
                    llmOutput.Add("talk", talk);
                    llmOutput.Add("responseList", responseList);
                    llmOutput.Add("interrupt", interrupt);
                    llmOutput.Add("answer", answer);

                    return JsonConvert.SerializeObject(llmOutput);
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
    }
}
