# Expressive Face Server

## Overview

The Expressive Face Server is a web-based application that controls and tests expressive facial animations via a web interface. It utilizes a combination of HTML, CSS, JavaScript, and server-side technologies to create an interactive environment where users can trigger facial expressions on a virtual face.

## Features

- **Real-time Facial Expression Control**: Use WebSocket communication to instantly update facial expressions based on user interaction.
- **Dynamic Animation Testing Interface**: Provides buttons to manually trigger animations such as blinking, talking, listening, etc.
- **Responsive Design**: Fully responsive HTML and CSS that adapts to different device screens.
- **Extensible**: Server and client-side code that can be extended for more complex behaviors and additional features.

## Installation

To set up the Expressive Face Server, follow these steps:

1. Clone the repository to your local machine.
2. Navigate to the project directory.
3. Install the required npm packages:

   ```bash
   npm install
   ```

4. Start the server:
   `bash
node server.js
`
   The server will start running on http://localhost:3000.

## Architecture

This project includes several components that work together to enable real-time facial animation:

- **Server (`server.js`)**: Configures an HTTP and WebSocket server using Express and Socket.IO to handle real-time interactions.
- **Main Interface (`face.html`)**: Hosts the facial interface that reacts to expressions triggered via web sockets.
- **Testing Interface (`face-testing.html`)**: Provides a user interface for manually testing different facial expressions.
- **Animation Controller (`eyes.js`)**: Manages and animates the facial expressions based on data received from the server.
- **Stylesheet (`face.css`)**: Defines the styles for the facial features and layout.

## HTML Usage

Once the server is running, open the following URLs in your browser to interact with the application:

- **Main Interface**: http://localhost:3000/face.html
- **Testing Interface**: http://localhost:3000/face-testing.html | Use the buttons provided in the testing interface to trigger different facial expressions.

## Server Usage

The server provides two POST endpoints to control facial expressions through the web interface:

### 1. Change Facial Expression

This endpoint allows you to change the facial expression by sending specific commands.

- **Endpoint**: `/express`
- **Method**: POST
- **Body**:

  ```json
  {
    "expression": "talking" // Replace "talking" with any predefined expression command.
  }
  ```

### 2. Execute Custom Function

- **Endpoint**: `/exec-func`
- **Method**: POST
- **Body**:

  ```json
  {
    "program": "anime.timeline({ easing: 'easeInOutQuad' }).add({ targets: this.elements.leftEye, translateX: '-25%', duration: 500 }).add({ targets: this.elements.rightEye, translateX: '25%', duration: 500 })"
  }
  ```

  You may generate these program using the prompt in face2codePrompt.json and any SOTA LLM. Reach out vantony1@jhu.edu if you have questions.

## Dependencies

- `Express`: Framework for handling HTTP server functionalities.
- `Socket.IO`: Facilitates real-time, bidirectional communication between web clients and the server.
- `Anime.js`: Enables smooth and complex animations.
- `Cors`: Allows the server to accept requests from different origins.

## Contributing

Contributions to this project are welcome! If you're interested in improving the Expressive Face Server, please follow these steps:

1. Fork the repository.
2. Create a new branch for your feature (`git checkout -b feature-xyz`).
3. Commit your changes (`git commit -am 'Add some feature'`).
4. Push to the branch (`git push origin feature-xyz`).
5. Create a new Pull Request.

Please ensure your commits are clear and the code is well-documented to help maintain the project's quality.

## License

The Expressive Face Server is open-sourced under the MIT license, which permits reuse, distribution, and modification for both private and commercial purposes, provided that the license is included with any substantial portions of the software.
