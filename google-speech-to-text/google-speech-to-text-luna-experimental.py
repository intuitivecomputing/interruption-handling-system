import zmq
import msgpack

import queue
import re
import sys
import time
import datetime
import json
import random

from google.cloud import speech
import pyaudio

# Audio recording parameters
STREAMING_LIMIT = 240000  # 4 minutes
SAMPLE_RATE = 16000
CHUNK_SIZE = int(SAMPLE_RATE)  # 1000ms

context2 = zmq.Context()
socket2 = context2.socket(zmq.PUB)
socket2.bind('tcp://127.0.0.1:12346')


# This function sends a message to PSI with wakeWord heard.
def publishWakewordDetected():

    epoch = datetime.datetime(1, 1, 1)
    now = datetime.datetime.utcnow()
    delta = now - epoch
    originatingTime = delta.total_seconds() * 10**7

    payload = {u"message": "wakeWordDetected", u"originatingTime": originatingTime}
    socket2.send_multipart(["wakeWordDetected".encode(), msgpack.dumps(payload)])

context = zmq.Context()
socket = context.socket(zmq.PUB)
socket.bind('tcp://127.0.0.1:12345')

# This function sends a message to PSI with wakeWord heard.
def publishSpeechDetected():
    epoch = datetime.datetime(1, 1, 1)
    now = datetime.datetime.utcnow()
    delta = now - epoch
    originatingTime = delta.total_seconds() * 10 ** 7

    payload = {u"message": "speechDetected", u"originatingTime": originatingTime}
    socket2.send_multipart(["speechDetected".encode(), msgpack.dumps(payload)])


context = zmq.Context()
socket = context.socket(zmq.PUB)
socket.bind('tcp://127.0.0.1:12345')


# This function sends a message to PSI with transcribed audio.
def publishTranscribedAudio(transcribedAudio):
    epoch = datetime.datetime(1, 1, 1)
    now = datetime.datetime.utcnow()
    delta = now - epoch
    originatingTime = delta.total_seconds() * 10 ** 7

    payload = {u"message": transcribedAudio, u"originatingTime": originatingTime}
    socket.send_multipart(["transcribedAudio".encode(), msgpack.dumps(payload)])


def get_current_time() -> int:
    """Return Current Time in MS.

    Returns:
        int: Current Time in MS.
    """

    return int(round(time.time() * 1000))


class ResumableMicrophoneStream:
    """Opens a recording stream as a generator yielding the audio chunks."""

    def __init__(
            self: object,
            rate: int,
            chunk_size: int,
            device_index: int = None,
    ) -> None:
        """Creates a resumable microphone stream.

        Args:
        self: The class instance.
        rate: The audio file's sampling rate.
        chunk_size: The audio file's chunk size.
        device_index: The index of the desired audio input device.

        returns: None
        """
        self._rate = rate
        self.chunk_size = chunk_size
        self._num_channels = 1
        self.device_index = device_index
        self._buff = queue.Queue()
        self.closed = True
        self.start_time = get_current_time()
        self.restart_counter = 0
        self.audio_input = []
        self.last_audio_input = []
        self.result_end_time = 0
        self.is_final_end_time = 0
        self.final_request_end_time = 0
        self.bridging_offset = 0
        self.last_transcript_was_final = False
        self.new_stream = True
        self._audio_interface = pyaudio.PyAudio()
        self._audio_stream = self._audio_interface.open(
            format=pyaudio.paInt16,
            channels=self._num_channels,
            rate=self._rate,
            input=True,
            frames_per_buffer=self.chunk_size,
            input_device_index=self.device_index,
            # Run the audio stream asynchronously to fill the buffer object.
            # This is necessary so that the input device's buffer doesn't
            # overflow while the calling thread makes network requests, etc.
            stream_callback=self._fill_buffer,
        )

    def __enter__(self: object) -> object:
        """Opens the stream.

        Args:
        self: The class instance.

        returns: None
        """
        self.closed = False
        return self

    def __exit__(
            self: object,
            type: object,
            value: object,
            traceback: object,
    ) -> object:
        """Closes the stream and releases resources.

        Args:
        self: The class instance.
        type: The exception type.
        value: The exception value.
        traceback: The exception traceback.

        returns: None
        """
        self._audio_stream.stop_stream()
        self._audio_stream.close()
        self.closed = True
        # Signal the generator to terminate so that the client's
        # streaming_recognize method will not block the process termination.
        self._buff.put(None)
        self._audio_interface.terminate()

    def _fill_buffer(
            self: object,
            in_data: object,
            *args: object,
            **kwargs: object,
    ) -> object:
        """Continuously collect data from the audio stream, into the buffer.

        Args:
        self: The class instance.
        in_data: The audio data as a bytes object.
        args: Additional arguments.
        kwargs: Additional arguments.

        returns: None
        """
        self._buff.put(in_data)
        return None, pyaudio.paContinue

    def generator(self: object) -> object:
        """Stream Audio from microphone to API and to local buffer

        Args:
            self: The class instance.

        returns:
            The data from the audio stream.
        """
        while not self.closed:
            data = []

            if self.new_stream and self.last_audio_input:
                chunk_time = STREAMING_LIMIT / len(self.last_audio_input)

                if chunk_time != 0:
                    if self.bridging_offset < 0:
                        self.bridging_offset = 0

                    if self.bridging_offset > self.final_request_end_time:
                        self.bridging_offset = self.final_request_end_time

                    chunks_from_ms = round(
                        (self.final_request_end_time - self.bridging_offset)
                        / chunk_time
                    )

                    self.bridging_offset = round(
                        (len(self.last_audio_input) - chunks_from_ms) * chunk_time
                    )

                    for i in range(chunks_from_ms, len(self.last_audio_input)):
                        data.append(self.last_audio_input[i])

                self.new_stream = False

            # Use a blocking get() to ensure there's at least one chunk of
            # data, and stop iteration if the chunk is None, indicating the
            # end of the audio stream.
            chunk = self._buff.get()
            self.audio_input.append(chunk)

            if chunk is None:
                return
            data.append(chunk)
            # Now consume whatever other data's still buffered.
            while True:
                try:
                    chunk = self._buff.get(block=False)

                    if chunk is None:
                        return
                    data.append(chunk)
                    self.audio_input.append(chunk)

                except queue.Empty:
                    break

            yield b"".join(data)


