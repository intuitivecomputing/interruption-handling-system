/**
 * @fileoverview
 * Setup and management of the Express server with Socket.IO for real-time interaction.
 * This file configures the HTTP and WebSocket server, handling routes to manage
 * facial expression changes through a web interface.
 *
 * @requires express: Framework for handling HTTP server functionalities.
 * @requires http: Module to create HTTP server.
 * @requires socket.io: Enables real-time, bidirectional communication.
 * @requires cors: Middleware to enable CORS (Cross-Origin Resource Sharing).
 */

const express = require("express");
const http = require("http");
const socketIo = require("socket.io");
const cors = require("cors");
const fs = require('fs');
const msgpack = require("msgpack-lite");

// Initialize express application
const app = express();
const server = http.createServer(app);

// Middleware to parse JSON and enable CORS
app.use(express.json());
app.use(cors());

/**
 * WebSocket configuration allowing connections from any origin and handling
 * GET and POST methods.
 */
const io = socketIo(server, {
  cors: {
    origin: "*",
    methods: ["GET", "POST"],
  },
});

/**
 * Handles WebSocket connections, logging when clients connect and disconnect.
 */
io.on("connection", (socket) => {
  console.log("New client connected");
  socket.on("disconnect", () => {
    console.log("Client disconnected");
  });
});

// Subscribe function to listen for new messages and populate the queue
async function subscribe() {
    const zmq = require("zeromq");
    const subscriber = new zmq.Subscriber();
    subscriber.connect("tcp://127.0.0.1:3000");
    subscriber.subscribe("facialExpressions");
    console.log("Subscribed to the topic 'facialExpressions'");

    for await (const [topic, msg] of subscriber) {
        const decodedMessage = msgpack.decode(msg);
        console.log(decodedMessage.message);
        const expression = decodedMessage.message; // Implement parseMessage to parse the zmq message
        //io.emit("change-expression", "reset");
        io.emit("change-expression", expression);
        //res.send({ status: "expression changed" });
    }
}

/**
 * Route to change facial expressions. Accepts expression data via POST request
 * and broadcasts it to all connected clients.
 *
 * @route POST /express
 * @param req - HTTP request object containing expression data.
 * @param res - HTTP response object for sending back status.
 */
// POST endpoint to trigger processing the queue
app.post('/processExpressions', (req, res) => {
    processExpressionQueue().then(() => {
        res.status(200).send({ message: 'Expressions processed successfully' });
    }).catch((error) => {
        console.error('Error processing expressions:', error);
        res.status(500).send({ message: 'Error processing expressions' });
    });
});

// Route to handle POST request to change expression
app.post('/express', (req, res) => {
    console.log("received expression:" + req.body.expression)
    const expression = req.body.expression;
    if (expression) {
        io.emit("change-expression", expression);
        res.status(200).send({ message: 'Expression changed successfully' });
    } else {
        res.status(400).send({ message: 'Expression not provided' });
    }
});

app.get('/emit-event', (req, res) => {
  const { event, data } = req.query;
  console.log(`Received request: event=${event}, data=${data}`);
  if (event && data) {
    try {
      io.emit(event, data);
      res.status(200).send(`Event ${event} emitted with data: ${data}`);
      console.log(`Response: Event ${event} emitted with data: ${data}`);
    } catch (error) {
      console.error('Error emitting event:', error);
      res.status(500).send('Error emitting event');
    }
  } else {
    console.error('Missing event or data');
    res.status(400).send('Missing event or data');
    console.log('Response: Missing event or data');
  }
});


/**
 * Route to execute a function received as a string. Evaluates and executes the function,
 * emitting results to clients. This route is used for dynamic expression adjustments.
 *
 * @route POST /exec-func
 * @param req - HTTP request object containing the function as a string.
 * @param res - HTTP response object for sending back execution status.
 */


// Server listening on environment-defined port or default to 3000
const PORT = process.env.PORT || 3000;
server.listen(PORT, () => {
    console.log(`Server running on port ${PORT}`);
    subscribe().catch(err => console.error(err));
});

/**
 * Future Considerations:
 * - Implement error handling for both routes to manage erroneous or malicious input.
 * - Review and address potential security implications of executing functions received from the network.
 */
