# DocManager
---
## What is DocManager?

DocManager is a simple RESTful API to manage digital documents that was developed according to the user stories described below:

---

### EPIC: As a *Manager*, I want users in my team to be able to manage digital documents, so they can upload, search and download documents in a central repository.

---
#### As an *Admin*, I want to be able to create/update/delete users, so I can manage who can have access to the system.

Requirements:
 - User can have up to two roles: User and Admin
 - Full name
 - Email as username (cannot be changed)
 - Password
 - Audit log
 - API Endpoints to create, delete and update users.

---
#### As an *User*, I want to be able to log into the system, so I can access the functionalities based on my role.

Requirements:
 - API endpoint to validate email and password.
 - Return JWT token to be used in subsequent API calls.
 - Token contains role.
 - Tokens are valid for one hour.
 
---
#### As an *User*, I want to be able to upload documents, so I can search and download them later.

Requirements:
 - Only authenticated users can upload documents.
 - API endpoint to upload documents.
 - List of tags comes in header, comma separated list.
 - Store Filename, Mime Type, size and list of tags.
 - Document data should be stored in a separate storage (like S3 or Buckets).
 - Generates and return an UUID to identify the file globally.

---
#### As an *User*, I want to be able to browse document details, so I can update them if necessary.

Requirements:
 - Only authenticated users can browse documents.
 - API endpoint to search documents based on UUID, name or tag.
 - Returns a list of documents that matches the provided criteria, with UUID, filename, size, mime type and list of tags.

---
#### As an *User*, I want to be able to download a document, so I can access the file contents.

Requirements:
 - Only authenticated users can download documents.
 - API endpoint to download a document based on UUID.
 - Read from binary data from the separate storage and trigger a download.

---
#### As an *User*, I want to be able to update the tag list of an existing document, so I can improve its search-ability.

Requirements:
 - Only authenticated users can modify document tags.
 - API endpoint to update a document tag list based on UUID.

---

## Technical details

The API was developed using .Net 8.0 with C# in Visual Studio 2022. It uses Clean Architecture, separating the code into three different type of layers: Controllers, Services and Repositories.
- **Controllers** are responsible for handling the HTTP aspects of the REST API (Verbs, Routes, Documentation for OpenAPI, Authorization levels, etc.)
- **Services** are responsible for implementing the business logic for each domain entity.
- **Repositories** are responsible for persisting those domain entities.
 
All the data is stored in a MongoDB docker instance that is automatically brought up when you run the application (by selecting `Docker Compose` on the `Run` button). Also this will bring up an instace of mongo-express running on port `8005` which is a Web GUI to manage MongoDB (open <http://localhost:8005>). The very first time you run the application, the MongoDB database will be seeded with initial data.

All endpoints are secured using JWT tokens to control authentication and authorization. Currently, the identity mechanism is running internally, using a MongoDB collection to store user data (Username, Hashed Password, Roles, etc.), however, it is easy to adjust the configuration to accept JWT tokens issued by external identity providers (like Azure AD).

After the application builds and initializes, Visual Studio will open a browser window pointing to the Swagger page, which shows the documentation for all endpoints and allows you test them.

The first endpoint to be tested is `POST /api/Auth`, which will generate a JWT token. The values provided as example should match the seeded data. This endpoint will return the token in the `"token"` property, just copy this value, then click on `Authorize` on the top right, paste the value in the text box and press `ENTER`. This should setup Swagger to send the token correctly to all other endpoints being tested.

### Design Choices:
 - All warnings are treated as errors.
 - The code itself is the source of documentation. All public endpoints have xmlDoc syntax headers with as much as information as possible that can be digested by Swagger.
 - All properties includes `<example>` tags when it makes sense.
 - Most of the initialization code was moved to `StartupSetup` static class.
 - Passwords are hashed using `HMAC-SHA-512` algorithm.
 - All exceptions are handled by `ExceptionMiddleware` class, it logs comprehensive information about the errors.
 - Exception Messages and CallStacks are returned to the consumer ONLY if the API is running in DEVELOPMENT mode (variable `ASPNETCORE_ENVIRONMENT`).
 - On the `Authorize` attribute, you can determine which role the Controller/Endpoint will require by passing either `User` or `Admin` as policy name (should match `RoleType` enum).
 - On async methods, `ValueTask` was preferred instead of `Task` to reduce memory allocations.
 - Input validations are mostly done on DTO objects using `DataAnnotations`.
 - Tests are run using `TestContainers` library which spawn one new separate instance of MongoDB for each test class, this allows the tests to be run in parallel, automatic cleanup, while preventing database state from breaking the tests.
 - Assertions are done using `Fluent Assertions` library.
 - Unfortunately I've ran out of time and couldn't write all tests that I wanted. I've developed only integration tests using `WebApplicationFactory` that tests the controllers at HTTP level, which should be enough to cover all layers.
