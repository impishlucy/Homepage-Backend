# Portfolio Backend

A clean, lightweight backend for a personal portfolio website built with

![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-512bd4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-512bd4?style=for-the-badge&logo=dotnet&logoColor=white)
![MVC](https://img.shields.io/badge/MVC-68217a?style=for-the-badge&logo=visualstudio&logoColor=white)
![LiteDB](https://img.shields.io/badge/LiteDB-1f2937?style=for-the-badge&logo=databricks&logoColor=white)
![Discord](https://img.shields.io/badge/Discord-5865f2?style=for-the-badge&logo=discord&logoColor=white)

It powers the portfolio frontend with API-driven content, admin functionality, and authentication for managing a personal website.

## Features

- Portfolio content API
- Project data management
- Home, about, contact, and imprint data
- Admin interface ready
- Discord login support
- Lightweight LiteDB storage
- MVC based backend structure
- Designed for a Next.js frontend

## Setup

Restore dependencies and run the backend:

    dotnet restore
    dotnet run

The API will be available at the configured backend URL, for example:

    http://localhost:5000

or:

    https://localhost:5001

## Production

Build and publish:

    dotnet publish -c Release

Run the published application:

    dotnet Website-Current-Backend.dll

## API

The frontend expects this backend on the matching API subdomain:

    https://api.your-domain.tld
