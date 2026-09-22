# Project22
# Order Intake

## Overview

Order Intake is a .NET application for processing and validating order intake requests.

The project contains the main application implementation and a separate test project for verifying the functionality of the Order Intake service.

## Project Structure

```text
Project22/
│
├── src/
│   └── OrderIntake/
│       ├── Order.cs
│       ├── OrderIntakeService.cs
│       ├── OrderResult.cs
│       └── ValidationError.cs
│
├── tests/
│   └── OrderIntake.Tests/
│       └── OrderIntakeServiceTests.cs
│
├── Project22.slnx
└── README.md
```

## Technologies

* C#
* .NET 10
* xUnit
* Git
* GitHub

## Application

The `OrderIntake` project contains the core application code, including:

* Order data representation
* Order intake processing
* Order result handling
* Validation error handling

## Testing

The `OrderIntake.Tests` project contains automated tests for the Order Intake functionality.

The project was tested using:

```bash
dotnet test
```

### Test Results

* Total tests: 10
* Passed: 10
* Failed: 0
* Skipped: 0

### Build Status

The project builds successfully using:

```bash
dotnet build
```

## How to Run

Clone the repository and open the solution in Visual Studio Code or another .NET-compatible IDE.

Restore dependencies:

```bash
dotnet restore
```

Build the project:

```bash
dotnet build
```

Run the tests:

```bash
dotnet test
```

## Repository

This repository contains the source code and automated tests for the Order Intake project.
