import serial
import time
import zmq
import json
import random
import threading

# Initialize ZeroMQ context and socket
context = zmq.Context()
socket = context.socket(zmq.SUB)
socket.connect("tcp://localhost:12348")
socket.setsockopt_string(zmq.SUBSCRIBE, '')  # Subscribe to all topics
print("Connected to ZMQ server")

gaze_aversion_active = False  # Initialize the global variable
robot_gaze_direction = "user"

# Initialize the serial connection to Arduino
try:
    ser = serial.Serial('COM3', 9600, timeout=1)
    time.sleep(2)  # Allow time for the serial connection to establish
    print("Serial connection established")
except Exception as e:
    print(f"Failed to connect to Arduino: {e}")
    exit(1)

# Command mapping from string to serial command
# command_mapping = {
#     'lookAtUser': '0,1',
#     'lookAtScreen': '0,2',
#
#     'lookAwayLeft': '0,3',
#     'lookAwayRight': '0,4',
#     'lookAwayUp': '0,5',
#
#     'leftNod': '0,6',
#     'rightNod': '0,7',
#
#     'leftDoubleNod': '0,8',
#     'rightDoubleNod': '0,9',
#
#     'thinking': '0,10',
# }
#

def start_gaze_aversion():
    global gaze_aversion_active
    global robot_gaze_direction
    gaze_aversion_active = True
    time.sleep(4)  # Wait for 2 seconds
    while gaze_aversion_active:
        # Randomly choose to lookAwayLeft, lookAwayRight
        if (robot_gaze_direction == "user"):
            look_away_cmd = random.choice(["0,3", "0,4"])
            if(look_away_cmd == "0,3"):
                robot_gaze_direction = "left"
            elif(look_away_cmd == "0,4"):
                robot_gaze_direction = "right"
            send_command(look_away_cmd)

        time.sleep(4)  # Look away for 4 seconds

        # Look at user
        send_command("0,1")
        robot_gaze_direction = "user"
        time.sleep(4)  # Look at the user for 2 seconds


def look_at_user():
    global gaze_aversion_active
    gaze_aversion_active = False
    send_command("0,1")


def look_at_screen():
    global gaze_aversion_active
    gaze_aversion_active = False
    send_command("0,2")


def look_away():
    global gaze_aversion_active
    global robot_gaze_direction
    gaze_aversion_active = False

    if (robot_gaze_direction == "user"):
        look_away_cmd = random.choice(["0,3", "0,4"])

        if (look_away_cmd == "0,3"):
            robot_gaze_direction = "left"
        elif (look_away_cmd == "0,4"):
            robot_gaze_direction = "right"

        send_command(look_away_cmd)


def nod():
    nod_cmd = random.choice(["0,6", "0,7"])
    send_command(nod_cmd)


def double_nod():
    nod_cmd = random.choice(["0,8", "0,9"])
    send_command(nod_cmd)


def thinking():
    global gaze_aversion_active
    gaze_aversion_active = False
    send_command("0,10")


def shut_down():
    global gaze_aversion_active
    gaze_aversion_active = False
    send_command("0,1")

#def send_command(command_str):
#    print(f"Sending command to Arduino: {command_str}")
#    ser.write(command_str.encode())
#    ser.write(b'\n')  # Send newline to signify end of command
#    print(f"Command sent: {command_str}")

def send_command(command_str):
    print(f"Sending command to Arduino: {command_str}")
    if ser.is_open:
        ser.write(command_str.encode())
        ser.write(b'\n')  # Send newline to signify end of command
        print(f"Command sent: {command_str}")
    else:
        print("Serial port not open")

if __name__ == '__main__':
    try:
        while True:
            try:
                # Set a timeout for the ZeroMQ socket
                socket.setsockopt(zmq.RCVTIMEO, 1000)  # 1000 ms timeout

                try:
                    message = socket.recv_string()
                except zmq.error.Again:
                    continue  # Timeout occurred, continue the loop

                if not message.strip():
                    print("Received empty message.")
                    continue

                print(f"Received raw message: {message}")
                try:
                    j = json.loads(message)
                except json.JSONDecodeError as e:
                    print(f"Error decoding JSON: {e}")
                    continue

                print(f"Received message: {j['message']}, originating time: {j['originatingTime']}")

                command_name = j['message']  # Receive command directly as a string

                if command_name == "lookAtUser":
                    look_at_user()
                elif command_name == "lookAtScreen":
                    look_at_screen()
                elif command_name == "lookAway":
                    look_away()
                elif command_name == "nod":
                    nod()
                elif command_name == "doubleNod":
                    double_nod()
                elif command_name == "thinking":
                    thinking()
                elif command_name == "startGazeAversion":
                    threading.Thread(target=start_gaze_aversion).start()
                else:
                    look_at_user()
                    print(f"Unknown command {command_name}")
                response = ser.readline().decode().strip()  # Read response from Arduino
                print(f"Arduino response: {response}")
            except KeyboardInterrupt:
                shut_down()
                print("Interrupted by user")
                break
            except Exception as e:
                print(f"Error processing message: {e}")
    except Exception as e:
        print(f"Server error: {e}")
    finally:
        ser.close()  # Ensure the serial connection is closed on exit
        socket.close()
        context.term()
        print("Server shut down")
