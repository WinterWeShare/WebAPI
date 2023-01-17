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
    - Contains two classes, one that is of not the database but has entity framework classes as it's parameters, and one that generates the PDF.
3. ./ Security
    - Contains classes that makes two-factor and session key authentication possible, also one that generates recovery codes.
