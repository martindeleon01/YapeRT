
# YapeRT

This is a distributed microservices system implemented with .NET 8 that simulates the processing of financial transactions, fraud validation, and asynchronous communication using Kafka. PostgreSQL is used as the shared persistence layer, and Docker is used for container orchestration








## Architecture Overview

The system consists of two microservices:

Transactions Service
- Receives new transactions via HTTP POST.
- Stores them in the Transactions table with status Pending.
- Persists messages to be sent in the SentMessages table (topic: "transaction-validate").

Includes:
- SendMessage: a background service that sends pending messages to Kafka.
- TransactionsStatusUpdate: a background service that consumes transaction-result messages and updates the transaction status accordingly.

AntiFraud Service
Subscribes to the "transaction-validate" Kafka topic.
Validates the transaction: If the amount is > 2000 or total daily amount by source or target account exceeds 20000, the transaction is Rejected.

Otherwise, it's Approved.

Persists the result (topic: "transaction-result") in the SentMessages table.
Messages are picked up and sent to Kafka by the SendMessage service in the Transactions microservice.

![alt text](https://raw.githubusercontent.com/martindeleon01/YapeRT/refs/heads/main/Diagram.jpg)

## Database

A shared PostgreSQL database: YapeDB

Tables used:
- Transactions: Stores all transaction records
- SentMessages: Track the messages to be sent via Kafka

## Messaging
Kafka is used for asynchronous communication via two topics:
- transaction-validate: Transaction messages to be validated.
- transaction-result: Result of validation (Approve/Rejected).

The SendMessage service in the Transaction microservice picks up all the messages with topic transaction-validate or transaction-result stored in the table SentMessages that have status Pending or Failed and the retry is less than 5 times.

## How to run the project
1. Build and Rund Docker Compose.

From the root directory, run:

docker-compose up --build

This will start
- Kafka + Zookeeper
- KafkaUI
- PostgreSQL
- Transactions Microservice
- Antifraud Microservice

2. Create database tables

Once all containers are up and PostgreSQL is ready, execute the CreateTable.sql script found in the DBScripts folder.

3. Test the API

Open the Transactions service using  Swagger UI
- http://localhost:5000/swagger/index.html
From here you can test:
- Create a transaction (post /api/Transactions)
- Retrieve a transaction (post /api/transactions/retrieveTransaction)

Or use Postman
- http://localhost:5000/api/transactions 
- http://localhost:5000/api/transactions/retrievetransactions
## Technologies

- Net 8
- PostgreSQL
- Kafka
- Docker
- xUnit
- Moq
