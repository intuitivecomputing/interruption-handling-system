'''
expected example json input:
{
'command': 'lookAtUser'
}
'''


import serial
import time
import zmq, json

socket = zmq.Context().socket(zmq.SUB)
socket.connect("tcp://localhost:12345")
socket.setsockopt_string(zmq.SUBSCRIBE, '')  # Use setsockopt_string
print("connected")

# context = zmq.Context()
# socket = context.socket(zmq.SUB)
# socket.connect("tcp://localhost:1235")
# context.setsockopt(zmq.SUBSCRIBE, '') # '' means all topics
# print("Subscribed to all topics on port 1235")
#
# Initialize the serial connection
ser = serial.Serial('COM3', 9600, timeout=1)
time.sleep(2)  # Allow time for the serial connection to establish
print("Serial connection established")

# Command mapping from string to serial command
command_mapping = {
    'lookAtUser': '0,1',
    'lookAtScreen': '0,2',
    'lookAway': '0,3',
    'headPanLeft': '0,4',
    'headPanRight': '0,5',
    'leftNod': '0,6',
    'leftDoubleNod': '0,7',
    'rightNod': '0,8',
    'rightDoubleNod': '0,9'
}

def send_command(command):
    if command and command in command_mapping:
        command = command_mapping[command]
        ser.write(command.encode())
        ser.write(b'\n')  # Send newline to signify end of command

if __name__ == '__main__':
    try:
        while True:
            [topic, message] = socket.recv_multipart()
            j = json.loads(message)
            print(j['message'])
            print(j['originatingTime'])

            command_name = j['message']  # Receive command directly as a string
            if command_name and command_name in command_mapping:
                command = command_mapping[command_name]
                ser.write(command.encode())
                ser.write(b'\\n')  # Send newline to signify end of command
                ser.readline().decode().strip()  # Read response but do not send it back
            else:
                print(f"Received invalid command: {command_name}")
    finally:
        ser.close()  # Ensure the serial connection is closed on exit
        socket.close()
        #context.term()