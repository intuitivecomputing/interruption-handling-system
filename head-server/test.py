import serial
import time
import random
import threading

# Define the serial port and baud rate.
ser = serial.Serial('COM3', 9600, timeout=1)

# Allow some time for the serial connection to establish
time.sleep(2)

gaze_aversion_active = False  # Initialize the global variable
gaze_thread = None  # Variable to keep track of the gaze aversion thread

def send_command(command):
    """Send a command to the Arduino."""
    ser.write(command.encode())
    ser.write(b'\n')  # Send newline to signify end of command
    print(f"Sent: {command}")
    # Optionally, wait for a response
    response = ser.readline().decode().strip()
    print(f"Received: {response}")

def start_gaze_aversion():
    global gaze_aversion_active
    gaze_aversion_active = True
    #time.sleep(2)  # Wait for 2 seconds
    while gaze_aversion_active:
        # Randomly choose to lookAwayLeft, lookAwayRight
        look_away_cmd = random.choice(["0,3", "0,4"])
        send_command(look_away_cmd)
        time.sleep(4)  # Look away for 4 seconds

        # Look at user
        send_command("0,1")
        time.sleep(3)  # Look at the user for 2 seconds

def stop_gaze_aversion():
    global gaze_aversion_active
    gaze_aversion_active = False
    if gaze_thread and gaze_thread.is_alive():
        gaze_thread.join()  # Wait for the thread to finish

def stop_gaze_aversion_and_look_away():
    global gaze_aversion_active
    gaze_aversion_active = False

def stop_gaze_aversion_and_look_away():
    global gaze_aversion_active
    gaze_aversion_active = False
    look_away_cmd = random.choice(["0,3", "0,4"])
    send_command(look_away_cmd)

def stop_gaze_aversion_and_look_at_user():
    global gaze_aversion_active
    gaze_aversion_active = False
    send_command("0,1")

def look_at_task():
    global gaze_aversion_active
    gaze_aversion_active = False
    send_command("0,2")

def execute_commands(commands):
    global gaze_thread
    for cmd in commands:
        # If a new command is received and a gaze aversion thread is running, stop it first
        if gaze_thread and gaze_thread.is_alive():
            stop_gaze_aversion()

        if cmd == "0,2":
            look_at_task()
        elif cmd == "0,11":
            gaze_thread = threading.Thread(target=start_gaze_aversion)
            gaze_thread.start()
        elif cmd == "0,12":
            stop_gaze_aversion_and_look_away()
        elif cmd == "0,13":
            stop_gaze_aversion_and_look_at_user()
        else:
            send_command(cmd)

        time.sleep(10)  # Wait a bit between commands

# List of commands
commands = [
    # "0,10",  # lookAwayUpLeft
    # "0,3",  # lookAwayLeft
    # "0,4",  # lookAwayRight
    # "0,5",  # lookAwayUp
    # "0,6",  # Left Nod
    # "0,7",  # Left Double Nod
    # "0,8",  # Right Nod
    # "0,9",   # Right Double Nod
    # "0,2",  # Look at Screen
    # "0,1",  # Look at User
    # "0,6",  # Left Nod
    # "0,8",  # Right Nod
    #"0,11",  # Start Gaze Aversion
    #"0,12",  # Stop Gaze Aversion and look away
    # "0,11",  # Start Gaze Aversion
    # "0,2", # Look at task
    # "0,11",  # Start Gaze Aversion
    # "0,1", # Look at user
    # "0,7",  # Left Double Nod
    # "0,13",  # Stop Gaze Aversion and look at user
    '0,10',  # thinking
    #'0,1',  # lookAtUser
    #'0,2',  # lookAtTask
    #'0,9',  # leftNod
    # "0,7",  # Right Nod
    #'0,1',  # lookAtUser
    #"0,11",  # lookAwayLeft
    '0,1',  # lookAtUser
]

# Execute commands
execute_commands(commands)

# Close the serial connection
ser.close()