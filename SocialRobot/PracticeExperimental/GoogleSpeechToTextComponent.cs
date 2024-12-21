using System;
using System.Net.Http;
using System.Text;
using System.Configuration;
using Microsoft.Psi;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using System.Linq;
using Google.Cloud.TextToSpeech.V1;

namespace PracticeExperimental
{
    public class GoogleSpeechtoText
    {
     
        private static TextToSpeechClient client;
        private static VoiceSelectionParams voiceSelection;
        private static AudioConfig audioConfig;

        public GoogleSpeechtoText(Pipeline p)
        {
            In = p.CreateReceiver<string>(this, googleSpeechToText, nameof(In));
            Out = p.CreateEmitter<byte[]>(this, nameof(Out));
            
            p.PipelineRun += initializeGoogleSpeechToText;
            p.PipelineCompleted += OnPipelineCompleted;
        }

        public Receiver<string> In { get; private set; }
        public Emitter<byte[]> Out { get; private set; }

        private void initializeGoogleSpeechToText(object sender, PipelineRunEventArgs e)
        {
            client = TextToSpeechClient.Create();
            
            // You can specify a particular voice, or ask the server to pick based
            // on specified criteria.
            voiceSelection = new VoiceSelectionParams
            {
                LanguageCode = "en-US",
                //Name = "en-US-Journey-O",
                Name = "en-US-Standard-H",
                SsmlGender = SsmlVoiceGender.Female
            };
            
            // The audio configuration determines the output format and speaking rate.
            audioConfig = new AudioConfig
            {
                //AudioEncoding = AudioEncoding.Mp3
                AudioEncoding = AudioEncoding.Linear16
            };
        }

        private void googleSpeechToText(string text, Envelope envelope)
        {
            
            SynthesisInput input = new SynthesisInput
            {
                Text = text
            };

            SynthesizeSpeechResponse response = client.SynthesizeSpeech(input, voiceSelection, audioConfig);

            byte[] output = response.AudioContent.ToByteArray();

            // Output the response
            Out.Post(output, envelope.OriginatingTime);
        }

        private void OnPipelineCompleted(object sender, PipelineCompletedEventArgs e)
        {
            
        }
    }
}
