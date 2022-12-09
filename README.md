# WebAPI
The main backend requirement for the third semester exam.

## General info
Both the WebClient and WpfAdmin has to use http requests through the API to communicate with the database.

## Structure
Two separate controllers have been made for these applications, each containing the appropriate methods and nothing more.
Inside the models class, there is:
1. ./EntityFramework:
    - Contains a 1:1 replica of the database tables and a database context class.
2. ./ Invoice
    - Contains a single class that is of not the database but has entity framework classes as it's parameters.
3. ./ Security
    - Contains two classes that makes two-factor and session key authentication possible.

## Security
API method calls need a session key (except a select few, such as inserting ) and the associated user's/admin's id.
