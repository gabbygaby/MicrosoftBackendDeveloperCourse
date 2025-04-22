
# User Management API using ASP.NET with Copilot Assistance

This User Management API was built using ASP.NET Core Web API and developed with assistance from GitHub Copilot. 


## Features

- CRUD Endpoints
    - GET "/" : Returns a root message.
    - GET "/users" : Retrieves all users.
    - GET "/users/{id:int}" : Retrieves a user by their ID.
    - POST "/users" : Adds a new user.
    - PUT "/users/{id:int}" : Updates an existing user by their ID.
    - DELETE "/users/{id:int}" : Deletes a user by their ID.
- Custom exception handling middleware
- Custom token validation middleware
- Custom request response logging middleware
- requests.http: Requests to test the endpoints and middleware


## Tech Stack

- ASP.NET Core Web API
- .NET 6+
- C#
- GitHub Copilot

