# Event-Driven Microservice Sample

Proof of concept using **RabbitMQ** for asynchronous communication and eventual consistency using integration events storing data each service on your own database and using outbox pattern 
> The sample is not intended for production use due to lack of other mechanisms needed to enforce resiliency, security, and best practices.

The samples is based on itnext.io post referenced at the end. The solution contains two microservices as PostService and UserService. Firing a command to create an user will raise an event consumed by PostService.

## Requirements

To run this sample you need:
- .NET Core 5
- Docker (on Linux, Mac or Windows, WSL backend is recommended for Docker Desktop on Windows)

## How to Run

1. Prepare a RabbitMQ env as bellow

> docker run -d  -p 15672:15672 -p 5672:5672 --hostname my-rabbit --name some-rabbit rabbitmq:3-management

2. Open http://localhost:15672/ on your browser with the username "guest" and the password "guest".

3. Create an Exchange with the name **"user"** with of type **"Fanout"**. After that create a queue named **"user.postservice"** for the exchange.

4. The two services on solution are set to run simultaneously (in visual studio) using a SQLite database.

5. Open the swagger for https://localhost:5012/swagger/index.html for PostService alongside with the swagger for UserService.

6. Create an user on UserService and note that an event will be emmited to PostService in the terminal (The implementation is on Program.cs).

7. Create an post on PostService usaing the user id created previously then get all the posts and see the id and name of user created trhough the exchange.

## Reference
https://itnext.io/how-to-build-an-event-driven-asp-net-core-microservice-architecture-e0ef2976f33f
https://itnext.io/the-outbox-pattern-in-event-driven-asp-net-core-microservice-architectures-10b8d9923885