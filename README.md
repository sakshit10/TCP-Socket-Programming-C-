# TCP-Socket-Programming-C-
TCP Client-Server Socket Programming Assignment in C# with AES Encryption

# TCP Socket Programming Assignment

A professional C# implementation of TCP Client-Server communication with nested dictionary lookup and encryption support.


Project Description

This project implements a TCP-based client-server application in C# that demonstrates:
- Socket programming using System.Net.Sockets
- Asynchronous programming with async/await
- Network protocol design and implementation
- Data serialization and communication

Features

Part A: Basic TCP Communication
- TCP server listening on configurable port (default: 5555)
- Nested dictionary data structure for key-value lookup
- Client sends queries in format: "SetX-Key"
- Server responds with timestamps based on retrieved value
- 1-second interval between responses

Part B: Encryption (Optional)
- AES-256 encryption support
- Secure message transmission
- Configurable encryption keys

Architecture

Server
- Accepts multiple concurrent client connections
- Handles each client in separate async task
- Performs nested dictionary lookup
- Sends timestamped responses

Client
- Connects to server via TCP
- Interactive console interface
- Sends formatted queries
- Displays server responses

Data Structure
```csharp
{
  "SetA": [{"One": 1, "Two": 2}],
  "SetB": [{"Three": 3, "Four": 4}],
  "SetC": [{"Five": 5, "Six": 6}],
  "SetD": [{"Seven": 7, "Eight": 8}],
  "SetE": [{"Nine": 9, "Ten": 10}]
}
```

How to Run

Prerequisites
- Visual Studio 2022
- .NET 6.0 or higher

Steps
1. Open `ProgrammingAssignment.sln` in Visual Studio
2. Build Solution (Ctrl+Shift+B)
3. Run Server:
   - Right-click Server project
   - Debug → Start New Instance
4. Run Client:
   - Right-click Client project
   - Debug → Start New Instance
5. Test with commands:
   - `SetA-Two` → Returns 2 timestamps
   - `SetE-Ten` → Returns 10 timestamps
   - `SetX-Invalid` → Returns EMPTY

Test Cases

| Input | Expected Output | Status |
|-------|----------------|--------|
| SetA-One | 1 timestamp | ✅ Pass |
| SetA-Two | 2 timestamps | ✅ Pass |
| SetE-Ten | 10 timestamps | ✅ Pass |
| SetX-Invalid | EMPTY | ✅ Pass |
| InvalidFormat | Error message | ✅ Pass |


Server Running
```
[STARTING] Server is starting on 127.0.0.1:5555
[LISTENING] Server is listening for connections...
[NEW CONNECTION] 127.0.0.1:54321 connected
```

Client Interaction
```
[INPUT] Enter request: SetA-Two
[SENT] SetA-Two
[RESPONSE 1] 16-10-2025 12:48:08
[RESPONSE 2] 16-10-2025 12:48:09
```

Technologies Used
- C# 10
- .NET 6.0
- System.Net.Sockets (TCP)
- System.Threading.Tasks (Async)
- System.Security.Cryptography (AES)

Key Concepts Demonstrated
- TCP/IP networking
- Client-server architecture
- Asynchronous programming
- Multi-threading
- Error handling
- Protocol design

Assignment Requirements

✅ Part A: Basic TCP client-server communication
✅ Nested dictionary lookup implementation
✅ Dynamic response based on value
✅ Timestamp responses with 1-second intervals
✅ EMPTY response for invalid queries
✅ Part B: Encryption/decryption support
✅ Multiple concurrent client support
✅ Clean, documented code


License
This project is submitted as an assignment.

---
**Developed with ❤️ using C# and .NET**

Your assignment is complete and professional!

**Your GitHub Link:**
```
https://github.com/sakshit10/ProgrammingAssignment