def listen_print_loop(responses: object, stream: object) -> None:
    """Iterates through server responses and prints them.

    The responses passed is a generator that will block until a response
    is provided by the server.

    Each response may contain multiple results, and each result may contain
    multiple alternatives; for details, see https://goo.gl/tjCPAU.  Here we
    print only the transcription for the top alternative of the top result.

    In this case, responses are provided for interim results as well. If the
    response is an interim one, print a line feed at the end of it, to allow
    the next result to overwrite it, until the response is a final one. For the
    final one, print a newline to preserve the finalized transcription.

    Arg:
        responses: The responses returned from the API.
        stream: The audio stream to be processed.
    """
    interim_printed = False  # Flag to track if an interim result has been printed
    wakeword_detected_sent = False # Flag to track if wakeword detected was sent

    for response in responses:
        if get_current_time() - stream.start_time > STREAMING_LIMIT:
            stream.start_time = get_current_time()
            break

        if not response.results:
            continue

        result = response.results[0]

        if not result.alternatives:
            continue

        transcript = result.alternatives[0].transcript

        result_seconds = 0
        result_micros = 0

        if result.result_end_time.seconds:
            result_seconds = result.result_end_time.seconds

        if result.result_end_time.microseconds:
            result_micros = result.result_end_time.microseconds

        stream.result_end_time = int((result_seconds * 1000) + (result_micros / 1000))

        corrected_time = (
                stream.result_end_time
                - stream.bridging_offset
                + (STREAMING_LIMIT * stream.restart_counter)
        )
        # Display interim results, but with a carriage return at the end of the
        # line, so subsequent lines will overwrite them.
        if not wakeword_detected_sent:
            if re.search(r"\b(luna|stop)\b", transcript, re.I):
                publishWakewordDetected()
                sys.stdout.write("Wakeword detected...\n")
                wakeword_detected_sent = True

        if result.is_final:
            interim_printed = False
            wakeword_detected_sent = False
            publishTranscribedAudio(transcript)
            sys.stdout.write(str(corrected_time) + ": " + transcript + "\n")

            stream.is_final_end_time = stream.result_end_time
            stream.last_transcript_was_final = True

        else:
            stream.last_transcript_was_final = False

            if not interim_printed and transcript.strip():
                sys.stdout.write(f"\nSpeech Detected\n")
                publishSpeechDetected()
                sys.stdout.flush()
                interim_printed = True  # Set the flag to True after printing the first interim result


def find_device_index(device_name: str) -> int:
    """Find the device index for the given device name."""
    p = pyaudio.PyAudio()
    device_index = None
    for i in range(p.get_device_count()):
        dev_info = p.get_device_info_by_index(i)
        if device_name in dev_info['name']:
            device_index = i
            break
    p.terminate()
    return device_index


def main() -> None:
    """start bidirectional streaming from microphone input to speech API"""
    device_name = "Microphone (TKGOU PnP USB Microphone)"
    device_index = find_device_index(device_name)

    if device_index is None:
        print(f"Microphone '{device_name}' not found.")
        sys.exit(1)

    client = speech.SpeechClient()
    config = speech.RecognitionConfig(
        encoding=speech.RecognitionConfig.AudioEncoding.LINEAR16,
        sample_rate_hertz=SAMPLE_RATE,
        language_code="en-US",
        max_alternatives=1,
    )

    streaming_config = speech.StreamingRecognitionConfig(
        config=config, interim_results=True
    )

    mic_manager = ResumableMicrophoneStream(SAMPLE_RATE, CHUNK_SIZE, device_index)

    with mic_manager as stream:
        while not stream.closed:
            sys.stdout.write(
                "\n" + ": NEW REQUEST\n"
            )

            stream.audio_input = []
            audio_generator = stream.generator()

            requests = (
                speech.StreamingRecognizeRequest(audio_content=content)
                for content in audio_generator
            )

            responses = client.streaming_recognize(streaming_config, requests)

            # Now, put the transcription responses to use.
            listen_print_loop(responses, stream)

            if stream.result_end_time > 0:
                stream.final_request_end_time = stream.is_final_end_time
            stream.result_end_time = 0
            stream.last_audio_input = []
            stream.last_audio_input = stream.audio_input
            stream.audio_input = []
            stream.restart_counter = stream.restart_counter + 1

            if not stream.last_transcript_was_final:
                sys.stdout.write("\n")
            stream.new_stream = True


if __name__ == "__main__":
    main()