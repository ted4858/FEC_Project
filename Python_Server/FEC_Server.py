import socket
import threading
import time
import random
import psycopg2

# Connect to the database
conn = psycopg2.connect(
    host="localhost",
    port="3000",
    database="FEC_DB",
    user="postgres",
    password="1234",
)

# Open a cursor to perform database operations
cur = conn.cursor()

data = []
value = []
data_str = ""
cycles_per_second = 1

def handle_data():
    global data
    global value
    global data_str
    global cycles_per_second

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

    # send random values at regular intervals
    while True:
        # create 2D array with title, 8 random values, and current time
        data = [["Channel", "PreBER", "Pre Errors", "Corrected", \
                 "PostBER", "Margin", "#Bits", "cycles_per_second", "ResultValue", \
                 "CurrentTime"]]
        for i in range(4):
            value = [f"Channel {i+1}"]
            # 이 반복문에 추후 데이터 계산 후 전송할 수
            for j in range(8):
                if (j == 6):
                    value.append(cycles_per_second)
                else :
                    value.append(random.randint(0, 100))
            value.append(time.strftime("%Y-%m-%d %H:%M:%S"))
            data.append(value)

            # Insert data into a table
            cur.execute(f"INSERT INTO Channel{i+1} (preber, \
                        preerrors, corrected, postber, margin, \
                        sharpbits, cycles_per_second, result_value, update_time) VALUES \
                        (%s, %s, %s, %s, %s, %s, %s, %s, %s)", \
                        (data[i+1][1], int(data[i+1][2]), int(data[i+1][3]), \
                         data[i+1][4], data[i+1][5], int(data[i+1][6]), \
                         int(data[i+1][7]), int(data[i+1][8]), data[i+1][9]))
            # Commit the transaction
            conn.commit()

        # send value to all clients with message delimiter
        data_str = "\n".join(["\t".join([str(val) for val in row]) for row in data])
        
        print(data)
        print(data_str)

        # wait for 1 second before sending the next value
        time.sleep(1)

    # Close the cursor and connection
    cur.close()
    conn.close()

def handle_client(client_socket, client_address):
    print('Connection from', client_address)

    flag = True

    # add client socket to the list
    with lock:
        clients.append(client_socket)

    # send random values at regular intervals
    while flag:
        with lock:
            for c in clients:
                try:
                    c.sendall(data_str.encode('utf-8'))
                except ConnectionResetError:
                    print(f"Client {client_address} forcibly closed the connection.")
                    clients.remove(c)
                    c.close()
                    flag = False

        # wait for 1 second before sending the next value
        time.sleep(1)

# create TCP socket
sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

# bind socket to a specific IP address and port
server_address = ('192.168.1.122', 6000)
sock.bind(server_address)

# listen for incoming connections
sock.listen()

# create a list to store client sockets
clients = []

# create a lock to synchronize access to the clients list
lock = threading.Lock()

# wait for a client to connect
print('Waiting for a connection...')

data_thread = threading.Thread(target=handle_data)
data_thread.start()

# loop indefinitely to handle multiple client connections
while True:
    # accept new client connection
    client_socket, client_address = sock.accept()

    # create new thread to handle client
    client_thread = threading.Thread(target=handle_client, args=(client_socket, client_address))
    client_thread.start()
