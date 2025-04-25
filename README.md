# Interruption Handling System for Conversational Robots

## Introduction
This repository accompanies the paper **"Interruption Handling for Conversational Robots"**. It contains the source code for an example of our interruption handling system integrated into an
LLM-powered social robot built on the Platform for Situated Intelligence (\psi). We prompt engineered a large language model (GPT-4o-2024-05-13) to generate contextualized robot speech and select fitting facial expressions, head movements, and task actions for a timed decision-making (dessert survival simulation), a contentious discussion (whether the federal government should abolish capital punishment), and a question and answer task (space exploration themed). For more details on the interruption handling system, please refer to our [manuscript](https://arxiv.org/pdf/2501.01568). For more detailed description of the robot implementation, see the [supplemental materials](https://intuitivecomputing.github.io/publications/2025-rss-cao-supp.pdf). 

- - - -

## Software and Hardware Requirements for Running
Environment:
- Windows 10 x64

Prerequisites:
- Python3
- Microsoft Platform for Situated Intelligence ([\psi](https://github.com/microsoft/psi))
- node.js
- React

- - - -

## Contents
In overview, the SocialRobot folder contains files to run the main psi program. The expressive-face-server contains the web app that dispays the robot's face. The google-speech-to-text folder contains the python script that runs the speech recognition and wakeword detection service. The head-server folder contains the head code for the arduino as well as the python head server that communicates between Psi and the Arduino. The web-app folder contains files to run the React web app containing the task information. 

## SocialRobot
There are three folders each containing the program to run for each task (practice, discussion, survival) in the study. Each folder contains the following files: a Google speech to text component, (_GoogleSpeechToTextComponent_), a Google text to speech component (_GoogleTextToSpeechComponent_), a component for classifying the type of interruption (LLMInterruptionHandlingComponent), a component to generate robot behavior (_LLMResponseComponent_), timer helper components (_TimerSecondsComponent_), and the main program. 

Open the SocialRobot.solutions in visual studios to select the program to run. 
_prompts.json_ contains all the prompts used in the study by the social robot programs. 

## expressive-face-server
_face.css _ contains the source code for the hand-crafted robot facial expressions. New facial expressions can be crafted by adjusting the positions and timing of the elements. 
_face.html_ displays the robot face called by the API. 
_face-testing.html_ display buttons to select the facial expression to display to help with testing. 
_server.js_ contains an Express server that takes HTTP requires to change the robot's facial expressions displayed on _face.html_. 

## google-speech-to-text
_google-speech-to-text-luna-experimental.py_ contains a program that runs the google speech to text service and detects the wakewords ("Luna" and "stop") from the transcribed interim speech. 

## head-server
The _head_movement_arduino_ folder contains a _head_movement_arduino.ino_ program that controls three servo motors in the robot. 
_test.py_ contains a program to help test the connection to the arduino. 
_testServer.py_ contains a program to help communicate head movements between main psi program and the arduino. 

## 

- - - -

## Usage

### Expressive Face
To launch the expressive face component:
1. Start the expressive face server:
  ```
  cd expressive-face-server
  python server.js
  ```
2. Open `face.html` in your browser to view the animated robot face.

### Task Interface
To launch the task interface:
1. Start the task server:
  ```
  cd web-app
  python server.js
  ```
2. Run the task interface web application:
  ```bash
  cd task-interface
  npm start
  ```

- - - -
## Questions

For questions that are not covered in this README, we welcome developers to open an issue or contact the author at [shiyecao@cs.jhu.edu](mailto:shiyecao@cs.jhu.edu). 

- - - -

## BibTeX
If you use our system in a scientific publication, please cite our work:
```
@inproceedings{cao2025Interruption,
  title={Interruption Handling for Conversational Robots},
  author={Cao, Shiye and Moon, Jiwon and Mahmood, Amama and Antony, Victor Nikhil and Xiao, Ziang and Liu, Anqi and Huang, Chien-Ming},
  year={2025}
}
```
