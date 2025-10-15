import socket
import asyncio
import json

SERVER_IP = "127.0.0.1"
SERVER_PORT = 9000

async def send_data(sock: socket.socket, data: dict) -> int:
    
    message = json.dumps(data).encode("utf-8")
    await asyncio.get_event_loop().sock_sendall(sock, message)
    return len(message)

async def receive_data(sock: socket.socket) -> str:
    
    loop = asyncio.get_event_loop()
    buffer = await loop.sock_recv(sock, 4096)
    if not buffer:
        raise ConnectionError("Server closed the connection")
    return buffer.decode("utf-8")

async def main():
    #  Create and connect socket
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    sock.setblocking(False)
    await asyncio.get_event_loop().sock_connect(sock, (SERVER_IP, SERVER_PORT))
    print(f"Connected to server {SERVER_IP}:{SERVER_PORT}")

    #  Send role
    role = {"role": "Subscriber"}
    await send_data(sock, role)
    print("Role sent.")

    # 3 Send subscription
    while True:
        name = input("Subscriber Name : ").strip()
        if not name:
            print("Input a valid name")
            continue
        topic = input("To what topic do you subscribe : ").strip()
        if not topic:
            print("Input a valid topic name")
            continue

        subscription = {"SubscriberName": name, "TopicName": topic}
        await send_data(sock, subscription)
        print(f"Subscribed to topic '{topic}' as '{name}'")
        break

    #  Receive messages
    print("Waiting for messages...")
    leftover = ""  

    try:
        while True:
            data = await receive_data(sock)
            data = leftover + data  

            # Split multiple messages that may arrive back-to-back
            messages = data.replace('}{', '}|{').split('|')
            leftover = ""  # reset leftover

            for i, msg_str in enumerate(messages):
                try:
                    msg = json.loads(msg_str)
                    if isinstance(msg, dict):
                        topic = msg.get("TopicName")
                        content = msg.get("MessageContent")
                        print(f"New message on topic '{topic}': {content}")
                    else:
                        
                        print(f"Received message: {msg}")
                except json.JSONDecodeError:
                    if i == len(messages) - 1:
                        leftover = msg_str
                    else:
                        print(f"Received malformed message: {msg_str}")

    except ConnectionError:
        print("Disconnected by broker.")
    except Exception as ex:
        print("Error:", ex)
    finally:
        sock.close()

if __name__ == "__main__":
    asyncio.run(main())
