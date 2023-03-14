import socket
import threading
import time
import random

def handle_client(client_socket, client_address):
    print('Connection from', client_address)

    flag = True

    # add client socket to the list
    with lock:
        clients.append(client_socket)

    # send random values at regular intervals
    while flag:
        # generate random value
        value = random.randint(0, 100)
        
        # send value to all clients with message delimiter
        data = str(value) + '\n'
        with lock:
            for c in clients:
                try:
                    c.sendall(data.encode('utf-8'))
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

# loop indefinitely to handle multiple client connections
while True:
    # accept new client connection
    client_socket, client_address = sock.accept()

    # create new thread to handle client
    client_thread = threading.Thread(target=handle_client, args=(client_socket, client_address))
    client_thread.start()