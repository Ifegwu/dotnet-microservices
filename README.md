# Play Economy - Microservices Architecture

A microservices-based e-commerce platform built with .NET, featuring a virtual economy system with Gil currency and inventory management.

## Architecture Overview

The system consists of the following microservices:

### Core Services
- **Play.Catalog**: Manages the catalog of items available in the store
- **Play.Inventory**: Handles user inventory and item management
- **Play.Identity**: Manages user authentication, authorization, and Gil currency
- **Play.Frontend**: React-based web application for user interaction

### Supporting Services
- **Play.Common**: Shared library containing common utilities and interfaces
- **Play.Infra**: Infrastructure components and configurations

## Prerequisites

- .NET 7.0 SDK or later
- Node.js 16.x or later (for Frontend)
- Docker and Docker Compose
- MongoDB
- RabbitMQ

## Getting Started

### 1. Clone the Repository
```bash
git clone https://github.com/yourusername/dotnet-microservices.git
cd dotnet-microservices
```

### 2. Set Up Common Library
```bash
cd Play.Common/src/Play.Common
dotnet pack -o ../../packages/
```

### 3. Configure Services

#### Play.Catalog Service
- Manages item catalog
- Handles item creation, updates, and deletion
- Publishes events for inventory updates

#### Play.Inventory Service
- Manages user inventory
- Handles item quantity tracking
- Processes item grants and subtractions

#### Play.Identity Service
- Handles user authentication and authorization
- Manages Gil currency transactions
- Provides user management functionality

#### Play.Frontend
- React-based web interface
- Provides user-friendly interaction with all services

### 4. Running the Services

#### Using Docker Compose
```bash
docker-compose up -d
```

#### Manual Setup
1. Start MongoDB
2. Start RabbitMQ
3. Run each service:
```bash
# Catalog Service
cd Play.Catalog/src/Play.Catalog.Service
dotnet run

# Inventory Service
cd Play.Inventory/src/Play.Inventory.Service
dotnet run

# Identity Service
cd Play.Identity/src/Play.Identity.Service
dotnet run

# Frontend
cd Play.Frontend
npm install
npm start
```

## Features

### User Management
- User registration and authentication
- Role-based access control
- User profile management

### Inventory System
- Item catalog management
- User inventory tracking
- Item quantity management

### Economy System
- Gil currency management
- Transaction processing
- Balance tracking

### Store System
- Item browsing
- Purchase processing
- Inventory updates

## Development

### Adding New Features
1. Create a new branch
2. Implement changes
3. Update tests
4. Submit pull request

### Testing
```bash
# Run all tests
dotnet test

# Run specific service tests
cd Play.Catalog/src/Play.Catalog.Service.Tests
dotnet test
```

### Common Library Updates
When updating the common library:
1. Update version in Play.Common.csproj
2. Build new package:
```bash
cd Play.Common/src/Play.Common
dotnet pack -o ../../packages/ -p:Version=<new-version>
```
3. Update references in other services

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit changes
4. Push to the branch
5. Create a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For support, please open an issue in the GitHub repository.
