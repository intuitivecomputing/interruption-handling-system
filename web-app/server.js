const express = require("express");
const http = require("http");
const socketIo = require("socket.io");
const cors = require("cors");
const msgpack = require("msgpack-lite");
const zmq = require("zeromq");
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
let itemList = Array(7).fill(null); // Initialize with 7 null values
/**
 * Handles WebSocket connections, logging when clients connect and disconnect.
 */
io.on("connection", (socket) => {
    console.log("New client connected");
    socket.on("update-list", (data) => {
        console.log("Received updated list from client:", data);
        itemList = data;
        io.emit("update-list", itemList); // Broadcast the parsed message to all clients
    });
    socket.on("disconnect", () => {
        console.log("Client disconnected");
    });
});

// Initialize the ZeroMQ publisher socket
const publisherSocket = zmq.socket("pub");
publisherSocket.bindSync("tcp://127.0.0.1:12347");
console.log('ZeroMQ publisher bound to port 12347');
function sendUpdatedList(newList) {
    var payload = {
        message: newList,
        originatingTime: new Date().toISOString()
    };
    publisherSocket.send(["updateItems", JSON.stringify(payload)]);
    console.log('Updated list:', payload);
}

function sendStarted() {
    var payload = {
        message: "[task started]",
        originatingTime: new Date().toISOString()
    };
    publisherSocket.send(["isStarted", JSON.stringify(payload)]);
    console.log('User started task');
}

function sendFinished() {
    var payload = {
        message: "[task finished]",
        originatingTime: new Date().toISOString()
    };
    publisherSocket.send(["isFinished", JSON.stringify(payload)]);
    console.log('User finished task');
}

// Subscribe function to listen for new messages and populate the queue
function subscribe() {
    const subscriber = zmq.socket("sub");
    subscriber.connect("tcp://127.0.0.1:12346"); // Correct port number
    subscriber.subscribe("itemList");
    console.log("Subscribed to the topic 'itemList'");
    subscriber.on("message", (topic, msg) => {
        const decodedMessage = msgpack.decode(msg);
        console.log(decodedMessage.message);
        const newList = parseMessage(decodedMessage.message); // Implement parseMessage to parse the zmq message
        if (newList) {
            itemList = newList;
            io.emit("change-itemList", itemList); // Broadcast the parsed message to all clients
        }
    });
}
// Call the subscribe function to start listening for messages
subscribe();
app.get("/", (req, res) => {
    res.send("Welcome to the Express server");
});

app.post("/update_item_list", (req, res) => {
    const { message } = req.body; // Extract message from the request body
    console.log("Received payload:", req.body);
    const item_list = parseMessage(message); // Pass the message string to parseMessage
    if (item_list && Array.isArray(item_list)) {
        itemList = item_list;
        sendUpdatedList(message);
        io.emit("update-list", itemList);  // Notify all clients about the update
        res.json(itemList);
    } else {
        console.error("Invalid item_list:", item_list);
        res.status(400).send({ error: "Invalid item_list" });
    }
});

let taskStarted = false; // Flag to track if task has already started
let taskDone = false;

app.post("/started", (req, res) => {
    if (!taskStarted) {
        sendStarted();
        taskStarted = true; // Update the flag once task is started
        res.status(200).send({ message: "Task started successfully" });
    } else {
        console.log("Task already started, ignoring additional request.");
        res.status(200).send({ message: "Task already started" });
    }
});

app.post("/finished", (req, res) => {
    if (!taskDone) {
        sendFinished();
        taskStarted = false;
        res.status(200).send({ message: "Task finished" });
    } else {
        console.log("Task already finished, ignoring additional request.");
        res.status(200).send({ message: "Task already finished" });
    }
});


/**
 * Parses a semicolon-separated message string into an item list.
 *
 * @param {string} message - The semicolon-separated message string.
 * @returns {Array} - Parsed list of items.
 */
function parseMessage(message) {
    const itemMap = {
        flashlight: "flashlight",
        jackknife: "jackknife",
        map: "air map of the area",
        compass: "magnetic compass",
        firstAid: "first-aid kit",
        salt: "bottle of salt tablets",
        water: "1 quart of water per person",
        sunglasses: "1 pair of sunglasses per person",
        overcoat: "1 overcoat per person",
        parachute: "parachute",
        raincoat: "plastic raincoat",
        pistol: "45 caliber pistol",
        animalBook: "a book entitled 'Edible Animals of the Desert'",
        vodka: "2 bottles of vodka",
        mirror: "cosmetic mirror"
    };
    const currentItemList = Array(7).fill(null);
    try {
        const parts = message.split(';').map(part => part.trim());
        parts.forEach((part, i) => {
            if (part !== "null" && i < currentItemList.length) {
                currentItemList[i] = itemMap[part] || part;
            }
        });
        return currentItemList;
    } catch (error) {
        console.error("Failed to parse message:", error);
        return null;
    }
}

// Server listening on environment-defined port or default to 4000
const PORT = process.env.PORT || 4000;
server.listen(PORT, () => {
    console.log(`Server running on port ${PORT}`);
});