import socket
import threading
import time
import random
import psycopg2
import struct

# Connect to the database
conn = psycopg2.connect(
    host="localhost",
    port="5432",
    database="FEC_DB",
    user="user2",
    password="1234",
)

# Open a cursor to perform database operations
cur = conn.cursor()

# create 2D array with title, 8 random values, and current time
data = [["Channel", "PreBER", "Pre Errors", "Corrected", \
            "PostBER", "Margin", "#Bits", "cycles_per_second", "ResultValue", \
            "CurrentTime"]]
data_str = ""
cycles_per_second = [10, 10, 10, 10]
time_comparison = [10, 10, 10, 10]

# check database connection
while True:
    try:
        cur.execute("SELECT 1")
        conn.commit()
        print("Database connection is working.")
        break
    except psycopg2.OperationalError:
        print("Database connection error. Retrying...")
        time.sleep(5)

# define a function for each thread
def handle_channel(channel_index):
    global data
    global data_str
    global cycles_per_second
    global time_comparison

    while True:
        if cycles_per_second[channel_index-1] == 1 or cycles_per_second[channel_index-1] <= time_comparison[channel_index-1]:
            # create an array with 8 random values and current time
            values = []
            for j in range(8):
                if j == 6:
                    values.append(cycles_per_second[channel_index-1])
                else:
                    values.append(random.randint(0, 100))
            values.append(time.strftime("%Y-%m-%d %H:%M:%S"))

            # update data array for the channel
            data[channel_index][1:] = values

            # insert data into a table
            cur.execute(f"INSERT INTO Channel{channel_index} (preber, \
                        preerrors, corrected, postber, margin, \
                        sharpbits, cycles_per_second, result_value, update_time) VALUES \
                        (%s, %s, %s, %s, %s, %s, %s, %s, %s)", \
                        (data[channel_index][1], int(data[channel_index][2]), int(data[channel_index][3]), \
                        data[channel_index][4], data[channel_index][5], int(data[channel_index][6]), \
                        int(data[channel_index][7]), int(data[channel_index][8]), data[channel_index][9]))

            # commit the transaction
            conn.commit()

            # send value to all clients with message delimiter
            data_str = "\n".join(["\t".join([str(val) for val in row]) for row in data])

            time_comparison[channel_index-1] = 1
        else:
            # create an array with 8 random values and current time
            values = []
            for j in range(8):
                if j == 6:
                    values.append(cycles_per_second[channel_index-1])
                else:
                    values.append(data[channel_index][j+1])
            values.append(time.strftime("%Y-%m-%d %H:%M:%S"))

            # update data array for the channel
            data[channel_index][1:] = values

            time_comparison[channel_index-1] += 1

        # wait for 1 second before sending the next value
        time.sleep(1)

    # Close the cursor and connection
    cur.close()
    conn.close()

# create 4 threads and start them
threads = []

for i in range(4):
    # initialize data array for the channel
    data.append([f"Channel {i+1}", "", 0, 0, "", "", 0, 0, 0, ""])

    # start the thread
    t = threading.Thread(target=handle_channel, args=(i+1,))
    t.start()
    threads.append(t)

def handle_client(client_socket, client_address):
    print("\n----------------------------------------")
    print('Connection from', client_address)
    print("----------------------------------------")
    print()

    # send random values at regular intervals
    while True:
        with lock:
            print(data)
            print(data_str)

            try:
                client_socket.sendall(data_str.encode('utf-8'))
            except ConnectionResetError:
                print("\n----------------------------------------------------------------")
                print(f"Client {client_address} forcibly closed the connection.")
                print("----------------------------------------------------------------")
                client_socket.close()
                break
            except:
                print("\n----------------------------------------------------------------")
                print(f"Client {client_address} forcibly closed the connection.")
                print("----------------------------------------------------------------")
                client_socket.close()
                break

        # wait for 1 second before sending the next value
        time.sleep(1)

# function to receive and check data from client
def receive_data(client_socket):
    global cycles_per_second

    while True:
        try:
            # receive data from client
            receive_data = client_socket.recv(1024)
            # Decode the data into the channel and timeValue variables
            channel_num, = struct.unpack('i', receive_data[:4])
            cycles_per_second[channel_num-1] = int(receive_data[4:].decode('utf-8').strip())
            time_comparison[channel_num-1] = 1
        except ConnectionResetError:
            break

# create TCP socket
sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

# bind socket to a specific IP address and port
server_address = ('192.168.1.124', 6000)
sock.bind(server_address)

# listen for incoming connections
sock.listen()

# create a list to store client sockets
clients = []

# create a lock to synchronize access to the clients list
lock = threading.Lock()

# wait for a client to connect
print('Waiting for a connection...')

# loop indefinitely to handle multiple client connections
while True:
    # accept new client connection
    client_socket, client_address = sock.accept()

    # create new thread to handle client
    client_thread = threading.Thread(target=handle_client, args=(client_socket, client_address))
    client_thread.start()

    # create new thread to handle client
    receive_data_thread = threading.Thread(target=receive_data, args=(client_socket,))
    receive_data_thread.start()
